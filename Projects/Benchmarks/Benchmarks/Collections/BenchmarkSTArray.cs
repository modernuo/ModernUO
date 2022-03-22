using System.Buffers;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server.Buffers;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net60)]
    public class BenchmarkSTArray
    {
        private static long[][] arrays = new long[16][];
        private static long[][] stArrays = new long[16][];
        private static long[][] newArrays = new long[16][];
        private static Queue<long>[] newQueue = new Queue<long>[16];

        [GlobalSetup]
        public void Setup()
        {
            // Allocate
            arrays = new long[16][];
            for (var i = 0; i < 16; i++)
            {
                arrays[i] = ArrayPool<long>.Shared.Rent(64);
            }

            stArrays = new long[16][];
            for (var i = 0; i < 16; i++)
            {
                stArrays[i] = STArrayPool<long>.Shared.Rent(64);
            }

            for (var i = 0; i < 16; i++)
            {
                ArrayPool<long>.Shared.Return(arrays[i]);
            }

            for (var i = 0; i < 16; i++)
            {
                STArrayPool<long>.Shared.Return(stArrays[i]);
            }
        }

        [Benchmark]
        public void ArrayPool()
        {
            for (var i = 0; i < 8; i++)
            {
                arrays[i] = ArrayPool<long>.Shared.Rent(64);
            }

            for (var i = 0; i < 8; i++)
            {
                ArrayPool<long>.Shared.Return(arrays[i], true);
            }
        }

        [Benchmark]
        public void STArrayPool()
        {
            for (var i = 0; i < 8; i++)
            {
                arrays[i] = STArrayPool<long>.Shared.Rent(64);
            }

            for (var i = 0; i < 8; i++)
            {
                STArrayPool<long>.Shared.Return(arrays[i], true);
            }
        }

        [Benchmark]
        public void NewArray()
        {
            for (var i = 0; i < 8; i++)
            {
                newArrays[i] = new long[64];
            }
        }

        [Benchmark]
        public void NewQueue()
        {
            for (var i = 0; i < 8; i++)
            {
                newQueue[i] = new Queue<long>();
                newQueue[i].EnsureCapacity(64);
            }
        }
    }
}
