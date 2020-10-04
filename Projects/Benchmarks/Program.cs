using BenchmarkDotNet.Running;

namespace Benchmarks
{
    public class Program
    {
        static void Main(string[] args)
        {
            var featureFlags = BenchmarkRunner.Run<BenchmarkFeatureFlags>();
            var packetConstruction = BenchmarkRunner.Run<BenchmarkPacketConstruction>();
        }
    }
}
