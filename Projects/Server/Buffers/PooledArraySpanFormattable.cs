/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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

public struct PooledArraySpanFormattable : ISpanFormattable, IDisposable
{
    private char[] _arrayToReturnToPool;
    private int _pos;

    public PooledArraySpanFormattable(char[] arrayToReturnToPool, int length)
    {
        _arrayToReturnToPool = arrayToReturnToPool;
        _pos = length;
    }

    public ReadOnlySpan<char> Chars => _arrayToReturnToPool.AsSpan(.._pos);

    public static implicit operator string(PooledArraySpanFormattable f) => f.ToString();

    public string ToString(string? format = null, IFormatProvider formatProvider = null)
    {
        var result = new string(_arrayToReturnToPool.AsSpan(0, _pos));
        Dispose();

        return result;
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
        Dispose();

        charsWritten = _pos;
        return true;
    }

    public void Dispose()
    {
        if (_arrayToReturnToPool != null)
        {
            STArrayPool<char>.Shared.Return(_arrayToReturnToPool);
            _arrayToReturnToPool = null;
        }
    }
}
