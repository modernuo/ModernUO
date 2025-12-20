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
    private const int _worldSaveCooldown = 1600;                // 3 seconds
    private static long _lastCheck;
    private static bool _isMounted;

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

        if (_lastCheck != now && delta < -16 || delta > 16 || _isMounted != from.Mounted)
        {
            logger.Debug("Movement Throttle Check: {Character} | Now: {Now} | NextMove: {NextMove} | Delta: {Delta} | Mounted: {Mounted}",
                from.RawName,
                now,
                nextMove,
                delta,
                from.Mounted
            );
        }

        _isMounted = from.Mounted;

        // Idle too long
        if (now - _lastWorldSave > _worldSaveCooldown && delta < -16)
        {
            // LogSuspiciousActivity(ns, delta, 0);
            return true;
        }

        _lastCheck = now;
        ns._nextMovementTime = now;
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
