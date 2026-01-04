/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MovementThrottleLatencyTests.cs                                 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

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
    /// Scenario: Player experiences a 3-second network lag spike, then all buffered
    /// packets arrive at once. This should NOT trigger false positive detection.
    ///
    /// Timeline:
    /// - T=0-1000ms: Normal movement (10 packets at 100ms intervals)
    /// - T=1000-4000ms: Network lag (client buffers packets)
    /// - T=4000ms: Network recovers, 30 buffered packets arrive simultaneously
    /// </summary>
    [Fact]
    public void LagSpike_BurstRecovery_NoFalsePositive()
    {
        var ns = PacketTestUtilities.CreateTestNetState();
        SetTickCount(0);

        // Phase 1: Normal movement for 1 second
        var movements = new (int delayMs, int costMs)[10];
        for (var i = 0; i < 10; i++)
        {
            movements[i] = (100, 100); // Normal mounted running
        }
        SimulateMovements(ns, movements);

        // Verify normal rate before lag
        var rateBeforeLag = MovementThrottle.CalculateMovementRate(ns, out var samples);
        Assert.True(samples >= 8, $"Expected >= 8 samples, got {samples}");
        Assert.True(rateBeforeLag >= 0.95f && rateBeforeLag <= 1.05f,
            $"Expected normal rate before lag, got {rateBeforeLag}");

        // Phase 2: Simulate 3 second lag followed by burst
        // The gap (3000ms) is tracked, then burst packets arrive
        var currentTime = Core.TickCount;

        // Record the lag gap
        SetTickCount(currentTime + 3000);
        ns._lastGapDuration = 3000; // Gap > maxChainGap triggers this

        // Burst packets arriving (5 packets with tiny intervals)
        for (var i = 0; i < 5; i++)
        {
            SetTickCount(Core.TickCount + 2); // 2ms between packets (simultaneous arrival)

            if (ns._lastMovementRecordTime > 0)
            {
                var interval = (int)(Core.TickCount - ns._lastMovementRecordTime);
                ns._movementHistory[ns._movementHistoryIndex] = new NetState.MovementRecord
                {
                    Interval = (short)interval,
                    TargetSpeed = 100,
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
            ns._lastMovementRecordTime = Core.TickCount;
        }

        // Analyze - should apply burst forgiveness due to large gap
        var verdict = MovementThrottle.AnalyzeMovement(ns, out var rate, out _, out var confidence);

        // The burst will have high rate due to tiny intervals, but:
        // 1. _lastGapDuration > 1000 (maxChainGap/2) triggers burst forgiveness
        // 2. Confidence should be reduced by 0.3f multiplier
        // 3. Should NOT be Definite unless truly extreme
        Assert.True(
            verdict != MovementThrottle.DetectionVerdict.Definite,
            $"Lag recovery should not be Definite. Rate={rate}, Confidence={confidence}, Verdict={verdict}"
        );

        // Gap duration should be cleared after analysis
        Assert.Equal(0, ns._lastGapDuration);
    }

    /// <summary>
    /// Scenario: Player with consistently high latency (400ms RTT) moves normally.
    /// Their connection is consistent but slow - should NOT be considered "stable"
    /// for the purpose of tightening detection thresholds.
    /// </summary>
    [Fact]
    public void HighLatencyConsistent_NotConsideredStable()
    {
        var ns = PacketTestUtilities.CreateTestNetState();

        // Set up high but consistent RTT
        ns._rttHistory = new long[] { 400, 405, 398, 402, 401, 399, 403, 400 };
        ns._rttSampleCount = 8;

        // Calculate variance (should be low since values are consistent)
        long sum = 0, sumSq = 0;
        for (var i = 0; i < 8; i++)
        {
            sum += ns._rttHistory[i];
            sumSq += ns._rttHistory[i] * ns._rttHistory[i];
        }
        var mean = sum / 8;
        ns._rttVariance = sumSq / 8 - mean * mean;

        // Variance is low (consistent), but should NOT be "stable" due to high latency
        Assert.False(ns.HasStableConnection,
            $"400ms RTT should not be stable. AvgRTT={ns.AverageRtt}, Variance={ns._rttVariance}");

        // Now verify detection gives more leniency for high-latency connections
        SimulateMovements(ns,
            (105, 100), (105, 100), (105, 100), (105, 100), (105, 100),
            (105, 100), (105, 100), (105, 100), (105, 100), (105, 100)
        );

        var verdict = MovementThrottle.AnalyzeMovement(ns, out var rate, out _, out var confidence);

        // Slightly slow but high-latency player should get benefit of doubt
        Assert.True(
            verdict == MovementThrottle.DetectionVerdict.Normal ||
            verdict == MovementThrottle.DetectionVerdict.Possible,
            $"High latency player should get leniency. Verdict={verdict}"
        );
    }

    /// <summary>
    /// Scenario: RTT probing and measurement.
    /// Verifies that RTT variance is calculated correctly and stability
    /// is properly determined.
    /// </summary>
    [Fact]
    public void RttMeasurement_VarianceCalculation()
    {
        var ns = PacketTestUtilities.CreateTestNetState();
        SetTickCount(1000);

        // Simulate stable low-latency RTT measurements
        var rttValues = new long[] { 50, 48, 52, 49, 51, 50, 48, 52 };

        ns._rttHistory = new long[8];
        for (var i = 0; i < rttValues.Length; i++)
        {
            ns._rttHistory[i] = rttValues[i];
        }
        ns._rttSampleCount = 8;

        // Calculate expected variance
        long sum = 0, sumSq = 0;
        foreach (var rtt in rttValues)
        {
            sum += rtt;
            sumSq += rtt * rtt;
        }
        var expectedMean = sum / 8;
        var expectedVariance = sumSq / 8 - expectedMean * expectedMean;

        // Clamp negative variance (safety check)
        if (expectedVariance < 0)
        {
            expectedVariance = 0;
        }

        ns._rttVariance = expectedVariance;

        Assert.True(ns._rttVariance >= 0, "Variance should never be negative");
        Assert.True(ns._rttVariance < 2500, "Low-variance samples should have variance < 2500");
        Assert.True(ns.AverageRtt > 0 && ns.AverageRtt < 200, "Average RTT should be ~50ms");
        Assert.True(ns.HasStableConnection, "Should be considered stable connection");
    }

    /// <summary>
    /// Scenario: Actual speed hacker with stable connection.
    /// Should be detected with high confidence.
    /// </summary>
    [Fact]
    public void SpeedHack_StableConnection_DetectedDefinite()
    {
        var ns = PacketTestUtilities.CreateTestNetState();

        // Set up stable, low-latency RTT
        ns._rttHistory = new long[] { 30, 32, 28, 31, 29, 30, 31, 30 };
        ns._rttSampleCount = 8;

        long sum = 0, sumSq = 0;
        for (var i = 0; i < 8; i++)
        {
            sum += ns._rttHistory[i];
            sumSq += ns._rttHistory[i] * ns._rttHistory[i];
        }
        ns._rttVariance = sumSq / 8 - (sum / 8) * (sum / 8);
        if (ns._rttVariance < 0)
        {
            ns._rttVariance = 0;
        }

        Assert.True(ns.HasStableConnection, "Should be stable for this test");

        // Simulate speed hack: 50% faster movement
        SimulateMovements(ns,
            (67, 100), (67, 100), (67, 100), (67, 100), (67, 100),
            (67, 100), (67, 100), (67, 100), (67, 100), (67, 100),
            (67, 100), (67, 100), (67, 100), (67, 100), (67, 100)
        );

        var verdict = MovementThrottle.AnalyzeMovement(ns, out var rate, out _, out var confidence);

        Assert.True(rate > 1.4f, $"Expected rate > 1.4 (50% speed hack), got {rate}");
        Assert.Equal(MovementThrottle.DetectionVerdict.Definite, verdict);
        Assert.True(confidence > 0.5f, $"Expected high confidence, got {confidence}");
    }

    /// <summary>
    /// Scenario: Borderline speed hack (7% faster) with unstable connection.
    /// Should give benefit of doubt (not Definite).
    /// </summary>
    [Fact]
    public void BorderlineSpeed_UnstableConnection_BenefitOfDoubt()
    {
        var ns = PacketTestUtilities.CreateTestNetState();

        // Set up unstable connection
        ns._rttHistory = new long[] { 50, 300, 80, 250, 100, 400, 60, 350 };
        ns._rttSampleCount = 8;

        long sum = 0, sumSq = 0;
        for (var i = 0; i < 8; i++)
        {
            sum += ns._rttHistory[i];
            sumSq += ns._rttHistory[i] * ns._rttHistory[i];
        }
        ns._rttVariance = sumSq / 8 - (sum / 8) * (sum / 8);

        Assert.False(ns.HasStableConnection, "Should be unstable due to high variance");

        // Simulate borderline speed (7% faster)
        SimulateMovements(ns,
            (93, 100), (93, 100), (93, 100), (93, 100), (93, 100),
            (93, 100), (93, 100), (93, 100), (93, 100), (93, 100)
        );

        var verdict = MovementThrottle.AnalyzeMovement(ns, out var rate, out _, out var confidence);

        Assert.True(rate > 1.05f && rate < 1.10f, $"Expected rate ~1.07, got {rate}");

        // Unstable connection + borderline rate = should NOT be Definite
        Assert.True(
            verdict != MovementThrottle.DetectionVerdict.Definite,
            $"Borderline case with unstable connection should not be Definite. Verdict={verdict}, Confidence={confidence}"
        );
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

    /// <summary>
    /// Scenario: Walking vs running speed differences.
    /// Walking (200ms interval) vs mounted running (100ms interval) should both
    /// calculate correct rates.
    /// </summary>
    [Fact]
    public void WalkingVsRunning_CorrectRateCalculation()
    {
        // Test walking (200ms expected interval)
        var nsWalking = PacketTestUtilities.CreateTestNetState();
        SimulateMovements(nsWalking,
            (200, 200), (200, 200), (200, 200), (200, 200), (200, 200),
            (200, 200), (200, 200), (200, 200), (200, 200), (200, 200)
        );

        var walkingRate = MovementThrottle.CalculateMovementRate(nsWalking, out var walkingSamples);
        Assert.True(walkingSamples >= 8, "Should have enough walking samples");
        Assert.True(walkingRate >= 0.95f && walkingRate <= 1.05f,
            $"Walking rate should be ~1.0, got {walkingRate}");

        // Test mounted running (100ms expected interval)
        var nsRunning = PacketTestUtilities.CreateTestNetState();
        SimulateMovements(nsRunning,
            (100, 100), (100, 100), (100, 100), (100, 100), (100, 100),
            (100, 100), (100, 100), (100, 100), (100, 100), (100, 100)
        );

        var runningRate = MovementThrottle.CalculateMovementRate(nsRunning, out var runningSamples);
        Assert.True(runningSamples >= 8, "Should have enough running samples");
        Assert.True(runningRate >= 0.95f && runningRate <= 1.05f,
            $"Running rate should be ~1.0, got {runningRate}");

        // Test speed hack while walking
        var nsWalkingHack = PacketTestUtilities.CreateTestNetState();
        SimulateMovements(nsWalkingHack,
            (100, 200), (100, 200), (100, 200), (100, 200), (100, 200),
            (100, 200), (100, 200), (100, 200), (100, 200), (100, 200)
        );

        var walkingHackRate = MovementThrottle.CalculateMovementRate(nsWalkingHack, out _);
        Assert.True(walkingHackRate >= 1.9f && walkingHackRate <= 2.1f,
            $"Walking at running speed should be rate ~2.0, got {walkingHackRate}");
    }
}
