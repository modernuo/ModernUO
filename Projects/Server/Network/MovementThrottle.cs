/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
    private static long _maxCreditThreshold = 2000; // 2 seconds
    private static int _consecutiveThrottleThreshold = 5;
    private static long _suspiciousLogCooldown = 60000; // 1 minute

    private static readonly ILogger logger = LogFactory.GetLogger(typeof(MovementThrottle));

    public static void Configure()
    {
        _maxCreditThreshold = ServerConfiguration.GetOrUpdateSetting(
            "movementThrottle.maxCreditThreshold",
            _maxCreditThreshold
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
        IncomingPackets.RegisterThrottler(0x02, &Throttle);
    }

    /// <summary>
    /// Throttles a movement packet.
    /// This is the function to call on your hot path.
    /// </summary>
    /// <returns>True if throttled, False if allowed.</returns>
    public static bool Throttle(int packetId, NetState ns)
    {
        var from = ns.Mobile;

        if (from?.Deleted != false || from.AccessLevel > AccessLevel.Player)
        {
            return false;
        }

        var now = Core.TickCount;
        var credit = ns._movementCredit;
        var nextMove = ns._nextMovementTime;
        var delta = now - nextMove;
        long cost;

        if (delta > 0)
        {
            cost = 0;
            credit = Math.Min(_maxCreditThreshold, credit + delta);
            ns._nextMovementTime = now;
            ns._consecutiveMovementThrottles = 0;
        }
        else
        {
            cost = -delta;
        }

        if (credit < cost)
        {
            ns._consecutiveMovementThrottles++;

            if (ns._consecutiveMovementThrottles >= _consecutiveThrottleThreshold)
            {
                LogSuspiciousActivity(ns, cost, credit);
            }

            return true;
        }

        ns._movementCredit = credit - cost;

        if (cost <= 0)
        {
            ns._consecutiveMovementThrottles--;
            if (ns._consecutiveMovementThrottles < 0)
            {
                ns._consecutiveMovementThrottles = 0;
            }
        }

        return false;
    }

    private static void LogSuspiciousActivity(NetState ns, long cost, long credit)
    {
        var now = Core.TickCount;
        if (now - ns._lastSuspiciousActivityLog < _suspiciousLogCooldown)
        {
            return;
        }

        ns._lastSuspiciousActivityLog = now;

        var from = ns.Mobile;
        logger.Warning(
            "Potential speed hack detected: {Character} | " +
            "Cost: {Cost}ms | Credit: {Credit}ms | " +
            "Consecutive Throttles: {Count}",
            from?.Name ?? "Unknown",
            cost,
            credit,
            ns._consecutiveMovementThrottles
        );
    }
}
