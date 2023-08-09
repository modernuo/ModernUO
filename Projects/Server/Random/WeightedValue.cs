/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: WeightedValue.cs                                                *
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

namespace Server.Random;

public struct WeightedValue<T>
{
    public T Value { get; }
    public int Weight { get; }

    public WeightedValue(int weight, T value)
    {
        Value = value;
        Weight = weight;
    }

    public bool IsValid => Weight > 0;

    public static implicit operator WeightedValue<T>(ValueTuple<int, T> pair) => new(pair.Item1, pair.Item2);
}
