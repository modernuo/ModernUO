using System;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class TimerTests
{
    [Theory]
    [InlineData(0L, 8L)]
    [InlineData(80L, 80L)]
    [InlineData(65L, 72L)]
    [InlineData(32767L, 32768L)]
    [InlineData(32768L, 32768L)]
    [InlineData(32833L, 32840L)]
    [InlineData(134217729L, 134217736L)]
    public void TestVariousTimes(long ticks, long expectedTicks)
    {
        var timerTicks = new TimerTicks();
        Timer.Init(timerTicks.Ticks);

        Timer.StartTimer(TimeSpan.FromMilliseconds(ticks), action);

        var tickCount = expectedTicks / 8;

        for (int i = 1; i <= tickCount; i++)
        {
            timerTicks.Ticks = i * 8;

            Timer.Slice(timerTicks.Ticks);
        }

        Assert.Equal(1, timerTicks.ExecutedCount);
        return;

        void action()
        {
            Assert.Equal(expectedTicks, timerTicks.Ticks);
            timerTicks.ExecutedCount++;
        }
    }

    [Theory]
    [InlineData(1000L, 1000L, 30000L, 30000L, 2)]
    public void TestIntervals(long delay, long expectedDelayTicks, long interval, long expectedIntervalTicks, int count)
    {
        var timerTicks = new TimerTicks();

        Timer.Init(timerTicks.Ticks);

        Timer.StartTimer(TimeSpan.FromMilliseconds(delay), TimeSpan.FromMilliseconds(interval), count, action);

        var tickCount = (expectedDelayTicks + (expectedIntervalTicks * count - 1)) / 8;

        for (int i = 1; i <= tickCount; i++)
        {
            timerTicks.Ticks = i * 8;

            Timer.Slice(timerTicks.Ticks);
        }

        Assert.Equal(count, timerTicks.ExecutedCount);
        return;

        void action()
        {
            timerTicks.ExpectedTicks += timerTicks.ExecutedCount++ == 0 ? expectedDelayTicks : expectedIntervalTicks;
            Assert.Equal(timerTicks.ExpectedTicks, timerTicks.Ticks);
        }
    }

    [Fact]
    public void TestTimerStartedOnTick()
    {
        var timerTicks = new TimerTicks();

        Timer.Init(timerTicks.Ticks);

        var timer = new SelfRunningTimer(timerTicks);
        timer.Start();

        Timer.Slice(128);
        Assert.Equal(1, timerTicks.ExecutedCount);
        Timer.Slice(256);
        Assert.Equal(2, timerTicks.ExecutedCount);
    }

    private class TimerTicks
    {
        public long ExpectedTicks;
        public long Ticks;
        public int ExecutedCount;
    }

    private class SelfRunningTimer : Timer
    {
        private readonly TimerTicks _timerTicks;
        public SelfRunningTimer(TimerTicks ticks) : base(TimeSpan.FromMilliseconds(100)) => _timerTicks = ticks;

        protected override void OnTick()
        {
            if (_timerTicks.ExecutedCount++ == 0)
            {
                Delay = TimeSpan.FromMilliseconds(100);
                Start();
            }
        }
    }
}
