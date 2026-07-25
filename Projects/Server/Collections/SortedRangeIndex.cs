/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SortedRangeIndex.cs                                             *
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
using System.Numerics;

namespace Server.Collections;

/// <summary>
/// Immutable, allocation-lean membership index over a set of inclusive integer ranges. Ranges are
/// stored as two parallel arrays (<c>_mins</c>/<c>_maxs</c>) sorted by minimum and coalesced into
/// disjoint runs, so a single binary search decides membership. Coalescing is required for
/// correctness: <see cref="Contains"/> only inspects the rightmost run whose minimum is &lt;= the
/// value, which is only sound when the runs never overlap or nest.
/// </summary>
public sealed class SortedRangeIndex<T> where T : IBinaryInteger<T>
{
    public static readonly SortedRangeIndex<T> Empty = new([], []);

    /// <summary>An inclusive <c>[Min, Max]</c> range. Singles are represented as <c>Min == Max</c>.</summary>
    public readonly record struct Range(T Min, T Max);

    /// <summary>Orders ranges ascending by minimum; the coalescing pass in <see cref="Build"/> requires this.</summary>
    public static readonly Comparison<Range> ByMin = static (a, b) => a.Min.CompareTo(b.Min);

    private readonly T[] _mins;
    private readonly T[] _maxs;

    private SortedRangeIndex(T[] mins, T[] maxs)
    {
        _mins = mins;
        _maxs = maxs;
    }

    public int Count => _mins.Length;

    /// <summary>
    /// True when <paramref name="value"/> falls in any range. Binary-searches for the rightmost run
    /// whose minimum is &lt;= the value, then tests that value against that run's maximum.
    /// </summary>
    public bool Contains(T value)
    {
        var lo = 0;
        var hi = _mins.Length - 1;
        var found = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (_mins[mid] <= value)
            {
                found = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return found >= 0 && value <= _maxs[found];
    }

    /// <summary>
    /// Builds an index from ranges that are already sorted ascending by <see cref="Range.Min"/> (sort
    /// the source with <see cref="ByMin"/> first). Overlapping and nested ranges are merged into disjoint
    /// runs. Two passes over the (pooled) input keep the run count exact so only the two final arrays are
    /// heap-allocated: pass one counts the runs, pass two fills the exact-size arrays.
    /// </summary>
    public static SortedRangeIndex<T> Build(ReadOnlySpan<Range> sortedByMin)
    {
        if (sortedByMin.IsEmpty)
        {
            return Empty;
        }

        // Pass 1: count the disjoint runs so the final arrays can be sized exactly.
        var runs = 1;
        var curMax = sortedByMin[0].Max;
        for (var i = 1; i < sortedByMin.Length; i++)
        {
            var r = sortedByMin[i];
            if (r.Min <= curMax)
            {
                if (r.Max > curMax)
                {
                    curMax = r.Max;
                }
            }
            else
            {
                runs++;
                curMax = r.Max;
            }
        }

        // Pass 2: write the coalesced runs into the exact-size final arrays.
        var mins = new T[runs];
        var maxs = new T[runs];
        var w = 0;
        mins[0] = sortedByMin[0].Min;
        maxs[0] = sortedByMin[0].Max;
        for (var i = 1; i < sortedByMin.Length; i++)
        {
            var r = sortedByMin[i];
            if (r.Min <= maxs[w])
            {
                if (r.Max > maxs[w])
                {
                    maxs[w] = r.Max;
                }
            }
            else
            {
                w++;
                mins[w] = r.Min;
                maxs[w] = r.Max;
            }
        }

        return new SortedRangeIndex<T>(mins, maxs);
    }
}
