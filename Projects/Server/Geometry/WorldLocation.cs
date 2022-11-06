/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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
using System.Buffers;
using System.Collections.Generic;

namespace Server;

[Parsable]
public struct WorldLocation : IPoint3D, IComparable<WorldLocation>, IEquatable<WorldLocation>, IEquatable<IEntity>,
    ISpanFormattable
{
    internal Point3D _loc;
    internal Map _map;

    public static readonly WorldLocation Zero = new(0, 0, 0, Map.Internal);

    [CommandProperty(AccessLevel.Counselor)]
    public Point3D Location
    {
        get => _loc;
        set => _loc = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int X
    {
        get => _loc.m_X;
        set => _loc.m_X = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int Y
    {
        get => _loc.m_Y;
        set => _loc.m_Y = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int Z
    {
        get => _loc.m_Z;
        set => _loc.m_Z = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public Map Map
    {
        get => _map;
        set => _map = value;
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
        _loc.m_X = x;
        _loc.m_Y = y;
        _loc.m_Z = z;
        _map = map;
    }

    public bool Equals(WorldLocation other) =>
        _loc.Equals(other._loc) && _map.MapID == other._map.MapID;

    public bool Equals(IEntity other) =>
        !ReferenceEquals(other, null) && _loc == other.Location &&
        _map.MapID == other.Map.MapID;

    public override bool Equals(object obj) =>
        obj is WorldLocation other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_loc, _map);

    public int CompareTo(WorldLocation other)
    {
        var locComparison = _loc.CompareTo(other._loc);
        return locComparison != 0 ? locComparison : Comparer<Map>.Default.Compare(_map, other._map);
    }

    public static implicit operator Point3D(WorldLocation worldLocation) => worldLocation.Location;

    public static bool operator ==(WorldLocation l, WorldLocation r) =>
        l._loc == r._loc && l._map == r._map;

    public static bool operator ==(WorldLocation l, IEntity r) =>
        !ReferenceEquals(r, null) && l._loc == r.Location && l._map == r.Map;

    public static bool operator !=(WorldLocation l, WorldLocation r) => l._loc != r._loc && l._map != r._map;

    public static bool operator !=(WorldLocation l, IEntity r) =>
        !ReferenceEquals(r, null) && l._loc != r.Location && l._map != r.Map;

    public static bool operator >(WorldLocation l, WorldLocation r) => l._loc > r._loc && l._map == r._map;

    public static bool operator >(WorldLocation l, IEntity r) =>
        !ReferenceEquals(r, null) && l._loc > r.Location && l._map == r.Map;

    public static bool operator <(WorldLocation l, WorldLocation r) => l._loc < r._loc && l._map == r._map;

    public static bool operator <(WorldLocation l, IEntity r) =>
        !ReferenceEquals(r, null) && l._loc < r.Location && l._map == r.Map;

    public static bool operator >=(WorldLocation l, WorldLocation r) => l._loc >= r._loc && l._map == r._map;

    public static bool operator >=(WorldLocation l, IEntity r) =>
        !ReferenceEquals(r, null) && l._loc >= r.Location && l._map == r.Map;

    public static bool operator <=(WorldLocation l, WorldLocation r) => l._loc <= r._loc && l._map == r._map;

    public static bool operator <=(WorldLocation l, IEntity r) =>
        !ReferenceEquals(r, null) && l._loc <= r.Location && l._map == r.Map;

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

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
    {
        if (_map == null)
        {
            return destination.TryWrite(provider, $"({_loc.m_X}, {_loc.m_Y}, {_loc.m_Z}) [(-null-)]", out charsWritten);
        }

        return destination.TryWrite(provider, $"({_loc.m_X}, {_loc.m_Y}, {_loc.m_Z}) [{_map}]", out charsWritten);
    }

    public override string ToString()
    {
        if (_map == null)
        {
            // Maximum number of characters that are needed to represent this:
            // 9 characters for (, , ) [(-null-)]
            const int staticLength = 17;
            // Up to 11 characters to represent each integer
            const int maxLength = staticLength + 11 * 3;
            Span<char> span = stackalloc char[maxLength];
            TryFormat(span, out var charsWritten, null, null);
            return span[..charsWritten].ToString();
        }
        else
        {

            int charsWritten;
            char[] array = ArrayPool<char>.Shared.Rent(128);
            Span<char> span = array.AsSpan();
            while (!TryFormat(span, out charsWritten, null, null))
            {
                array = ArrayPool<char>.Shared.Rent(array.Length * 2);
                span = array.AsSpan();
            }

            return span[..charsWritten].ToString();
        }
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
        // format and formatProvider are not doing anything right now, so use the
        // default ToString implementation.
        return ToString();
    }
}
