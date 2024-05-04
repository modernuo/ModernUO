/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OrderedSet.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server.Collections;

/// <summary>
/// A data structure designed for scenarios where the order of insertion is important.
/// Note: This is not particularly effecient.
/// </summary>
/// <typeparam name="T">The type of elements in the set.</typeparam>
public class OrderedSet<T>
{
    private readonly Dictionary<T, int> _dictionary;
    private readonly List<T> _list;

    public OrderedSet(IEqualityComparer<T> comparer = null)
    {
        _dictionary = new Dictionary<T, int>(comparer);
        _list = [];
    }

    public int Count => _dictionary.Count;

    public int Add(T value)
    {
        ref var order = ref CollectionsMarshal.GetValueRefOrAddDefault(_dictionary, value, out var exists);
        if (exists)
        {
            return order;
        }

        _list.Add(value);
        return order = _list.Count - 1;
    }

    public bool Contains(T value) => _dictionary.ContainsKey(value);

    public void Clear()
    {
        _dictionary.Clear();
        _list.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<T>.Enumerator GetEnumerator() => _list.GetEnumerator();
}
