/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SaveSampler.cs                                                  *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Runtime.CompilerServices;

namespace Server;

/// <summary>
/// Per-worker xorshift64* stream that picks which clean entities a delta save re-serializes for
/// verification. Reseeded from the save index and worker index every save so the sample is
/// fresh each time (a persistently wrong instance is caught with probability 1 - (1 - rate)^N
/// over N saves) yet reproducible for a given save.
/// </summary>
public sealed class SaveSampler
{
    private ulong _state = 0x9E3779B97F4A7C15UL;

    public void Reseed(int saveIndex, int workerIndex)
    {
        // splitmix64 over the pair so nearby seeds produce unrelated streams.
        var z = (ulong)saveIndex * 0x9E3779B97F4A7C15UL + (ulong)(workerIndex + 1) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;
        _state = z == 0 ? 0x9E3779B97F4A7C15UL : z;
    }

    /// <summary>Converts a rate in [0, 1] into the threshold <see cref="Hit" /> compares against.</summary>
    public static uint Threshold(double rate) =>
        rate <= 0 ? 0 : rate >= 1 ? uint.MaxValue : (uint)(rate * uint.MaxValue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Hit(uint threshold)
    {
        if (threshold == 0)
        {
            return false;
        }

        var x = _state;
        x ^= x >> 12;
        x ^= x << 25;
        x ^= x >> 27;
        _state = x;

        return (uint)((x * 0x2545F4914F6CDD1DUL) >> 32) < threshold || threshold == uint.MaxValue;
    }
}
