/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Point2D.cs                                                      *
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

namespace Server;

[Parsable]
public struct Point2D
    : IPoint2D, IComparable<Point2D>, IComparable<IPoint2D>, IEquatable<object>, IEquatable<Point2D>,
        IEquatable<IPoint2D>
{
    internal int m_X;
    internal int m_Y;

    public static readonly Point2D Zero = new(0, 0);

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

    public Point2D(int x, int y)
    {
        m_X = x;
        m_Y = y;
    }

    public Point2D(Point2D p) : this(p.X, p.Y)
    {
    }

    public override string ToString() => $"({m_X}, {m_Y})";

    public static Point2D Parse(string value)
    {
        var start = value.IndexOfOrdinal('(');
        var end = value.IndexOf(',', start + 1);

        Utility.ToInt32(value.AsSpan(start + 1, end - (start + 1)).Trim(), out var x);

        start = end;
        end = value.IndexOf(')', start + 1);

        Utility.ToInt32(value.AsSpan(start + 1, end - (start + 1)).Trim(), out var y);

        return new Point2D(x, y);
    }

    public bool Equals(Point2D other) => m_X == other.m_X && m_Y == other.m_Y;

    public bool Equals(IPoint2D other) =>
        m_X == other?.X && m_Y == other.Y;

    public override bool Equals(object obj) => obj is Point2D other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(m_X, m_Y);

    public static bool operator ==(Point2D l, Point2D r) => l.m_X == r.m_X && l.m_Y == r.m_Y;

    public static bool operator !=(Point2D l, Point2D r) => l.m_X != r.m_X || l.m_Y != r.m_Y;

    public static bool operator ==(Point2D l, IPoint2D r) => !ReferenceEquals(r, null) && l.m_X == r.X && l.m_Y == r.Y;

    public static bool operator !=(Point2D l, IPoint2D r) => !ReferenceEquals(r, null) && (l.m_X != r.X || l.m_Y != r.Y);

    public static bool operator >(Point2D l, Point2D r) => l.m_X > r.m_X && l.m_Y > r.m_Y;

    public static bool operator >(Point2D l, IPoint2D r) => !ReferenceEquals(r, null) && l.m_X > r.X && l.m_Y > r.Y;

    public static bool operator <(Point2D l, Point2D r) => l.m_X < r.m_X && l.m_Y < r.m_Y;

    public static bool operator <(Point2D l, IPoint2D r) => !ReferenceEquals(r, null) && l.m_X < r.X && l.m_Y < r.Y;

    public static bool operator >=(Point2D l, Point2D r) => l.m_X >= r.m_X && l.m_Y >= r.m_Y;

    public static bool operator >=(Point2D l, IPoint2D r) => !ReferenceEquals(r, null) && l.m_X >= r.X && l.m_Y >= r.Y;

    public static bool operator <=(Point2D l, Point2D r) => l.m_X <= r.m_X && l.m_Y <= r.m_Y;

    public static bool operator <=(Point2D l, IPoint2D r) => !ReferenceEquals(r, null) && l.m_X <= r.X && l.m_Y <= r.Y;

    public int CompareTo(Point2D other)
    {
        var xComparison = m_X.CompareTo(other.m_X);
        return xComparison != 0 ? xComparison : m_Y.CompareTo(other.m_Y);
    }

    public int CompareTo(IPoint2D other)
    {
        var xComparison = m_X.CompareTo(other.X);
        return xComparison != 0 ? xComparison : m_Y.CompareTo(other.Y);
    }
}
