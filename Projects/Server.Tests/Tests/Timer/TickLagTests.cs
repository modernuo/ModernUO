using Xunit;

namespace Server.Tests;

/// <summary>
/// Tick lag is how far behind schedule the timer wheel actually turned, which is the quantity that
/// corresponds to player-felt lag. It is deliberately not the loop's iteration rate: a loop that
/// spins fast while the wheel falls behind is lagging, and a loop that sleeps while the wheel keeps
/// up is healthy.
/// </summary>
[Collection("Sequential Server Tests")]
public class TickLagTests
{
    [Fact]
    public void OnTimeSliceReportsNoLag()
    {
        Timer.Init(0);
        Timer.ResetPeakTickLag();

        // Arriving exactly one tick rate later is on schedule, not late.
        Timer.Slice(8);

        Assert.Equal(0, Timer.LastTickLag);
        Assert.Equal(0, Timer.PeakTickLag);
    }

    [Fact]
    public void EarlySliceReportsNoLag()
    {
        Timer.Init(0);
        Timer.ResetPeakTickLag();

        Timer.Slice(3);

        Assert.Equal(0, Timer.LastTickLag);
    }

    [Fact]
    public void LateSliceReportsLagBeyondOneTick()
    {
        Timer.Init(0);
        Timer.ResetPeakTickLag();

        // 30ms since the last turn, against an 8ms tick rate: 22ms of that was owed and not paid.
        Timer.Slice(30);

        Assert.Equal(22, Timer.LastTickLag);
    }

    [Fact]
    public void PeakRetainsWorstAndLastTracksMostRecent()
    {
        Timer.Init(0);
        Timer.ResetPeakTickLag();

        Timer.Slice(100); // badly late
        var peakAfterSpike = Timer.PeakTickLag;

        // Catch back up: the wheel advanced to 96, so arriving at 104 is one tick, on time.
        Timer.Slice(104);

        Assert.True(peakAfterSpike > 0, "The spike should have registered as lag.");
        Assert.Equal(0, Timer.LastTickLag);
        Assert.Equal(peakAfterSpike, Timer.PeakTickLag);
    }

    // SkippedTicks and TotalTurns are monotonic by design, since the loop's backoff reads them
    // alongside the reporter. Tests difference them the same way real consumers do.
    [Fact]
    public void OnTimeSliceSkipsNothing()
    {
        Timer.Init(0);
        var turns = Timer.TotalTurns;
        var skipped = Timer.SkippedTicks;

        Timer.Slice(8);

        Assert.Equal(1, Timer.TotalTurns - turns);
        Assert.Equal(0, Timer.SkippedTicks - skipped);
    }

    [Fact]
    public void CompressedTurnsCountAsSkippedTicks()
    {
        Timer.Init(0);
        var turns = Timer.TotalTurns;
        var skipped = Timer.SkippedTicks;

        // 30ms against an 8ms rate: three turns come due at once, so two slots fired late.
        Timer.Slice(30);

        Assert.Equal(3, Timer.TotalTurns - turns);
        Assert.Equal(2, Timer.SkippedTicks - skipped);
    }

    [Fact]
    public void OneSlotLateMissesTheTickBudgetButNotTheFrameBudget()
    {
        // 16ms: two turns, so the oldest slot fired 8ms late. Anything on an 8ms cadence has
        // missed; anything on 16ms has not.
        Timer.Init(0);
        var tick = Timer.MissedTickDeadlines;
        var frame = Timer.MissedFrameDeadlines;

        Timer.Slice(16);

        Assert.Equal(1, Timer.MissedTickDeadlines - tick);
        Assert.Equal(0, Timer.MissedFrameDeadlines - frame);
    }

    [Fact]
    public void TwoSlotsLateMissesTheFrameBudget()
    {
        // 24ms: three turns, so the oldest slot fired 16ms late. Nothing on a 16ms cadence can
        // absorb that, which is the case the adaptive backoff exists to catch.
        Timer.Init(0);
        var tick = Timer.MissedTickDeadlines;
        var frame = Timer.MissedFrameDeadlines;

        Timer.Slice(24);

        Assert.Equal(1, Timer.MissedTickDeadlines - tick);
        Assert.Equal(1, Timer.MissedFrameDeadlines - frame);
    }

    [Fact]
    public void OnScheduleTurnsMissNeitherBudget()
    {
        Timer.Init(0);
        var tick = Timer.MissedTickDeadlines;
        var frame = Timer.MissedFrameDeadlines;

        for (var t = 8; t <= 80; t += 8)
        {
            Timer.Slice(t);
        }

        Assert.Equal(0, Timer.MissedTickDeadlines - tick);
        Assert.Equal(0, Timer.MissedFrameDeadlines - frame);
    }

    [Fact]
    public void SteadyTurnsAccumulateWithoutSkipping()
    {
        // Arriving exactly on each boundary is the healthy case: many turns, nothing skipped.
        Timer.Init(0);
        var turns = Timer.TotalTurns;
        var skipped = Timer.SkippedTicks;

        for (var t = 8; t <= 80; t += 8)
        {
            Timer.Slice(t);
        }

        Assert.Equal(10, Timer.TotalTurns - turns);
        Assert.Equal(0, Timer.SkippedTicks - skipped);
    }

    [Fact]
    public void ResetClearsPeakSoWindowsAreIndependent()
    {
        Timer.Init(0);
        Timer.ResetPeakTickLag();

        Timer.Slice(50);
        Assert.True(Timer.PeakTickLag > 0);

        Timer.ResetPeakTickLag();

        Assert.Equal(0, Timer.PeakTickLag);
    }
}
