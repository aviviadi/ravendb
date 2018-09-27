using System;
using System.Diagnostics;
using System.IO;
using Sparrow;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Raven.Client.Documents.Commands.Batches;
using Raven.Client.Documents.Operations;
using Raven.Server.Routing;
using Raven.Server.ServerWide.Context;
using Sparrow.Json;
using Sparrow.Logging;
using Voron.Impl.Backup;

namespace Raven.Server.Documents.Handlers
{
    public class BulkInsertHandler : DatabaseRequestHandler
    {
        [RavenAction("/databases/*/bulk_insert", "POST", AuthorizationStatus.ValidUser)]
        public async Task BulkInsert()
        {
            var operationCancelToken = CreateOperationToken();
            var id = GetLongQueryString("id");

            await Database.Operations.AddOperation(Database, "Bulk Insert", Operations.Operations.OperationType.BulkInsert,
                progress => DoBulkInsert(progress, operationCancelToken.Token),
                id,
                token: operationCancelToken
            );
        }

        private static string f = Guid.NewGuid().ToString();
        public static int count;

        private async Task<IOperationResult> DoBulkInsert(Action<IOperationProgress> onProgress, CancellationToken token)
        {
            var progress = new BulkInsertProgress();
            try
            {
                var logger = LoggingSource.Instance.GetLogger<MergedInsertBulkCommand>(Database.Name);
                using (ContextPool.AllocateOperationContext(out JsonOperationContext context))
                using (var buffer = JsonOperationContext.ManagedPinnedBuffer.LongLivedInstance())
                using (ContextPool.AllocateOperationContext(out JsonOperationContext docsCtx))
                {
                    var requestBodyStream = RequestBodyStream();
                    var ms = new MemoryStream();
                    requestBodyStream.CopyTo(ms);
                    ms.Position = 0;

                    
                    try
                    {

                        using (var parser = new BatchRequestParser.ReadMany(context, ms, buffer, token))
                        {
                            await parser.Init();

                            var array = new BatchRequestParser.CommandData[8];
                            var numberOfCommands = 0;
                            long totalSize = 0;
                            while (true)
                            {
                                using (ContextPool.AllocateOperationContext(out JsonOperationContext readCtx))
                                {
                                    var task = parser.MoveNext(readCtx);
                                    if (task == null)
                                        break;

                                    token.ThrowIfCancellationRequested();

                                    // if we are going to wait on the network, flush immediately
                                    // if we are going to wait on the network, flush immediately
                                    /*if ((task.IsCompleted == false && numberOfCommands > 0) ||
                                        // but don't batch too much anyway
                                        totalSize > 16 * Voron.Global.Constants.Size.Megabyte)*/
                                    if(numberOfCommands > 0)
                                    {
                                        await Database.TxMerger.Enqueue(new MergedInsertBulkCommand
                                        {
                                            Commands = array,
                                            NumberOfCommands = numberOfCommands,
                                            Database = Database,
                                            Logger = logger,
                                            TotalSize = totalSize
                                        });

                                        progress.BatchCount++;
                                        progress.Processed += numberOfCommands;
                                        progress.LastProcessedId = array[numberOfCommands - 1].Id;

                                        onProgress(progress);

                                        docsCtx.Reset();
                                        docsCtx.Renew();

                                        numberOfCommands = 0;
                                        totalSize = 0;
                                    }

                                    var commandData = await task;
                                    if (commandData.Type == CommandType.None)
                                        break;

                                    totalSize += commandData.Document.Size;
                                    if (numberOfCommands >= array.Length)
                                        Array.Resize(ref array, array.Length * 2);

                                    // need to copy to stable location, because readCtx
                                    // will be gone the next loop, and docCtx buffers the data
                                    if (commandData.Document != null)
                                        commandData.Document = commandData.Document.Clone(docsCtx);
                                    array[numberOfCommands++] = commandData;
                                }
                            }

                            if (numberOfCommands > 0)
                            {
                                await Database.TxMerger.Enqueue(new MergedInsertBulkCommand
                                {
                                    Commands = array,
                                    NumberOfCommands = numberOfCommands,
                                    Database = Database,
                                    Logger = logger,
                                    TotalSize = totalSize
                                });

                                progress.BatchCount++;
                                progress.Processed += numberOfCommands;
                                progress.LastProcessedId = array[numberOfCommands - 1].Id;

                                onProgress(progress);
                            }
                        }
                    }
                    catch (Exception e)
                    {                        
                        ms.Position = 0;
                        var filename = "/tmp/" + Guid.NewGuid() + ".txt";
                        Console.WriteLine("ZZ Filename is " + filename);
                        Console.Out.Flush();
                        FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite);
                        ms.CopyTo(fs);
                        fs.Flush();
                        fs.Dispose();
                        Console.WriteLine("Written " + filename);
                        Console.Out.Flush();
                        Console.ReadKey();
                        throw;
                    }

                }

                HttpContext.Response.StatusCode = (int)HttpStatusCode.Created;

                return new BulkOperationResult
                {
                    Total = progress.Processed
                };
            }
            catch (Exception e)
            {
                HttpContext.Response.Headers["Connection"] = "close";
                throw new InvalidOperationException("Failed to process bulk insert " + progress, e);
            }
        }


        private class MergedInsertBulkCommand : TransactionOperationsMerger.MergedTransactionCommand
        {
            public Logger Logger;
            public DocumentDatabase Database;
            public BatchRequestParser.CommandData[] Commands;
            public int NumberOfCommands;
            public long TotalSize;
            public override int Execute(DocumentsOperationContext context)
            {
                for (int i = 0; i < NumberOfCommands; i++)
                {
                    var cmd = Commands[i];
                    Debug.Assert(cmd.Type == CommandType.PUT);
                    try
                    {
                        Database.DocumentsStorage.Put(context, cmd.Id, null, cmd.Document);
                    }
                    catch (Voron.Exceptions.VoronConcurrencyErrorException)
                    {
                        // RavenDB-10581 - If we have a concurrency error on "doc-id/" 
                        // this means that we have existing values under the current etag
                        // we'll generate a new (random) id for them. 

                        // The TransactionMerger will re-run us when we ask it to as a 
                        // separate transaction

                        for (; i < NumberOfCommands; i++)
                        {
                            cmd = Commands[i];
                            if (cmd.Id?.EndsWith('/') == true)
                            {
                                cmd.Id = MergedPutCommand.GenerateNonConflictingId(Database, cmd.Id);
                                RetryOnError = true;
                            }
                        }

                        throw;
                    }
                }
                if (Logger.IsInfoEnabled)
                {
                    Logger.Info($"Merged {NumberOfCommands:#,#;;0} operations ({Math.Round(TotalSize / 1024d, 1):#,#.#;;0} kb)");
                }
                return NumberOfCommands;
            }
        }
    }
}
