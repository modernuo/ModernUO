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
