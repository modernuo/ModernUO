using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Server.Network;

namespace Benchmarks
{
    [MemoryDiagnoser, SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class PacketBroadcast
    {
        private Pipe<byte>[] _pipes = new Pipe<byte>[1500];

        [GlobalSetup]
        public void SetUp()
        {
            for (var i = 0; i < _pipes.Length; i++)
            {
                _pipes[i] = new Pipe<byte>(new byte[8192]);
            }
        }

        [Benchmark]
        public int TestCircularBuffer()
        {
            var text = "This is some really long text that we want to handle. It should take a little bit to encode this.";
            foreach (var pipe in _pipes)
            {

            }
        }
    }
}
