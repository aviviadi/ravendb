using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Timers;
using Sparrow.Binary;
using Sparrow.Platform.Posix;

namespace Sparrow
{
    public static unsafe class Memory
    {
        public static bool DisableFence = true;
        public const bool RecordStack = false;
        public const bool RecordHistory = false;
        
        public const int CompareInlineVsCallThreshold = 0/*256*/;


        public static int Move(byte* dest, byte* src, int count)
        {
            VerifyMappedRange(dest, src, count);
            return Syscall.Move(dest, src, count);
        }
        
        
        
        public static int Compare(byte* p1, byte* p2, int size)
        {
            return CompareInline(p1, p2, size);
        }
        
        public static int Compare(byte* p1, byte* p2, long size)
        {
            return CompareInline(p1, p2, (int)size);
        }

        public static int Compare(byte* p1, byte* p2, int size, out int position)
        {
            return CompareInline(p1, p2, size, out position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CompareInline(void* p1, void* p2, int size)
        {
            VerifyMappedRange(p1, p2, size);
            // If we use an unmanaged bulk version with an inline compare the caller site does not get optimized properly.
            // If you know you will be comparing big memory chunks do not use the inline version. 
            if (size > CompareInlineVsCallThreshold)
                goto UnmanagedCompare;

            byte* bpx = (byte*)p1;
            byte* bpy = (byte*)p2;

            // PERF: This allows us to do pointer arithmetics and use relative addressing using the 
            //       hardware instructions without needed an extra register.            
            long offset = bpy - bpx;

            if ((size & 7) == 0)
                goto ProcessAligned;

            // We process first the "unaligned" size.
            ulong xor;
            if ((size & 4) != 0)
            {
                xor = *((uint*)bpx) ^ *((uint*)(bpx + offset));
                if (xor != 0)
                    goto Tail;

                bpx += 4;
            }

            if ((size & 2) != 0)
            {
                xor = (ulong)(*((ushort*)bpx) ^ *((ushort*)(bpx + offset)));
                if (xor != 0)
                    goto Tail;

                bpx += 2;
            }

            if ((size & 1) != 0)
            {
                int value = *bpx - *(bpx + offset);
                if (value != 0)
                    return value;

                bpx += 1;
            }

            ProcessAligned:

            byte* end = (byte*)p1 + size;
            byte* loopEnd = end - 16;
            while (bpx <= loopEnd)
            {
                // PERF: JIT will emit: ```{op} {reg}, qword ptr [rdx+rax]```
                if (*((ulong*)bpx) != *(ulong*)(bpx + offset))
                    goto XorTail;

                if (*((ulong*)(bpx + 8)) != *(ulong*)(bpx + 8 + offset))
                {
                    bpx += 8;
                    goto XorTail;
                }


                bpx += 16;
            }

            if (bpx < end)
                goto XorTail;

            return 0;

            XorTail:
            xor = *((ulong*)bpx) ^ *(ulong*)(bpx + offset);

            Tail:

            // Fast-path for equals
            if (xor == 0)
                return 0;

            // PERF: This is a bit twiddling hack. Given that bitwise xoring 2 values flag the bits difference, 
            //       we can use that we know we are running on little endian hardware and the very first bit set 
            //       will correspond to the first byte which is different. 

            bpx += Bits.TrailingZeroesInBytes(xor);
            return *bpx - *(bpx + offset);

            UnmanagedCompare:
            return UnmanagedMemory.Compare((byte*)p1, (byte*)p2, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CompareInline(void* p1, void* p2, int size, out int position)
        {
            VerifyMappedRange(p1, p2, size);
            byte* bpx = (byte*)p1;
            byte* bpy = (byte*)p2;

            long offset = bpy - bpx;
            if (size < 8)
                goto ProcessSmall;

            int l = size >> 3; // (Equivalent to size / 8)

            ulong xor;
            for (int i = 0; i < l; i++, bpx += 8)
            {
                xor = *((ulong*)bpx) ^ *(ulong*)(bpx + offset);
                if (xor != 0)
                    goto Tail;
            }

            ProcessSmall:

            if ((size & 4) != 0)
            {
                xor = *((uint*)bpx) ^ *((uint*)(bpx + offset));
                if (xor != 0)
                    goto Tail;

                bpx += 4;
            }

            if ((size & 2) != 0)
            {
                xor = (ulong)(*((ushort*)bpx) ^ *((ushort*)(bpx + offset)));
                if (xor != 0)
                    goto Tail;

                bpx += 2;
            }

            position = (int)(bpx - (byte*)p1);

            if ((size & 1) != 0)
            {
                return *bpx - *(bpx + offset);
            }

            return 0;

            Tail:

            int p = Bits.TrailingZeroesInBytes(xor);

            position = (int)(bpx - (byte*)p1) + p;
            return *(bpx + p) - *(bpx + p + offset);
        }

        /// <summary>
        /// Bulk copy is optimized to handle copy operations where n is statistically big. While it will use a faster copy operation for 
        /// small amounts of memory, when you have smaller than 2048 bytes calls (depending on the target CPU) it will always be
        /// faster to call .Copy() directly.
        /// </summary>
        
        private static void BulkCopy(void* dest, void* src, long n)
        {
            VerifyMappedRange(dest, src, n);
            UnmanagedMemory.Copy((byte*)dest, (byte*)src, n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(void* dest, void* src, uint n)
        {
            VerifyMappedRange(dest, src, n);
            //Console.WriteLine("in1");
            //Console.Out.Flush();
            Unsafe.CopyBlock(dest, src, n);
            //Console.WriteLine("ou1");
            //Console.Out.Flush();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(void* dest, void* src, long n)
        {
            if (n < uint.MaxValue)
            {
                VerifyMappedRange(dest, src, (uint)n);
                //Console.WriteLine("in2");
                //Console.Out.Flush();
                Unsafe.CopyBlock(dest, src, (uint)n); // Common code-path
                //Console.WriteLine("ou2");
                //Console.Out.Flush();
                return;
            }

            VerifyMappedRange(dest, src, n);
            BulkCopy(dest, src, n);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr SyscallCopy(byte* dest, byte* src, long n)
        {
            Copy(dest, src, n);
            return IntPtr.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set(byte* dest, byte value, uint n)
        {
            VerifyMappedRange(dest, n);
            Unsafe.InitBlock(dest, value, n);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr Set(byte* dest, int value, long n)
        {
            VerifyMappedRange(dest, n);
            Unsafe.InitBlock(dest, (byte)value, (uint)n);
            return IntPtr.Zero;            
        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static void Set(byte* dest, byte value, int n)
//        {
//            VerifyMappedRange(dest, n);
//            Unsafe.InitBlock(dest, value, (uint)n);
//        }

        /// <summary>
        /// Set is optimized to handle copy operations where n is statistically small.       
        /// </summary>
        /// <remarks>This is a forced inline version, use with care.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetInline(byte* dest, byte value, long n)
        {
            if (n == 0)
                goto Finish;

            if (n < int.MaxValue)
            {
                VerifyMappedRange(dest, (uint)n);
                Unsafe.InitBlock(dest, value, (uint)n);
            }
            else
            {
                VerifyMappedRange(dest, n);
                UnmanagedMemory.Set(dest, value, n);
            }

            Finish:
            ;
        }

        private class AllocData
        {
            public string Allocator { get; set; }
            public UIntPtr Length { get; set; }
            public string StackTrace { get; set; }
        }
        
        private static readonly SortedList<ulong, List<AllocData>> Ranges = new SortedList<ulong, List<AllocData>>();
        private static readonly SortedList<long, List<AllocData>> RangesOfCpy = new SortedList<long, List<AllocData>>();
        private static readonly List<string> History = new List<string>();
        private static readonly object Lockobj = new object();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterVerification(byte *start, ulong length, string allocator)
        {
            RegisterVerification(new IntPtr(start), new UIntPtr(length), allocator);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnregisterVerification(byte *start, ulong length, string allocator)
        {
            UnregisterVerification(new IntPtr(start), new UIntPtr(length), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterVerification(IntPtr start, UIntPtr length, string allocator)
        {
            if (DisableFence)
                return;
            var s = (ulong)start.ToInt64();
            if (s == 0)
            {
                Console.WriteLine("WTF? Zero Addr ?? ");
                Console.WriteLine(Environment.StackTrace);
                Console.ReadKey();
            }
            lock (Lockobj)
            {
                if (Ranges.TryGetValue(s, out var lst) == false)
                {
                    lst = new List<AllocData>();
                    Ranges[s] = lst;
                }

                lst.Add(new AllocData {                    
                    Length = length,
                    Allocator = allocator,
                    StackTrace = RecordStack ? Environment.StackTrace : "_"
                });
                if (RecordHistory)
                    History.Add($"[{s}={length.ToUInt64()}]");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnregisterVerification(IntPtr start, UIntPtr length, string deallocator)
        {
            if (DisableFence)
                return;
            var s = (ulong)start.ToInt64();
            lock (Lockobj)
            {
                if (Ranges.TryGetValue(s, out var lst) == false)
                    goto err1;

                var removed = false;
                foreach (var item in lst)
                {
                    if (item.Length.ToUInt64() != length.ToUInt64()) 
                        continue;
                    // History.Add($"{{{s}={item.Length.ToUInt64()}}}");
                    lst.Remove(item);
                    removed = true;
                    break;
                }
                if (removed == false)
                    goto err2;

                if (lst.Count == 0)
                    Ranges.Remove(s);
                
                return;
                
                err1:
                {
                    Console.WriteLine($"Trying to free a non allocated address - {{{s},{length.ToUInt64()},{deallocator}}}");
                    Console.WriteLine("=================================================================================");
                    Console.WriteLine(Environment.StackTrace);
                    Console.WriteLine("=================================================================================");

                    DumpRanges();
                    
                    Console.Out.Flush();
                    return;
                }
                
                err2:
                {
                    Console.WriteLine($"Trying to free an allocated address but no matching length - {{{s},{length.ToUInt64()},{deallocator}}}");
                    Console.WriteLine("=================================================================================");
                    foreach (var item in lst)
                    {
                        Console.Write($"{{{item.Length}/{item.Allocator}}},");
                    }

                    Console.WriteLine("Done");
                    Console.WriteLine("=================================================================================");
                    Console.WriteLine(Environment.StackTrace);
                    Console.WriteLine("=================================================================================");

                    DumpRanges();
                    
                    Console.Out.Flush();
                }
            }
        }        

        private static (ulong Addr, List<AllocData>) GetNearestAddress(ulong addr)
        {
            // Check to see if we need to search the list.
            lock (Lockobj)
            {
                if (Ranges == null || Ranges.Count <= 0)
                {
                    // Console.WriteLine("ADIADI :: Empty for " + addr);
                    return (long.MaxValue, null);
                }

                if (Ranges.Count == 1)
                {
                    // Console.WriteLine("ADIADI :: First for " + addr);
                    return (Ranges.Keys[0], Ranges.Values[0]);
                }


                // Setup the variables needed to find the closest index
                var lower = 0;
                var upper = Ranges.Count - 1;
                var index = (lower + upper) / 2;

                // Find the closest index (rounded down)
                var searching = true;
                while (searching)
                {
                    var comparisonResult = addr.CompareTo(Ranges.Keys[index]);
                    if (comparisonResult == 0)
                    {
                        // Console.WriteLine($"ADIADI :: {index} for " + addr);
                        return (Ranges.Keys[index], Ranges.Values[index]);
                    }
                    else if (comparisonResult < 0)
                    {
                        upper = index - 1;
                    }
                    else
                    {
                        lower = index + 1;
                    }

                    index = (lower + upper) / 2;
                    if (lower > upper)
                    {
                        searching = false;
                    }
                }

                // Check to see if we are under or over the max values.
                if (index >= Ranges.Count - 1)
                {
                    // Console.WriteLine($"ADIADI :: > {Ranges.Count - 1} for " + addr);
                    return (Ranges.Keys[Ranges.Count - 1],Ranges.Values[Ranges.Count - 1]);
                }

                if (index < 0)
                {
                    // Console.WriteLine($"ADIADI :: < First for " + addr);
                    return (Ranges.Keys[0], Ranges.Values[0]);
                }

//                // Check to see if we should have rounded up instead
//                if (Ranges.Keys[index + 1] - addr < addr - (Ranges.Keys[index]))
//                {
//                    index++;
//                }

                // Return the correct/closest string
                // Console.WriteLine($"ADIADI :: ~= {index} for " + addr);
                return (Ranges.Keys[index], Ranges.Values[index]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void VerifyMappedRange(void* p1, void* p2, long size)
        {
            if (DisableFence)
            return;
            VerifyMappedRange(p1, size, "dest");
            VerifyMappedRange(p2, size, "src");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void VerifyMappedRange(void* p1, long size, string debug = "None")
        {
            if (DisableFence)
                return;
            lock (Lockobj)
            {
                var (pAllowedStart, lst) = GetNearestAddress(new UIntPtr(p1).ToUInt64());
                if (lst == null)
                {
                    Console.WriteLine($"WTF??? No nearest address for {new IntPtr(p1).ToInt64()} and size {size} / debug={debug}");
                    Console.WriteLine(Environment.StackTrace);
                    DumpRanges();
                    return;
                }

                var pStart = new UIntPtr(p1).ToUInt64();
                var pEnd = pStart + (ulong)size - 1;

                if (size < 0)
                {
                    Console.WriteLine($"WTF??? size<0 for {new IntPtr(p1).ToInt64()} and size {size} / debug={debug}");
                    Console.WriteLine(Environment.StackTrace);
                    DumpRanges();
                    return;
                }

                foreach (var length in lst)
                {
                    var len = length.Length.ToUInt64();
                    var pAllowedEnd = pAllowedStart + len - 1;

                    if (pAllowedStart > pAllowedEnd)
                    {
                        Console.WriteLine($"Pretty wierd.. alloc zero or less at {pAllowedStart} at length={len}");
                        DumpRanges();
                        pAllowedEnd = pAllowedStart;
                    }

                    if (pStart >= pAllowedStart && pEnd <= pAllowedEnd)
                        return; // found in range
                }

                Console.WriteLine($"WTF??? {{{new IntPtr(p1).ToInt64()},{(ulong)size},{debug}}}");
                Console.WriteLine($"SomeInfo={pAllowedStart}/{pStart}/{pEnd}");
                Console.WriteLine("=================================================================================");
                Console.WriteLine(Environment.StackTrace);
                Console.WriteLine("=================================================================================");

                DumpRanges();

                var now = DateTime.Now;
                Console.WriteLine(now + "Now setting timer to one second ahead");
                Console.Out.Flush();
                
                
            }
        }

        
        

        private static void DumpRanges()
        {
            Console.WriteLine("DumpRanges:");
            Console.WriteLine("----------");
            var c = 0;
            foreach (var range in Ranges)
            {
                Console.Write($"[{c++}] addr={range.Key}\t");
                foreach (var l in range.Value)
                {
                    if (RecordStack)
                        Console.WriteLine($"{{{l.Length}/{l.Allocator}}} :: {Environment.NewLine}{l.StackTrace} :: {Environment.NewLine}");
                    else
                        Console.Write($"{{{l.Length}/{l.Allocator}}},");
                }
                Console.WriteLine("Done");
            }
            Console.WriteLine("----------");

            if (RecordHistory)
            {
                Console.WriteLine("History:");
                Console.WriteLine("----------");
                c = 0;
                foreach (var item in History)
                {
                    Console.WriteLine($"[{c++}] {item}");
                }

                Console.WriteLine("----------");
            }
        }

        public static void BufferMemoryCopy(byte* from, byte* to, uint sizeToCopyFrom, uint sizeToCopyTo)
        {
            VerifyMappedRange(from, sizeToCopyFrom, "BufferFrom");
            VerifyMappedRange(from, sizeToCopyTo, "BufferTo");
            Buffer.MemoryCopy(from, to, sizeToCopyFrom, sizeToCopyTo);
        }

        public static void MarshalFreeHGlobal(IntPtr headerSpaceValue)
        {
            Console.WriteLine("FreeIn");
            Console.Out.Flush();
            Marshal.FreeHGlobal(headerSpaceValue);
            Console.WriteLine("FreeOu");
            Console.Out.Flush();
        }
    }
}
