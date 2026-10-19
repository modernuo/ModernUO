using Server.Network;
using Xunit;

namespace Server.Tests.Network;

/// <summary>
/// MovementThrottle burst forgiveness against a full, wrapped movement history.
/// </summary>
[Collection("Sequential Server Tests")]
public class MovementThrottleLatencyTests
{
    [Fact]
    public void AnalyzeMovement_BurstAfterGap_WrappedFullHistory_ForgivesAndClearsGap()
    {
        var ns = PacketTestUtilities.CreateTestNetState();

        // The production recorder is private and only reachable through a full movement request, so
        // the history is laid out directly: 17 normal steps, then a 3-step burst ending at the wrap
        ns._movementHistory = new NetState.MovementRecord[20];

        for (var i = 0; i < 17; i++)
        {
            ns._movementHistory[i] = new NetState.MovementRecord
            {
                Interval = 100,
                TargetSpeed = 100,
                QueueDepth = 0,
                Flags = 0
            };
        }

        for (var i = 17; i < 20; i++)
        {
            ns._movementHistory[i] = new NetState.MovementRecord
            {
                Interval = 10,
                TargetSpeed = 100,
                QueueDepth = 0,
                Flags = 0
            };
        }

        ns._movementHistoryIndex = 0;
        ns._movementHistoryFull = true;

        // A lag spike longer than the chain gap preceded the burst
        ns._lastGapDuration = 2500;

        var verdict = MovementThrottle.AnalyzeMovement(ns, out var rate, out _, out var confidence);

        Assert.Equal(0, ns._lastGapDuration);
        Assert.True(
            confidence < 0.5f,
            $"Burst after gap should have low confidence. Confidence={confidence}, Rate={rate}"
        );
        Assert.True(
            verdict != MovementThrottle.DetectionVerdict.Definite,
            $"Should not be Definite after burst forgiveness. Verdict={verdict}"
        );
    }
}
