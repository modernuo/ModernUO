using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server.Collections;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class BenchmarkOrderedHashSet
    {
        private readonly string[] _iterations = new string[16];

        [IterationSetup]
        public void IterationSetup()
        {
            for (var i = 0; i < _iterations.Length; i++)
            {
                _iterations[i] = i.ToString();
            }
        }

        [Benchmark]
        public int UsingList()
        {
            var list = new List<string>();
            for (int i = 0; i < _iterations.Length / 2; i++)
            {
                AddIfNotPresent(list, _iterations[i]);
            }

            for (int i = 0; i < _iterations.Length; i++)
            {
                AddIfNotPresent(list, _iterations[i]);
            }

            for (int i = 0; i < list.Count; i++)
            {
                list[i].ToString();
            }

            return list.Count;
        }

        private static int AddIfNotPresent<T>(List<T> list, T item)
        {
            var index = list.IndexOf(item);
            if (index > -1)
            {
                return index;
            }

            list.Add(item);
            return list.Count - 1;
        }

        [Benchmark]
        public int UsingOrderedHashSet()
        {
            var ordered = new OrderedHashSet<string>();
            for (int i = 0; i < _iterations.Length / 2; i++)
            {
                ordered.GetOrAdd(_iterations[i]).ToString();
            }

            for (int i = 0; i < _iterations.Length; i++)
            {
                ordered.GetOrAdd(_iterations[i]).ToString();
            }

            foreach (var str in ordered)
            {
                str.ToString();
            }

            return ordered.Count;
        }

        [Benchmark]
        public int UsingPooledOrderedHashSet()
        {
            var ordered = new PooledOrderedHashSet<string>();
            for (int i = 0; i < _iterations.Length / 2; i++)
            {
                ordered.GetOrAdd(_iterations[i]).ToString();
            }

            for (int i = 0; i < _iterations.Length; i++)
            {
                ordered.GetOrAdd(_iterations[i]).ToString();
            }

            foreach (var str in ordered)
            {
                str.ToString();
            }

            return ordered.Count;
        }

        [Benchmark]
        public int UsingHashSet()
        {
            var hashSet = new HashSet<(string, int)>(new OrderedStringComparer());
            for (int i = 0; i < _iterations.Length / 2; i++)
            {
                hashSet.Add((_iterations[i], i));
            }

            for (int i = 0; i < _iterations.Length; i++)
            {
                hashSet.Add((_iterations[i], i));
            }

            foreach (var str in hashSet)
            {
                str.ToString();
            }

            return hashSet.Count;
        }

        private class OrderedStringComparer : EqualityComparer<(string, int)>
        {
            public override bool Equals((string, int) x, (string, int) y) => x.Item1.Equals(y.Item1, System.StringComparison.Ordinal);

            public override int GetHashCode((string, int) obj) => obj.Item1.GetHashCode();
        }
    }
}
