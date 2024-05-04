/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GrowOnlySet.cs                                                  *
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

namespace Server.Collections;

public class GrowOnlySet<T>
{
    private readonly SortedSet<(T Value, int Order)> _set;
    private int _next;

    public GrowOnlySet(OrderedSetComparer<T> comparer = null) =>
        _set = new SortedSet<(T, int)>(comparer ?? OrderedSetComparer<T>.Default);

    public int Count => _set.Count;

    public int Add(T value)
    {
        if (_set.TryGetValue((value, _next), out var entry))
        {
            return entry.Order;
        }

        _set.Add((value, _next));
        return _next++;
    }

    public bool Contains(T value) => _set.Contains((value, 0));

    public void Clear()
    {
        _set.Clear();
        _next = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(_set.GetEnumerator());

    public ref struct Enumerator
    {
        private SortedSet<(T Value, int Order)>.Enumerator _enumerator;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator(SortedSet<(T Value, int Order)>.Enumerator enumerator) => _enumerator = enumerator;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => _enumerator.MoveNext();

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _enumerator.Current.Value;
        }
    }

}
