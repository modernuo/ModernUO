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
using Server.Logging;

namespace Server.Network;

/// <summary>
/// A high-performance, token-bucket-based movement throttle
/// combined with a lightweight, consecutive-throttle-based
/// speed hack detection system.
/// </summary>
public static class MovementThrottle
{
    internal static long _lastWorldSave;
    private static int _idleThreshold = 1200; // 1.2 seconds
    private static int _consecutiveThrottleThreshold = 5;
    private static int _suspiciousLogCooldown = 60000;          // 1 minute
    private static int _suspiciousActivityBroadcastCooldown = 2000; // 2s
    private const int _worldSaveCooldown = 3000;                // 3 seconds

    private static readonly ILogger logger = LogFactory.GetLogger(typeof(MovementThrottle));

    public static void Configure()
    {
        _idleThreshold = ServerConfiguration.GetOrUpdateSetting(
            "movementThrottle.idleThreshold",
            _idleThreshold
        );

        _consecutiveThrottleThreshold = ServerConfiguration.GetOrUpdateSetting(
            "movementThrottle.consecutiveThrottleThreshold",
            _consecutiveThrottleThreshold
        );

        _suspiciousLogCooldown = ServerConfiguration.GetOrUpdateSetting(
            "movementThrottle.suspiciousLogCooldown",
            _suspiciousLogCooldown
        );
    }

    public static unsafe void Initialize()
    {
        _lastWorldSave = Core.TickCount;
        IncomingPackets.RegisterThrottler(0x02, &Throttle);
    }

    public static bool Throttle(int packetId, NetState ns)
    {
        var from = ns.Mobile;

        if (from?.Deleted != false || from.AccessLevel > AccessLevel.Player)
        {
            return false;
        }

        var now = Core.TickCount;
        var nextMove = ns._nextMovementTime;
        var delta = now - nextMove;
        logger.Debug("Movement Throttle Check: {Character} | Now: {Now} | NextMove: {NextMove} | Delta: {Delta} | Credit: {Credit}",
            from.RawName,
            now,
            nextMove,
            delta,
            ns._movementCredit
        );

        // Idle too long
        if (delta > _idleThreshold)
        {
            ns._movementCredit = _idleThreshold;
            ns._nextMovementTime = now;
            ns._consecutiveMovementThrottles = 0;
            ns._movementThrottled = false;
            return false;
        }

        long cost;
        var credit = ns._movementCredit;
        if (delta > 0)
        {
            credit = Math.Min(_idleThreshold, credit + delta);
            cost = 0;
            ns._nextMovementTime = now;

            if (--ns._consecutiveMovementThrottles < 0)
            {
                ns._consecutiveMovementThrottles = 0;
            }
        }
        else
        {
            cost = -delta;
        }

        var remainingCredit = credit - cost;
        if (remainingCredit < 0)
        {
            // It's been at least 3s since the last world save, and we hit the threshold
            if (!ns._movementThrottled && now - _lastWorldSave >= _worldSaveCooldown)
            {
                ++ns._consecutiveMovementThrottles;
                ns._movementThrottled = true;
                logger.Debug("Movement throttle triggered for {Character}. Consecutive throttles: {Count}",
                    from.RawName,
                    ns._consecutiveMovementThrottles
                );

                if (ns._consecutiveMovementThrottles >= _consecutiveThrottleThreshold)
                {
                    LogSuspiciousActivity(ns, cost, credit);
                }
            }

            return true;
        }

        ns._movementCredit = remainingCredit;

        // if (cost <= 0)
        // {
        //     ns._consecutiveMovementThrottles--;
        //     if (ns._consecutiveMovementThrottles < 0)
        //     {
        //         ns._consecutiveMovementThrottles = 0;
        //     }
        // }

        ns._movementThrottled = false;
        return false;
    }

    private static void LogSuspiciousActivity(NetState ns, long cost, long credit)
    {
        var now = Core.TickCount;
        var from = ns.Mobile;
        string name = null;

        if (now - ns._lastSuspiciousActivityLog >= _suspiciousLogCooldown)
        {
            ns._lastSuspiciousActivityLog = now;
            name = from.RawName;
            logger.Warning(
                "Potential speed hack detected: {Character} | " +
                "Cost: {Cost}ms | Credit: {Credit}ms | " +
                "Consecutive Throttles: {Count}",
                name,
                cost,
                credit,
                ns._consecutiveMovementThrottles
            );
        }

        if (now - ns._lastSuspiciousActivityBroadcast >= _suspiciousActivityBroadcastCooldown)
        {
            ns._lastSuspiciousActivityBroadcast = now;
            World.Broadcast(0x35, true, $"Staff Alert: Potential speed hack detected for character {name ?? from.RawName}.");
        }
    }
}
