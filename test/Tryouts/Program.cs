using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastTests;
using FastTests.Server.Documents.Queries.Parser;
using FastTests.Voron.Backups;
using FastTests.Voron.Compaction;
using Orders;
using RachisTests.DatabaseCluster;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations.Indexes;
using Raven.Client.Documents.Queries;
using Raven.Tests.Core.Utils.Entities;
using SlowTests.Authentication;
using SlowTests.Bugs;
using SlowTests.Bugs.MapRedue;
using SlowTests.Client;
using SlowTests.Client.Attachments;
using SlowTests.Issues;
using SlowTests.MailingList;
using Sparrow.Logging;
using StressTests.Client.Attachments;
using Xunit;

namespace Tryouts
{
    public static class Program
    {
        public static async Task Main()
        {
            Console.WriteLine("Press any key,...");
            Console.Out.Flush();
            Console.ReadKey();
            
               for (var i = 0; i < 100; i++)
                {
                    try
                    {
                        var tasks = new Task[4];
                        var tests = Enumerable.Range(0, tasks.Length).Select(x => new SlowTests.SlowTests.Issues.RavenDB_2812()).ToArray();

                        for (var j = 0; j < tasks.Length; j++)
                        {
                            var k = j;
                            tasks[j] = Task.Run(async () => await tests[k].ShouldProperlyPageResults());
                        }

                        await Task.WhenAll(tasks);
                        for (var j = 0; j < tasks.Length; j++)
                        {
                            tests[j].Dispose();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);

                    }

                    Console.WriteLine(i);
                }
            }
        }
    }

