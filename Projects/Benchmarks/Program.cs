using BenchmarkDotNet.Running;

namespace Benchmarks
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            // var featureFlags = BenchmarkRunner.Run<BenchmarkFeatureFlags>();
            // var packetConstruction = BenchmarkRunner.Run<BenchmarkPacketConstruction>();
            var broadcast = BenchmarkRunner.Run<BenchmarkPacketBroadcast>();
        }
    }
}
