using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Raven.Server.Documents.Handlers;
using Sparrow.Json;

namespace Tryouts
{
    public static class Program
    {
        public static async Task Main()
        {
            if (GC.TryStartNoGCRegion(1_000_000_000, 1_000_000_000) == false)
                Console.WriteLine("Failed To Suppress GC");
            JsonContextPool pool = new JsonContextPool();

            Console.WriteLine("Starting...");
            byte[] filebytes = File.ReadAllBytes("/home/cesar/Sources/tmpcommand.txt");

            BatchRequestParser.Expected = filebytes;
            string md5 = Convert.ToBase64String(MD5.Create().ComputeHash(filebytes));

            for (int bl = 0; bl < 1000; bl++)
            {
                if (bl % 10 == 0)
                {
                    Console.WriteLine("\rbl =  " + bl + "  ...  ");
                    Console.Out.Flush();
                }

                const int numOfTasks = 12;
                Task[] tasks = new Task[numOfTasks];
                for (int i = 0; i < numOfTasks; i++)
                {
                    int k = i;

                    tasks[i] = Task.Run(async () =>
                    {
                        for (int yy = 0; yy < 6; yy++)
                        {
                            JsonOperationContext.ManagedPinnedBuffer buffer = JsonOperationContext.ManagedPinnedBuffer.RawNew();
                            using (JsonOperationContext context = JsonOperationContext.ShortTermSingleUse())
                            {
                                MemoryStream ms = new MemoryStream(filebytes);

                                string md5_2 = Convert.ToBase64String(MD5.Create().ComputeHash(filebytes));
                                if (md5_2 != md5)
                                {
                                    Console.WriteLine("WTH?????!!!!");
                                    Console.Out.Flush();
                                }

                                using (BatchRequestParser.ReadMany parser = new BatchRequestParser.ReadMany(context, ms, buffer, new CancellationToken()))
                                {
                                    await parser.Init();
                                    for (int j = 0; j < 50; j++)
                                    {
                                        BatchRequestParser.CommandData doc = await parser.MoveNext(context);

                                        if (doc.Document.TryGetMember("Name", out object name) == false)
                                        {
                                            Console.WriteLine("Can't get Name");
                                            Console.Out.Flush();
                                        }
                                        else
                                        {
                                            if (!name.ToString().Equals("user/" + j))
                                            {
                                                Console.WriteLine("**************** name == " + name + " while expected == " + "user/" + j);
                                                Console.Out.Flush();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    });
                }

                try
                {
                    Task.WaitAll(tasks);
                }
                catch (Exception e)
                {
                    Exception ei = e;
                    while (ei != null)
                    {
                        Console.WriteLine(ei.Message);
                        ei = ei.InnerException;
                    }

                    string md5_2 = Convert.ToBase64String(MD5.Create().ComputeHash(filebytes));
                    if (md5_2 != md5)
                    {
                        Console.WriteLine("WT                    ?????!!!!");
                        Console.Out.Flush();
                    }

                    throw;
                }
            }
        }
    }
}
