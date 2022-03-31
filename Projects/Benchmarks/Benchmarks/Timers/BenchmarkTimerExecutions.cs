using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Server
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net60, warmupCount: 20, targetCount: 20)]
    public class BenchmarkTimerExecutions
    {
        private const int timerCount = 1000;
        private CancellationTokenSource _cancellationTokenSource;
        private static SemaphoreSlim _slim;

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
            RUOTimer.TimerThread.Set();
            _cancellationTokenSource.Cancel();
            RUOTimer.TimerThread.CleanupForTesting();
            Timer.ClearAllTimers(0);
            GC.Collect();
        }

        [Benchmark]
        public void RUOTimerExecutions()
        {
            _slim = new SemaphoreSlim(1);

            for (var i = 0; i < timerCount; i++)
            {
                new TestRUOTimer(TimeSpan.FromMilliseconds(1), i).Start();
            }

            RUOTimer.TimerThread.m_TickCount += 8;
            RUOTimer.TimerThread.Set();
            _slim.Wait();
        }

        [Benchmark]
        public void MUOTimerExecutions()
        {
            for (var i = 0; i < timerCount; i++)
            {
                new TestMUOTimer(TimeSpan.FromMilliseconds(1), i).Start();
            }

            Timer.Slice(8);
        }

        public class TestRUOTimer : RUOTimer
        {
            private int _amount;

            public TestRUOTimer(TimeSpan delay, int amount) : base(delay) => _amount = amount;

            protected override void OnTick()
            {
                var b = 6 * _amount;
                if (_amount == timerCount - 1)
                {
                    _slim.Release();
                }
            }
        }

        public class TestMUOTimer : Timer
        {
            private int _amount;

            public TestMUOTimer(TimeSpan delay, int amount) : base(delay) => _amount = amount;

            protected override void OnTick()
            {
                var b = 6 * _amount;
            }
        }
    }
}
