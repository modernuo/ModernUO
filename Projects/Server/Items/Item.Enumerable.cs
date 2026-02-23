/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Item.Enumerable.cs                                              *
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
using System.Runtime.InteropServices;
using Server.Collections;

namespace Server;

public partial class Item
{
    /// <summary>
    ///     Performs a breadth-first search through all the <see cref="Item" />s and
    ///     nested <see cref="Item" />s within this <see cref="Item" />.
    /// </summary>
    /// <remarks>
    ///     DO NOT consume, delete, or move items while iterating with any FindItemByType or FindItems overloads
    /// </remarks>
    /// <example>
    /// <code>
    ///     var total = 0;
    ///
    ///     foreach (var gold in cont.FindItemsByType&lt;Gold&gt;())
    ///     {
    ///         total += gold.Amount;
    ///     }
    /// </code>
    /// </example>
    /// <typeparam name="T">Type of objects being searched for</typeparam>
    /// <param name="recurse">
    ///     Optional: If true, the search will recursively
    ///     check any nested <see cref="Item" />s; otherwise, nested
    ///     <see cref="Item" />s will not be searched.
    /// </param>
    /// <param name="predicate">
    ///     Optional: A predicate to check if the <see cref="Item" />
    ///     of type <typeparamref name="T" /> is one of the targets of the search.
    /// </param>
    /// <returns>
    ///     An enumerator for iterating through <see cref="Item" />s of type <typeparamref name="T" /> that match the optional
    ///     <paramref name="predicate" />.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FindItemsByTypeEnumerator<T> FindItemsByType<T>(bool recurse = true, Predicate<T> predicate = null) where T : Item =>
        new(this, recurse, predicate);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FindItemsByTypeEnumerator<Item> FindItemsByType(Type type, bool recurse = true) =>
        new(this, recurse, type.IsInstanceOfType);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FindItemsByTypeEnumerator<Item> FindItemsByType(Type[] types, bool recurse = true) =>
        new(this, recurse, item => item.InTypeList(types));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FindItemsByTypeEnumerator<Item> FindItems(bool recurse = true, Predicate<Item> predicate = null) =>
        new(this, recurse, predicate);

    /// <summary>
    ///     Safely enumerates items using a breadth-first search through all the <see cref="Item" />s and
    ///     nested <see cref="Item" />s within this <see cref="Item" />.
    /// </summary>
    /// <remarks>
    ///    Use EnumerateItemsByType for situations where the item might be manipulated, consumed, or moved.
    ///    Note: This method scans through the container before returning the enumerator for iteration and therefore
    ///    incurs a performance penalty from the overhead.
    /// </remarks>
    /// <example>
    /// <code>
    ///     using var queue = cont.EnumerateItemsByType&lt;Item&gt;();
    ///     foreach (var item in queue)
    ///     {
    ///         if (item.LootType is not LootType.Blessed)
    ///         {
    ///             item.Delete();
    ///         }
    ///     }
    /// </code>
    /// </example>
    /// <typeparam name="T">Type of objects being searched for</typeparam>
    /// <param name="recurse">
    ///     Optional: If true, the search will recursively
    ///     check any nested <see cref="Item" />s; otherwise, nested
    ///     <see cref="Item" />s will not be searched.
    /// </param>
    /// <param name="predicate">
    ///     Optional: A predicate to check if the <see cref="Item" />
    ///     of type <typeparamref name="T" /> is one of the targets of the search.
    /// </param>
    /// <returns>
    ///     An enumerator for iterating through <see cref="Item" />s of type <typeparamref name="T" /> that match the optional
    ///     <paramref name="predicate" />.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledRefQueue<T> EnumerateItemsByType<T>(bool recurse = true, Predicate<T> predicate = null) where T : Item
    {
        var queue = PooledRefQueue<T>.Create(128);

        foreach (var item in FindItemsByType(recurse, predicate))
        {
            queue.Enqueue(item);
        }

        return queue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledRefQueue<Item> EnumerateItemsByType(Type type, bool recurse = true)
    {
        var queue = PooledRefQueue<Item>.Create(128);

        foreach (var item in FindItemsByType<Item>(recurse))
        {
            if (type.IsInstanceOfType(item))
            {
                queue.Enqueue(item);
            }
        }

        return queue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledRefQueue<Item> EnumerateItemsByType(Type[] types, bool recurse = true)
    {
        var queue = PooledRefQueue<Item>.Create(128);

        foreach (var item in FindItemsByType<Item>(recurse))
        {
            if (item.InTypeList(types))
            {
                queue.Enqueue(item);
            }
        }

        return queue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledRefQueue<Item> EnumerateItems(bool recurse = true, Predicate<Item> predicate = null) =>
        EnumerateItemsByType(recurse, predicate);

    public PooledRefList<T> ListItemsByType<T>(bool recurse = true, Predicate<T> predicate = null) where T : Item
    {
        var list = PooledRefList<T>.Create(128);

        foreach (var item in FindItemsByType(recurse, predicate))
        {
            list.Add(item);
        }

        return list;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledRefList<Item> ListItemsByType(Type type, bool recurse = true)
    {
        var list = PooledRefList<Item>.Create(128);

        foreach (var item in FindItemsByType<Item>(recurse))
        {
            if (type.IsInstanceOfType(item))
            {
                list.Add(item);
            }
        }

        return list;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledRefList<Item> ListItemsByType(Type[] types, bool recurse = true)
    {
        var list = PooledRefList<Item>.Create(128);

        foreach (var item in FindItemsByType<Item>(recurse))
        {
            if (item.InTypeList(types))
            {
                list.Add(item);
            }
        }

        return list;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledRefList<Item> ListItems(bool recurse = true, Predicate<Item> predicate = null) =>
        ListItemsByType(recurse, predicate);

    public ref struct FindItemsByTypeEnumerator<T> where T : Item
    {
        private const string InvalidOperation_EnumFailedVersion =
            "Item was modified after enumerator was instantiated. Use Item.EnumerateItems method instead for safe enumerations.";

        private PooledRefQueue<Item> _containers;
        private Span<Item> _items;
        private int _index;
        private T _current;
        private readonly bool _recurse;
        private readonly Predicate<T> _predicate;
        private Item _currentContainer;
        private int _version;

        public FindItemsByTypeEnumerator(Item container, bool recurse, Predicate<T> predicate)
        {
            _containers = PooledRefQueue<Item>.Create(_recurse ? 64 : 0);

            if (container != null)
            {
                var items = container.LookupItems();
                if (items != null)
                {
                    _items = CollectionsMarshal.AsSpan(items);
                }

                _currentContainer = container;
                _version = container.LookupContainerVersion();
            }

            _current = default;
            _index = 0;
            _recurse = recurse;
            _predicate = predicate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => SetNextItem() || _recurse && SetNextContainer();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SetNextContainer()
        {
            while (_containers.TryDequeue(out var c))
            {
                _currentContainer = c;
                _items = CollectionsMarshal.AsSpan(c.LookupItems());
                _index = 0;
                _version = c.LookupContainerVersion();

                if (SetNextItem())
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SetNextItem()
        {
            if (_version != _currentContainer.LookupContainerVersion())
            {
                throw new InvalidOperationException(InvalidOperation_EnumFailedVersion);
            }

            while (_index < _items.Length)
            {
                var item = _items[_index++];
                if (_recurse && item.LookupItems() is { Count: > 0 } items)
                {
                    _containers.Enqueue(item);
                }

                if (item is T t && _predicate?.Invoke(t) != false)
                {
                    if (_version != _currentContainer.LookupContainerVersion())
                    {
                        throw new InvalidOperationException(InvalidOperation_EnumFailedVersion);
                    }

                    _current = t;
                    return true;
                }
            }

            return false;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _containers.Dispose();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FindItemsByTypeEnumerator<T> GetEnumerator() => this;
    }
}
