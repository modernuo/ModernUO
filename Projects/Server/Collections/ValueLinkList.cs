/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ValueLinkList.cs                                                *
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

namespace Server.Collections;

public interface IValueLinkListNode<T> where T : class
{
    public T Next { get; set; }
    public T Previous { get; set; }
    public bool OnLinkList { get; set; }
}

public struct ValueLinkList<T> where T : class, IValueLinkListNode<T>
{
    public int Count { get; internal set; }
    internal T _first;
    internal T _last;

    public int Version { get; private set; }

    public void Remove(T node)
    {
        if (node == null)
        {
            return;
        }

        if (!node.OnLinkList)
        {
            throw new ArgumentException("Attempted to remove a node that is not on the list.");
        }

        if (node.Previous == null)
        {
            // If previous is null, then it is the first element.
            if (_first != node)
            {
                throw new ArgumentException("Attempted to remove a node that is not on the list.");
            }

            if (_first == _last)
            {
                _last = null;
                _first = null;
            }
            else
            {
                _first = node.Next;
            }

            if (node.Next != null)
            {
                node.Next.Previous = null;
            }
        }
        else
        {
            node.Previous.Next = node.Next;

            // If next is null, then it is the last element.
            if (node.Next == null)
            {
                _last = node.Previous;
            }
            else
            {
                node.Next.Previous = node.Previous;
            }
        }

        node.Next = null;
        node.Previous = null;
        node.OnLinkList = false;
        Count--;
        Version++;

        if (Count < 0)
        {
            throw new Exception("Count is negative!");
        }
    }

    // Remove all entries before this node, not including this node.
    public void RemoveAllBefore(T e)
    {
        if (e == null)
        {
            return;
        }

        if (!e.OnLinkList)
        {
            throw new ArgumentException("Attempted to remove nodes before a node that is not on the list.");
        }

        if (e.Previous == null)
        {
            return;
        }

        var current = e.Previous;
        e.Previous = null;

        while (current != null)
        {
            var previous = current.Previous;

            current.OnLinkList = false;
            current.Next = null;
            current.Previous = null;
            Count--;

            if (Count < 0)
            {
                throw new Exception("Count is negative!");
            }

            current = previous;
        }

        _first = e;
        Version++;
    }

    // Remove all entries after this node, not including this node.
    public void RemoveAllAfter(T e)
    {
        if (e == null)
        {
            return;
        }

        if (!e.OnLinkList)
        {
            throw new ArgumentException("Attempted to remove nodes after a node that is not on the list.");
        }

        if (e.Next == null)
        {
            return;
        }

        var current = e.Next;
        e.Next = null;

        while (current != null)
        {
            var next = current.Next;

            current.OnLinkList = false;
            current.Next = null;
            current.Previous = null;
            Count--;

            if (Count < 0)
            {
                throw new Exception("Count is negative!");
            }

            current = next;
        }

        _last = e;
        Version++;
    }

    public void AddLast(T e)
    {
        if (e == null)
        {
            return;
        }

        if (e.OnLinkList)
        {
            throw new ArgumentException("Attempted to add a node that is already on a list.");
        }

        if (_last != null)
        {
            AddAfter(_last, e);
        }
        else
        {
            _first = e;
            _last = e;
            Count = 1;
            Version++;
            e.OnLinkList = true;
        }
    }

    public void AddFirst(T e)
    {
        if (e == null)
        {
            return;
        }

        if (e.OnLinkList)
        {
            throw new ArgumentException("Attempted to add a node that is already on a list.");
        }

        if (_first != null)
        {
            AddBefore(_first, e);
        }
        else
        {
            _first = e;
            _last = e;
            Count = 1;
            Version++;
            e.OnLinkList = true;
        }
    }

    public void AddBefore(T existing, T node)
    {
        if (node == null)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(existing);

        if (!existing.OnLinkList)
        {
            throw new ArgumentException($"Argument '{nameof(existing)}' must be a node on a list.");
        }

        if (node.OnLinkList)
        {
            throw new ArgumentException("Attempted to add a node that is already on a list.");
        }

        node.Next = existing;
        node.Previous = existing.Previous;

        if (existing.Previous != null)
        {
            existing.Previous.Next = node;
        }
        else
        {
            _first = node;
        }

        existing.Previous = node;
        node.OnLinkList = true;
        Count++;
        Version++;
    }

    public void AddAfter(T existing, T node)
    {
        if (node == null)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(existing);

        if (!existing.OnLinkList)
        {
            throw new ArgumentException($"Argument '{nameof(existing)}' must be a node on a list.");
        }

        if (node.OnLinkList)
        {
            throw new ArgumentException("Attempted to add a node that is already on a list.");
        }

        node.Previous = existing;
        node.Next = existing.Next;

        if (existing.Next != null)
        {
            existing.Next.Previous = node;
        }
        else
        {
            _last = node;
        }

        existing.Next = node;
        node.OnLinkList = true;
        Count++;
        Version++;
    }

    public void RemoveAll()
    {
        var current = _first;
        while (current != null)
        {
            var next = current.Next;

            current.OnLinkList = false;
            current.Next = null;
            current.Previous = null;
            current = next;
        }

        _first = null;
        _last = null;
        Count = 0;
        Version++;
    }

    public void AddLast(ref ValueLinkList<T> otherList, T start, T end)
    {
        // Should we check if start and end actually exist on the other list?
        if (otherList.Count == 0 || otherList.Count == 1 && (start != end || otherList._first != start))
        {
            throw new ArgumentException("Attempted to add nodes that are not on the specified linklist.");
        }

        if (start.Previous != null)
        {
            start.Previous.Next = end.Next;
        }
        else
        {
            // Start is first
            otherList._first = end.Next;
        }

        if (end.Next != null)
        {
            end.Next.Previous = start.Previous;
        }
        else
        {
            otherList._last = start.Previous;
        }

        var count = 1;
        var current = start;

        // Assume start and end are in the right order, or bad things happen (crash).
        while (current != end)
        {
            count++;
            current = current.Next;
        }

        otherList.Count -= count;

        if (otherList.Count < 0)
        {
            throw new Exception("Count is negative!");
        }

        if (_last != null)
        {
            _last.Next = start;
            start.Previous = _last;
        }
        else
        {
            _first = start;
        }

        _last = end;
        Count += count;
        Version++;
    }

    public T[] ToArray()
    {
        var arr = new T[Count];

        var index = 0;
        foreach (var t in this)
        {
            arr[index++] = t;
        }

        return arr;
    }

    public ref struct ValueListEnumerator
    {
        private bool _started;
        private T _current;
        private ref readonly ValueLinkList<T> _linkList;
        private int _version;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueListEnumerator(in ValueLinkList<T> linkList)
        {
            _linkList = ref linkList;
            _started = false;
            _current = null;
            _version = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_started)
            {
                _current = _linkList._first;
                _started = true;
                _version = _linkList.Version;
            }
            else if (_linkList.Version != _version)
            {
                throw new InvalidOperationException(CollectionThrowStrings.InvalidOperation_EnumFailedVersion);
            }
            else
            {
                _current = _current.Next;
            }

            return _current != null;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }

    public ref struct DescendingValueListEnumerator
    {
        private bool _started;
        private T _current;
        private ref readonly ValueLinkList<T> _linkList;
        private int _version;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DescendingValueListEnumerator(in ValueLinkList<T> linkList)
        {
            _linkList = ref linkList;
            _started = false;
            _current = null;
            _version = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_started)
            {
                _current = _linkList._last;
                _started = true;
                _version = _linkList.Version;
            }
            else if (_linkList.Version != _version)
            {
                throw new InvalidOperationException(CollectionThrowStrings.InvalidOperation_EnumFailedVersion);
            }
            else
            {
                _current = _current.Previous;
            }

            return _current != null;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DescendingValueListEnumerator GetEnumerator() => this;
    }
}

public static class ValueLinkListExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueLinkList<T>.ValueListEnumerator GetEnumerator<T>(this in ValueLinkList<T> linkList)
        where T : class, IValueLinkListNode<T> => new(in linkList);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueLinkList<T>.DescendingValueListEnumerator ByDescending<T>(this in ValueLinkList<T> linkList)
        where T : class, IValueLinkListNode<T> => new(in linkList);
}
