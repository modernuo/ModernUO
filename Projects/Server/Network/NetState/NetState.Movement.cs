/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: NetState.Movement.cs                                            *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Server.Logging;

namespace Server.Network;

public partial class NetState
{
    private static readonly ILogger movementLogger = LogFactory.GetLogger(typeof(NetState));

    // Per-connection movement logging (RTT instrumentation, etc.)
    // Can be enabled at runtime for specific connections
    internal bool _movementLogging;

    /// <summary>
    /// Gets or sets whether movement/RTT logging is enabled for this connection.
    /// When enabled, logs detailed RTT probe and response timing.
    /// </summary>
    public bool MovementLogging
    {
        get => _movementLogging;
        set => _movementLogging = value;
    }

    internal struct QueuedMovement
    {
        public Direction Direction;
        public int Sequence;
    }

    // Movement history record for rate calculation (8 bytes, cache-aligned)
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct MovementRecord
    {
        public short Interval;      // Time since previous packet (ms), capped at 32767
        public ushort TargetSpeed;  // Expected interval (100ms mounted running, etc.)
        public byte QueueDepth;     // Queue size when received
        public byte Flags;          // MovementRecordFlags
        public short Reserved;      // Padding to 8 bytes for cache alignment
    }

    [Flags]
    internal enum MovementRecordFlags : byte
    {
        None = 0,
        Running = 1,
        Mounted = 2,
        DirectionChangeOnly = 4,  // Cost was 0 (turn in place)
        WasQueued = 8             // Packet was queued, not executed immediately
    }

    // Movement queue state
    internal Queue<QueuedMovement> _movementQueue;          // Lazy initialized
    internal long _movementCredit;                          // Credit buffer for timing jitter
    internal long _nextMovementTime = Core.TickCount;       // When next movement is allowed
    internal int _sustainedQueueDepth;                      // Tracks sustained high queue depth
    internal long _lastQueueDepthCheck;                     // Throttle depth check frequency
    internal bool _hasQueuedMovements;                      // Fast check for Slice()

    // Movement history for rate-based speed hack detection (lazy initialized)
    internal MovementRecord[] _movementHistory;             // Circular buffer
    internal int _movementHistoryIndex;                     // Next write position (also serves as count until full)
    internal bool _movementHistoryFull;                     // True once buffer has wrapped
    internal long _lastMovementRecordTime;                  // For calculating intervals

    // Detection state
    internal int _consecutiveHighRateSeconds;               // Sustained detection counter
    internal long _lastSpeedHackNotification;               // Rate-limit notifications
    internal int _lastGapDuration;                          // Duration of last gap > maxChainGap (for burst forgiveness)

    // Movement packet rate tracking (for speed hack detection)
    internal long _movementWindowStart;                     // Start of current 1-second window
    internal int _movementsInWindow;                        // Count in current window
    internal int _peakMovementRate;                         // Highest rate seen (packets/sec)

    /// <summary>
    /// Resets movement state when sequence needs to be cleared (paralysis, teleport, map change, etc.)
    /// </summary>
    public void ResetMovementState()
    {
        _movementQueue?.Clear();
        Sequence = 0;
        _nextMovementTime = Core.TickCount;
        _movementCredit = 0;
        _hasQueuedMovements = false;
        _sustainedQueueDepth = 0;

        // Reset movement history - next movement starts a new chain
        _lastMovementRecordTime = 0;
        _movementHistoryIndex = 0;
        _movementHistoryFull = false;

        // Reset detection state - sustained detection loses context on teleport/map change
        _consecutiveHighRateSeconds = 0;
        _lastGapDuration = 0;
        _rttProbeInterval = RttProbeIntervalNormal;

        // Reset packet rate window
        _movementWindowStart = 0;
        _movementsInWindow = 0;
    }

    /// <summary>
    /// Tracks movement packet rate. Called for each movement packet received.
    /// Returns the current rate (packets per second in the last window).
    /// </summary>
    public int TrackMovementRate()
    {
        var now = Core.TickCount;

        // Check if we're in a new 1-second window
        if (now - _movementWindowStart >= 1000)
        {
            // Record peak rate if this window had movements
            if (_movementsInWindow > _peakMovementRate)
            {
                _peakMovementRate = _movementsInWindow;
            }

            // Start new window
            _movementWindowStart = now;
            _movementsInWindow = 1;
            return 1;
        }

        // Same window, increment count
        _movementsInWindow++;
        return _movementsInWindow;
    }

    /// <summary>
    /// Gets the current movement rate (packets in the current 1-second window).
    /// </summary>
    public int CurrentMovementRate => _movementsInWindow;

    /// <summary>
    /// Gets the peak movement rate observed for this session.
    /// </summary>
    public int PeakMovementRate => _peakMovementRate;

    // RTT Measurement Configuration
    private const int RttProbeIntervalNormal = 5000;      // Normal: probe every 5 seconds
    private const int RttProbeIntervalSuspicious = 2000;  // Suspicious: probe every 2 seconds
    private const int RttProbeIntervalDefinite = 1000;    // Definite cheater: probe every 1 second
    private const int RttProbeJitter = 500;               // Random jitter to prevent bursts
    private const int RttHistorySize = 8;                 // Keep 8 samples
    private const long StableVarianceThreshold = 2500;    // Variance < 50ms std dev = stable
    private const long MaxStableLatency = 200;            // Max RTT (ms) for "stable" connection

    // RTT state
    internal long _rttProbeTime;                            // When we sent the probe (0 = not waiting)
    internal long _lastRtt;                                 // Most recent RTT measurement
    internal long[] _rttHistory;                            // Rolling history (lazy init)
    internal int _rttHistoryIndex;                          // Current position in history
    internal int _rttSampleCount;                           // Number of samples collected (saturates at RttHistorySize)
    internal long _rttVariance;                             // Calculated variance for stability
    internal long _nextRttProbe;                            // When to send next probe
    internal int _rttProbeInterval = RttProbeIntervalNormal; // Current probe interval

    // High-resolution timestamp for RTT measurement (Stopwatch ticks, not game loop ticks)
    private long _rttProbeTimestampHiRes;

    /// <summary>
    /// Sets the RTT probe interval based on suspicion level.
    /// More suspicious = more frequent probes for better evidence.
    /// </summary>
    public void SetProbeFrequency(int suspicionLevel)
    {
        _rttProbeInterval = suspicionLevel switch
        {
            >= 3 => RttProbeIntervalDefinite,   // Definite cheater
            >= 2 => RttProbeIntervalSuspicious, // Likely cheater
            _ => RttProbeIntervalNormal         // Normal or Possible
        };
    }

    /// <summary>
    /// Sends an RTT probe if enough time has passed since the last one.
    /// Called from movement validation when player is actively moving.
    /// </summary>
    public void MaybeSendRttProbe()
    {
        // Only probe logged-in players
        if (Mobile?.Deleted != false)
        {
            return;
        }

        var now = Core.TickCount;

        // Don't send if we're still waiting for a response
        if (_rttProbeTime > 0)
        {
            // Timeout after 10 seconds - connection is probably dead or very laggy
            if (now - _rttProbeTime > 10000)
            {
                _rttProbeTime = 0;
                _rttProbeTimestampHiRes = 0;
            }
            return;
        }

        // First probe: send immediately when player starts moving
        // Subsequent probes: send when interval has passed
        if (_nextRttProbe == 0 || now >= _nextRttProbe)
        {
            _rttProbeTime = now;
            _rttProbeTimestampHiRes = Stopwatch.GetTimestamp();
            _nextRttProbe = now + _rttProbeInterval + Utility.Random(RttProbeJitter);

            if (_movementLogging)
            {
                movementLogger.Debug(
                    "[RTT-Probe] {Account}: Sending probe at TickCount={TickCount}",
                    Account?.Username ?? _toString, now
                );
            }

            this.SendClientVersionRequest();
        }
    }

    /// <summary>
    /// Records an RTT measurement when ClientVersion response is received.
    /// </summary>
    public void RecordRttMeasurement()
    {
        var nowHiRes = Stopwatch.GetTimestamp();
        var now = Core.TickCount;

        if (_rttProbeTime <= 0)
        {
            // Not expecting a response (client-initiated version send) - ignore silently
            return;
        }

        var rtt = now - _rttProbeTime;

        // High-resolution RTT in microseconds
        var rttHiResUs = (nowHiRes - _rttProbeTimestampHiRes) * 1_000_000 / Stopwatch.Frequency;

        if (_movementLogging)
        {
            movementLogger.Debug(
                "[RTT-Response] {Account}: {Rtt}ms (HiRes: {RttHiRes:F2}ms)",
                Account?.Username ?? _toString, rtt, rttHiResUs / 1000.0
            );
        }

        _rttProbeTime = 0;
        _rttProbeTimestampHiRes = 0;

        // Sanity check - RTT should be positive and reasonable
        if (rtt is <= 0 or > 10000)
        {
            if (_movementLogging)
            {
                movementLogger.Debug(
                    "[RTT-Response] {Account}: Invalid RTT {Rtt}ms, discarding",
                    Account?.Username ?? _toString, rtt
                );
            }
            return;
        }

        // Lazy init history
        _rttHistory ??= new long[RttHistorySize];

        // Update history
        _rttHistory[_rttHistoryIndex++ & (RttHistorySize - 1)] = rtt;
        _lastRtt = rtt;

        // Track sample count (saturates at buffer size)
        if (_rttSampleCount < RttHistorySize)
        {
            _rttSampleCount++;
        }

        // Recalculate variance
        UpdateRttVariance();

        if (_movementLogging)
        {
            movementLogger.Debug(
                "[RTT-Response] {Account}: Recorded RTT={Rtt}ms, Avg={Avg}ms, Var={Var}, Samples={Samples}, Stable={Stable}",
                Account?.Username ?? _toString, rtt, AverageRtt, _rttVariance, _rttSampleCount, HasStableConnection
            );
        }
    }

    /// <summary>
    /// Calculates the variance of RTT measurements for connection stability assessment.
    /// </summary>
    private void UpdateRttVariance()
    {
        if (_rttHistory == null)
        {
            _rttVariance = 0;
            return;
        }

        long sum = 0;
        long sumSq = 0;
        int count = 0;

        for (int i = 0; i < RttHistorySize; i++)
        {
            var sample = _rttHistory[i];
            if (sample > 0)
            {
                sum += sample;
                sumSq += sample * sample;
                count++;
            }
        }

        if (count < 2)
        {
            _rttVariance = 0;
            return;
        }

        var mean = sum / count;
        _rttVariance = sumSq / count - mean * mean;

        // Safety clamp: integer division rounding can produce negative variance
        if (_rttVariance < 0)
        {
            _rttVariance = 0;
        }
    }

    /// <summary>
    /// Gets the average RTT from recent measurements.
    /// </summary>
    public long AverageRtt
    {
        get
        {
            if (_rttHistory == null)
            {
                return 0;
            }

            long sum = 0;
            int count = 0;

            for (int i = 0; i < RttHistorySize; i++)
            {
                var sample = _rttHistory[i];
                if (sample > 0)
                {
                    sum += sample;
                    count++;
                }
            }

            return count > 0 ? sum / count : 0;
        }
    }

    /// <summary>
    /// Returns true if the connection has stable, low-variance, low-latency connection.
    /// Requires at least 3 samples to make a stability determination.
    /// Checks both variance (consistency) and absolute latency (quality).
    /// </summary>
    public bool HasStableConnection =>
        _rttSampleCount >= 3 &&
        _rttVariance < StableVarianceThreshold &&
        AverageRtt > 0 &&
        AverageRtt < MaxStableLatency;
}
