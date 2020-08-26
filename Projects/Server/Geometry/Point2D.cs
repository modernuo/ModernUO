/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: Point2D.cs - Created: 2020/05/31 - Updated: 2020/05/31          *
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
    public struct Point2D
        : IPoint2D, IComparable<Point2D>, IComparable<IPoint2D>, IEquatable<object>, IEquatable<Point2D>,
            IEquatable<IPoint2D>
    {
        internal int m_X;
        internal int m_Y;

        public static readonly Point2D Zero = new Point2D(0, 0);

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

        public Point2D(IPoint2D p) : this(p.X, p.Y)
        {
        }

        public override string ToString() => $"({m_X}, {m_Y})";

        public static Point2D Parse(string value)
        {
            var start = value.IndexOf('(');
            var end = value.IndexOf(',', start + 1);

            Utility.ToInt32(value.Substring(start + 1, end - (start + 1)).Trim(), out var x);

            start = end;
            end = value.IndexOf(')', start + 1);

            Utility.ToInt32(value.Substring(start + 1, end - (start + 1)).Trim(), out var y);

            return new Point2D(x, y);
        }

        public bool Equals(Point2D other) => m_X == other.m_X && m_Y == other.m_Y;

        public bool Equals(IPoint2D other) =>
            !ReferenceEquals(other, null) && m_X == other.X && m_Y == other.Y;

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
            if (xComparison != 0) return xComparison;
            return m_Y.CompareTo(other.m_Y);
        }

        public int CompareTo(IPoint2D other)
        {
            var xComparison = m_X.CompareTo(other.X);
            if (xComparison != 0) return xComparison;
            return m_Y.CompareTo(other.Y);
        }
    }
}
