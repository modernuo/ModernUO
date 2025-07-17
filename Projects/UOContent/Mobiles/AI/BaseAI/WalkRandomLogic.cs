/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: WalkRandomLogic.cs                                              *
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
using Server.Engines.Spawners;

namespace Server.Mobiles;

public abstract partial class BaseAI
{
    public virtual void WalkRandom(int chanceToNotMove, int chanceToDir, int steps)
    {
        if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves || chanceToNotMove <= 0)
        {
            return;
        }

        var maxSteps = Math.Min(steps, 3);

        for (var i = 0; i < maxSteps; i++)
        {
            if (Utility.Random(1 + chanceToNotMove) == 0)
            {
                DoMove(GetRandomDirection(chanceToDir));
            }
        }
    }

    public virtual void WalkRandomInHome(int chanceToNotMove, int chanceToDir, int steps)
    {
        if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves)
        {
            return;
        }

        if (m_Mobile.Home == Point3D.Zero)
        {
            WalkRandomNoHome(chanceToNotMove, chanceToDir, steps);
        }
        else
        {
            WalkRandomWithHome(chanceToNotMove, chanceToDir, steps);
        }
    }

    private void WalkRandomNoHome(int chanceToNotMove, int chanceToDir, int steps)
    {
        if (m_Mobile.Spawner is RegionSpawner rs)
        {
            var region = rs.SpawnRegion;

            if (m_Mobile.Region.AcceptsSpawnsFrom(region))
            {
                m_Mobile.WalkRegion = region;

                WalkRandom(chanceToNotMove, chanceToDir, steps);

                m_Mobile.WalkRegion = null;
            }
            else if (region.GoLocation != Point3D.Zero && Utility.RandomBool())
            {
                DoMove(m_Mobile.GetDirectionTo(region.GoLocation));
            }
            else
            {
                WalkRandom(chanceToNotMove, chanceToDir, 1);
            }
        }
        else
        {
            WalkRandom(chanceToNotMove, chanceToDir, steps);
        }
    }

    private void WalkRandomWithHome(int chanceToNotMove, int chanceToDir, int steps)
    {
        if (m_Mobile.RangeHome == 0)
        {
            if (m_Mobile.Location != m_Mobile.Home)DoMove(m_Mobile.GetDirectionTo(m_Mobile.Home));
            {
                return;
            }
        }

        for (var i = 0; i < steps; i++)
        {
            var currDist = (int)m_Mobile.GetDistanceToSqrt(m_Mobile.Home);

            if (currDist > m_Mobile.RangeHome)
            {
                DoMove(m_Mobile.GetDirectionTo(m_Mobile.Home));
            }
            else if (currDist < m_Mobile.RangeHome * 2 / 3 || Utility.Random(10) <= 5)
            {
                WalkRandom(chanceToNotMove, chanceToDir, 1);
            }
            else
            {
                DoMove(m_Mobile.GetDirectionTo(m_Mobile.Home));
            }
        }
    }

    private Direction GetRandomDirection(int chanceToDir)
    {
        var randomMove = Utility.Random(8 * (chanceToDir + 1));

        if (randomMove < 8)
        {
            return (Direction)randomMove;
        }

        return m_Mobile.Direction;
    }
}
