/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: FixedLengthList.cs                                              *
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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Server.Collections;

[CollectionBuilder(typeof(FixedLengthListExtensions), "Create")]
public class FixedLengthList<T> : IList<T>
{
    private readonly T[] _buffer;
    private int _head;  // Points to the next insert position
    private int _count; // Current number of elements

    public int Capacity => _buffer.Length;
    public int Count => _count;
    public bool IsReadOnly => false;

    private int StartIndex
    {
        get
        {
            var startIdx = _head - _count;
            return startIdx >= 0 ? startIdx : startIdx + _buffer.Length;
        }
    }

    private int EndIndex
    {
        get
        {
            var endIdx = _head - 1;
            return endIdx >= 0 ? endIdx : endIdx + _buffer.Length;
        }
    }

    public FixedLengthList(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity, nameof(capacity));
        _buffer = capacity == 0 ? [] : new T[capacity];
    }

    public void Add(T item)
    {
        _buffer[_head] = item;
        if (++_head >= _buffer.Length)
        {
            _head = 0;
        }

        if (_count < _buffer.Length)
        {
            _count++;
        }
    }

    public void Clear()
    {
        Array.Clear(_buffer, 0, _buffer.Length);
        _head = 0;
        _count = 0;
    }

    public bool Contains(T item) => IndexOf(item) != -1;

    public int IndexOf(T item)
    {
        var startIdx = StartIndex;
        for (var i = 0; i < _count; i++)
        {
            var index = startIdx + i;
            if (index >= _buffer.Length)
            {
                index -= _buffer.Length;
            }

            if (EqualityComparer<T>.Default.Equals(_buffer[index], item))
            {
                return i;
            }
        }
        return -1;
    }

    public void Insert(int index, T item)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _count, nameof(index)); // Allow inserting at Count (appending)

        var actualIndex = StartIndex + index;
        if (actualIndex >= _buffer.Length)
        {
            actualIndex -= _buffer.Length;
        }

        // Shift elements right
        for (var i = _count == _buffer.Length ? _head : _count; i > index; i--)
        {
            var fromIndex = StartIndex + i - 1;
            if (fromIndex >= _buffer.Length)
            {
                fromIndex -= _buffer.Length;
            }

            var toIndex = StartIndex + i;
            if (toIndex >= _buffer.Length)
            {
                toIndex -= _buffer.Length;
            }

            _buffer[toIndex] = _buffer[fromIndex];
        }

        _buffer[actualIndex] = item;

        if (_count < _buffer.Length)
        {
            _count++;
            if (++_head >= _buffer.Length)
            {
                _head = 0;
            }
        }
    }

    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index == -1)
        {
            return false;
        }

        RemoveAt(index);
        return true;
    }

    public void RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _count, nameof(index));

        var actualIndex = StartIndex + index;
        if (actualIndex >= _buffer.Length)
        {
            actualIndex -= _buffer.Length;
        }

        // If removing the most recently added item, move the head backwards
        var mostRecent = EndIndex;

        if (actualIndex == mostRecent)
        {
            _head = mostRecent;
        }
        else
        {
            // Shift elements left to fill the gap
            for (var i = index; i < _count - 1; i++)
            {
                var nextIndex = actualIndex + 1;
                if (nextIndex >= _buffer.Length)
                {
                    nextIndex -= _buffer.Length;
                }

                _buffer[actualIndex] = _buffer[nextIndex];
                actualIndex = nextIndex;
            }
        }

        _count--;
        _buffer[actualIndex] = default!; // Clear the removed item
    }

    public T this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _count, nameof(index));

            var idx = StartIndex + index;
            if (idx >= _buffer.Length)
            {
                idx -= _buffer.Length;
            }

            return _buffer[idx];
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _count, nameof(index));

            var idx = StartIndex + index;
            if (idx >= _buffer.Length)
            {
                idx -= _buffer.Length;
            }

            _buffer[idx] = value;
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (arrayIndex < 0 || arrayIndex + _count > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        var span = array.AsSpan(arrayIndex);
        var startIdx = StartIndex;

        if (startIdx + _count <= _buffer.Length)
        {
            _buffer.AsSpan(startIdx, _count).CopyTo(span);
        }
        else
        {
            var firstPart = _buffer.Length - startIdx;
            _buffer.AsSpan(startIdx, firstPart).CopyTo(span);
            _buffer.AsSpan(0, _count - firstPart).CopyTo(span[firstPart..]);
        }
    }

    public Enumerator GetEnumerator(bool reverse = false) => new(_buffer, StartIndex, _count, reverse);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

    public Enumerator GetEnumerator(int offset, int length, bool reverse = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset, nameof(offset));
        ArgumentOutOfRangeException.ThrowIfNegative(length, nameof(length));
        if (offset + length > _count)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        return new Enumerator(_buffer, StartIndex + offset, length, reverse);
    }

    public struct Enumerator : IEnumerator<T>, IEnumerable<T>
    {
        private readonly T[] _buffer;
        private readonly int _startIndex;
        private readonly int _endIndex;
        private readonly bool _reverse;
        private int _index;

        public Enumerator(T[] buffer, int startIdx, int count, bool reverse = false)
        {
            if (count <= 0)
            {
                return;
            }

            _buffer = buffer;
            _reverse = reverse;

            if (reverse)
            {
                _startIndex = startIdx + count;
                if (_startIndex >= _buffer.Length)
                {
                    _startIndex -= _buffer.Length;
                }
                _endIndex = startIdx;
                // if (_endIndex < 0)
                // {
                //     _endIndex += _buffer.Length;
                // }
            }
            else
            {
                _startIndex = startIdx - 1;
                _endIndex = startIdx + count;
                if (_endIndex >= _buffer.Length)
                {
                    _endIndex -= _buffer.Length;
                }
            }

            Reset();
        }

        public bool MoveNext()
        {
            if (_index == _endIndex)
            {
                return false;
            }

            if (_reverse)
            {
                if (--_index < 0)
                {
                    _index = _buffer.Length - 1;
                }
            }
            else if (++_index == _buffer.Length)
            {
                _index = 0;
            }

            return true;
        }

        public void Reset()
        {
            _index = _startIndex;
        }

        object IEnumerator.Current => Current;

        public T Current => _buffer[_index];

        public void Dispose()
        {
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();
    }
}

public class FixedLengthListExtensions
{
    public static FixedLengthList<T> Create<T>(ReadOnlySpan<T> items)
    {
        var list = new FixedLengthList<T>(items.Length);
        for (var i = 0; i < items.Length; i++)
        {
            list.Add(items[i]);
        }

        return list;
    }
}
