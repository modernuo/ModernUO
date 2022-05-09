/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: LocalizationInterpolationHandler.cs                             *
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
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Server;

[InterpolatedStringHandler]
public ref struct LocalizationInterpolationHandler
{
    private static string[] _empty = Array.Empty<string>();

    private char[]? _arrayToReturnToPool;
    private Span<char> _chars;
    private int _pos;

    private int _index;
    private string?[] _slices;
    private string? _current;

    public LocalizationInterpolationHandler(int literalLength, int formattedCount, LocalizationEntry entry, out bool isValid)
    {
        _slices = entry.TextSlices;
        _chars = _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(256);
        isValid = true;

        _pos = 0;
        _index = 0;
        _current = null;
    }

    public LocalizationInterpolationHandler(int literalLength, int formattedCount, int number, out bool isValid)
        : this(literalLength, formattedCount, number, Localization.FallbackLanguage, out isValid)
    {
    }

    public LocalizationInterpolationHandler(int literalLength, int formattedCount, int number, string lang, out bool isValid)
    {
        if (Localization.TryGetLocalization(lang, number, out var entry))
        {
            _slices = entry.TextSlices;
            _chars = _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(256);
            isValid = true;
        }
        else
        {
            _slices = _empty;
            _chars = _arrayToReturnToPool = default;
            isValid = false;
        }

        _pos = 0;
        _index = 0;
        _current = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool MoveNext()
    {
        if ((uint)_index >= (uint)_slices.Length)
        {
            return false;
        }

        _current = _slices[_index++];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ReadyToAppend()
    {
        if (!MoveNext())
        {
            return false;
        }

        if (_current == null)
        {
            return true;
        }

        AppendStringDirect(_current);
        return MoveNext();
    }

    public void AppendLiteral(string value)
    {
    }

    private void AppendStringDirect(string value)
    {
        if (value.TryCopyTo(_chars[_pos..]))
        {
            _pos += value.Length;
        }
        else
        {
            GrowThenCopyString(value);
        }
    }

    public void AppendFormatted<T>(T value)
    {
        if (!ReadyToAppend())
        {
            return;
        }

        string? s;
        if (value is IFormattable)
        {
            // If the value can format itself directly into our buffer, do so.
            if (value is ISpanFormattable)
            {
                int charsWritten;
                while (!((ISpanFormattable)value).TryFormat(_chars[_pos..], out charsWritten, default, default)) // constrained call avoiding boxing for value types
                {
                    Grow();
                }

                _pos += charsWritten;
                return;
            }

            s = ((IFormattable)value).ToString(format: null, default); // constrained call avoiding boxing for value types
        }
        else
        {
            s = value?.ToString();
        }

        if (s is not null)
        {
            AppendStringDirect(s);
        }
    }

    public void AppendFormatted<T>(T value, string? format)
    {
        if (!ReadyToAppend())
        {
            return;
        }

        string? s;
        if (value is IFormattable)
        {
            // If the value can format itself directly into our buffer, do so.
            if (value is ISpanFormattable)
            {
                int charsWritten;
                while (!((ISpanFormattable)value).TryFormat(_chars[_pos..], out charsWritten, format, default)) // constrained call avoiding boxing for value types
                {
                    Grow();
                }

                _pos += charsWritten;
                return;
            }

            s = ((IFormattable)value).ToString(format, default); // constrained call avoiding boxing for value types
        }
        else
        {
            s = value?.ToString();
        }

        if (s is not null)
        {
            AppendStringDirect(s);
        }
    }

    public void AppendFormatted<T>(T value, int alignment)
    {
        if (!ReadyToAppend())
        {
            return;
        }

        var startingPos = _pos;
        AppendFormatted(value);
        if (alignment != 0)
        {
            AppendOrInsertAlignmentIfNeeded(startingPos, alignment);
        }
    }

    public void AppendFormatted<T>(T value, int alignment, string? format)
    {
        if (!ReadyToAppend())
        {
            return;
        }

        var startingPos = _pos;
        AppendFormatted(value, format);
        if (alignment != 0)
        {
            AppendOrInsertAlignmentIfNeeded(startingPos, alignment);
        }
    }

    public void AppendFormatted(ReadOnlySpan<char> value)
    {
        if (!ReadyToAppend())
        {
            return;
        }

        if (value.TryCopyTo(_chars[_pos..]))
        {
            _pos += value.Length;
        }
        else
        {
            GrowThenCopySpan(value);
        }
    }

    public void AppendFormatted(ReadOnlySpan<char> value, int alignment, string? format = null)
    {
        if (!ReadyToAppend())
        {
            return;
        }

        var leftAlign = false;
        if (alignment < 0)
        {
            leftAlign = true;
            alignment = -alignment;
        }

        var paddingRequired = alignment - value.Length;
        if (paddingRequired <= 0)
        {
            // The value is as large or larger than the required amount of padding,
            // so just write the value.
            AppendFormatted(value);
            return;
        }

        // Write the value along with the appropriate padding.
        EnsureCapacityForAdditionalChars(value.Length + paddingRequired);
        if (leftAlign)
        {
            value.CopyTo(_chars[_pos..]);
            _pos += value.Length;
            _chars.Slice(_pos, paddingRequired).Fill(' ');
            _pos += paddingRequired;
        }
        else
        {
            _chars.Slice(_pos, paddingRequired).Fill(' ');
            _pos += paddingRequired;
            value.CopyTo(_chars[_pos..]);
            _pos += value.Length;
        }
    }

    public void AppendFormatted(object? value, int alignment = 0, string? format = null) =>
        AppendFormatted<object?>(value, alignment, format);

    public void AppendFormatted(string? value)
    {
        if (ReadyToAppend())
        {
            if (value?.TryCopyTo(_chars[_pos..]) == true)
            {
                _pos += value.Length;
            }
            else
            {
                AppendFormattedSlow(value);
            }
        }
    }

    public void AppendFormatted(string? value, int alignment, string? format = null) =>
        AppendFormatted<string?>(value, alignment, format);

    private void AppendOrInsertAlignmentIfNeeded(int startingPos, int alignment)
    {
        var charsWritten = _pos - startingPos;

        var leftAlign = false;
        if (alignment < 0)
        {
            leftAlign = true;
            alignment = -alignment;
        }

        var paddingNeeded = alignment - charsWritten;
        if (paddingNeeded > 0)
        {
            EnsureCapacityForAdditionalChars(paddingNeeded);

            if (leftAlign)
            {
                _chars.Slice(_pos, paddingNeeded).Fill(' ');
            }
            else
            {
                _chars.Slice(startingPos, charsWritten).CopyTo(_chars[(startingPos + paddingNeeded)..]);
                _chars.Slice(startingPos, paddingNeeded).Fill(' ');
            }

            _pos += paddingNeeded;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AppendFormattedSlow(string? value)
    {
        if (value is not null)
        {
            EnsureCapacityForAdditionalChars(value.Length);
            value.CopyTo(_chars[_pos..]);
            _pos += value.Length;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacityForAdditionalChars(int additionalChars)
    {
        if (_chars.Length - _pos < additionalChars)
        {
            Grow(additionalChars);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopyString(string value)
    {
        Grow(value.Length);
        value.CopyTo(_chars[_pos..]);
        _pos += value.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopySpan(ReadOnlySpan<char> value)
    {
        Grow(value.Length);
        value.CopyTo(_chars[_pos..]);
        _pos += value.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // keep consumers as streamlined as possible
    private void Grow(int additionalChars)
    {
        GrowCore((uint)_pos + (uint)additionalChars);
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // keep consumers as streamlined as possible
    private void Grow()
    {
        GrowCore((uint)_chars.Length + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowCore(uint requiredMinCapacity)
    {
        var newCapacity = Math.Max(requiredMinCapacity, Math.Min((uint)_chars.Length * 2, 1073741823));
        var arraySize = (int)Math.Clamp(newCapacity, 256, int.MaxValue);

        var newArray = ArrayPool<char>.Shared.Rent(arraySize);
        _chars[.._pos].CopyTo(newArray);

        var toReturn = _arrayToReturnToPool;
        _chars = _arrayToReturnToPool = newArray;

        if (toReturn is not null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    internal ReadOnlySpan<char> Text => _chars[.._pos];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Clear()
    {
        var toReturn = _arrayToReturnToPool;
        this = default; // defensive clear
        if (toReturn is not null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    public string ToStringAndClear()
    {
        if (MoveNext() && _current != null)
        {
            AppendStringDirect(_current);
        }

        var result = new string(Text);
        Clear();
        return result;
    }

    public char[] ToPooledArray(out int length)
    {
        if (MoveNext() && _current != null)
        {
            AppendStringDirect(_current);
        }

        length = _pos;
        return _arrayToReturnToPool;
    }
}
