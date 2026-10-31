using System.Net;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class IPRateLimiterTests
{
    private const int MaxAttempts = 10;
    private const long TimeWindow = 10_000;
    private const long MaxBackoff = 3_600_000;

    private static readonly IPAddress _ip = IPAddress.Parse("203.0.113.7");

    private static IPRateLimiter CreateLimiter() => new(MaxAttempts, TimeWindow, 1000, 2.0, MaxBackoff);

    [Theory]
    [InlineData(1_000_000L)]
    [InlineData(-4_000_000_000_000L)]
    public void Cleanup_EvictsIdleAddresses(long startTicks)
    {
        var previousTicks = Core._tickCount;

        try
        {
            Core._tickCount = startTicks;
            var limiter = CreateLimiter();

            Assert.True(limiter.Verify(_ip, out _));
            Assert.Equal(1, limiter.Count);

            limiter.Cleanup(startTicks + TimeWindow);
            Assert.Equal(1, limiter.Count);

            limiter.Cleanup(startTicks + TimeWindow + 1);
            Assert.Equal(0, limiter.Count);
        }
        finally
        {
            Core._tickCount = previousTicks;
        }
    }

    [Fact]
    public void Cleanup_KeepsBlockedAddressUntilBlockExpires()
    {
        var previousTicks = Core._tickCount;

        try
        {
            var start = 1_000_000L;
            Core._tickCount = start;
            var limiter = CreateLimiter();

            for (var i = 0; i < MaxAttempts; i++)
            {
                Assert.True(limiter.Verify(_ip, out _));
            }

            // Attempts during the first short block escalate the next one to the cap
            for (var i = 0; i < 20; i++)
            {
                Assert.False(limiter.Verify(_ip, out _));
            }

            Core._tickCount += 2000;
            var blockedAt = Core._tickCount;
            Assert.False(limiter.Verify(_ip, out _));

            limiter.Cleanup(blockedAt + TimeWindow + 1);
            Assert.Equal(1, limiter.Count);

            Core._tickCount = blockedAt + TimeWindow + 1;
            Assert.False(limiter.Verify(_ip, out _));

            limiter.Cleanup(blockedAt + MaxBackoff);
            Assert.Equal(0, limiter.Count);
        }
        finally
        {
            Core._tickCount = previousTicks;
        }
    }

    [Fact]
    public void Cleanup_IsBoundedPerCallAndGetsPastBlockedAddresses()
    {
        var previousTicks = Core._tickCount;

        try
        {
            var start = 1_000_000L;
            Core._tickCount = start;
            var limiter = new IPRateLimiter(MaxAttempts, TimeWindow, 60_000, 2.0, MaxBackoff);

            // Blocked addresses at the front of the queue must not stall the idle ones behind them
            var blocked = IPAddress.Parse("203.0.113.1");
            for (var i = 0; i <= MaxAttempts; i++)
            {
                limiter.Verify(blocked, out _);
            }

            const int idleCount = 3000;
            for (var i = 0; i < idleCount; i++)
            {
                Assert.True(limiter.Verify(new IPAddress([198, 51, (byte)(i >> 8), (byte)i]), out _));
            }

            Assert.Equal(idleCount + 1, limiter.Count);

            // Idle long enough to evict, short of the escalated block on the first address
            var now = start + TimeWindow + 1;
            limiter.Cleanup(now);
            Assert.True(limiter.Count > idleCount + 1 - 1100);
            Assert.True(limiter.Count < idleCount + 1);

            for (var i = 0; i < 10 && limiter.Count > 1; i++)
            {
                limiter.Cleanup(now);
            }

            Assert.Equal(1, limiter.Count);
        }
        finally
        {
            Core._tickCount = previousTicks;
        }
    }

    [Fact]
    public void Verify_AdmitsNewAddressWhenTicksAreNegative()
    {
        var previousTicks = Core._tickCount;

        try
        {
            Core._tickCount = -4_000_000_000_000L;
            var limiter = CreateLimiter();

            Assert.True(limiter.Verify(_ip, out var attempts));
            Assert.Equal(1, attempts);
        }
        finally
        {
            Core._tickCount = previousTicks;
        }
    }
}
