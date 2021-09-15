using BenchmarkDotNet.Running;

namespace Benchmarks
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            // var featureFlags = BenchmarkRunner.Run<BenchmarkFeatureFlags>();
            // var packetConstruction = BenchmarkRunner.Run<BenchmarkPacketConstruction>();
            // var broadcast = BenchmarkRunner.Run<BenchmarkPacketBroadcast>();
            // var stringHelpers = BenchmarkRunner.Run<BenchmarkStringHelpers>();
            var indexList = BenchmarkRunner.Run<BenchmarkOrderedHashSet>();
            // var textEncoding = BenchmarkRunner.Run<BenchmarkTextEncoding>();
            // var logging = BenchmarkRunner.Run<BenchmarkConsoleLogging>();
            // var gumpPacket = BenchmarkRunner.Run<OutgoingGumpPacketBenchmarks>();
        }
    }
}
