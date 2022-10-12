/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ArrayEnumerator.cs                                              *
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
using System.Collections;
using System.Collections.Generic;

namespace Server.Collections;

/// <summary>
/// Non-thread safe, non-guarded enumerator for classes that have internal arrays.
/// Recommended to copy this and use it as a nested struct.
/// Recommend adding version checking to properly guard against modification during enumeration.
/// </summary>
/// <typeparam name="T"></typeparam>
public struct ArrayEnumerator<T> : IEnumerator<T>
{
    private readonly T[] _array;
    private int _index;
    private T? _current;

    public ArrayEnumerator(T[] array)
    {
        _array = array;
        _index = 0;
        _current = default;
    }

    public void Dispose()
    {
    }

    public bool MoveNext()
    {
        T[] localList = _array;

        if ((uint)_index < (uint)localList.Length)
        {
            _current = _array[_index++];
            return true;
        }

        return false;
    }

    public T? Current => _current!;

    object IEnumerator.Current
    {
        get
        {
            if (_index == 0 || _index == _array.Length + 1)
            {
                throw new InvalidOperationException(nameof(_index));
            }

            return _current;
        }
    }

    void IEnumerator.Reset()
    {
        _index = 0;
        _current = default;
    }
}
