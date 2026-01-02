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
using Server.Logging;
using Server.Mobiles;

namespace Server.Network;

/// <summary>
/// Movement throttle system using a hybrid credit + queue approach.
/// Credit buffer absorbs small timing jitter from legitimate players.
/// Queue handles larger bursts, draining at proper intervals.
/// Speed hacks detected by sustained high queue depth.
/// </summary>
public static class MovementThrottle
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(MovementThrottle));

    // Configuration values
    private static int _maxCredit = 200;           // Max credit buffer (ms)
    private static int _maxRttBonus = 150;         // Max extra credit for high-latency players (ms)
    private static int _softQueueLimit = 6;        // Start logging when reached
    private static int _hardQueueLimit = 10;       // Reject and clear at this limit
    private static int _sustainedThreshold = 10;   // Sustained abuse threshold (seconds)
    private static int _suspiciousLogCooldown = 60000; // Log cooldown (ms)

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

        _sustainedThreshold = ServerConfiguration.GetOrUpdateSetting(
            "movementThrottle.sustainedThreshold",
            _sustainedThreshold
        );

        _suspiciousLogCooldown = ServerConfiguration.GetOrUpdateSetting(
            "movementThrottle.suspiciousLogCooldown",
            _suspiciousLogCooldown
        );
    }

    public static void Initialize()
    {
        // No longer registering a packet-level throttle
        // All validation now happens in MovementReq handler with full context
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

        // Calculate movement cost - this has FULL CONTEXT (mounted, running, direction change)
        var cost = mobile.ComputeMovementSpeed(dir);

        // Calculate timing delta: positive = on-time/late, negative = early
        var delta = now - ns._nextMovementTime;

        // If there are already queued movements, add to queue to maintain order
        if (ns._hasQueuedMovements)
        {
            QueueMovement(ns, dir, seq, now);
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
            QueueMovement(ns, dir, seq, now);
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
    private static void QueueMovement(NetState ns, Direction dir, int seq, long now)
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
            Sequence = seq,
            QueuedAt = now
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

        foreach (var ns in toProcess)
        {
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
    /// Uses RTT data and packet rate to distinguish between lag bursts and speed hacks.
    /// </summary>
    private static void TrackSustainedQueueDepth(NetState ns)
    {
        var now = Core.TickCount;

        // Only check every 1 second
        if (now - ns._lastQueueDepthCheck < 1000)
        {
            return;
        }

        ns._lastQueueDepthCheck = now;

        var rate = ns.CurrentMovementRate;
        var hasHighQueue = ns._movementQueue?.Count >= _softQueueLimit;
        var hasExcessiveRate = rate > MaxExpectedPacketRate;

        if (hasHighQueue || hasExcessiveRate)
        {
            // High queue depth or excessive packet rate - but is it suspicious?
            // Stable, low-latency connection with issues = more suspicious
            // Unstable or high-latency connection = could be legitimate lag burst
            if (ns.HasStableConnection && ns.AverageRtt < 100)
            {
                // Stable connection shouldn't have problems - escalate faster
                ns._sustainedQueueDepth += hasExcessiveRate ? 3 : 2;
            }
            else if (ns._rttVariance > 10000 || ns.AverageRtt > 300)
            {
                // Very unstable or high-latency connection - don't penalize as harshly
                if (ns._sustainedQueueDepth < _sustainedThreshold)
                {
                    ns._sustainedQueueDepth++;
                }
            }
            else
            {
                // Normal case
                ns._sustainedQueueDepth += hasExcessiveRate ? 2 : 1;
            }

            if (ns._sustainedQueueDepth >= _sustainedThreshold)
            {
                LogSuspiciousActivity(ns);
            }
        }
        else
        {
            // Queue is healthy and rate is normal, decay the counter
            ns._sustainedQueueDepth = Math.Max(0, ns._sustainedQueueDepth - 1);
        }
    }

    /// <summary>
    /// Logs suspicious movement activity (potential speed hack).
    /// </summary>
    private static void LogSuspiciousActivity(NetState ns)
    {
        var now = Core.TickCount;

        // Rate-limit logging
        if (now - ns._lastSuspiciousActivityLog < _suspiciousLogCooldown)
        {
            return;
        }

        ns._lastSuspiciousActivityLog = now;

        var mobile = ns.Mobile;
        logger.Warning(
            "Potential speed hack: {Character} ({Account}) | " +
            "Queue: {QueueDepth} | Rate: {Rate}/s (peak: {PeakRate}/s) | " +
            "Sustained: {SustainedCount}s | RTT: {Rtt}ms (stable: {Stable}) | " +
            "Location: {Location} Map: {Map} | IP: {IP}",
            mobile?.RawName ?? "Unknown",
            ns.Account?.Username ?? "Unknown",
            ns._movementQueue?.Count ?? 0,
            ns.CurrentMovementRate,
            ns.PeakMovementRate,
            ns._sustainedQueueDepth,
            ns.AverageRtt,
            ns.HasStableConnection,
            mobile?.Location,
            mobile?.Map?.Name,
            ns.Address
        );
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
}
