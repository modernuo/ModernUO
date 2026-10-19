using Server.Network;
using Xunit;

namespace Server.Tests.Network;

/// <summary>
/// Manual tests for MovementThrottle latency scenarios.
/// These tests simulate specific timing scenarios and should NOT run in CI/CD.
///
/// To run these tests manually:
///   dotnet test --filter "Category=Manual"
///
/// To exclude from CI/CD, add to your test command:
///   dotnet test --filter "Category!=Manual"
/// </summary>
[Collection("Sequential Server Tests")]
[Trait("Category", "Manual")]
public class MovementThrottleLatencyTests
{
    /// <summary>
    /// Sets the simulated tick count for testing.
    /// </summary>
    private static void SetTickCount(long ticks)
    {
        Core._tickCount = ticks;
    }

    /// <summary>
    /// Simulates recording movements at specific intervals.
    /// </summary>
    private static void SimulateMovements(NetState ns, params (int delayMs, int costMs)[] movements)
    {
        ns._movementHistory ??= new NetState.MovementRecord[20];
        ns._movementHistoryIndex = 0;
        ns._movementHistoryFull = false;
        ns._lastMovementRecordTime = 0;

        var currentTime = 0L;

        foreach (var (delayMs, costMs) in movements)
        {
            currentTime += delayMs;
            SetTickCount(currentTime);

            if (ns._lastMovementRecordTime > 0)
            {
                var interval = (int)(currentTime - ns._lastMovementRecordTime);

                // Skip chain-breaking gaps (> 2000ms) like the real code does
                if (interval > 2000)
                {
                    ns._lastGapDuration = interval;
                    ns._lastMovementRecordTime = currentTime;
                    continue;
                }

                ns._movementHistory[ns._movementHistoryIndex] = new NetState.MovementRecord
                {
                    Interval = (short)interval,
                    TargetSpeed = (ushort)costMs,
                    QueueDepth = 0,
                    Flags = 0
                };

                ns._movementHistoryIndex++;
                if (ns._movementHistoryIndex >= 20)
                {
                    ns._movementHistoryIndex = 0;
                    ns._movementHistoryFull = true;
                }
            }

            ns._lastMovementRecordTime = currentTime;
        }
    }

    /// <summary>
    /// Scenario: Verifies that burst forgiveness triggers correctly and clears gap duration.
    /// </summary>
    [Fact]
    public void MultipleLagSpikes_NoAccumulatedSuspicion()
    {
        var ns = PacketTestUtilities.CreateTestNetState();
        SetTickCount(0);

        // Set up history buffer with mostly normal movements and a burst at the end
        ns._movementHistory = new NetState.MovementRecord[20];

        // Fill with 17 normal movements (100ms intervals)
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

        // Add 3 burst movements at the end (10ms intervals = burst)
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

        ns._movementHistoryIndex = 0; // Next write position (wrapped)
        ns._movementHistoryFull = true; // Buffer is full

        // Set gap duration (simulating a lag spike before the burst)
        ns._lastGapDuration = 2500;

        // Analyze - burst forgiveness should trigger
        var verdict = MovementThrottle.AnalyzeMovement(ns, out var rate, out var samples, out var confidence);

        // Key assertions:
        // 1. Gap duration should be cleared (burst forgiveness triggered)
        Assert.Equal(0, ns._lastGapDuration);

        // 2. Confidence should be reduced by 0.3x multiplier
        Assert.True(
            confidence < 0.5f,
            $"Burst after gap should have low confidence. Confidence={confidence}, Rate={rate}"
        );

        // 3. Should NOT be Definite despite the burst (forgiveness applied)
        Assert.True(
            verdict != MovementThrottle.DetectionVerdict.Definite,
            $"Should not be Definite after burst forgiveness. Verdict={verdict}"
        );
    }
}
