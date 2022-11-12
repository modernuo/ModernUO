/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Rectangle3D.cs                                                  *
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
public struct Rectangle3D : IEquatable<Rectangle3D>, ISpanFormattable
{
    private Point3D _start;
    private Point3D _end;

    public Rectangle3D(Point3D start, Point3D end)
    {
        _start = start;
        _end = end;
    }

    public Rectangle3D(int x, int y, int z, int width, int height, int depth)
    {
        _start = new Point3D(x, y, z);
        _end = new Point3D(x + width, y + height, z + depth);
    }

    [CommandProperty(AccessLevel.Counselor)]
    public Point3D Start
    {
        get => _start;
        set => _start = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public Point3D End
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
    public int Z
    {
        get => _start.m_Z;
        set => _start.m_Z = value;
    }

    [CommandProperty(AccessLevel.Counselor)]
    public int Width => _end.X - _start.X;

    [CommandProperty(AccessLevel.Counselor)]
    public int Height => _end.Y - _start.Y;

    [CommandProperty(AccessLevel.Counselor)]
    public int Depth => _end.Z - _start.Z;

    public bool Equals(Rectangle3D other) => _start == other._start && _end == other._end;

    public override bool Equals(object obj) => obj is Rectangle3D other && Equals(other);

    public static bool operator ==(Rectangle3D l, Rectangle3D r) => l._start == r._start && l._end == r._end;

    public static bool operator !=(Rectangle3D l, Rectangle3D r) => l._start != r._start || l._end != r._end;

    public override int GetHashCode() => HashCode.Combine(_start, _end);

    public void MakeHold(Rectangle3D r)
    {
        if (r._start.m_X < _start.m_X)
        {
            _start.m_X = r._start.m_X;
        }

        if (r._start.m_Y < _start.m_Y)
        {
            _start.m_Y = r._start.m_Y;
        }

        if (r._start.m_Z < _start.m_Z)
        {
            _start.m_Z = r._start.m_Z;
        }

        if (r._end.m_X > _end.m_X)
        {
            _end.m_X = r._end.m_X;
        }

        if (r._end.m_Y > _end.m_Y)
        {
            _end.m_Y = r._end.m_Y;
        }

        if (r._end.m_Z < _end.m_Z)
        {
            _end.m_Z = r._end.m_Z;
        }
    }

    public bool Contains(Point3D p) =>
        p.m_X >= _start.m_X
        && p.m_X < _end.m_X
        && p.m_Y >= _start.m_Y
        && p.m_Y < _end.m_Y
        && p.m_Z >= _start.m_Z
        && p.m_Z < _end.m_Z;

    public bool Contains(Point2D p) =>
        p.m_X >= _start.m_X
        && p.m_X < _end.m_X
        && p.m_Y >= _start.m_Y
        && p.m_Y < _end.m_Y;

    public bool Contains(IPoint2D p) =>
        p.X >= _start.m_X
        && p.X < _end.m_X
        && p.Y >= _start.m_Y
        && p.Y < _end.m_Y;

    public bool Contains(IPoint3D p) =>
        p.X >= _start.m_X
        && p.X < _end.m_X
        && p.Y >= _start.m_Y
        && p.Y < _end.m_Y
        && p.Z >= _start.m_Z
        && p.Z < _end.m_Z;

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
        => destination.TryWrite(provider, $"({X}, {Y}, {Z})+({Width}, {Height}, {Depth})", out charsWritten);

    public override string ToString()
    {
        // Maximum number of characters that are needed to represent this:
        // 13 characters for (, , )+(, , )
        // Up to 11 characters to represent each integer
        const int maxLength = 13 + 11 * 6;
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
    public static Rectangle3D Parse(string s) => Parse(s, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rectangle3D Parse(string s, IFormatProvider provider) => Parse(s.AsSpan(), provider);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(string s, IFormatProvider provider, out Rectangle3D result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static Rectangle3D Parse(ReadOnlySpan<char> s, IFormatProvider provider)
    {
        s = s.Trim();

        if (!s.StartsWithOrdinal('(') || !s.EndsWithOrdinal(')'))
        {
            throw new FormatException($"The input string '{s}' was not in a correct format.");
        }

        var firstComma = s.IndexOfOrdinal(',');
        if (firstComma == -1)
        {
            throw new FormatException($"The input string '{s}' was not in a correct format.");
        }

        var secondComma = s[(firstComma + 1)..].IndexOfOrdinal(',');
        if (secondComma == -1)
        {
            throw new FormatException($"The input string '{s}' was not in a correct format.");
        }

        var thirdComma = s[(secondComma + 1)..].IndexOfOrdinal(',');
        if (thirdComma == -1)
        {
            throw new FormatException($"The input string '{s}' was not in a correct format.");
        }

        var fourthComma = s[(thirdComma + 1)..].IndexOfOrdinal(',');
        if (fourthComma == -1)
        {
            throw new FormatException($"The input string '{s}' was not in a correct format.");
        }

        var fifthComma = s[(fourthComma + 1)..].IndexOfOrdinal(',');
        if (fifthComma == -1)
        {
            throw new FormatException($"The input string '{s}' was not in a correct format.");
        }

        var x = s.Slice(1, firstComma - 1).Trim();
        var y = s.Slice(firstComma + 1, secondComma - firstComma - 1).Trim();
        var z = s.Slice(secondComma + 1, thirdComma - secondComma - 1).Trim();
        var w = s.Slice(thirdComma + 1, fourthComma - thirdComma - 1).Trim();
        var h = s.Slice(fourthComma + 1, fifthComma - fourthComma - 1).Trim();
        var d = s.Slice(fifthComma + 1, s.Length - fifthComma - 2).Trim();

        return new Rectangle3D(
            Utility.ToInt32(x),
            Utility.ToInt32(y),
            Utility.ToInt32(z),
            Utility.ToInt32(w),
            Utility.ToInt32(h),
            Utility.ToInt32(d)
        );
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out Rectangle3D result)
    {
        s = s.Trim();

        if (!s.StartsWithOrdinal('(') || !s.EndsWithOrdinal(')'))
        {
            result = default;
            return false;
        }

        var firstComma = s.IndexOfOrdinal(',');
        if (firstComma == -1)
        {
            result = default;
            return false;
        }

        var secondComma = s[(firstComma + 1)..].IndexOfOrdinal(',');
        if (secondComma == -1)
        {
            result = default;
            return false;
        }

        var thirdComma = s[(secondComma + 1)..].IndexOfOrdinal(',');
        if (thirdComma == -1)
        {
            result = default;
            return false;
        }

        var fourthComma = s[(thirdComma + 1)..].IndexOfOrdinal(',');
        if (fourthComma == -1)
        {
            result = default;
            return false;
        }

        var fifthComma = s[(fourthComma + 1)..].IndexOfOrdinal(',');
        if (fifthComma == -1)
        {
            result = default;
            return false;
        }

        var first = s.Slice(1, firstComma - 1).Trim();
        if (!Utility.ToInt32(first, out var x))
        {
            result = default;
            return false;
        }
        var second = s.Slice(firstComma + 1, secondComma - firstComma - 1).Trim();
        if (!Utility.ToInt32(second, out var y))
        {
            result = default;
            return false;
        }

        var third = s.Slice(secondComma + 1, thirdComma - secondComma - 1).Trim();
        if (!Utility.ToInt32(third, out var z))
        {
            result = default;
            return false;
        }

        var fourth = s.Slice(thirdComma + 1, fourthComma - thirdComma - 1).Trim();
        if (!Utility.ToInt32(fourth, out var w))
        {
            result = default;
            return false;
        }

        var fifth = s.Slice(fourthComma + 1, fifthComma - fourthComma - 1).Trim();
        if (!Utility.ToInt32(fifth, out var h))
        {
            result = default;
            return false;
        }
        var sixth = s.Slice(fifthComma + 1, s.Length - fifthComma - 2).Trim();
        if (!Utility.ToInt32(sixth, out var d))
        {
            result = default;
            return false;
        }

        result = new Rectangle3D(x, y, z, w, h, d);
        return true;
    }
}
