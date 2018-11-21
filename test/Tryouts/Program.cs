using System;
using System.Threading.Tasks;
using FastTests;
using FastTests.Server.Documents.Queries.Parser;
using FastTests.Voron.Backups;
using FastTests.Voron.Compaction;
using RachisTests.DatabaseCluster;
using Raven.Client.Documents.Queries;
using Raven.Tests.Core.Utils.Entities;
using SlowTests.Authentication;
using SlowTests.Bugs.MapRedue;
using SlowTests.Client;
using SlowTests.Client.Attachments;
using SlowTests.Issues;
using SlowTests.MailingList;
using Sparrow.Logging;
using StressTests.Client.Attachments;
using Voron;
using Xunit;

namespace Tryouts
{
    public unsafe static class Program
    {
        public static void Main(string[] args)
        {
            var options = StorageEnvironmentOptions.ForPath("test2");
            options.ManualFlushing = true;
            options.ForceUsing32BitsPager = true;
            var env = new StorageEnvironment(options);

            using (var txw = env.WriteTransaction())
            {
                var tree = txw.CreateTree("test");

                byte[] value = new byte[8200];
                for (int i = 0; i < value.Length; i++)
                {
                    value[i] = 1;
                }
                tree.Add("test", value);
                for (int i = 0; i < value.Length; i++)
                {
                    value[i] = 2;
                }
                tree.Add("test2", value);
                
                txw.Commit();
            }

            using (var txr = env.ReadTransaction())
            {
                var tree = txr.ReadTree("test");

                var r = tree.Read("test");
                if (r.Reader.Length != 8200)
                {
                    Console.WriteLine("size");
                }
                for (int i = 0; i < r.Reader.Length; i++)
                {
                    if (r.Reader.Base[i] != 1)
                    {
                        Console.WriteLine("Err");
                    }
                }
                
                r = tree.Read("test2");
                if (r.Reader.Length != 8200)
                {
                    Console.WriteLine("size");
                }
                for (int i = 0; i < r.Reader.Length; i++)
                {
                    if (r.Reader.Base[i] != 2)
                    {
                        Console.WriteLine("Err");
                    }
                }
            }

            Console.WriteLine("Success");
        }
    }
}
