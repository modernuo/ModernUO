using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server;
using Server.Items;

namespace Benchmarks
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class BenchmarkFeatureFlags
    {
        public Dictionary<Type, FeatureFlag<Item>> m_Dictionary;
        public ILookup<Type, FeatureFlag<Item>> m_Lookup;

        public Type[] m_TypesToLookUp;

        [GlobalSetup]
        public void Setup()
        {
            RNGCryptoServiceProvider csp = new RNGCryptoServiceProvider();

            string file = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "UOContent.dll");
            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);

            m_Dictionary = new Dictionary<Type, FeatureFlag<Item>>();
            List<FeatureFlag<Item>> m_Types = new List<FeatureFlag<Item>>();
            m_TypesToLookUp = new Type[100];

            foreach (var type in assembly.GetTypes())
            {
                if (typeof(Item).IsAssignableFrom(type))
                {
                    m_Dictionary.Add(type, new FeatureFlag<Item>());
                    m_Types.Add(new FeatureFlag<Item>{Type = type});
                }
            }

            Console.WriteLine("Dictionary Size: {0}", m_Dictionary.Count);
            Console.WriteLine("Lookup Size: {0}", m_Types.Count);

            m_Dictionary.TrimExcess();
            m_Lookup = m_Types.ToLookup(f => f.Type);
            Span<byte> bytes = stackalloc byte[4];

            for (int i = 0; i < 100; i++)
            {
                csp.GetBytes(bytes);
                m_TypesToLookUp[i] = m_Types[(int)(BinaryPrimitives.ReadUInt32BigEndian(bytes) % m_Types.Count)].Type;
            }
        }

        [Benchmark]
        public FeatureFlag<Item> TestDictionary()
        {
            for (int i = 0; i < 100; i++)
            {
                m_Dictionary.TryGetValue(typeof(ExplosionPotion), out var ff);
                if (i == 99)
                {
                    return ff;
                }
            }

            return null;
        }

        [Benchmark]
        public FeatureFlag<Item> TestLookup()
        {
            FeatureFlag<Item> ff;
            for (int i = 0; i < 100; i++)
            {
                ff = m_Lookup[typeof(ExplosionPotion)].GetEnumerator().Current;
                if (i == 99)
                {
                    return ff;
                }
            }

            return null;
        }
    }
}
