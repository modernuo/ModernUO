/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MovementThrottleTests.cs                                        *
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
/// Unit tests for MovementThrottle rate calculation and detection logic.
/// These tests are CI/CD safe - they don't depend on real timing.
/// </summary>
[Collection("Sequential Server Tests")]
public class MovementThrottleTests
{
    /// <summary>
    /// Creates a test NetState with movement history pre-populated.
    /// </summary>
    private static NetState CreateNetStateWithHistory(params (short interval, ushort targetSpeed)[] records)
    {
        var ns = PacketTestUtilities.CreateTestNetState();

        if (records.Length == 0)
        {
            return ns;
        }

        // Initialize history buffer
        ns._movementHistory = new NetState.MovementRecord[20];
        ns._movementHistoryIndex = 0;
        ns._movementHistoryFull = false;

        foreach (var (interval, targetSpeed) in records)
        {
            ns._movementHistory[ns._movementHistoryIndex] = new NetState.MovementRecord
            {
                Interval = interval,
                TargetSpeed = targetSpeed,
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

        return ns;
    }

    [Fact]
    public void CalculateMovementRate_NoHistory_ReturnsOne()
    {
        var ns = CreateNetStateWithHistory();

        var rate = MovementThrottle.CalculateMovementRate(ns, out var sampleCount);

        Assert.Equal(1.0f, rate);
        Assert.Equal(0, sampleCount);
    }

    [Fact]
    public void CalculateMovementRate_InsufficientSamples_ReturnsOne()
    {
        // Less than 8 samples (minSamplesForRate default)
        var ns = CreateNetStateWithHistory(
            (100, 100),
            (100, 100),
            (100, 100)
        );

        var rate = MovementThrottle.CalculateMovementRate(ns, out var sampleCount);

        Assert.Equal(1.0f, rate);
        // When insufficient samples, method returns early with sampleCount = 0
        Assert.Equal(0, sampleCount);
    }

    [Fact]
    public void CalculateMovementRate_NormalSpeed_ReturnsOne()
    {
        // 10 samples at exactly expected speed (100ms interval, 100ms target)
        var ns = CreateNetStateWithHistory(
            (100, 100), (100, 100), (100, 100), (100, 100), (100, 100),
            (100, 100), (100, 100), (100, 100), (100, 100), (100, 100)
        );

        var rate = MovementThrottle.CalculateMovementRate(ns, out var sampleCount);

        Assert.Equal(1.0f, rate);
        Assert.Equal(10, sampleCount);
    }

    [Fact]
    public void CalculateMovementRate_SpeedHack_ReturnsHighRate()
    {
        // Moving at 50ms intervals when 100ms is expected = 2x speed
        var ns = CreateNetStateWithHistory(
            (50, 100), (50, 100), (50, 100), (50, 100), (50, 100),
            (50, 100), (50, 100), (50, 100), (50, 100), (50, 100)
        );

        var rate = MovementThrottle.CalculateMovementRate(ns, out var sampleCount);

        Assert.Equal(2.0f, rate);
        Assert.Equal(10, sampleCount);
    }

    [Fact]
    public void CalculateMovementRate_SlowMovement_ReturnsLowRate()
    {
        // Moving at 200ms intervals when 100ms is expected = 0.5x speed
        var ns = CreateNetStateWithHistory(
            (200, 100), (200, 100), (200, 100), (200, 100), (200, 100),
            (200, 100), (200, 100), (200, 100), (200, 100), (200, 100)
        );

        var rate = MovementThrottle.CalculateMovementRate(ns, out var sampleCount);

        Assert.Equal(0.5f, rate);
        Assert.Equal(10, sampleCount);
    }

    [Fact]
    public void CalculateMovementRate_MixedSpeeds_ReturnsAverageRate()
    {
        // Mix of mounted running (100ms target) at varying speeds
        // Total target: 1000ms, Total actual: 900ms = 1.11 rate
        var ns = CreateNetStateWithHistory(
            (80, 100), (100, 100), (90, 100), (100, 100), (80, 100),
            (90, 100), (100, 100), (80, 100), (90, 100), (90, 100)
        );

        var rate = MovementThrottle.CalculateMovementRate(ns, out var sampleCount);

        // 1000 / 900 = 1.111...
        Assert.True(rate > 1.10f && rate < 1.12f, $"Expected rate ~1.11, got {rate}");
        Assert.Equal(10, sampleCount);
    }

    [Fact]
    public void DetectRecentBurst_NoBurst_ReturnsZero()
    {
        // Normal intervals, no burst
        var ns = CreateNetStateWithHistory(
            (100, 100), (100, 100), (100, 100), (100, 100), (100, 100),
            (100, 100), (100, 100), (100, 100), (100, 100), (100, 100)
        );

        var (burstSize, precedingGap) = MovementThrottle.DetectRecentBurst(ns);

        Assert.Equal(0, burstSize);
    }

    [Fact]
    public void DetectRecentBurst_BurstDetected_ReturnsBurstSizeAndGap()
    {
        // Last 4 packets arrived in burst (< 15ms intervals), preceded by 500ms gap
        var ns = CreateNetStateWithHistory(
            (100, 100), (100, 100), (100, 100), (100, 100), (100, 100),
            (500, 100), (5, 100), (5, 100), (5, 100), (5, 100)
        );

        var (burstSize, precedingGap) = MovementThrottle.DetectRecentBurst(ns);

        Assert.Equal(4, burstSize);
        Assert.Equal(500, precedingGap);
    }

    [Fact]
    public void DetectRecentBurst_SmallBurst_DetectsCorrectly()
    {
        // 2 packets in burst
        var ns = CreateNetStateWithHistory(
            (100, 100), (100, 100), (100, 100), (100, 100), (100, 100),
            (100, 100), (100, 100), (100, 100), (200, 100), (10, 100)
        );

        var (burstSize, precedingGap) = MovementThrottle.DetectRecentBurst(ns);

        Assert.Equal(1, burstSize);
        Assert.Equal(200, precedingGap);
    }

    [Fact]
    public void AnalyzeMovement_NormalMovement_ReturnsNormal()
    {
        var ns = CreateNetStateWithHistory(
            (100, 100), (100, 100), (100, 100), (100, 100), (100, 100),
            (100, 100), (100, 100), (100, 100), (100, 100), (100, 100)
        );

        var verdict = MovementThrottle.AnalyzeMovement(ns, out var rate, out var sampleCount, out var confidence);

        Assert.Equal(MovementThrottle.DetectionVerdict.Normal, verdict);
        Assert.Equal(1.0f, rate);
        Assert.Equal(10, sampleCount);
    }

    [Fact]
    public void AnalyzeMovement_ClearSpeedHack_ReturnsDefinite()
    {
        // 30% faster than expected - clear speed hack
        var ns = CreateNetStateWithHistory(
            (77, 100), (77, 100), (77, 100), (77, 100), (77, 100),
            (77, 100), (77, 100), (77, 100), (77, 100), (77, 100)
        );

        var verdict = MovementThrottle.AnalyzeMovement(ns, out var rate, out var sampleCount, out var confidence);

        Assert.Equal(MovementThrottle.DetectionVerdict.Definite, verdict);
        Assert.True(rate > 1.25f, $"Expected rate > 1.25, got {rate}");
    }

    [Fact]
    public void AnalyzeMovement_ModerateSpeedHack_ReturnsPossibleOrLikely()
    {
        // 8% faster than expected - moderate, should be Possible or Likely
        var ns = CreateNetStateWithHistory(
            (93, 100), (93, 100), (93, 100), (93, 100), (93, 100),
            (93, 100), (93, 100), (93, 100), (93, 100), (93, 100)
        );

        // Set up stable, low-latency RTT (required for Likely verdict path)
        ns._rttHistory = [50, 52, 48, 51, 49, 50, 51, 50];
        ns._rttSampleCount = 8;
        ns._rttVariance = 4; // Low variance

        var verdict = MovementThrottle.AnalyzeMovement(ns, out var rate, out var sampleCount, out var confidence);

        Assert.True(
            verdict == MovementThrottle.DetectionVerdict.Possible ||
            verdict == MovementThrottle.DetectionVerdict.Likely,
            $"Expected Possible or Likely, got {verdict}. Rate={rate}, Confidence={confidence}"
        );
        Assert.True(rate > 1.05f && rate < 1.15f, $"Expected rate ~1.07, got {rate}");
    }

    [Fact]
    public void AnalyzeMovement_BurstAfterLargeGap_ReducesConfidence()
    {
        // Simulate lag recovery: large gap followed by burst of packets
        var ns = CreateNetStateWithHistory(
            (100, 100), (100, 100), (100, 100), (100, 100), (100, 100),
            (5, 100), (5, 100), (5, 100), (5, 100), (5, 100)
        );

        // Set up the gap duration (simulates what RecordMovement does)
        ns._lastGapDuration = 1500; // Gap > maxChainGap/2 (1000)

        var verdict = MovementThrottle.AnalyzeMovement(ns, out var rate, out var sampleCount, out var confidence);

        // Even though rate is high, confidence should be reduced due to burst forgiveness
        // The gap duration should trigger the forgiveness path
        Assert.True(confidence < 0.5f, $"Expected low confidence due to burst forgiveness, got {confidence}");

        // After analysis, gap duration should be reset
        Assert.Equal(0, ns._lastGapDuration);
    }

    [Fact]
    public void AnalyzeMovement_HighQueue_IncreasesConfidence()
    {
        var ns = CreateNetStateWithHistory(
            (95, 100), (95, 100), (95, 100), (95, 100), (95, 100),
            (95, 100), (95, 100), (95, 100), (95, 100), (95, 100)
        );

        // Simulate high queue depth (modified client indicator)
        ns._movementQueue = new System.Collections.Generic.Queue<NetState.QueuedMovement>();
        for (var i = 0; i < 5; i++)
        {
            ns._movementQueue.Enqueue(new NetState.QueuedMovement());
        }

        var verdict = MovementThrottle.AnalyzeMovement(ns, out var rate, out var sampleCount, out var confidence);

        // Queue > 4 indicates modified client - should be Definite
        Assert.Equal(MovementThrottle.DetectionVerdict.Definite, verdict);
    }

    [Fact]
    public void AnalyzeMovement_StableConnectionLowLatency_IncreasesConfidence()
    {
        var ns = CreateNetStateWithHistory(
            (95, 100), (95, 100), (95, 100), (95, 100), (95, 100),
            (95, 100), (95, 100), (95, 100), (95, 100), (95, 100)
        );

        // Set up stable, low-latency RTT
        ns._rttHistory = [50, 52, 48, 51, 49, 50, 51, 50];
        ns._rttSampleCount = 8;
        ns._rttVariance = 4; // Low variance

        var verdict = MovementThrottle.AnalyzeMovement(ns, out var rate, out var sampleCount, out var confidence);

        // With stable connection, even small anomalies are more suspicious
        Assert.True(confidence > 0.2f, $"Expected higher confidence for stable connection, got {confidence}");
    }

    [Fact]
    public void AnalyzeMovement_UnstableConnection_ReducesConfidence()
    {
        var ns = CreateNetStateWithHistory(
            (90, 100), (90, 100), (90, 100), (90, 100), (90, 100),
            (90, 100), (90, 100), (90, 100), (90, 100), (90, 100)
        );

        // Set up unstable connection (high variance)
        ns._rttHistory = [50, 200, 80, 350, 100, 250, 90, 300];
        ns._rttSampleCount = 8;
        ns._rttVariance = 15000; // High variance

        var verdictUnstable = MovementThrottle.AnalyzeMovement(ns, out _, out _, out var confidenceUnstable);

        // Reset and test with stable connection for comparison
        var nsStable = CreateNetStateWithHistory(
            (90, 100), (90, 100), (90, 100), (90, 100), (90, 100),
            (90, 100), (90, 100), (90, 100), (90, 100), (90, 100)
        );
        nsStable._rttHistory = [50, 52, 48, 51, 49, 50, 51, 50];
        nsStable._rttSampleCount = 8;
        nsStable._rttVariance = 4;

        var verdictStable = MovementThrottle.AnalyzeMovement(nsStable, out _, out _, out var confidenceStable);

        // Unstable connection should have lower confidence
        Assert.True(
            confidenceUnstable < confidenceStable,
            $"Expected unstable ({confidenceUnstable}) < stable ({confidenceStable})"
        );
    }

    [Fact]
    public void HasStableConnection_LowLatencyLowVariance_ReturnsTrue()
    {
        var ns = PacketTestUtilities.CreateTestNetState();

        // Stable: low variance, enough samples, low latency
        ns._rttHistory = [50, 52, 48, 51, 49, 50, 51, 50];
        ns._rttSampleCount = 8;
        ns._rttVariance = 4; // Low variance < 2500

        Assert.True(ns.HasStableConnection);
    }

    [Fact]
    public void HasStableConnection_HighLatency_ReturnsFalse()
    {
        var ns = PacketTestUtilities.CreateTestNetState();

        // High but consistent latency (500ms) - should NOT be considered stable
        ns._rttHistory = [500, 502, 498, 501, 499, 500, 501, 500];
        ns._rttSampleCount = 8;
        ns._rttVariance = 4; // Low variance, but latency > 200ms

        Assert.False(ns.HasStableConnection, "High latency connection should not be considered stable");
    }

    [Fact]
    public void HasStableConnection_HighVariance_ReturnsFalse()
    {
        var ns = PacketTestUtilities.CreateTestNetState();

        // Low average latency but high variance
        ns._rttHistory = [50, 200, 30, 180, 40, 150, 60, 170];
        ns._rttSampleCount = 8;
        ns._rttVariance = 5000; // High variance > 2500

        Assert.False(ns.HasStableConnection);
    }

    [Fact]
    public void HasStableConnection_InsufficientSamples_ReturnsFalse()
    {
        var ns = PacketTestUtilities.CreateTestNetState();

        // Only 2 samples
        ns._rttHistory = [50, 50, 0, 0, 0, 0, 0, 0];
        ns._rttSampleCount = 2;
        ns._rttVariance = 0;

        Assert.False(ns.HasStableConnection, "Needs at least 3 samples");
    }
}
