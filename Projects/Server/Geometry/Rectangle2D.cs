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
using System.Runtime.CompilerServices;

namespace Server;

[NoSort]
[PropertyObject]
public struct Rectangle2D : IEquatable<Rectangle2D>, ISpanFormattable, ISpanParsable<Rectangle2D>
{
    private Point2D _start;
    private Point2D _end;

    public Rectangle2D(Point2D start, Point2D end)
    {
        _start = start;
        _end = end;
    }

    public Rectangle2D(int x, int y, int width, int height)
    {
        _start = new Point2D(x, y);
        _end = new Point2D(x + width, y + height);
    }

    public void Set(int x, int y, int width, int height)
    {
        _start = new Point2D(x, y);
        _end = new Point2D(x + width, y + height);
    }

    [CommandProperty(AccessLevel.Counselor)]
    public Point2D Start
    {
        get => _start;
        set => _start = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public Point2D End
    {
        get => _end;
        set => _end = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int X
    {
        get => _start.m_X;
        set => _start.m_X = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int Y
    {
        get => _start.m_Y;
        set => _start.m_Y = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int Width
    {
        get => _end.m_X - _start.m_X;
        set => _end.m_X = _start.m_X + value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int Height
    {
        get => _end.m_Y - _start.m_Y;
        set => _end.m_Y = _start.m_Y + value;
    }

    public void MakeHold(Rectangle2D r)
    {
        if (r._start.m_X < _start.m_X)
        {
            _start.m_X = r._start.m_X;
        }

        if (r._start.m_Y < _start.m_Y)
        {
            _start.m_Y = r._start.m_Y;
        }

        if (r._end.m_X > _end.m_X)
        {
            _end.m_X = r._end.m_X;
        }

        if (r._end.m_Y > _end.m_Y)
        {
            _end.m_Y = r._end.m_Y;
        }
    }

    public bool Equals(Rectangle2D other) => _start == other._start && _end == other._end;

    public override bool Equals(object obj) => obj is Rectangle2D other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_start, _end);

    public static bool operator ==(Rectangle2D l, Rectangle2D r) => l._start == r._start && l._end == r._end;

    public static bool operator !=(Rectangle2D l, Rectangle2D r) => l._start != r._start || l._end != r._end;

    public bool Contains(Point3D p) =>
        _start.m_X <= p.m_X && _start.m_Y <= p.m_Y && _end.m_X > p.m_X && _end.m_Y > p.m_Y;

    public bool Contains(Point2D p) =>
        _start.m_X <= p.m_X && _start.m_Y <= p.m_Y && _end.m_X > p.m_X && _end.m_Y > p.m_Y;

    public bool Contains(int x, int y) =>
        _start.m_X <= x && _start.m_Y <= y && _end.m_X > x && _end.m_Y > y;

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
        => destination.TryWrite(provider, $"({X}, {Y})+({Width}, {Height})", out charsWritten);

    public override string ToString()
    {
        // Maximum number of characters that are needed to represent this:
        // 9 characters for (, )+(, )
        // Up to 11 characters to represent each integer
        const int maxLength = 9 + 11 * 4;
        Span<char> span = stackalloc char[maxLength];
        TryFormat(span, out var charsWritten, null, null);
        return span[..charsWritten].ToString();
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
        // format and formatProvider are not doing anything right now, so use the
        // default ToString implementation.
        return ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rectangle2D Parse(string s) => Parse(s, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rectangle2D Parse(string s, IFormatProvider provider) => Parse(s.AsSpan(), provider);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(string s, IFormatProvider provider, out Rectangle2D result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static Rectangle2D Parse(ReadOnlySpan<char> s, IFormatProvider provider)
    {
        s = s.Trim();

        var delimiter = s.IndexOfOrdinal('+');
        if (delimiter == -1)
        {
            throw new FormatException($"The input string '{s}' was not in a correct format.");
        }

        if (!Point2D.TryParse(s[..delimiter], provider, out var start))
        {
            throw new FormatException($"The input string '{s}' was not in a correct format.");
        }

        if (!Point2D.TryParse(s[(delimiter + 1)..], provider, out var end))
        {
            throw new FormatException($"The input string '{s}' was not in a correct format.");
        }

        return new Rectangle2D(start, end);
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out Rectangle2D result)
    {
        s = s.Trim();

        var delimiter = s.IndexOfOrdinal('+');
        if (delimiter == -1)
        {
            result = default;
            return false;
        }

        if (!Point2D.TryParse(s[..delimiter], provider, out var start))
        {
            result = default;
            return false;
        }

        if (!Point2D.TryParse(s[(delimiter + 1)..], provider, out var end))
        {
            result = default;
            return false;
        }

        result = new Rectangle2D(start, end);
        return true;
    }
}
