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

namespace Server.Network;

public static class MovementThrottle
{
    private static long _movementThrottleReset; // 1 second
    private static long _throttleThreshold; // 400 milliseconds

    public static void Configure()
    {
        _movementThrottleReset = ServerConfiguration.GetOrUpdateSetting("movement.throttleReset", 1000);
        _throttleThreshold = ServerConfiguration.GetOrUpdateSetting("movement.throttleThreshold", 400);
    }

    public static unsafe void Initialize()
    {
        IncomingPackets.RegisterThrottler(0x02, &Throttle);
    }

    public static bool Throttle(int packetId, NetState ns, out bool drop)
    {
        drop = false;

        var from = ns.Mobile;

        if (from?.Deleted != false || from.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        long now = Core.TickCount;
        long credit = ns._movementCredit;
        long nextMove = ns._nextMovementTime;

        // Reset system if idle for more than 1 second
        if (now - nextMove + _movementThrottleReset > 0)
        {
            ns._movementCredit = 0;
            ns._nextMovementTime = now;
            return true;
        }

        long cost = nextMove - now;

        if (credit < cost)
        {
            // Not enough credit, therefore throttled
            return false;
        }

        // On the next event loop, the player receives up to 400ms in grace latency
        ns._movementCredit = Math.Min(_throttleThreshold, credit - cost);
        return true;
    }
}
