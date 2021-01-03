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
        private List<string> _list;
        private OrderedHashSet<string> _ordered;
        private HashSet<string> _hashSet;
        private const int iterations = 16;

        [IterationSetup]
        public void IterationSetup()
        {
            _list = new List<string>();
            _ordered = new OrderedHashSet<string>();
            _hashSet = new HashSet<string>();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _list = null;
            _ordered = null;
            _hashSet = null;
        }

        [Benchmark]
        public int UsingList()
        {
            for (int i = 0; i < iterations / 2; i++)
            {
                AddIfNotPresent(_list, i.ToString());
            }

            for (int i = 0; i < iterations; i++)
            {
                AddIfNotPresent(_list, i.ToString());
            }

            for (int i = 0; i < iterations * 2; i++)
            {
                AddIfNotPresent(_list, i.ToString());
            }

            for (int i = 0; i < _list.Count; i++)
            {
                _list[i].ToString();
            }

            return _list.Count;
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
            for (int i = 0; i < iterations / 2; i++)
            {
                _ordered.GetOrAdd(i.ToString());
            }

            for (int i = 0; i < iterations; i++)
            {
                _ordered.GetOrAdd(i.ToString());
            }

            for (int i = 0; i < iterations * 2; i++)
            {
                _ordered.GetOrAdd(i.ToString());
            }

            foreach (var str in _ordered)
            {
                str.ToString();
            }

            return _ordered.Count;
        }

        [Benchmark]
        public int UsingHashSet()
        {
            for (int i = 0; i < iterations / 2; i++)
            {
                _hashSet.Add(i.ToString());
            }

            for (int i = 0; i < iterations; i++)
            {
                _hashSet.Add(i.ToString());
            }

            for (int i = 0; i < iterations * 2; i++)
            {
                _hashSet.Add(i.ToString());
            }

            foreach (var str in _hashSet)
            {
                str.ToString();
            }

            return _hashSet.Count;
        }
    }
}
