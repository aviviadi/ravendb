using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SlowTests.Client.Counters;
using SlowTests.Cluster;
using SlowTests.Issues;
using SlowTests.Voron;
using Sparrow.Server.Platform;
using StressTests.Cluster;

namespace Tryouts
{
    public unsafe static class Program
    {

        private const string LIBRVNPAL = @"D:\ravendb-v4.2\x64\Debug\Raven.Pal.dll";

        [DllImport(LIBRVNPAL, SetLastError = true, CharSet =CharSet.Auto)]
        public static extern PalFlags.FailCodes rvn_write_header(
            string filename,
            void* header,
            Int32 size,
            out Int32 errorCode);

        public unsafe static async Task Main(string[] args)
        {
            int a = 1;
            var v = @"D:\ravendb-v4.2\src\Raven.Server\testhebrew\Databases\עורב\header.one";
            //Console.WriteLine(Process.GetCurrentProcess().Id);
            //Console.Read();
            var  sss = rvn_write_header(v, (void*)&a, 4, out int bbb);
            Console.WriteLine(sss);
            Console.WriteLine(bbb);

        }
    }
}
