/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Rectangle2D.cs                                                  *
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

[NoSort]
[Parsable]
[PropertyObject]
public struct Rectangle2D
{
    public bool Equals(Rectangle2D other) => m_Start == other.m_Start && m_End == other.m_End;

    public static bool operator ==(Rectangle2D l, Rectangle2D r) => l.m_Start == r.m_Start && l.m_End == r.m_End;

    public static bool operator !=(Rectangle2D l, Rectangle2D r) => l.m_Start != r.m_Start || l.m_End != r.m_End;

    public override int GetHashCode() => HashCode.Combine(m_Start, m_End);

    private Point2D m_Start;
    private Point2D m_End;

    public Rectangle2D(Point2D start, Point2D end)
    {
        m_Start = start;
        m_End = end;
    }

    public Rectangle2D(int x, int y, int width, int height)
    {
        m_Start = new Point2D(x, y);
        m_End = new Point2D(x + width, y + height);
    }

    public void Set(int x, int y, int width, int height)
    {
        m_Start = new Point2D(x, y);
        m_End = new Point2D(x + width, y + height);
    }

    public static Rectangle2D Parse(string value)
    {
        var start = value.IndexOfOrdinal('(');
        var end = value.IndexOf(',', start + 1);

        Utility.ToInt32(value.AsSpan(start + 1, end - (start + 1)).Trim(), out var x);

        start = end;
        end = value.IndexOf(',', start + 1);

        Utility.ToInt32(value.AsSpan(start + 1, end - (start + 1)).Trim(), out var y);

        start = end;
        end = value.IndexOf(',', start + 1);

        Utility.ToInt32(value.AsSpan(start + 1, end - (start + 1)).Trim(), out var w);

        start = end;
        end = value.IndexOf(')', start + 1);

        Utility.ToInt32(value.AsSpan(start + 1, end - (start + 1)).Trim(), out var h);

        return new Rectangle2D(x, y, w, h);
    }

    [CommandProperty(AccessLevel.Counselor)]
    public Point2D Start
    {
        get => m_Start;
        set => m_Start = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public Point2D End
    {
        get => m_End;
        set => m_End = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int X
    {
        get => m_Start.m_X;
        set => m_Start.m_X = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int Y
    {
        get => m_Start.m_Y;
        set => m_Start.m_Y = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int Width
    {
        get => m_End.m_X - m_Start.m_X;
        set => m_End.m_X = m_Start.m_X + value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int Height
    {
        get => m_End.m_Y - m_Start.m_Y;
        set => m_End.m_Y = m_Start.m_Y + value;
    }

    public void MakeHold(Rectangle2D r)
    {
        if (r.m_Start.m_X < m_Start.m_X)
        {
            m_Start.m_X = r.m_Start.m_X;
        }

        if (r.m_Start.m_Y < m_Start.m_Y)
        {
            m_Start.m_Y = r.m_Start.m_Y;
        }

        if (r.m_End.m_X > m_End.m_X)
        {
            m_End.m_X = r.m_End.m_X;
        }

        if (r.m_End.m_Y > m_End.m_Y)
        {
            m_End.m_Y = r.m_End.m_Y;
        }
    }

    public readonly bool Contains(Point3D p) =>
        m_Start.m_X <= p.m_X && m_Start.m_Y <= p.m_Y && m_End.m_X > p.m_X && m_End.m_Y > p.m_Y;

    public readonly bool Contains(Point2D p) =>
        m_Start.m_X <= p.m_X && m_Start.m_Y <= p.m_Y && m_End.m_X > p.m_X && m_End.m_Y > p.m_Y;

    public readonly bool Contains(int x, int y) =>
        m_Start.m_X <= x && m_Start.m_Y <= y && m_End.m_X > x && m_End.m_Y > y;

    public override string ToString() => $"({X}, {Y})+({Width}, {Height})";
}
