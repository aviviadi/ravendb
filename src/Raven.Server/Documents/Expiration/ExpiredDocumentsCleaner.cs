//-----------------------------------------------------------------------
// <copyright file="ExpiredDocumentsCleaner.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Raven.Client.Documents.Operations.Expiration;
using Raven.Client.Documents.Operations.Refresh;
using Raven.Client.ServerWide;
using Raven.Server.Background;
using Raven.Server.NotificationCenter.Notifications;
using Raven.Server.ServerWide.Context;
using Sparrow.Json;
using Sparrow.Logging;
using Sparrow.Platform;
using Voron;

namespace Raven.Server.Documents.Expiration
{
    public class ExpiredDocumentsCleaner : BackgroundWorkBase
    {
        internal static int BatchSize = PlatformDetails.Is32Bits == false
            ? 4096
            : 1024;

        private readonly DocumentDatabase _database;
        private readonly TimeSpan _refreshPeriod;
        private readonly TimeSpan _expirationPeriod;

        public ExpirationConfiguration ExpirationConfiguration { get; }
        public RefreshConfiguration RefreshConfiguration { get; }

        private ExpiredDocumentsCleaner(DocumentDatabase database, ExpirationConfiguration expirationConfiguration, RefreshConfiguration refreshConfiguration) : base(database.Name, database.DatabaseShutdown)
        {
            ExpirationConfiguration = expirationConfiguration;
            RefreshConfiguration = refreshConfiguration;
            _database = database;
            _expirationPeriod = TimeSpan.FromSeconds(ExpirationConfiguration?.DeleteFrequencyInSec ?? 60);
            _refreshPeriod = TimeSpan.FromSeconds(RefreshConfiguration?.RefreshFrequencyInSec ?? 60);
        }

        public static ExpiredDocumentsCleaner LoadConfigurations(DocumentDatabase database, DatabaseRecord dbRecord, ExpiredDocumentsCleaner expiredDocumentsCleaner)
        {
            try
            {
                if (dbRecord.Expiration == null && dbRecord.Refresh == null)
                {
                    expiredDocumentsCleaner?.Dispose();
                    return null;
                }

                if (expiredDocumentsCleaner != null)
                {
                    // no changes
                    if (Equals(expiredDocumentsCleaner.ExpirationConfiguration, dbRecord.Expiration) &&
                        Equals(expiredDocumentsCleaner.RefreshConfiguration, dbRecord.Refresh))
                        return expiredDocumentsCleaner;
                }

                expiredDocumentsCleaner?.Dispose();
                if (dbRecord.Expiration?.Disabled == true && dbRecord.Refresh?.Disabled == true)
                    return null;

                var cleaner = new ExpiredDocumentsCleaner(database, dbRecord.Expiration, dbRecord.Refresh);
                cleaner.Start();
                return cleaner;
            }
            catch (Exception e)
            {
                const string msg = "Cannot enable expired documents cleaner as the configuration record is not valid.";
                database.NotificationCenter.Add(AlertRaised.Create(
                    database.Name,
                    $"Expiration error in {database.Name}", msg,
                    AlertType.RevisionsConfigurationNotValid, NotificationSeverity.Error, database.Name));

                var logger = LoggingSource.Instance.GetLogger<ExpiredDocumentsCleaner>(database.Name);
                if (logger.IsOperationsEnabled)
                    logger.Operations(msg, e);

                return null;
            }
        }

        protected override Task DoWork()
        {
            var expiration = DoExpirationWork();
            var refresh = DoRefreshWork();

            return Task.WhenAll(expiration, refresh);
        }

        private async Task DoRefreshWork()
        {
            while (RefreshConfiguration?.Disabled == false)
            {
                await WaitOrThrowOperationCanceled(_refreshPeriod);

                await RefreshDocs();
            }
        }

        private async Task DoExpirationWork()
        {
            while (ExpirationConfiguration?.Disabled == false)
            {
                await WaitOrThrowOperationCanceled(_expirationPeriod);

                await CleanupExpiredDocs();
            }
        }

        internal Task CleanupExpiredDocs()
        {
            return CleanupDocs(forExpiration: true);
        }

        internal Task RefreshDocs()
        {
            return CleanupDocs(forExpiration: false);
        }

        private async Task CleanupDocs(bool forExpiration)
        {
            var currentTime = _database.Time.GetUtcNow();

            try
            {
                if (Logger.IsInfoEnabled)
                    Logger.Info($"Trying to find {(forExpiration ? "expired" : "require refreshing")} documents to delete");

                DatabaseTopology topology;
                using (_database.ServerStore.ContextPool.AllocateOperationContext(out TransactionOperationContext serverContext))
                using (serverContext.OpenReadTransaction())
                {
                    topology = _database.ServerStore.Cluster.ReadDatabaseTopology(serverContext, _database.Name);
                }

                var isFirstInTopology = string.Equals(topology.AllNodes.FirstOrDefault(), _database.ServerStore.NodeTag, StringComparison.OrdinalIgnoreCase);

                using (_database.DocumentsStorage.ContextPool.AllocateOperationContext(out DocumentsOperationContext context))
                {
                    context.Reset();
                    context.Renew();

                    while (true)
                    {
                        using (context.OpenReadTransaction())
                        {
                            var expired =
                                forExpiration ?
                                    _database.DocumentsStorage.ExpirationStorage.GetExpiredDocuments(context, currentTime, isFirstInTopology, BatchSize, out var duration, CancellationToken) :
                                    _database.DocumentsStorage.ExpirationStorage.GetDocumentsToRefresh(context, currentTime, isFirstInTopology, BatchSize, out duration, CancellationToken);

                            if (expired == null || expired.Count == 0)
                                return;

                            var command = new DeleteExpiredDocumentsCommand(expired, _database, forExpiration);
                            await _database.TxMerger.Enqueue(command);
                            if (Logger.IsInfoEnabled)
                                Logger.Info($"Successfully {(forExpiration ? "deleted" : "refreshed")} {command.DeletionCount:#,#;;0} documents in {duration.ElapsedMilliseconds:#,#;;0} ms.");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // this will stop processing
                throw;
            }
            catch (Exception e)
            {
                if (Logger.IsOperationsEnabled)
                    Logger.Operations($"Failed to expire/refresh documents on {_database.Name} which are older than {currentTime}", e);
            }
        }

        internal class DeleteExpiredDocumentsCommand : TransactionOperationsMerger.MergedTransactionCommand
        {
            private readonly Dictionary<Slice, List<(Slice LowerId, string Id)>> _expired;
            private readonly DocumentDatabase _database;
            private readonly bool _forExpiration;

            public int DeletionCount;

            public DeleteExpiredDocumentsCommand(Dictionary<Slice, List<(Slice LowerId, string Id)>> expired, DocumentDatabase database, bool forExpiration)
            {
                _expired = expired;
                _database = database;
                _forExpiration = forExpiration;
            }

            protected override int ExecuteCmd(DocumentsOperationContext context)
            {
                DeletionCount =
                    _forExpiration
                        ? _database.DocumentsStorage.ExpirationStorage.DeleteDocumentsExpiration(context, _expired)
                        : _database.DocumentsStorage.ExpirationStorage.RefreshDocuments(context, _expired);

                return DeletionCount;
            }

            public override TransactionOperationsMerger.IReplayableCommandDto<TransactionOperationsMerger.MergedTransactionCommand> ToDto(JsonOperationContext context)
            {

                var keyValuePairs = new KeyValuePair<Slice, List<(Slice LowerId, string Id)>>[_expired.Count];
                var i = 0;
                foreach (var item in _expired)
                {
                    keyValuePairs[i] = item;
                    i++;
                }

                return new DeleteExpiredDocumentsCommandDto
                {
                    ForExpiration = _forExpiration,
                    Expired = keyValuePairs
                };
            }
        }
    }

    internal class DeleteExpiredDocumentsCommandDto : TransactionOperationsMerger.IReplayableCommandDto<ExpiredDocumentsCleaner.DeleteExpiredDocumentsCommand>
    {
        public ExpiredDocumentsCleaner.DeleteExpiredDocumentsCommand ToCommand(DocumentsOperationContext context, DocumentDatabase database)
        {
            var expired = new Dictionary<Slice, List<(Slice LowerId, string Id)>>();
            foreach (var item in Expired)
            {
                expired[item.Key] = item.Value;
            }
            var command = new ExpiredDocumentsCleaner.DeleteExpiredDocumentsCommand(expired, database, ForExpiration);
            return command;
        }

        public bool ForExpiration { get; set; }

        public KeyValuePair<Slice, List<(Slice LowerId, string Id)>>[] Expired { get; set; }
    }
}
