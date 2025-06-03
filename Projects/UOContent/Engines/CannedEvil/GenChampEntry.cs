/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GenChampEntry.cs                                                *
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

namespace Server.Engines.CannedEvil;

public class ChampionEntry
{
    public readonly bool _randomizeType;
    public readonly ChampionSpawnType _type;
    public readonly Point3D _signLocation;
    public readonly Type _champType;
    public readonly Map _map;
    public readonly Point3D _ejectLocation;
    public readonly Map _ejectMap;

    public ChampionEntry(Type champtype, Point3D signloc, Map map, Point3D ejectloc, Map ejectmap) :
        this(champtype, ChampionSpawnType.Abyss, signloc, map, ejectloc, ejectmap, true)
    {
    }

    public ChampionEntry(
        Type champtype, ChampionSpawnType type, Point3D signloc, Map map, Point3D ejectloc, Map ejectmap,
        bool randomizetype = false
    )
    {
        _champType = champtype;
        _randomizeType = randomizetype;
        _type = type;
        _signLocation = signloc;
        _map = map;
        _ejectLocation = ejectloc;
        _ejectMap = ejectmap;
    }
}
