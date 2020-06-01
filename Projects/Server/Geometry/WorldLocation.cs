/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: WorldLocation.cs - Created: 2020/05/31 - Updated: 2020/05/31    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;

namespace Server
{
  [Parsable]
  public struct WorldLocation : IPoint3D, IComparable<WorldLocation>, IEquatable<object>, IEquatable<WorldLocation>, IEquatable<IEntity>
  {
    internal int m_X;
    internal int m_Y;
    internal int m_Z;
    internal Map m_Map;

    public static readonly WorldLocation Zero = new WorldLocation(0, 0, 0, Map.Internal);

    [CommandProperty(AccessLevel.Counselor)]
    public int X
    {
      get => m_X;
      set => m_X = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int Y
    {
      get => m_Y;
      set => m_Y = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int Z
    {
      get => m_Z;
      set => m_Z = value;
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

    public WorldLocation(IPoint2D p, Map map) : this(p.X, p.Y, 0, map)
    {
    }

    public WorldLocation(int x, int y, Map map) : this(x, y, 0, map)
    {
    }

    public WorldLocation(IPoint3D p, Map map) : this(p.X, p.Y, p.Z, map)
    {
    }

    public WorldLocation(int x, int y, int z, Map map)
    {
      m_X = x;
      m_Y = y;
      m_Z = z;
      m_Map = map;
    }

    public override string ToString() =>
      $"({m_X}, {m_Y}, {m_Z}, {m_Map?.ToString() ?? "(-null-)"})";

    public bool Equals(WorldLocation other) =>
      m_X == other.m_X && m_Y == other.m_Y && m_Z == other.m_Z && m_Map.MapID == other.m_Map.MapID;

    public bool Equals(IEntity other) =>
      !ReferenceEquals(other, null) && m_X == other.X && m_Y == other.Y && m_Z == other.Z &&
      m_Map.MapID == other.Map.MapID;

    public override bool Equals(object obj) =>
      obj is WorldLocation other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(m_X, m_Y, m_Z, m_Map);

    public int CompareTo(WorldLocation other)
    {
      var xComparison = m_X.CompareTo(other.m_X);
      if (xComparison != 0) return xComparison;
      var yComparison = m_Y.CompareTo(other.m_Y);
      if (yComparison != 0) return yComparison;
      var zComparison = m_Z.CompareTo(other.m_Z);
      if (zComparison != 0) return zComparison;
      return m_Map.MapID.CompareTo(other.m_Map);
    }

    public static bool operator ==(WorldLocation l, WorldLocation r) =>
      l.m_X == r.m_X && l.Y == r.m_Y && l.m_Z == r.m_Z && l.m_Map == r.m_Map;

    public static bool operator ==(WorldLocation l, IEntity r) =>
      !ReferenceEquals(r, null) && l.m_X == r.X && l.Y == r.Y && l.m_Z == r.Z && l.m_Map == r.Map;

    public static bool operator !=(WorldLocation l, WorldLocation r) =>
      l.m_X != r.m_X && l.Y != r.m_Y && l.m_Z != r.m_Z && l.m_Map != r.m_Map;

    public static bool operator !=(WorldLocation l, IEntity r) =>
      !ReferenceEquals(r, null) && l.m_X != r.X && l.Y != r.Y && l.m_Z != r.Z && l.m_Map != r.Map;

    public static bool operator >(WorldLocation l, WorldLocation r) =>
      l.m_X > r.m_X && l.Y > r.m_Y && l.m_Z > r.m_Z && l.m_Map == r.m_Map;

    public static bool operator >(WorldLocation l, IEntity r) =>
      !ReferenceEquals(r, null) && l.m_X > r.X && l.Y > r.Y && l.m_Z > r.Z && l.m_Map == r.Map;

    public static bool operator <(WorldLocation l, WorldLocation r) =>
      l.m_X < r.m_X && l.Y < r.m_Y && l.m_Z < r.m_Z && l.m_Map == r.m_Map;

    public static bool operator <(WorldLocation l, IEntity r) =>
      !ReferenceEquals(r, null) && l.m_X < r.X && l.Y < r.Y && l.m_Z < r.Z && l.m_Map == r.Map;

    public static bool operator >=(WorldLocation l, WorldLocation r) =>
      l.m_X >= r.m_X && l.Y >= r.m_Y && l.m_Z >= r.m_Z && l.m_Map == r.m_Map;

    public static bool operator >=(WorldLocation l, IEntity r) =>
      !ReferenceEquals(r, null) && l.m_X >= r.X && l.Y >= r.Y && l.m_Z >= r.Z && l.m_Map == r.Map;

    public static bool operator <=(WorldLocation l, WorldLocation r) =>
      l.m_X <= r.m_X && l.Y <= r.m_Y && l.m_Z <= r.m_Z && l.m_Map == r.m_Map;

    public static bool operator <=(WorldLocation l, IEntity r) =>
      !ReferenceEquals(r, null) && l.m_X <= r.X && l.Y <= r.Y && l.m_Z <= r.Z && l.m_Map == r.Map;

    public static WorldLocation Parse(string value)
    {
      var start = value.IndexOf('(');
      var end = value.IndexOf(',', start + 1);

      Utility.ToInt32(value.Substring(start + 1, end - (start + 1)).Trim(), out int x);

      start = end;
      end = value.IndexOf(',', start + 1);

      Utility.ToInt32(value.Substring(start + 1, end - (start + 1)).Trim(), out int y);

      start = end;
      end = value.IndexOf(',', start + 1);

      Utility.ToInt32(value.Substring(start + 1, end - (start + 1)).Trim(), out int z);

      start = end;
      end = value.IndexOf(')', start + 1);

      var map = Map.Parse(value.Substring(start + 1, end - (start + 1)).Trim());

      return new WorldLocation(x, y, z, map);
    }
  }
}
