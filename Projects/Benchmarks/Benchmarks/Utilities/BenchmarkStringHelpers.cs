using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server.Buffers;

namespace Benchmarks.BenchmarkUtilities
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class BenchmarkStringHelpers
    {
        private readonly string[] names =
        {
            "Kamron", "Owyn", "Luthius", "Jaedan", "Vorspire", "other people",
            "Kamron-2", "Owyn-2", "Luthius-2", "Jaedan-2", "Vorspire-2", "other people too"
        };

        private int length;

        [GlobalSetup]
        public void Setup()
        {
            var chrs = ArrayPool<char>.Shared.Rent(65535);
            ArrayPool<char>.Shared.Return(chrs);
            length = 0;

            for (int i = 0; i < names.Length; i++)
            {
                length += names.Length;
            }

            length += 2 * (names.Length - 1) + 3;
        }

        [Benchmark]
        public string BenchmarkStringBuilder()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < names.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(i == names.Length - 1 ? ", and" : ", ");
                }

                sb.Append(names[i]);
            }

            return sb.ToString();
        }

        [Benchmark]
        public string BenchmarkValueStringBuilderWithStack()
        {
            using var sb = new ValueStringBuilder(stackalloc char[length]);
            for (var i = 0; i < names.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(i == names.Length - 1 ? ", and" : ", ");
                }

                sb.Append(names[i]);
            }

            return sb.ToString();
        }

        [Benchmark]
        public string BenchmarkValueStringBuilderWithRentedBuffer()
        {
            using var sb = new ValueStringBuilder(stackalloc char[32]);
            for (var i = 0; i < names.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(i == names.Length - 1 ? ", and" : ", ");
                }

                sb.Append(names[i]);
            }

            return sb.ToString();
        }
    }
}
