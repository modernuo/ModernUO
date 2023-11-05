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
using Server.Collections;

namespace Server.Items;

// Adds support for the specific value link list on sectors for multis, separate from items
public partial class BaseMulti
{
    // Sectors, specifically for multis
    public BaseMulti SectorMultiNext { get; set; }
    public BaseMulti SectorMultiPrevious { get; set; }
    public bool OnSectorMultiLinkList { get; set; }
}

public struct SectorMultiValueLinkList
{
    public int Count { get; internal set; }
    internal BaseMulti _first;
    internal BaseMulti _last;

    public int Version { get; private set; }

    public void Remove(BaseMulti node)
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
            // If SectorMultiPrevious is null, then it is the first element.
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
                _first = node.SectorMultiNext;
            }

            if (node.SectorMultiNext != null)
            {
                node.SectorMultiNext.SectorMultiPrevious = null;
            }
        }
        else
        {
            node.SectorMultiPrevious.SectorMultiNext = node.SectorMultiNext;

            // If SectorMultiNext is null, then it is the last element.
            if (node.SectorMultiNext == null)
            {
                _last = node.SectorMultiPrevious;
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
        Version++;

        if (Count < 0)
        {
            throw new Exception("Count is negative!");
        }
    }

    // Remove all entries before this node, not including this node.
    public void RemoveAllBefore(BaseMulti e)
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
            var SectorMultiPrevious = current.SectorMultiPrevious;

            current.OnSectorMultiLinkList = false;
            current.SectorMultiNext = null;
            current.SectorMultiPrevious = null;
            Count--;

            if (Count < 0)
            {
                throw new Exception("Count is negative!");
            }

            current = SectorMultiPrevious;
        }

        _first = e;
        Version++;
    }

    // Remove all entries after this node, not including this node.
    public void RemoveAllAfter(BaseMulti e)
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
            var SectorMultiNext = current.SectorMultiNext;

            current.OnSectorMultiLinkList = false;
            current.SectorMultiNext = null;
            current.SectorMultiPrevious = null;
            Count--;

            if (Count < 0)
            {
                throw new Exception("Count is negative!");
            }

            current = SectorMultiNext;
        }

        _last = e;
        Version++;
    }

    public void AddLast(BaseMulti e)
    {
        if (e == null)
        {
            return;
        }

        if (e.OnSectorMultiLinkList)
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
            e.OnSectorMultiLinkList = true;
        }
    }

    public void AddFirst(BaseMulti e)
    {
        if (e == null)
        {
            return;
        }

        if (e.OnSectorMultiLinkList)
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
            e.OnSectorMultiLinkList = true;
        }
    }

    public void AddBefore(BaseMulti existing, BaseMulti node)
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
            _first = node;
        }

        existing.SectorMultiPrevious = node;
        node.OnSectorMultiLinkList = true;
        Count++;
        Version++;
    }

    public void AddAfter(BaseMulti existing, BaseMulti node)
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
            _last = node;
        }

        existing.SectorMultiNext = node;
        node.OnSectorMultiLinkList = true;
        Count++;
        Version++;
    }

    public void RemoveAll()
    {
        var current = _first;
        while (current != null)
        {
            var SectorMultiNext = current.SectorMultiNext;

            current.OnSectorMultiLinkList = false;
            current.SectorMultiNext = null;
            current.SectorMultiPrevious = null;
            current = SectorMultiNext;
        }

        _first = null;
        _last = null;
        Count = 0;
        Version++;
    }

    public void AddLast(ref SectorMultiValueLinkList otherList, BaseMulti start, BaseMulti end)
    {
        // Should we check if start and end actually exist on the other list?
        if (otherList.Count == 0 || otherList.Count == 1 && (start != end || otherList._first != start))
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
            otherList._first = end.SectorMultiNext;
        }

        if (end.SectorMultiNext != null)
        {
            end.SectorMultiNext.SectorMultiPrevious = start.SectorMultiPrevious;
        }
        else
        {
            otherList._last = start.SectorMultiPrevious;
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

        if (otherList.Count < 0)
        {
            throw new Exception("Count is negative!");
        }

        if (_last != null)
        {
            _last.SectorMultiNext = start;
            start.SectorMultiPrevious = _last;
        }
        else
        {
            _first = start;
        }

        _last = end;
        Count += count;
        Version++;
    }

    public BaseMulti[] ToArray()
    {
        var arr = new BaseMulti[Count];

        var index = 0;
        foreach (var t in this)
        {
            arr[index++] = t;
        }

        return arr;
    }

    public ref struct SectorMultiValueListEnumerator
    {
        private bool _started;
        private BaseMulti _current;
        private ref readonly SectorMultiValueLinkList _linkList;
        private int _version;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SectorMultiValueListEnumerator(in SectorMultiValueLinkList linkList)
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
                _current = _current.SectorMultiNext;
            }

            return _current != null;
        }

        public BaseMulti Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }

    public ref struct DescendingSectorMultiValueListEnumerator
    {
        private bool _started;
        private BaseMulti _current;
        private ref readonly SectorMultiValueLinkList _linkList;
        private int _version;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DescendingSectorMultiValueListEnumerator(in SectorMultiValueLinkList linkList)
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
                _current = _current.SectorMultiPrevious;
            }

            return _current != null;
        }

        public BaseMulti Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DescendingSectorMultiValueListEnumerator GetEnumerator() => this;
    }
}

public static class SectorMultiValueLinkListExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SectorMultiValueLinkList.SectorMultiValueListEnumerator GetEnumerator(this in SectorMultiValueLinkList linkList)
        => new(in linkList);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SectorMultiValueLinkList.DescendingSectorMultiValueListEnumerator ByDescending(this in SectorMultiValueLinkList linkList)
        => new(in linkList);
}
