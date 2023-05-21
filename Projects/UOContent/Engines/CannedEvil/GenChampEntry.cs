/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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

namespace Server.Engines.CannedEvil
{
    public record ChampionEntry
    {
        public readonly bool m_RandomizeType;
        public readonly ChampionSpawnType m_Type;
        public readonly Point3D m_SignLocation;
        public readonly Type m_ChampType;
        public readonly Map m_Map;
        public readonly Point3D m_EjectLocation;
        public readonly Map m_EjectMap;

        public ChampionEntry(Type champtype, Point3D signloc, Map map, Point3D ejectloc, Map ejectmap) :
            this(champtype, ChampionSpawnType.Abyss, signloc, map, ejectloc, ejectmap, true)
        {
        }

        public ChampionEntry(
            Type champtype, ChampionSpawnType type, Point3D signloc, Map map, Point3D ejectloc, Map ejectmap,
            bool randomizetype = false
        )
        {
            m_ChampType = champtype;
            m_RandomizeType = randomizetype;
            m_Type = type;
            m_SignLocation = signloc;
            m_Map = map;
            m_EjectLocation = ejectloc;
            m_EjectMap = ejectmap;
        }
    }
}
