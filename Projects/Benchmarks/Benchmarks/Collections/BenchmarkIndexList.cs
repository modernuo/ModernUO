using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server.Collections;

namespace Benchmarks
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class BenchmarkIndexList
    {
        public List<string> _list;
        public IndexList<string> _indexList;

        [GlobalSetup]
        public void Setup()
        {
            _list = new List<string>();
            _indexList = new IndexList<string>();
        }

        [Benchmark]
        public int UsingList()
        {
            for (int i = 0; i < 32; i++)
            {
                AddIfNotPresent(_list, i.ToString());
            }

            for (int i = 0; i < 64; i++)
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
        public int UsingIndexList()
        {
            for (int i = 0; i < 32; i++)
            {
                _indexList.Add(i.ToString());
            }

            for (int i = 0; i < 64; i++)
            {
                _indexList.Add(i.ToString());
            }

            foreach (var i in _indexList.GetSortedList())
            {
                i.Key.ToString();
            }

            return _indexList.Count;
        }
    }
}
