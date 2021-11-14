using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server.Random;

namespace Benchmarks.Benchmarks.Rng
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net60)]
    public class BenchmarkXoshiro
    {
        private Random _random;
        private Xoshiro256PlusPlus _xoshiro256PlusPlus;

        [GlobalSetup]
        public void Setup()
        {
            _xoshiro256PlusPlus = new Xoshiro256PlusPlus();
            _random = new Random();
        }

        [Benchmark]
        public int SystemRandomULong() => _random.Next(10000);

        [Benchmark]
        public int XoshiroRandomULong() => _xoshiro256PlusPlus.Next(10000);

        [Benchmark]
        public double SystemRandomDouble() => _random.NextDouble();

        [Benchmark]
        public double XoshiroRandomDouble() => _xoshiro256PlusPlus.NextDouble();

        [Benchmark]
        public int SystemRandomMinMax() => _random.Next(5000, 85000);

        [Benchmark]
        public int XoshiroRandomMinMax()
        {
            const int min = 5000;
            const int max = 85000;

            return min + (int)_xoshiro256PlusPlus.Next((uint)(max - min + 1));
        }
    }
}
