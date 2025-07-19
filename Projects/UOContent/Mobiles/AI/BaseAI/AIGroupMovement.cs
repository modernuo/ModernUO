/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AIGroupMovement.cs                                              *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program. If not, see <http://www.gnu.org/licenses/>.  *
 ************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Collections;

namespace Server.Mobiles;

public abstract partial class BaseAI
{
    private static readonly Dictionary<BaseCreature, Point3D> _reservedPositions = new();
    private static long _lastGroupUpdateTime;

    private static void CleanupReservedPositions()
    {
        using var toRemove = PooledRefQueue<BaseCreature>.Create();

        foreach (var (m, p) in _reservedPositions)
        {
            if (m?.Deleted != false || m.GetDistanceToSqrt(p) < 1)
            {
                toRemove.Enqueue(m);
            }
        }

        while (toRemove.Count > 0)
        {
            _reservedPositions.Remove(toRemove.Dequeue());
        }
    }

    private bool UseGroupMovement(Mobile target) =>
        Mobile.Combatant == target
        && !Mobile.Controlled
        && CountNearbyAllies(target) > 0;

    public static bool MoveToWithGroup(BaseAI ai, Mobile target, bool run, int range)
    {
        if (Core.TickCount - _lastGroupUpdateTime > 1000)
        {
            CleanupReservedPositions();
            _lastGroupUpdateTime = Core.TickCount;
        }

        var mobile = ai.Mobile;
        var allies = ai.GetNearbyAllies(target);
        var optimalPosition = ai.CalculateOptimalPosition(target, ref allies, range);
        try
        {
            if (optimalPosition == Point3D.Zero)
            {
                return ai.MoveToWithCollisionAvoidance(target, run, range);
            }

            _reservedPositions[mobile] = optimalPosition;

            var direction = mobile.GetDirectionTo(optimalPosition);

            if (Utility.Random(3) == 0)
            {
                direction = GetAdjustedDirection(direction);
            }

            return ai.DoMove(direction, true);
        }
        finally
        {
            allies.Dispose();
        }
    }

    private int CountNearbyAllies(Mobile target)
    {
        var allies = 0;
        foreach (var m in Mobile.GetMobilesInRange(8))
        {
            if (m != Mobile && m.Combatant == target && m is BaseCreature { Controlled: false } bc
                && bc.Team == Mobile.Team)
            {
                allies++;
            }
        }

        return allies;
    }

    private PooledRefList<BaseCreature> GetNearbyAllies(Mobile target)
    {
        var allies = PooledRefList<BaseCreature>.Create();

        foreach (var m in Mobile.GetMobilesInRange(8))
        {
            if (m != Mobile && m.Combatant == target && m is BaseCreature { Controlled: false } bc
                && bc.Team == Mobile.Team)
            {
                allies.Add(bc);
            }
        }

        return allies;
    }

    private Point3D CalculateOptimalPosition(Mobile target, ref PooledRefList<BaseCreature> allies, int range)
    {
        var targetLoc = target.Location;
        var bestPosition = Point3D.Zero;
        var bestScore = -1.0;

        for (var x = -range; x <= range; x++)
        {
            for (var y = -range; y <= range; y++)
            {
                if (Math.Abs(x) + Math.Abs(y) != range)
                {
                    continue;
                }

                var testLoc = new Point3D(targetLoc.X + x, targetLoc.Y + y, targetLoc.Z);
                var distance = Mobile.GetDistanceToSqrt(testLoc);

                if (distance < range ||
                    distance > range + 3 || !CanMoveTo(testLoc))
                {
                    continue;
                }

                var score = ScorePosition(testLoc, distance, target, ref allies);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = testLoc;

                    if (score > 20)
                    {
                        break;
                    }
                }
            }
        }

        return bestPosition;
    }

    private double ScorePosition(Point3D position, double currentDistance, Mobile target, ref PooledRefList<BaseCreature> allies)
    {
        var score = -(currentDistance * 2);

        for (var i = 0; i < allies.Count; i++)
        {
            var ally = allies[i];
            var allyDistance = ally.GetDistanceToSqrt(position);

            if (allyDistance < 2)
            {
                score -= 50;
            }
            else if (allyDistance < 3)
            {
                score -= 20;
            }
        }

        foreach (var (m, p) in _reservedPositions)
        {
            if (m != Mobile && p.GetDistanceToSqrt(position) < 2)
            {
                score -= 30;
            }
        }

        if (Mobile.Map != null && Mobile.Map.LineOfSight(position, target.Location))
        {
            score += 10;
        }

        return score + Utility.RandomDouble() * 5;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanMoveTo(Point3D location) =>
        Mobile.Map?.CanFit(location.X, location.Y, location.Z, 16, false, false) == true;

    private static Direction GetAdjustedDirection(Direction original)
    {
        var adjustment = Utility.Random(3) - 1;
        var newDir = (int)original + adjustment;

        if (newDir < 0)
        {
            return (Direction)(newDir + 8);
        }

        if (newDir >= 8)
        {
            return (Direction)(newDir - 8);
        }

        return (Direction)newDir;
    }
}
