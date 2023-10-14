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
    public T First { get; internal set; }
    public T Last { get; internal set; }

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
            if (First != node)
            {
                throw new ArgumentException("Attempted to remove a node that is not on the list.");
            }

            if (First == Last)
            {
                Last = null;
                First = null;
            }
            else
            {
                First = node.Next;
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
                Last = node.Previous;
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

            current = previous;
        }

        First = e;
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

            current = next;
        }

        Last = e;
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

        if (Last != null)
        {
            AddAfter(Last, e);
        }
        else
        {
            First = e;
            Last = e;
            Count++;
        }

        e.OnLinkList = true;
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

        if (First != null)
        {
            AddBefore(First, e);
        }
        else
        {
            First = e;
            Last = e;
            Count++;
        }

        e.OnLinkList = true;
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
            First = node;
        }

        existing.Previous = node;
        node.OnLinkList = true;
        Count++;
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
            Last = node;
        }

        existing.Next = node;
        node.OnLinkList = true;
        Count++;
    }

    public void RemoveAll()
    {
        var current = First;
        while (current != null)
        {
            var next = current.Next;

            current.OnLinkList = false;
            current.Next = null;
            current.Previous = null;
            current = next;
        }

        First = null;
        Last = null;
        Count = 0;
    }

    public void AddLast(ref ValueLinkList<T> otherList, T start, T end)
    {
        // Should we check if start and end actually exist on the other list?
        if (otherList.Count == 0 || otherList.Count == 1 && (start != end || otherList.First != start))
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
            otherList.First = end.Next;
        }

        if (end.Next != null)
        {
            end.Next.Previous = start.Previous;
        }
        else
        {
            otherList.Last = start.Previous;
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

        if (Last != null)
        {
            Last.Next = start;
            start.Previous = Last;
        }
        else
        {
            First = start;
        }

        Last = end;
        Count += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueListEnumerator GetEnumerator() => new(First);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DescendingValueListEnumerator ByDescending() => new(Last);

    public ref struct ValueListEnumerator
    {
        private T _head;
        private T _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueListEnumerator(T head)
        {
            _head = head;
            _current = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_current == null)
            {
                _current = _head;
                _head = null;
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
        private T _tail;
        private T _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DescendingValueListEnumerator(T head)
        {
            _tail = head;
            _current = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_current == null)
            {
                _current = _tail;
                _tail = null;
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
