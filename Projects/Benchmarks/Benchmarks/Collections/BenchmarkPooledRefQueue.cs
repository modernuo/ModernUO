using System.Buffers;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server.Buffers;
using Server.Collections;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net60)]
    public class BenchmarkPooledRefQueue
    {
        [GlobalSetup]
        public void Setup()
        {
            // Allocate
            var arrays = new long[16][];
            for (var i = 0; i < 16; i++)
            {
                arrays[i] = ArrayPool<long>.Shared.Rent(64);
            }

            var stArrays = new long[16][];
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
        public void UseQueue()
        {
            for (var i = 0; i < 8; i++)
            {
                var queue = new Queue<long>();
                for (var j = 0; j < 32; j++)
                {
                    queue.Enqueue(j);
                }

                for (var j = 0; j < 32; j++)
                {
                    var num = queue.Dequeue();
                }
            }
        }

        [Benchmark]
        public void UsePooledRefQueue()
        {
            for (var i = 0; i < 8; i++)
            {
                using var queue = PooledRefQueue<long>.Create();
                for (var j = 0; j < 32; j++)
                {
                    queue.Enqueue(j);
                }

                for (var j = 0; j < 32; j++)
                {
                    var num = queue.Dequeue();
                }
            }
        }

        [Benchmark]
        public void UsePooledRefQueueMT()
        {
            for (var i = 0; i < 8; i++)
            {
                using var queue = PooledRefQueue<long>.CreateMT();
                for (var j = 0; j < 32; j++)
                {
                    queue.Enqueue(j);
                }

                for (var j = 0; j < 32; j++)
                {
                    var num = queue.Dequeue();
                }
            }
        }
    }
}
