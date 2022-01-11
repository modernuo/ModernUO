using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server.Random;

namespace Benchmarks.Benchmarks.Rng
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net60)]
    public class BenchmarkDoubleVsFixed
    {
        private Xoshiro256PlusPlus _xoshiro256PlusPlus;

        [GlobalSetup]
        public void Setup()
        {
            _xoshiro256PlusPlus = new Xoshiro256PlusPlus();
        }

        [Benchmark]
        public bool NextDouble() => 50.1 < _xoshiro256PlusPlus.NextDouble() * 100;

        [Benchmark]
        public bool NextFixedInt() => 501 < _xoshiro256PlusPlus.Next(1000);

        [Benchmark]
        public bool NextHighResDouble() => 50.1 < _xoshiro256PlusPlus.NextDoubleHighRes() * 100;
    }
}
