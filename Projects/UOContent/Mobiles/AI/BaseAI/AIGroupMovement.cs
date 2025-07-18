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

        foreach (var kvp in _reservedPositions)
        {
            if (kvp.Key == null || kvp.Key.Deleted || kvp.Key.GetDistanceToSqrt(kvp.Value) < 1)
            {
                toRemove.Enqueue(kvp.Key);
            }
        }
        foreach (var creature in toRemove)
        {
            _reservedPositions.Remove(creature);
        }
    }

    private bool UseGroupMovement(Mobile target) =>
        m_Mobile.Combatant == target
        && !m_Mobile.Controlled
        && CountNearbyAllies(target) > 0;

    public static bool MoveToWithGroup(BaseAI ai, Mobile target, bool run, int range)
    {
        if (Core.TickCount - _lastGroupUpdateTime > 1000)
        {
            CleanupReservedPositions();
            _lastGroupUpdateTime = Core.TickCount;
        }

        var m_Mobile = ai.m_Mobile;
        var allies = ai.GetNearbyAllies(target);
        try
        {
            var optimalPosition = ai.CalculateOptimalPosition(target, ref allies, range);

            if (optimalPosition != Point3D.Zero)
            {
                _reservedPositions[m_Mobile] = optimalPosition;

                var direction = m_Mobile.GetDirectionTo(optimalPosition);

                if (Utility.Random(100) < 30)
                {
                    direction = GetAdjustedDirection(direction);
                }

                return ai.DoMove(direction, true);
            }

            return ai.MoveToWithCollisionAvoidance(target, run, range);
        }
        finally
        {
            allies.Dispose();
        }
    }

    private int CountNearbyAllies(Mobile target)
    {
        var allies = 0;
        foreach (var m in m_Mobile.GetMobilesInRange(8))
        {
            if (m != m_Mobile && m.Combatant == target && m is BaseCreature { Controlled: false } bc
                && bc.Team == m_Mobile.Team)
            {
                allies++;
            }
        }

        return allies;
    }

    private PooledRefList<BaseCreature> GetNearbyAllies(Mobile target)
    {
        var allies = PooledRefList<BaseCreature>.Create();

        foreach (var m in m_Mobile.GetMobilesInRange(8))
        {
            if (m != m_Mobile && m.Combatant == target && m is BaseCreature { Controlled: false } bc
                && bc.Team == m_Mobile.Team)
            {
                allies.Add(bc);
            }
        }

        return allies;
    }

    private Point3D CalculateOptimalPosition(Mobile target, ref PooledRefList<BaseCreature> allies, int range)
    {
        var targetLoc = target.Location;
        var positions = new List<Point3D>();

        for (var x = -range; x <= range; x++)
        {
            for (var y = -range; y <= range; y++)
            {
                if (Math.Abs(x) + Math.Abs(y) != range)
                {
                    continue;
                }

                var testLoc = new Point3D(targetLoc.X + x, targetLoc.Y + y, targetLoc.Z);

                if (m_Mobile.GetDistanceToSqrt(testLoc) >= range && m_Mobile.GetDistanceToSqrt(testLoc) <= range + 3)
                {
                    positions.Add(testLoc);
                }
            }
        }

        var bestPosition = Point3D.Zero;
        var bestScore = double.MinValue;

        foreach (var pos in positions)
        {
            if (CanMoveTo(pos))
            {
                var score = ScorePosition(pos, target, ref allies);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = pos;

                    if (score > 20)
                    {
                        break;
                    }
                }
            }
        }

        return bestPosition;
    }

    private double ScorePosition(Point3D position, Mobile target, ref PooledRefList<BaseCreature> allies)
    {
        var score = 0.0;

        var currentDistance = m_Mobile.GetDistanceToSqrt(position);

        score -= currentDistance * 2;

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
            if (m != m_Mobile && GetDistanceToSqrt(p, position) < 2)
            {
                score -= 30;
            }
        }

        if (m_Mobile.Map != null && m_Mobile.Map.LineOfSight(position, target.Location))
        {
            score += 10;
        }

        return score + Utility.RandomDouble() * 5;
    }

    private static double GetDistanceToSqrt(Point3D from, Point3D to)
    {
        var xDelta = from.X - to.X;
        var yDelta = from.Y - to.Y;
        return Math.Sqrt(xDelta * xDelta + yDelta * yDelta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanMoveTo(Point3D location) =>
        m_Mobile.Map?.CanFit(location.X, location.Y, location.Z, 16, false, false) == true;

    private static Direction GetAdjustedDirection(Direction original)
    {
        var adjustment = Utility.Random(3) - 1;
        var newDir = (int)original + adjustment;

        if (newDir < 0)
        {
            newDir += 8;
        }
        else if (newDir >= 8)
        {
            newDir -= 8;
        }

        return (Direction)newDir;
    }
}
