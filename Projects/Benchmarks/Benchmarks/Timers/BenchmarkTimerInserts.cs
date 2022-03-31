using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Server
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net60, warmupCount: 20, targetCount: 20)]
    public class BenchmarkTimerInserts
    {
        private const int timerCount = 1000;
        private CancellationTokenSource _cancellationTokenSource;

        [GlobalSetup]
        public void Setup()
        {
            Core.Profiling = false;
            Timer.Init(0);

            RUOTimer.TimerThread ttObj = new RUOTimer.TimerThread();
            _cancellationTokenSource = new CancellationTokenSource();
            var timerThread = new Thread(() => ttObj.TimerMain(_cancellationTokenSource.Token))
            {
                Name = "Timer Thread"
            };

            timerThread.Start();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _cancellationTokenSource.Cancel();
            RUOTimer.TimerThread.Set();
            RUOTimer.TimerThread.CleanupForTesting();
            Timer.ClearAllTimers(0);
            GC.Collect();
        }

        [Benchmark]
        public void RUOTimerInserts()
        {
            for (var i = 0; i < timerCount; i++)
            {
                new RUOTimer(TimeSpan.Zero).Start();
            }
            RUOTimer.TimerThread.Set();
        }

        [Benchmark]
        public void MUOTimerInserts()
        {
            for (var i = 0; i < timerCount; i++)
            {
                new Timer(TimeSpan.Zero).Start();
            }
        }
    }
}
