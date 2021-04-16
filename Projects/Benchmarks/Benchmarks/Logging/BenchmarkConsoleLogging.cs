using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Serilog;
using Serilog.Core;

namespace Benchmarks
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class BenchmarkConsoleLogging
    {
        private const string text = "Sample message";

        private Logger logger;

        [IterationSetup]
        public void IterationSetup()
        {
            logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            logger = null;
        }

        [Benchmark]
        public void TestConsoleWriteLine()
        {
            for (int i = 0; i < 100; i++)
            {
                Console.WriteLine(text);
            }
        }

        [Benchmark]
        public void TestSerilogConsoleSink()
        {
            for (int i = 0; i < 100; i++)
            {
                logger.Information(text);
            }
        }
    }
}
