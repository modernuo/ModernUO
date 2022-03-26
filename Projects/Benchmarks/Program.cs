using BenchmarkDotNet.Running;
using Benchmarks.EntitiesSelectors;
using Benchmarks.ItemSelectors;
using Benchmarks.MobileSelectors;
using Benchmarks.MultiSelectors;
using Benchmarks.MultiTilesSelectors;

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
            // var indexList = BenchmarkRunner.Run<BenchmarkOrderedHashSet>();
            // var textEncoding = BenchmarkRunner.Run<BenchmarkTextEncoding>();
            // var logging = BenchmarkRunner.Run<BenchmarkConsoleLogging>();
            // var gumpPacket = BenchmarkRunner.Run<OutgoingGumpPacketBenchmarks>();
            // var rngTest = BenchmarkRunner.Run<BenchmarkXoshiro>();
            //var doubleRngText = BenchmarkRunner.Run<BenchmarkDoubleVsFixed>();

            //var mapEntitiesSelectors = BenchmarkRunner.Run<MapEntitiesSelectors>();
            //var mapMobilesSelectors = BenchmarkRunner.Run<MapMobileSelectors>();
            //var mapMultiTilesSelectors = BenchmarkRunner.Run<MapMultiTilesSelectors>();
            //var mapMultiSelectors = BenchmarkRunner.Run<MapMultiSelectors>();
            // var mapItemsSelectors = BenchmarkRunner.Run<MapItemSelectors>();
            // var stArray = BenchmarkRunner.Run<BenchmarkSTArray>();
            // var pooledRefQueue = BenchmarkRunner.Run<BenchmarkPooledRefQueue>();
        }
    }
}
