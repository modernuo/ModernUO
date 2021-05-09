using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks.BenchmarkText
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class BenchmarkCreatingStrings
    {
        private byte[] text;

        [GlobalSetup]
        public void Setup()
        {
            text = Encoding.ASCII.GetBytes("Some random text that is really long pew pew pew\0\0\0\0\0\0\0\0\0\0\0");
        }

        [Benchmark]
        public string TestGetStringWithoutTerminators()
        {
            var span = text.AsSpan();
            var terminator = span.IndexOf((byte)0);

            return Encoding.ASCII.GetString(span[..terminator]);
        }

        [Benchmark]
        public string TestGetCharsWithStringCtorWithoutTerminators()
        {
            var span = text.AsSpan();
            var terminator = span.IndexOf((byte)0);
            Span<char> chars = stackalloc char[terminator];
            Encoding.ASCII.GetChars(span[..terminator], chars);
            return new string(chars);
        }

        [Benchmark]
        public unsafe string TestWithPtrStringCtorWithoutTerminators()
        {
            fixed (byte* ptr = text)
            {
                return new string((sbyte*)ptr, 0, text.Length, Encoding.ASCII);
            }
        }

        [Benchmark]
        public string TestGetString() => Encoding.ASCII.GetString(text);

        [Benchmark]
        public string TestGetCharsWithStringCtor()
        {
            Span<char> chars = stackalloc char[text.Length];
            Encoding.ASCII.GetChars(text, chars);
            return new string(chars);
        }

        [Benchmark]
        public unsafe string TestWithPtrStringCtor()
        {
            fixed (byte* ptr = text)
            {
                return new string((sbyte*)ptr, 0, text.Length, Encoding.ASCII);
            }
        }
    }
}
