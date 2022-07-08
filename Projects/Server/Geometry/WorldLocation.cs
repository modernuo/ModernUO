/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: WorldLocation.cs                                                *
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
using System.Collections.Generic;

namespace Server
{
    [Parsable]
    public struct WorldLocation
        : IPoint3D, IComparable<WorldLocation>, IEquatable<object>, IEquatable<WorldLocation>, IEquatable<IEntity>
    {
        internal Point3D m_Loc;
        internal Map m_Map;

        public static readonly WorldLocation Zero = new(0, 0, 0, Map.Internal);

        [CommandProperty(AccessLevel.Counselor)]
        public Point3D Location
        {
            get => m_Loc;
            set => m_Loc = value;
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int X
        {
            get => m_Loc.m_X;
            set => m_Loc.m_X = value;
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int Y
        {
            get => m_Loc.m_Y;
            set => m_Loc.m_Y = value;
        }

        [CommandProperty(AccessLevel.Counselor)]
        public int Z
        {
            get => m_Loc.m_Z;
            set => m_Loc.m_Z = value;
        }

        [CommandProperty(AccessLevel.Counselor)]
        public Map Map
        {
            get => m_Map;
            set => m_Map = value;
        }

        public WorldLocation(IEntity e) : this(e.Location.X, e.Location.Y, e.Location.Z, e.Map)
        {
        }

        public WorldLocation(Point2D p, Map map) : this(p.X, p.Y, 0, map)
        {
        }

        public WorldLocation(int x, int y, Map map) : this(x, y, 0, map)
        {
        }

        public WorldLocation(Point3D p, Map map) : this(p.X, p.Y, p.Z, map)
        {
        }

        public WorldLocation(int x, int y, int z, Map map)
        {
            m_Loc.m_X = x;
            m_Loc.m_Y = y;
            m_Loc.m_Z = z;
            m_Map = map;
        }

        public override string ToString() =>
            $"({m_Loc.m_X}, {m_Loc.m_Y}, {m_Loc.m_Z}, {m_Map?.ToString() ?? "(-null-)"})";

        public bool Equals(WorldLocation other) =>
            m_Loc.Equals(other.m_Loc) && m_Map.MapID == other.m_Map.MapID;

        public bool Equals(IEntity other) =>
            !ReferenceEquals(other, null) && m_Loc == other.Location &&
            m_Map.MapID == other.Map.MapID;

        public override bool Equals(object obj) =>
            obj is WorldLocation other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(m_Loc, m_Map);

        public int CompareTo(WorldLocation other)
        {
            var locComparison = m_Loc.CompareTo(other.m_Loc);
            return locComparison != 0 ? locComparison : Comparer<Map>.Default.Compare(m_Map, other.m_Map);
        }

        public static implicit operator Point3D(WorldLocation worldLocation) => worldLocation.Location;

        public static bool operator ==(WorldLocation l, WorldLocation r) =>
            l.m_Loc == r.m_Loc && l.m_Map == r.m_Map;

        public static bool operator ==(WorldLocation l, IEntity r) =>
            !ReferenceEquals(r, null) && l.m_Loc == r.Location && l.m_Map == r.Map;

        public static bool operator !=(WorldLocation l, WorldLocation r) => l.m_Loc != r.m_Loc && l.m_Map != r.m_Map;

        public static bool operator !=(WorldLocation l, IEntity r) =>
            !ReferenceEquals(r, null) && l.m_Loc != r.Location && l.m_Map != r.Map;

        public static bool operator >(WorldLocation l, WorldLocation r) => l.m_Loc > r.m_Loc && l.m_Map == r.m_Map;

        public static bool operator >(WorldLocation l, IEntity r) =>
            !ReferenceEquals(r, null) && l.m_Loc > r.Location && l.m_Map == r.Map;

        public static bool operator <(WorldLocation l, WorldLocation r) => l.m_Loc < r.m_Loc && l.m_Map == r.m_Map;

        public static bool operator <(WorldLocation l, IEntity r) =>
            !ReferenceEquals(r, null) && l.m_Loc < r.Location && l.m_Map == r.Map;

        public static bool operator >=(WorldLocation l, WorldLocation r) => l.m_Loc >= r.m_Loc && l.m_Map == r.m_Map;

        public static bool operator >=(WorldLocation l, IEntity r) =>
            !ReferenceEquals(r, null) && l.m_Loc >= r.Location && l.m_Map == r.Map;

        public static bool operator <=(WorldLocation l, WorldLocation r) => l.m_Loc <= r.m_Loc && l.m_Map == r.m_Map;

        public static bool operator <=(WorldLocation l, IEntity r) =>
            !ReferenceEquals(r, null) && l.m_Loc <= r.Location && l.m_Map == r.Map;

        public static WorldLocation Parse(string value)
        {
            var start = value.IndexOfOrdinal('(');
            var end = value.IndexOf(',', start + 1);

            Utility.ToInt32(value.AsSpan(start + 1, end - (start + 1)).Trim(), out var x);

            start = end;
            end = value.IndexOf(',', start + 1);

            Utility.ToInt32(value.AsSpan(start + 1, end - (start + 1)).Trim(), out var y);

            start = end;
            end = value.IndexOf(',', start + 1);

            Utility.ToInt32(value.AsSpan(start + 1, end - (start + 1)).Trim(), out var z);

            start = end;
            end = value.IndexOf(')', start + 1);

            var map = Map.Parse(value.AsSpan(start + 1, end - (start + 1)).Trim());

            return new WorldLocation(x, y, z, map);
        }
    }
}
