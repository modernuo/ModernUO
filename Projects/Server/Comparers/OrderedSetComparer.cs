/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OrderedSetComparer.cs                                           *
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

namespace Server;

public class OrderedSetComparer<T> : IComparer<(T Value, int Order)>
{
    public static readonly OrderedSetComparer<T> Default = new(Comparer<T>.Default);

    private readonly IComparer<T> _comparer;

    public OrderedSetComparer(IComparer<T> comparer) => _comparer = comparer;

    public int Compare((T Value, int Order) x, (T Value, int Order) y)
    {
        int result = _comparer.Compare(x.Value, y.Value);

        return result == 0 ? 0 : x.Order.CompareTo(y.Order);
    }
}
