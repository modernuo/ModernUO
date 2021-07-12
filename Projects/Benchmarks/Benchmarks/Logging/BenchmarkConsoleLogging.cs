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
        private Logger asyncLogger;

        [GlobalSetup]
        public void GlobalSetup()
        {
            logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            asyncLogger = new LoggerConfiguration()
                .WriteTo.Async(a => a.Console())
                .CreateLogger();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            logger = null;
            asyncLogger = null;
        }

        [Benchmark]
        public void TestConsoleWriteLine()
        {
            for (int i = 0; i < 10000; i++)
            {
                Console.WriteLine(text);
            }
        }

        [Benchmark]
        public void TestSerilogConsoleSink()
        {
            for (int i = 0; i < 10000; i++)
            {
                logger.Information(text);
            }
        }

        [Benchmark]
        public void TestSerilogAsyncConsoleSink()
        {
            for (int i = 0; i < 10000; i++)
            {
                asyncLogger.Information(text);
            }
        }
    }
}
