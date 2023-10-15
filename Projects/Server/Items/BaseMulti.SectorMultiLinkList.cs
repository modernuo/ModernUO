/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BaseMulti.SectorMultiLinkList.cs                                *
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

namespace Server.Items;

// Adds support for the specific value link list on sectors for multis, separate from items
public partial class BaseMulti : BaseMulti.ISectorMultiLinkListNode<BaseMulti>
{
    public interface ISectorMultiLinkListNode<T> where T : class
    {
        public T SectorMultiNext { get; set; }
        public T SectorMultiPrevious { get; set; }
        public bool OnSectorMultiLinkList { get; set; }
    }

    public struct SectorMultiLinkList<T> where T : class, ISectorMultiLinkListNode<T>
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

            if (!node.OnSectorMultiLinkList)
            {
                throw new ArgumentException("Attempted to remove a node that is not on the list.");
            }

            if (node.SectorMultiPrevious == null)
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
                    First = node.SectorMultiNext;
                }

                if (node.SectorMultiNext != null)
                {
                    node.SectorMultiNext.SectorMultiPrevious = null;
                }
            }
            else
            {
                node.SectorMultiPrevious.SectorMultiNext = node.SectorMultiNext;

                // If next is null, then it is the last element.
                if (node.SectorMultiNext == null)
                {
                    Last = node.SectorMultiPrevious;
                }
                else
                {
                    node.SectorMultiNext.SectorMultiPrevious = node.SectorMultiPrevious;
                }
            }

            node.SectorMultiNext = null;
            node.SectorMultiPrevious = null;
            node.OnSectorMultiLinkList = false;
            Count--;
        }

        // Remove all entries before this node, not including this node.
        public void RemoveAllBefore(T e)
        {
            if (e == null)
            {
                return;
            }

            if (!e.OnSectorMultiLinkList)
            {
                throw new ArgumentException("Attempted to remove nodes before a node that is not on the list.");
            }

            if (e.SectorMultiPrevious == null)
            {
                return;
            }

            var current = e.SectorMultiPrevious;
            e.SectorMultiPrevious = null;

            while (current != null)
            {
                var previous = current.SectorMultiPrevious;

                current.OnSectorMultiLinkList = false;
                current.SectorMultiNext = null;
                current.SectorMultiPrevious = null;
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

            if (!e.OnSectorMultiLinkList)
            {
                throw new ArgumentException("Attempted to remove nodes after a node that is not on the list.");
            }

            if (e.SectorMultiNext == null)
            {
                return;
            }

            var current = e.SectorMultiNext;
            e.SectorMultiNext = null;

            while (current != null)
            {
                var next = current.SectorMultiNext;

                current.OnSectorMultiLinkList = false;
                current.SectorMultiNext = null;
                current.SectorMultiPrevious = null;
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

            if (e.OnSectorMultiLinkList)
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

            e.OnSectorMultiLinkList = true;
        }

        public void AddFirst(T e)
        {
            if (e == null)
            {
                return;
            }

            if (e.OnSectorMultiLinkList)
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

            e.OnSectorMultiLinkList = true;
        }

        public void AddBefore(T existing, T node)
        {
            if (node == null)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(existing);

            if (!existing.OnSectorMultiLinkList)
            {
                throw new ArgumentException($"Argument '{nameof(existing)}' must be a node on a list.");
            }

            if (node.OnSectorMultiLinkList)
            {
                throw new ArgumentException("Attempted to add a node that is already on a list.");
            }

            node.SectorMultiNext = existing;
            node.SectorMultiPrevious = existing.SectorMultiPrevious;

            if (existing.SectorMultiPrevious != null)
            {
                existing.SectorMultiPrevious.SectorMultiNext = node;
            }
            else
            {
                First = node;
            }

            existing.SectorMultiPrevious = node;
            node.OnSectorMultiLinkList = true;
            Count++;
        }

        public void AddAfter(T existing, T node)
        {
            if (node == null)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(existing);

            if (!existing.OnSectorMultiLinkList)
            {
                throw new ArgumentException($"Argument '{nameof(existing)}' must be a node on a list.");
            }

            if (node.OnSectorMultiLinkList)
            {
                throw new ArgumentException("Attempted to add a node that is already on a list.");
            }

            node.SectorMultiPrevious = existing;
            node.SectorMultiNext = existing.SectorMultiNext;

            if (existing.SectorMultiNext != null)
            {
                existing.SectorMultiNext.SectorMultiPrevious = node;
            }
            else
            {
                Last = node;
            }

            existing.SectorMultiNext = node;
            node.OnSectorMultiLinkList = true;
            Count++;
        }

        public void RemoveAll()
        {
            var current = First;
            while (current != null)
            {
                var next = current.SectorMultiNext;

                current.OnSectorMultiLinkList = false;
                current.SectorMultiNext = null;
                current.SectorMultiPrevious = null;
                current = next;
            }

            First = null;
            Last = null;
            Count = 0;
        }

        public void AddLast(ref SectorMultiLinkList<T> otherList, T start, T end)
        {
            // Should we check if start and end actually exist on the other list?
            if (otherList.Count == 0 || otherList.Count == 1 && (start != end || otherList.First != start))
            {
                throw new ArgumentException("Attempted to add nodes that are not on the specified linklist.");
            }

            if (start.SectorMultiPrevious != null)
            {
                start.SectorMultiPrevious.SectorMultiNext = end.SectorMultiNext;
            }
            else
            {
                // Start is first
                otherList.First = end.SectorMultiNext;
            }

            if (end.SectorMultiNext != null)
            {
                end.SectorMultiNext.SectorMultiPrevious = start.SectorMultiPrevious;
            }
            else
            {
                otherList.Last = start.SectorMultiPrevious;
            }

            var count = 1;
            var current = start;

            // Assume start and end are in the right order, or bad things happen (crash).
            while (current != end)
            {
                count++;
                current = current.SectorMultiNext;
            }

            otherList.Count -= count;

            if (Last != null)
            {
                Last.SectorMultiNext = start;
                start.SectorMultiPrevious = Last;
            }
            else
            {
                First = start;
            }

            Last = end;
            Count += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SectorMultiListEnumerator GetEnumerator() => new(First);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DescendingSectorMultiListEnumerator ByDescending() => new(Last);

        public ref struct SectorMultiListEnumerator
        {
            private T _head;
            private T _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public SectorMultiListEnumerator(T head)
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
                    _current = _current.SectorMultiNext;
                }

                return _current != null;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }
        }

        public ref struct DescendingSectorMultiListEnumerator
        {
            private T _tail;
            private T _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DescendingSectorMultiListEnumerator(T head)
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
                    _current = _current.SectorMultiPrevious;
                }

                return _current != null;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DescendingSectorMultiListEnumerator GetEnumerator() => this;
        }
    }

    // Sectors, specifically for multis
    public BaseMulti SectorMultiNext { get; set; }
    public BaseMulti SectorMultiPrevious { get; set; }
    public bool OnSectorMultiLinkList { get; set; }
}
