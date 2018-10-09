using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Raven.Server.Documents.Handlers;
using Sparrow.Json;

namespace Tryouts
{
    public static class Program
    {
        public static async Task Main()
        {

            var pool = new JsonContextPool();
            
            Console.WriteLine("Starting...");
            var filebytes = File.ReadAllBytes("/home/cesar/Sources/tmpcommand.txt");
            for (var bl = 0; bl < 1000; bl++) 
            {
                if (bl % 10 == 0)
                {
                  Console.WriteLine("\rbl =  " + bl + "  ...  ");
                  Console.Out.Flush();
                }

                const int numOfTasks = 12;
                var tasks = new Task[numOfTasks];
                for (int i = 0; i < numOfTasks; i++)
                {                    
                    var k = i;

                    tasks[i] = Task.Run(async ()=>
                    {
                        for (int yy=0; yy < 6; yy++)
                        {
                            var buffer = JsonOperationContext.ManagedPinnedBuffer.RawNew();
                            using(var context = JsonOperationContext.ShortTermSingleUse())
                            {
                                MemoryStream ms = new MemoryStream(filebytes);

                                using (var parser = new BatchRequestParser.ReadMany(context, ms, buffer, new System.Threading.CancellationToken()))
                                {
                                    await parser.Init();
                                    for(int j=0; j<50; j++)
                                    {
                                        var doc = await parser.MoveNext(context);
                                        
                                        if (doc.Document.TryGetMember("Name", out var name) == false)
                                        {
                                            Console.WriteLine("Can't get Name");
                                            Console.Out.Flush();
                                        }
                                        else
                                        {
                                            if (!name.ToString().Equals("user/"+ j))
                                            {
                                                Console.WriteLine("**************** name == " + name + " while expected == " + "user/"+ j);
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
                catch(Exception e)
                {
                  var ei = e;
                  while(ei != null){
                      System.Console.WriteLine(ei.Message);  
                      ei = ei.InnerException;
                  }
                  throw;
                }
            }
        }
    }
}