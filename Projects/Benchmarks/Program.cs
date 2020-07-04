using BenchmarkDotNet.Running;

namespace Benchmarks
{
  class Program
  {
    static void Main(string[] args)
    {
      // var summary = BenchmarkRunner.Run<BenchmarkFeatureFlags>();
      var summar = BenchmarkRunner.Run<BenchmarkPacketConstruction>();
    }
  }
}
