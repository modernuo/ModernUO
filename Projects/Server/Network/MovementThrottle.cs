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
using System.Runtime.CompilerServices;

namespace Server.Network;

public static class MovementThrottle
{
    private class DebtState
    {
        public long TimeDebt;
        public long LastDecayTime;
        public long LastMovementTime;
    }

    private static readonly ConditionalWeakTable<NetState, DebtState> _debtStates = new();
    private static long _maxJitter;
    private static long _maxTimeDebt;
    private static long _debtDecayTime;
    private static long _debtDecayAmount;
    private static long _idleResetThreshold;

    public static void Configure()
    {
        _maxJitter = ServerConfiguration.GetOrUpdateSetting("movement.maxJitter", 75);
        _maxTimeDebt = ServerConfiguration.GetOrUpdateSetting("movement.maxTimeDebt", 100);
        _debtDecayTime = ServerConfiguration.GetOrUpdateSetting("movement.debtDecayTime", 500);
        _debtDecayAmount = ServerConfiguration.GetOrUpdateSetting("movement.debtDecayAmount", 100);
        _idleResetThreshold = ServerConfiguration.GetOrUpdateSetting("movement.idleResetThreshold", 1000);
    }

    public static unsafe void Initialize()
    {
        IncomingPackets.RegisterThrottler(0x02, &Throttle);
    }

    public static bool Throttle(int packetId, NetState ns)
    {
        var from = ns.Mobile;
        if (from?.Deleted != false || from.AccessLevel > AccessLevel.Player)
        {
            return false;
        }

        long now = Core.TickCount;
        long nextMove = ns._nextMovementTime;

        var debt = _debtStates.GetOrCreateValue(ns);
        
        // Initialize first use
        if (debt.LastMovementTime == 0)
        {
            debt.LastDecayTime = now;
            debt.LastMovementTime = now;
        }

        // Reset debt if idle
        if (now - debt.LastMovementTime > _idleResetThreshold)
        {
            debt.TimeDebt = 0;
            debt.LastDecayTime = now;
            ns._nextMovementTime = now;
        }

        // Decay debt over time
        long timeSinceDecay = now - debt.LastDecayTime;
        if (timeSinceDecay >= _debtDecayTime)
        {
            long periods = timeSinceDecay / _debtDecayTime;
            debt.TimeDebt = Math.Max(0, debt.TimeDebt - (periods * _debtDecayAmount));
            debt.LastDecayTime += periods * _debtDecayTime;
        }

        // First move or past allowed time
        if (nextMove == 0 || now >= nextMove)
        {
            debt.TimeDebt = Math.Max(0, debt.TimeDebt - Math.Max(0, now - nextMove));
            debt.LastMovementTime = now;
            return false;
        }

        // Add debt for early movement beyond jitter tolerance
        long early = nextMove - now;
        if (early > _maxJitter)
        {
            debt.TimeDebt = Math.Min(_maxTimeDebt * 10, debt.TimeDebt + (early - _maxJitter));
        }

        // Block if debt exceeds threshold
        if (debt.TimeDebt > _maxTimeDebt)
        {
            return true;
        }

        debt.LastMovementTime = now;
        return false;
    }
}
