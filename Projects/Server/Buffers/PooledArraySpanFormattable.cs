/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PooledArraySpanFormattable.cs                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

#nullable enable
using System;

namespace Server.Buffers;

/// <summary>
/// Wrapper for STArray backed strings and char buffers that will be used in InterpolatedStringHandlers.
/// The wrapper prevents intermediate strings from being created unnecessarily.
/// Note: TryFormat can only be called once. Using the PooledArraySpanFormattable after calling TryFormat will throw.
/// To use the span multiple times, use the Chars property directly instead.
/// </summary>
public struct PooledArraySpanFormattable : ISpanFormattable, IDisposable
{
    private char[] _arrayToReturnToPool;
    private int _pos;
    private string _value;

    public PooledArraySpanFormattable(char[] arrayToReturnToPool, int length)
    {
        _arrayToReturnToPool = arrayToReturnToPool;
        _pos = length;
        _value = null;
    }

    public ReadOnlySpan<char> Chars => _arrayToReturnToPool.AsSpan(.._pos);

    public static implicit operator string(PooledArraySpanFormattable f) => f.ToString();

    public string ToString(string? format = null, IFormatProvider formatProvider = null)
    {
        _value ??= new string(_arrayToReturnToPool.AsSpan(0, _pos));

        STArrayPool<char>.Shared.Return(_arrayToReturnToPool);
        _arrayToReturnToPool = null;

        // We don't dispose so we can call ToString() multiple times with idempotence.
        return _value;
    }

    public bool TryFormat(
        Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default,
        IFormatProvider provider = null
    )
    {
        if (destination.Length < _pos)
        {
            charsWritten = 0;
            return false;
        }

        _arrayToReturnToPool.AsSpan(0, _pos).CopyTo(destination);
        charsWritten = _pos;

        // Interpolated string handlers do not dispose, but we need to return the chars to the array.
        Dispose();
        return true;
    }

    public void Dispose()
    {
        STArrayPool<char>.Shared.Return(_arrayToReturnToPool);
        _arrayToReturnToPool = null;
        this = default; // Defensive clear
    }
}
