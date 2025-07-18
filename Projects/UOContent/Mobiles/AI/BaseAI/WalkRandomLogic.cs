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
        if (_mobile.Deleted || _mobile.DisallowAllMoves || chanceToNotMove <= 0)
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
        if (_mobile.Deleted || _mobile.DisallowAllMoves)
        {
            return;
        }

        if (_mobile.Home == Point3D.Zero)
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
        if (_mobile.Spawner is RegionSpawner rs)
        {
            var region = rs.SpawnRegion;

            if (_mobile.Region.AcceptsSpawnsFrom(region))
            {
                _mobile.WalkRegion = region;

                WalkRandom(chanceToNotMove, chanceToDir, steps);

                _mobile.WalkRegion = null;
            }
            else if (region.GoLocation != Point3D.Zero && Utility.RandomBool())
            {
                DoMove(_mobile.GetDirectionTo(region.GoLocation));
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
        if (_mobile.RangeHome == 0 && _mobile.Location != _mobile.Home)
        {
            DoMove(_mobile.GetDirectionTo(_mobile.Home));
            return;
        }

        for (var i = 0; i < steps; i++)
        {
            var currDist = (int)_mobile.GetDistanceToSqrt(_mobile.Home);

            if (currDist > _mobile.RangeHome)
            {
                DoMove(_mobile.GetDirectionTo(_mobile.Home));
            }
            else if (currDist < _mobile.RangeHome * 2 / 3 || Utility.Random(10) <= 5)
            {
                WalkRandom(chanceToNotMove, chanceToDir, 1);
            }
            else
            {
                DoMove(_mobile.GetDirectionTo(_mobile.Home));
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

        return _mobile.Direction;
    }
}
