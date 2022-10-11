/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Point3D.cs                                                      *
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

namespace Server;

[Parsable]
public struct Point3D
    : IPoint3D, IComparable<Point3D>, IComparable<IPoint3D>, IEquatable<object>, IEquatable<Point3D>,
        IEquatable<IPoint3D>
{
    internal int m_X;
    internal int m_Y;
    internal int m_Z;

    public static readonly Point3D Zero = new(0, 0, 0);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point3D(IPoint3D p) : this(p.X, p.Y, p.Z)
    {
    }

    public Point3D(Point3D p) : this(p.X, p.Y, p.Z)
    {
    }

    public Point3D(Point2D p, int z) : this(p.X, p.Y, z)
    {
    }

    public Point3D(int x, int y, int z)
    {
        m_X = x;
        m_Y = y;
        m_Z = z;
    }

    public override string ToString() => $"({m_X}, {m_Y}, {m_Z})";

    public bool Equals(Point3D other) => m_X == other.m_X && m_Y == other.m_Y && m_Z == other.m_Z;

    public bool Equals(IPoint3D other) =>
        m_X == other?.X && m_Y == other.Y && m_Z == other.Z;

    public override bool Equals(object obj) => obj is Point3D other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(m_X, m_Y, m_Z);

    public static Point3D Parse(string value)
    {
        var start = value.IndexOfOrdinal('(');
        var end = value.IndexOf(',', start + 1);

        Utility.ToInt32(value.AsSpan(start + 1, end - (start + 1)).Trim(), out var x);

        start = end;
        end = value.IndexOf(',', start + 1);

        Utility.ToInt32(value.AsSpan(start + 1, end - (start + 1)).Trim(), out var y);

        start = end;
        end = value.IndexOf(')', start + 1);

        Utility.ToInt32(value.AsSpan(start + 1, end - (start + 1)).Trim(), out var z);

        return new Point3D(x, y, z);
    }

    public static bool operator ==(Point3D l, Point3D r) => l.m_X == r.m_X && l.m_Y == r.m_Y && l.m_Z == r.m_Z;

    public static bool operator ==(Point3D l, IPoint3D r) =>
        !ReferenceEquals(r, null) && l.m_X == r.X && l.m_Y == r.Y && l.m_Z == r.Z;

    public static bool operator !=(Point3D l, Point3D r) => l.m_X != r.m_X || l.m_Y != r.m_Y || l.m_Z != r.m_Z;

    public static bool operator !=(Point3D l, IPoint3D r) =>
        !ReferenceEquals(r, null) && (l.m_X != r.X || l.m_Y != r.Y || l.m_Z != r.Z);

    public static bool operator >(Point3D l, Point3D r) => l.m_X > r.m_X && l.m_Y > r.m_Y && l.m_Z > r.m_Z;

    public static bool operator >(Point3D l, IPoint3D r) =>
        !ReferenceEquals(r, null) && l.m_X > r.X && l.m_Y > r.Y && l.m_Z > r.Z;

    public static bool operator <(Point3D l, Point3D r) => l.m_X < r.m_X && l.m_Y < r.m_Y && l.m_Z > r.m_Z;

    public static bool operator <(Point3D l, IPoint3D r) =>
        !ReferenceEquals(r, null) && l.m_X < r.X && l.m_Y < r.Y && l.m_Z > r.Z;

    public static bool operator >=(Point3D l, Point3D r) => l.m_X >= r.m_X && l.m_Y >= r.m_Y && l.m_Z > r.m_Z;

    public static bool operator >=(Point3D l, IPoint3D r) =>
        !ReferenceEquals(r, null) && l.m_X >= r.X && l.m_Y >= r.Y && l.m_Z > r.Z;

    public static bool operator <=(Point3D l, Point3D r) => l.m_X <= r.m_X && l.m_Y <= r.m_Y && l.m_Z > r.m_Z;

    public static bool operator <=(Point3D l, IPoint3D r) =>
        !ReferenceEquals(r, null) && l.m_X <= r.X && l.m_Y <= r.Y && l.m_Z > r.Z;

    public int CompareTo(Point3D other)
    {
        var xComparison = m_X.CompareTo(other.m_X);
        if (xComparison != 0)
        {
            return xComparison;
        }

        var yComparison = m_Y.CompareTo(other.m_Y);
        if (yComparison != 0)
        {
            return yComparison;
        }

        return m_Z.CompareTo(other.m_Z);
    }

    public int CompareTo(IPoint3D other)
    {
        var xComparison = m_X.CompareTo(other.X);
        if (xComparison != 0)
        {
            return xComparison;
        }

        var yComparison = m_Y.CompareTo(other.Y);
        if (yComparison != 0)
        {
            return yComparison;
        }

        return m_Z.CompareTo(other.Z);
    }
}
