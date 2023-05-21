/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CircularBuffer.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace System.Buffers;

public readonly ref struct CircularBuffer<T>
{
    private readonly Span<T> _first;
    private readonly Span<T> _second;

    public int Length { get; }

    public CircularBuffer(ArraySegment<T>[] buffers) : this(buffers[0], buffers[1])
    {
    }

    public CircularBuffer(Span<T> first, Span<T> second)
    {
        _first = first;
        _second = second;
        Length = first.Length + second.Length;
    }

    public T this[int index]
    {
        get
        {
            if (index < 0 || index > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return index < _first.Length ? _first[index] : _second[index - _first.Length];
        }
        set
        {
            if (index < 0 || index > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (index < _first.Length)
            {
                _first[index] = value;
            }
            else
            {
                _second[index - _first.Length] = value;
            }
        }
    }

    public void CopyFrom(ReadOnlySpan<T> bytes)
    {
        var remaining = bytes.Length;
        var offset = 0;

        if (remaining == 0)
        {
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            var buffer = i == 0 ? _first : _second;

            if (buffer.Length == 0)
            {
                continue;
            }

            var sz = Math.Min(remaining, buffer.Length);
            bytes.Slice(offset, sz).CopyTo(buffer);

            remaining -= sz;
            offset += sz;

            if (remaining == 0)
            {
                return;
            }
        }

        throw new OutOfMemoryException();
    }

    public void CopyTo(Span<T> bytes)
    {
        if (bytes.Length < Length)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes));
        }

        if (_first.Length > 0)
        {
            _first.CopyTo(bytes);
        }

        if (_second.Length > 0)
        {
            _second.CopyTo(bytes[_first.Length..]);
        }
    }

    public CircularBuffer<T> Slice(int offset, int count)
    {
        var firstCount = Math.Min(count, _first.Length - offset);
        var first = offset < _first.Length
            ? _first.Slice(offset, firstCount)
            : Span<T>.Empty;

        var secondCount = offset > _first.Length ? count : count - firstCount;
        var second = secondCount > 0 ? _second.Slice(Math.Max(0, offset - _first.Length), secondCount) : Span<T>.Empty;

        return new CircularBuffer<T>(first, second);
    }

    public Span<T> GetSpan(int index)
    {
        if (index is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return index == 0 ? _first : _second;
    }
}
