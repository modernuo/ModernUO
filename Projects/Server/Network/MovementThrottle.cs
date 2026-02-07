/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MovementThrottle.cs                                             *
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
using System.Runtime.CompilerServices;
using Server.Logging;

namespace Server.Network;

/// <summary>
/// Movement throttle system using a hybrid credit + queue approach.
/// Credit buffer absorbs small timing jitter from legitimate players.
/// Queue handles larger bursts, draining at proper intervals.
/// Speed hacks detected by movement rate analysis and sustained queue depth.
/// </summary>
public static class MovementThrottle
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(MovementThrottle));

    // Configuration values
    private static int _maxCredit = 200;           // Max credit buffer (ms)
    private static int _maxRttBonus = 150;         // Max extra credit for high-latency players (ms)
    private static int _softQueueLimit = 6;        // Start logging when reached
    private static int _hardQueueLimit = 10;       // Reject and clear at this limit

    // Movement history configuration
    private static int _movementHistorySize = 20;          // Circular buffer size
    private static int _minSamplesForRate = 8;             // Minimum movements to calculate rate
    private static int _maxChainGap = 2000;                // Gap (ms) that breaks a movement chain
    private static float _suspiciousRateThreshold = 1.05f; // 5% faster than expected
    private static float _definiteRateThreshold = 1.10f;   // 10% faster than expected
    private static int _speedHackNotificationCooldown = 300000; // 5 minutes between notifications per player

    // Client movement limits (UO protocol constants)
    // The client can have at most 5 unacknowledged movements pending.
    // This means our server-side queue should never exceed 4 with an unmodified client.
    // Queue > 4 indicates client modification (not just speed hack checkbox in Cheat Engine).
    private const int ClientMaxUnackedMovements = 5;
    private const int MaxQueueWithUnmodifiedClient = ClientMaxUnackedMovements - 1;  // 4

    // Debug logging - enable for testing speed hack detection
    private static bool _debugLogging = true;

    // Track NetStates with queued movements for efficient processing
    private static readonly HashSet<NetState> _netStatesWithQueuedMovements = new(256);

    /// <summary>
    /// Gets the dynamic credit buffer for a connection based on measured RTT.
    /// High-latency players get more tolerance; stable low-latency gets tighter security.
    /// </summary>
    private static int GetDynamicCredit(NetState ns)
    {
        var avgRtt = ns.AverageRtt;

        // No RTT data yet - use default
        if (avgRtt <= 0)
        {
            return _maxCredit;
        }

        // Stable, low-latency connection - use base credit (tighter security)
        if (ns.HasStableConnection && avgRtt < 50)
        {
            return _maxCredit;
        }

        // Add RTT-based bonus for higher latency, capped at max bonus
        var rttBonus = Math.Min(avgRtt / 2, _maxRttBonus);
        return _maxCredit + (int)rttBonus;
    }

    public static void Configure()
    {
        _maxCredit = ServerConfiguration.GetOrUpdateSetting(
            "movementThrottle.maxCredit",
            _maxCredit
        );

        _softQueueLimit = ServerConfiguration.GetOrUpdateSetting(
            "movementThrottle.softQueueLimit",
            _softQueueLimit
        );

        _hardQueueLimit = ServerConfiguration.GetOrUpdateSetting(
            "movementThrottle.hardQueueLimit",
            _hardQueueLimit
        );

        _movementHistorySize = ServerConfiguration.GetOrUpdateSetting(
            "movementThrottle.movementHistorySize",
            _movementHistorySize
        );

        _minSamplesForRate = ServerConfiguration.GetOrUpdateSetting(
            "movementThrottle.minSamplesForRate",
            _minSamplesForRate
        );

        _suspiciousRateThreshold = (float)ServerConfiguration.GetOrUpdateSetting(
            "movementThrottle.suspiciousRateThreshold",
            _suspiciousRateThreshold
        );

        _definiteRateThreshold = (float)ServerConfiguration.GetOrUpdateSetting(
            "movementThrottle.definiteRateThreshold",
            _definiteRateThreshold
        );

        _debugLogging = ServerConfiguration.GetOrUpdateSetting(
            "movementThrottle.debugLogging",
            _debugLogging
        );
    }

    /// <summary>
    /// Called from MovementReq packet handler. Validates timing and either
    /// executes the movement immediately or queues it for later execution.
    /// </summary>
    public static void ValidateAndQueueMovement(NetState ns, Mobile mobile, Direction dir, int seq)
    {
        if (mobile?.Deleted != false)
        {
            return;
        }

        // Staff bypass all throttling
        if (mobile.AccessLevel > AccessLevel.Player)
        {
            ExecuteMovement(ns, mobile, dir, seq);
            return;
        }

        // Check for sequence mismatch (sequence was reset by paralysis, teleport, etc.)
        if (ns.Sequence == 0 && seq != 0)
        {
            RejectAndReset(ns, mobile, seq);
            return;
        }

        var now = Core.TickCount;

        // Track movement packet rate (packets per second)
        ns.TrackMovementRate();

        // RTT probe - only when actively moving (event-driven, not global loop)
        ns.MaybeSendRttProbe();

        // Calculate movement cost - this has FULL CONTEXT (mounted, running, direction change)
        var cost = mobile.ComputeMovementSpeed(dir);

        // Record movement in history for rate analysis (do this early, before any returns)
        RecordMovement(ns, now, cost, dir, mobile);

        // Periodic rate-based detection (every 2 seconds, regardless of queue depth)
        if (now - ns._lastQueueDepthCheck >= 2000)
        {
            ns._lastQueueDepthCheck = now;
            var verdict = CheckAndNotifyStaff(ns);

            // Adjust probe frequency based on detection verdict
            ns.SetProbeFrequency((int)verdict);
        }

        // Calculate timing delta: positive = on-time/late, negative = early
        var delta = now - ns._nextMovementTime;

        // If there are already queued movements, add to queue to maintain order
        if (ns._hasQueuedMovements)
        {
            QueueMovement(ns, dir, seq);
            return;
        }

        // Get dynamic credit limit based on connection latency
        var dynamicCredit = GetDynamicCredit(ns);

        // Handle early packets with credit buffer
        if (delta < 0)
        {
            // Packet arrived early
            var earlyAmount = -delta;

            // Can we absorb this with credit?
            // Credit can go negative up to -dynamicCredit (debt limit)
            if (ns._movementCredit - earlyAmount >= -dynamicCredit)
            {
                // Use credit to cover early arrival
                ns._movementCredit -= earlyAmount;
                // Execute immediately since credit covers it
                ExecuteMovement(ns, mobile, dir, seq);
                return;
            }

            // Credit exhausted - must queue
            QueueMovement(ns, dir, seq);
            return;
        }

        // On-time or late - rebuild credit (capped at dynamic max)
        if (delta > 0)
        {
            ns._movementCredit = Math.Min(ns._movementCredit + delta, dynamicCredit);
        }

        // Execute immediately
        ExecuteMovement(ns, mobile, dir, seq);
    }

    /// <summary>
    /// Executes a single movement and updates state.
    /// </summary>
    private static void ExecuteMovement(NetState ns, Mobile mobile, Direction dir, int seq)
    {
        if (!mobile.Move(dir))
        {
            // Movement failed (blocked, paralyzed, frozen, etc.)
            RejectAndReset(ns, mobile, seq);
            return;
        }

        // Success - Mobile.Move() already updated _nextMovementTime and sent ack
        // Update sequence
        var newSeq = seq + 1;
        if (newSeq == 256)
        {
            newSeq = 1;
        }
        ns.Sequence = newSeq;
    }

    /// <summary>
    /// Queues a movement for later execution.
    /// </summary>
    private static void QueueMovement(NetState ns, Direction dir, int seq)
    {
        // Lazy initialize queue
        ns._movementQueue ??= new Queue<NetState.QueuedMovement>(_hardQueueLimit);

        // Check hard limit
        if (ns._movementQueue.Count >= _hardQueueLimit)
        {
            LogQueueOverflow(ns);
            RejectAndReset(ns, ns.Mobile, seq);
            return;
        }

        // Check soft limit for abuse tracking
        if (ns._movementQueue.Count >= _softQueueLimit)
        {
            TrackSustainedQueueDepth(ns);
        }

        // Enqueue
        ns._movementQueue.Enqueue(new NetState.QueuedMovement
        {
            Direction = dir,
            Sequence = seq
        });

        ns._hasQueuedMovements = true;
        _netStatesWithQueuedMovements.Add(ns);
    }

    /// <summary>
    /// Rejects a movement and resets movement state.
    /// </summary>
    private static void RejectAndReset(NetState ns, Mobile mobile, int seq)
    {
        ns.SendMovementRej(seq, mobile);
        ns.ResetMovementState();
        _netStatesWithQueuedMovements.Remove(ns);
    }

    /// <summary>
    /// Processes queued movements for all NetStates. Called from NetState.Slice().
    /// </summary>
    public static void ProcessAllQueues()
    {
        if (_netStatesWithQueuedMovements.Count == 0)
        {
            return;
        }

        // Process each NetState with queued movements
        // Use a snapshot to avoid modification during iteration
        var toProcess = new List<NetState>(_netStatesWithQueuedMovements);

        for (var i = 0; i < toProcess.Count; i++)
        {
            var ns = toProcess[i];
            if (!ns.Running)
            {
                _netStatesWithQueuedMovements.Remove(ns);
                continue;
            }

            ProcessMovementQueue(ns);
        }
    }

    /// <summary>
    /// Processes the movement queue for a single NetState.
    /// </summary>
    public static void ProcessMovementQueue(NetState ns)
    {
        var mobile = ns.Mobile;
        if (mobile?.Deleted != false)
        {
            ClearQueue(ns);
            return;
        }

        // Staff don't queue
        if (mobile.AccessLevel > AccessLevel.Player)
        {
            DrainQueueImmediately(ns, mobile);
            return;
        }

        var now = Core.TickCount;

        while (ns._movementQueue?.Count > 0)
        {
            // Check if it's time to execute
            if (now < ns._nextMovementTime)
            {
                // Not yet - leave remaining items in queue for next Slice
                break;
            }

            var movement = ns._movementQueue.Dequeue();

            // Validate sequence
            if (ns.Sequence == 0 && movement.Sequence != 0)
            {
                // Sequence was reset (paralysis, teleport, etc.)
                RejectAndReset(ns, mobile, movement.Sequence);
                return;
            }

            // Execute the move
            if (!mobile.Move(movement.Direction))
            {
                // Movement failed
                RejectAndReset(ns, mobile, movement.Sequence);
                return;
            }

            // Success - update sequence
            var newSeq = movement.Sequence + 1;
            if (newSeq == 256)
            {
                newSeq = 1;
            }
            ns.Sequence = newSeq;

            // Refresh time for next iteration
            now = Core.TickCount;
        }

        // Update tracking
        ns._hasQueuedMovements = ns._movementQueue?.Count > 0;
        if (!ns._hasQueuedMovements)
        {
            _netStatesWithQueuedMovements.Remove(ns);
        }
    }

    /// <summary>
    /// Drains the entire queue immediately for staff members.
    /// </summary>
    private static void DrainQueueImmediately(NetState ns, Mobile mobile)
    {
        while (ns._movementQueue?.Count > 0)
        {
            var movement = ns._movementQueue.Dequeue();

            if (!mobile.Move(movement.Direction))
            {
                RejectAndReset(ns, mobile, movement.Sequence);
                return;
            }

            var newSeq = movement.Sequence + 1;
            if (newSeq == 256)
            {
                newSeq = 1;
            }
            ns.Sequence = newSeq;
        }

        ClearQueue(ns);
    }

    /// <summary>
    /// Clears the movement queue for a NetState.
    /// </summary>
    private static void ClearQueue(NetState ns)
    {
        ns._movementQueue?.Clear();
        ns._hasQueuedMovements = false;
        _netStatesWithQueuedMovements.Remove(ns);
    }

    // Maximum expected packets per second (mounted running = 100ms = 10/sec, plus tolerance)
    private const int MaxExpectedPacketRate = 12;

    /// <summary>
    /// Tracks sustained queue depth for speed hack detection.
    /// Now a no-op since rate checking happens inline in ValidateAndQueueMovement.
    /// Kept for future use if additional queue-specific checks are needed.
    /// </summary>
    private static void TrackSustainedQueueDepth(NetState ns)
    {
        // Rate-based detection now happens inline in ValidateAndQueueMovement
        // This method is called when queue >= soft limit but detection no longer depends on that
    }

    /// <summary>
    /// Logs when a queue overflow occurs (hard limit reached).
    /// </summary>
    private static void LogQueueOverflow(NetState ns)
    {
        var mobile = ns.Mobile;
        logger.Information(
            "Movement queue overflow: {Character} ({Account}) | " +
            "Queue reached hard limit: {Limit} | IP: {IP}",
            mobile?.RawName ?? "Unknown",
            ns.Account?.Username ?? "Unknown",
            _hardQueueLimit,
            ns.Address
        );
    }

    /// <summary>
    /// Resets movement timing for all connected players after a world save.
    /// This prevents post-save burst rejections.
    /// </summary>
    public static void ResetAllMovementTiming()
    {
        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile?.Deleted == false)
            {
                ns._nextMovementTime = Core.TickCount;
                ns._movementCredit = _maxCredit; // Give full credit after save
            }
        }
    }

    /// <summary>
    /// Records a movement in the history buffer for rate analysis.
    /// This is a hot path - optimized for minimal allocations and branches.
    /// Skips recording for chain-breaking movements (first in sequence, long gaps).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RecordMovement(NetState ns, long now, int cost, Direction dir, Mobile mobile)
    {
        // Calculate interval since last movement
        var interval = ns._lastMovementRecordTime > 0
            ? (int)(now - ns._lastMovementRecordTime)
            : -1; // -1 indicates first movement (no previous time)

        // Skip recording for chain-breaking movements - they waste buffer space
        // and get skipped during analysis anyway. Still update timestamp for next movement.
        if (interval <= 0 || interval > _maxChainGap)
        {
            ns._lastMovementRecordTime = now;

            // Use RTT to distinguish "stopped moving" vs "lagged"
            // - Stable low-latency connection with gap >> RTT → player stopped, reset history
            // - Unstable/high-latency connection with gap → might be lag, preserve history
            if (interval > _maxChainGap)
            {
                var avgRtt = ns.AverageRtt;
                var shouldReset = ns.HasStableConnection && avgRtt > 0 && avgRtt < 200 && avgRtt < interval / 4;

                if (shouldReset)
                {
                    ns._movementHistoryIndex = 0;
                    ns._movementHistoryFull = false;
                }

                // Track gap duration for burst forgiveness logic
                // A large gap followed by a burst of packets = likely lag recovery, not speed hack
                ns._lastGapDuration = interval;

                if (_debugLogging && mobile?.RawName != null)
                {
                    var action = shouldReset ? "history reset" : "history preserved (possible lag)";
                    logger.Debug(
                        "[Movement] {Name}: SKIP recording (gap {Gap}ms > {MaxGap}ms, " +
                        "RTT={RTT}ms stable={Stable} → {Action})",
                        mobile.RawName, interval, _maxChainGap, avgRtt, ns.HasStableConnection, action
                    );
                }
            }
            else if (_debugLogging && mobile?.RawName != null)
            {
                logger.Debug("[Movement] {Name}: SKIP recording (first in chain)", mobile.RawName);
            }

            return;
        }

        // Lazy initialize buffer
        ns._movementHistory ??= new NetState.MovementRecord[_movementHistorySize];

        // Build flags
        // Direction-only changes (cost=0) don't contribute to rate calculation.
        // Don't record them - they would pollute interval measurements.
        // Example bug if recorded: direction changes between real moves make
        // the next real move's interval artificially short, inflating rate.
        if (cost == 0)
        {
            if (_debugLogging && mobile?.RawName != null)
            {
                logger.Debug(
                    "[Movement] {Name}: SKIP direction-only change (preserves interval measurement)",
                    mobile.RawName
                );
            }
            return;
        }

        var flags = NetState.MovementRecordFlags.None;
        if ((dir & Direction.Running) != 0)
        {
            flags |= NetState.MovementRecordFlags.Running;
        }
        if (mobile.Mounted)
        {
            flags |= NetState.MovementRecordFlags.Mounted;
        }
        if (ns._hasQueuedMovements)
        {
            flags |= NetState.MovementRecordFlags.WasQueued;
        }

        // Write to circular buffer (struct assignment, no heap allocation)
        // interval is already validated: > 0 and <= _maxChainGap (2000ms < short.MaxValue)
        ref var record = ref ns._movementHistory[ns._movementHistoryIndex];
        record.Interval = (short)interval;
        record.TargetSpeed = (ushort)cost;
        record.QueueDepth = (byte)Math.Min(ns._movementQueue?.Count ?? 0, 255);
        record.Flags = (byte)flags;

        // Advance index
        ns._movementHistoryIndex++;
        if (ns._movementHistoryIndex >= _movementHistorySize)
        {
            ns._movementHistoryIndex = 0;
            ns._movementHistoryFull = true;
        }

        ns._lastMovementRecordTime = now;

        // Debug logging
        if (_debugLogging && mobile?.RawName != null)
        {
            var historyCount = ns._movementHistoryFull ? _movementHistorySize : ns._movementHistoryIndex;
            logger.Debug(
                "[Movement] {Name}: interval={Interval}ms target={Target}ms queue={Queue} " +
                "flags={Flags} history={History}/{MaxHistory} RTT={RTT}ms",
                mobile.RawName, interval, cost, record.QueueDepth,
                flags, historyCount, _movementHistorySize, ns.AverageRtt
            );
        }
    }

    /// <summary>
    /// Calculates the movement rate ratio: targetTime / actualTime.
    /// Returns 1.0 for normal speed, >1.0 for faster than allowed.
    /// </summary>
    /// <param name="ns">The NetState to analyze</param>
    /// <param name="sampleCount">Output: number of samples used in calculation</param>
    /// <returns>Rate ratio (1.0 = normal, 1.1 = 10% faster)</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CalculateMovementRate(NetState ns, out int sampleCount)
    {
        sampleCount = 0;

        if (ns._movementHistory == null)
        {
            return 1.0f;
        }

        var historyCount = ns._movementHistoryFull ? _movementHistorySize : ns._movementHistoryIndex;
        if (historyCount < _minSamplesForRate)
        {
            return 1.0f;
        }

        long totalTarget = 0;
        long totalActual = 0;
        var count = 0;

        // Walk backwards through history from most recent
        // All entries are guaranteed valid (interval > 0 and <= _maxChainGap) due to
        // filtering in RecordMovement() and history reset on chain breaks.
        for (var i = 0; i < historyCount; i++)
        {
            // Calculate index going backwards from current position
            var idx = ns._movementHistoryIndex - 1 - i;
            if (idx < 0)
            {
                idx += _movementHistorySize;
            }

            ref readonly var record = ref ns._movementHistory[idx];

            // Skip direction-only changes (cost = 0) - they don't contribute to movement rate
            if ((record.Flags & (byte)NetState.MovementRecordFlags.DirectionChangeOnly) != 0)
            {
                continue;
            }

            totalTarget += record.TargetSpeed;
            totalActual += record.Interval;
            count++;
        }

        sampleCount = count;

        if (count < _minSamplesForRate || totalActual <= 0)
        {
            return 1.0f;
        }

        return (float)totalTarget / totalActual;
    }

    /// <summary>
    /// Dumps movement history for debugging rate calculation.
    /// </summary>
    private static void DumpMovementHistory(NetState ns, int limit)
    {
        if (ns._movementHistory == null)
        {
            logger.Debug("  [History] null");
            return;
        }

        var historyCount = ns._movementHistoryFull ? _movementHistorySize : ns._movementHistoryIndex;
        var showCount = Math.Min(limit, historyCount);
        long totalTarget = 0;
        long totalActual = 0;

        logger.Debug("  [History] Showing {ShowCount} of {HistoryCount} entries:", showCount, historyCount);

        for (var i = 0; i < showCount; i++)
        {
            var idx = ns._movementHistoryIndex - 1 - i;
            if (idx < 0)
            {
                idx += _movementHistorySize;
            }

            ref readonly var record = ref ns._movementHistory[idx];
            var flags = (NetState.MovementRecordFlags)record.Flags;

            totalTarget += record.TargetSpeed;
            totalActual += record.Interval;

            var cumRate = totalActual > 0 ? (float)totalTarget / totalActual : 0;
            logger.Debug(
                "    [{Index}] interval={Interval}ms target={Target}ms queue={Queue} flags={Flags} cumRate={CumRate:F3}",
                i, record.Interval, record.TargetSpeed, record.QueueDepth, flags, cumRate
            );
        }
    }

    /// <summary>
    /// Detects if recent movements arrived as a burst (multiple packets with near-zero intervals).
    /// </summary>
    /// <param name="ns">The NetState to analyze</param>
    /// <returns>Tuple of (burst size, preceding gap in ms)</returns>
    public static (int burstSize, int precedingGap) DetectRecentBurst(NetState ns)
    {
        if (ns._movementHistory == null)
        {
            return (0, 0);
        }

        var historyCount = ns._movementHistoryFull ? _movementHistorySize : ns._movementHistoryIndex;
        if (historyCount < 2)
        {
            return (0, 0);
        }

        const int burstThreshold = 15; // Packets within 15ms are "simultaneous"
        var burstSize = 0;
        var precedingGap = 0;

        for (var i = 0; i < historyCount; i++)
        {
            var idx = ns._movementHistoryIndex - 1 - i;
            if (idx < 0)
            {
                idx += _movementHistorySize;
            }

            var interval = ns._movementHistory[idx].Interval;

            // All intervals are guaranteed > 0 due to filtering in RecordMovement()
            if (interval <= burstThreshold)
            {
                burstSize++;
            }
            else
            {
                precedingGap = interval;
                break;
            }
        }

        return (burstSize, precedingGap);
    }

    /// <summary>
    /// Detection verdict levels for speed hack analysis.
    /// </summary>
    public enum DetectionVerdict
    {
        Normal,    // Rate within tolerance
        Possible,  // Slight anomaly, could be network
        Likely,    // Multiple signals point to cheating
        Definite   // Clear speed hacking
    }

    /// <summary>
    /// Analyzes movement patterns and calculates detection confidence.
    /// </summary>
    /// <param name="ns">The NetState to analyze</param>
    /// <param name="rate">Output: calculated movement rate</param>
    /// <param name="sampleCount">Output: number of samples used</param>
    /// <param name="confidence">Output: confidence score 0.0-1.0</param>
    /// <returns>Detection verdict</returns>
    public static DetectionVerdict AnalyzeMovement(
        NetState ns,
        out float rate,
        out int sampleCount,
        out float confidence)
    {
        rate = CalculateMovementRate(ns, out sampleCount);
        confidence = 0f;

        // Not enough data
        if (sampleCount < _minSamplesForRate)
        {
            return DetectionVerdict.Normal;
        }

        var averageRtt = ns.AverageRtt;

        // Detailed rate breakdown for debugging
        if (_debugLogging)
        {
            logger.Debug("[MovementAnalysis] Rate={Rate:F3}, Samples={Samples}, RTT={RTT}ms",
                rate, sampleCount, averageRtt);
            DumpMovementHistory(ns, sampleCount);
        }

        // Signal 1: Movement rate (primary signal)
        if (rate > 1.15f)
        {
            confidence += 0.5f;
        }
        else if (rate > _definiteRateThreshold)
        {
            confidence += 0.35f;
        }
        else if (rate > _suspiciousRateThreshold)
        {
            confidence += 0.2f;
        }
        else if (rate > 1.02f)
        {
            confidence += 0.1f;
        }

        // Signal 2: Packet rate (secondary signal)
        var packetRate = ns.CurrentMovementRate;
        if (packetRate > 15)
        {
            confidence += 0.15f;
        }
        else if (packetRate > MaxExpectedPacketRate)
        {
            confidence += 0.1f;
        }

        // Signal 2b: Queue depth (critical for ACK-throttled speed hacks)
        // The UO client limits unacked movements to 5, so our queue should max at 4.
        // When going straight at high speed, ACK flow limits packet rate, but queue stays high.
        // Queue > 4 indicates a modified client (not just Cheat Engine speed hack).
        var queueDepth = ns._movementQueue?.Count ?? 0;
        if (queueDepth > MaxQueueWithUnmodifiedClient)
        {
            confidence += 0.5f;  // Modified client - extremely suspicious
        }
        else if (queueDepth >= MaxQueueWithUnmodifiedClient)
        {
            confidence += 0.25f;  // At client limit - speed hack with unmodified client
        }
        else if (queueDepth >= 2)
        {
            confidence += 0.1f;
        }

        // Signal 3: RTT correlation (modifier)
        if (ns.HasStableConnection && averageRtt < 100)
        {
            // Stable, low-latency connection - problems are more suspicious
            if (rate > 1.02f)
            {
                confidence += 0.2f;
            }
        }
        else if (ns._rttVariance > 10000 || averageRtt > 300)
        {
            // Unstable connection - reduce confidence
            confidence *= 0.6f;
        }

        // Signal 4: Burst pattern analysis
        var (burstSize, _) = DetectRecentBurst(ns);

        // Forgiveness for lag recovery: burst preceded by large gap
        // A legitimate lag spike will have a large gap followed by burst of packets arriving at once
        // These packets will have HIGH rate (tiny intervals) but it's not cheating
        if (burstSize >= 3 && ns._lastGapDuration > _maxChainGap / 2)
        {
            // Recent large gap + burst = lag recovery, strong forgiveness
            confidence *= 0.3f;
            ns._lastGapDuration = 0; // Reset after applying forgiveness
        }
        else if (burstSize >= 3 && rate < _suspiciousRateThreshold)
        {
            // Burst pattern with normal rate = likely legitimate lag
            confidence *= 0.5f;
        }

        // Signal 5: Sample count modifier
        if (sampleCount < 10)
        {
            confidence *= 0.7f;
        }
        else if (sampleCount >= 20)
        {
            confidence = Math.Min(confidence * 1.1f, 1.0f);
        }

        // Determine verdict
        if (rate > _definiteRateThreshold && confidence > 0.6f)
        {
            return DetectionVerdict.Definite;
        }

        if (rate > 1.20f)
        {
            return DetectionVerdict.Definite;
        }

        // Modified client: queue exceeds what unmodified client can produce
        // This is impossible with unmodified client, so it's definite regardless of persistence
        if (queueDepth > MaxQueueWithUnmodifiedClient)
        {
            return DetectionVerdict.Definite;
        }

        // Queue-based detection: requires SUSTAINED high queue, not momentary spike
        // A legitimate lag burst can momentarily fill queue to 4, but it drains quickly.
        // Speed hackers maintain queue at 4 continuously because they send at max ACK rate.
        // We use Likely here and require sustained detection before alerting.
        if (queueDepth >= MaxQueueWithUnmodifiedClient && ns.HasStableConnection && averageRtt < 100)
        {
            // Stable low-latency connection with high queue - suspicious but needs sustained check
            return DetectionVerdict.Likely;
        }

        if (queueDepth >= 3 && ns.HasStableConnection)
        {
            return DetectionVerdict.Likely;
        }

        if (rate > _suspiciousRateThreshold && confidence > 0.4f && ns.HasStableConnection)
        {
            return DetectionVerdict.Likely;
        }

        if (rate > 1.03f && confidence > 0.2f)
        {
            return DetectionVerdict.Possible;
        }

        if (packetRate > 14)
        {
            return DetectionVerdict.Possible;
        }

        if (queueDepth >= 2 && ns.HasStableConnection)
        {
            return DetectionVerdict.Possible;
        }

        return DetectionVerdict.Normal;
    }

    /// <summary>
    /// Checks if staff should be notified about suspicious movement.
    /// Called periodically during sustained queue depth tracking.
    /// Returns the detection verdict for probe frequency adjustment.
    /// </summary>
    public static DetectionVerdict CheckAndNotifyStaff(NetState ns)
    {
        var verdict = AnalyzeMovement(ns, out var rate, out var sampleCount, out var confidence);

        // Debug logging
        if (_debugLogging && ns.Mobile?.RawName != null)
        {
            var (burstSize, _) = DetectRecentBurst(ns);
            var probeStatus = ns._rttProbeTime > 0 ? "pending" : "idle";
            var queueDepth = ns._movementQueue?.Count ?? 0;
            logger.Debug(
                "[RateCheck] {Name}: rate={Rate:F3} samples={Samples} verdict={Verdict} " +
                "confidence={Confidence:P0} queue={Queue} burst={Burst} sustained={Sustained}s",
                ns.Mobile.RawName, rate, sampleCount, verdict, confidence, queueDepth, burstSize, ns._consecutiveHighRateSeconds
            );
            logger.Debug(
                "    RTT: avg={Avg}ms last={Last}ms var={Var} samples={RttSamples} stable={Stable} probe={Probe}",
                ns.AverageRtt, ns._lastRtt, ns._rttVariance, ns._rttSampleCount, ns.HasStableConnection, probeStatus
            );
        }

        // Update sustained counter based on verdict
        if (verdict >= DetectionVerdict.Likely)
        {
            ns._consecutiveHighRateSeconds += 2; // Called every ~2 seconds
        }
        else
        {
            ns._consecutiveHighRateSeconds = Math.Max(0, ns._consecutiveHighRateSeconds - 1);
        }

        // Determine if we should notify
        var shouldNotify = false;
        var urgency = "";

        if (verdict == DetectionVerdict.Definite)
        {
            shouldNotify = true;
            urgency = "HIGH";
        }
        else if (verdict == DetectionVerdict.Likely && ns._consecutiveHighRateSeconds >= 10)
        {
            shouldNotify = true;
            urgency = "MEDIUM";
        }
        else if (verdict == DetectionVerdict.Possible && ns._consecutiveHighRateSeconds >= 30)
        {
            shouldNotify = true;
            urgency = "LOW";
        }

        if (shouldNotify)
        {
            if (_debugLogging)
            {
                logger.Debug(
                    "[ALERT] {Urgency} - {Name}: rate={Rate:F3} verdict={Verdict} confidence={Confidence:P0}",
                    urgency, ns.Mobile?.RawName, rate, verdict, confidence
                );
            }
            NotifyStaff(ns, rate, sampleCount, confidence, verdict, urgency);
        }

        return verdict;
    }

    /// <summary>
    /// Sends notification to staff about potential speed hacking.
    /// Rate-limited per player.
    /// </summary>
    private static void NotifyStaff(
        NetState ns,
        float rate,
        int sampleCount,
        float confidence,
        DetectionVerdict verdict,
        string urgency)
    {
        var now = Core.TickCount;

        // Rate-limit notifications per player
        if (now - ns._lastSpeedHackNotification < _speedHackNotificationCooldown)
        {
            return;
        }

        ns._lastSpeedHackNotification = now;

        var mobile = ns.Mobile;

        // Log to file with full details
        logger.Warning(
            "[{Urgency}] Speed hack detected: {Character} ({Account}) | " +
            "Rate: {Rate:F2} ({Samples} samples) | Verdict: {Verdict} | Confidence: {Confidence:P0} | " +
            "PacketRate: {PacketRate}/s (peak: {PeakRate}/s) | RTT: {Rtt}ms (stable: {Stable}) | " +
            "Sustained: {Sustained}s | Queue: {Queue} | Location: {Location} Map: {Map} | IP: {IP}",
            urgency,
            mobile?.RawName ?? "Unknown",
            ns.Account?.Username ?? "Unknown",
            rate,
            sampleCount,
            verdict,
            confidence,
            ns.CurrentMovementRate,
            ns.PeakMovementRate,
            ns.AverageRtt,
            ns.HasStableConnection,
            ns._consecutiveHighRateSeconds,
            ns._movementQueue?.Count ?? 0,
            mobile?.Location,
            mobile?.Map?.Name,
            ns.Address
        );

        // Invoke callback for staff notification (if configured)
        // Server operators can hook this to broadcast to staff
        OnSpeedHackDetected?.Invoke(ns, mobile, rate, verdict, urgency);
    }

    /// <summary>
    /// Event raised when a speed hack is detected.
    /// Server operators can subscribe to broadcast to staff or take other actions.
    /// </summary>
    public static event Action<NetState, Mobile, float, DetectionVerdict, string> OnSpeedHackDetected;
}
