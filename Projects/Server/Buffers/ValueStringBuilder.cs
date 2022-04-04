// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server.Buffers;

public ref struct ValueStringBuilder
{
    private char[] _arrayToReturnToPool;
    private Span<char> _chars;
    private int _length;

    // If this ctor is used, you cannot pass in stackalloc ROS for append/replace.
    public ValueStringBuilder(ReadOnlySpan<char> initialString) : this(initialString.Length)
    {
        Append(initialString);
    }

    public ValueStringBuilder(ReadOnlySpan<char> initialString, Span<char> initialBuffer) : this(initialBuffer)
    {
        Append(initialString);
    }

    public ValueStringBuilder(Span<char> initialBuffer)
    {
        _arrayToReturnToPool = null;
        _chars = initialBuffer;
        _length = 0;
    }

    // If this ctor is used, you cannot pass in stackalloc ROS for append/replace.
    public ValueStringBuilder(int initialCapacity)
    {
        _arrayToReturnToPool = STArrayPool<char>.Shared.Rent(initialCapacity);
        _chars = _arrayToReturnToPool;
        _length = 0;
    }

    public int Length => _length;

    public int Capacity => _chars.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _length = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureCapacity(int capacity)
    {
        if (capacity > _chars.Length)
        {
            Grow(capacity - Length);
        }
    }

    /// <summary>
    /// Get a pinnable reference to the builder.
    /// Does not ensure there is a null char after <see cref="Length"/>
    /// This overload is pattern matched in the C# 7.3+ compiler so you can omit
    /// the explicit method call, and write eg "fixed (char* c = builder)"
    /// </summary>
    public ref char GetPinnableReference() => ref MemoryMarshal.GetReference(_chars);

    /// <summary>
    /// Get a pinnable reference to the builder.
    /// </summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
    public ref char GetPinnableReference(bool terminate)
    {
        if (terminate)
        {
            EnsureCapacity(_length + 1);
            _chars[_length] = '\0';
        }
        return ref MemoryMarshal.GetReference(_chars);
    }

    public ref char this[int index] => ref _chars[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => _chars[.._length].ToString();

    /// <summary>Returns the underlying storage of the builder.</summary>
    public Span<char> RawChars => _chars;

    /// <summary>
    /// Returns a span around the contents of the builder.
    /// </summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
    public ReadOnlySpan<char> AsSpan(bool terminate)
    {
        if (terminate)
        {
            EnsureCapacity(_length + 1);
            _chars[_length] = '\0';
        }
        return _chars[.._length];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> AsSpan() => _chars[.._length];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> AsSpan(int start) => _chars[start..];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> AsSpan(int start, int length) => _chars.Slice(start, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCopyTo(Span<char> destination, out int charsWritten)
    {
        if (_chars[.._length].TryCopyTo(destination))
        {
            charsWritten = _length;
            return true;
        }

        charsWritten = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Insert(int index, char value, int count)
    {
        if (_length > _chars.Length - count)
        {
            Grow(count);
        }

        int remaining = _length - index;
        _chars.Slice(index, remaining).CopyTo(_chars[(index + count)..]);
        _chars.Slice(index, count).Fill(value);
        _length += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Insert(int index, string s)
    {
        if (s == null)
        {
            return;
        }

        int count = s.Length;

        if (_length > _chars.Length - count)
        {
            Grow(count);
        }

        int remaining = _length - index;
        _chars.Slice(index, remaining).CopyTo(_chars[(index + count)..]);
        s.AsSpan().CopyTo(_chars[index..]);
        _length += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char c)
    {
        int pos = _length;
        if ((uint)pos < (uint)_chars.Length)
        {
            _chars[pos] = c;
            _length = pos + 1;
        }
        else
        {
            GrowAndAppend(c);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(int value, NumberFormatInfo info = null)
    {
        if (value >= 0)
        {
            Append((uint)value);
            return;
        }

        Append((info ?? NumberFormatInfo.CurrentInfo).NegativeSign);
        Append((uint)-value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Append(uint value)
    {
        int bufferLength = value.CountDigits();

        int pos = _length;
        if ((uint)pos + (uint)bufferLength >= _chars.Length)
        {
            Grow(bufferLength);
        }

        if (bufferLength == 1)
        {
            _chars[pos] = (char)(value + '0');
            _length = pos + 1;
            return;
        }

        fixed (char* buffer = _chars[pos..])
        {
            char* p = buffer + bufferLength;
            do
            {
                value = Utility.DivRem(value, 10, out uint remainder);
                *--p = (char)(remainder + '0');
            } while (value != 0);
        }

        _length = pos + bufferLength;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string s)
    {
        if (s == null)
        {
            return;
        }

        int pos = _length;
        if (s.Length == 1 && (uint)pos < (uint)_chars.Length) // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
        {
            _chars[pos] = s[0];
            _length = pos + 1;
        }
        else
        {
            AppendSlow(s);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLine(string s)
    {
        if (s == null)
        {
            return;
        }

        // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
        if (s.Length == 1)
        {
            Append(s[0]);
        }
        else
        {
            AppendSlow(s);
        }

        Append(Environment.NewLine);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendSlow(string s)
    {
        int pos = _length;
        if (pos > _chars.Length - s.Length)
        {
            Grow(s.Length);
        }

        s.AsSpan().CopyTo(_chars[pos..]);
        _length += s.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char c, int count)
    {
        if (_length > _chars.Length - count)
        {
            Grow(count);
        }

        Span<char> dst = _chars.Slice(_length, count);
        for (int i = 0; i < dst.Length; i++)
        {
            dst[i] = c;
        }
        _length += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Append(char* value, int length)
    {
        int pos = _length;
        if (pos > _chars.Length - length)
        {
            Grow(length);
        }

        Span<char> dst = _chars.Slice(_length, length);
        for (int i = 0; i < dst.Length; i++)
        {
            dst[i] = *value++;
        }
        _length += length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(ReadOnlySpan<char> value)
    {
        int pos = _length;
        if (pos > _chars.Length - value.Length)
        {
            Grow(value.Length);
        }

        value.CopyTo(_chars[_length..]);
        _length += value.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<char> AppendSpan(int length)
    {
        int origPos = _length;
        if (origPos > _chars.Length - length)
        {
            Grow(length);
        }

        _length = origPos + length;
        return _chars.Slice(origPos, length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndAppend(char c)
    {
        Grow(1);
        Append(c);
    }

#nullable enable
    /// <summary>
    /// Resize the internal buffer either by doubling current buffer size or
    /// by adding <paramref name="additionalCapacityBeyondPos"/> to
    /// <see cref="Length"/> whichever is greater.
    /// </summary>
    /// <param name="additionalCapacityBeyondPos">
    /// Number of chars requested beyond current position.
    /// </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalCapacityBeyondPos)
    {
        char[] poolArray = STArrayPool<char>.Shared.Rent(Math.Max(_length + additionalCapacityBeyondPos, _chars.Length * 2));

        _chars[.._length].CopyTo(poolArray);

        char[] toReturn = _arrayToReturnToPool;
        _chars = _arrayToReturnToPool = poolArray;
        if (toReturn != null)
        {
            STArrayPool<char>.Shared.Return(toReturn);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        char[] toReturn = _arrayToReturnToPool;
        this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
        if (toReturn != null)
        {
            STArrayPool<char>.Shared.Return(toReturn);
        }
    }
#nullable restore

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceAny(ReadOnlySpan<char> oldChars, ReadOnlySpan<char> newChars, int startIndex, int count)
    {
        int currentLength = _length;
        if ((uint)startIndex > (uint)currentLength)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        if (count < 0 || startIndex > currentLength - count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var slice = _chars;

        while (true)
        {
            var indexOf = slice.IndexOfAny(oldChars);
            if (indexOf == -1)
            {
                break;
            }

            var chr = slice[indexOf];

            slice[indexOf] = newChars[oldChars.IndexOf(chr)];
            slice = slice[(indexOf + 1)..];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Replace(char oldChar, char newChar, int startIndex, int count)
    {
        int currentLength = _length;
        if ((uint)startIndex > (uint)currentLength)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        if (count < 0 || startIndex > currentLength - count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var slice = _chars;

        while (true)
        {
            var indexOf = slice.IndexOf(oldChar);
            if (indexOf == -1)
            {
                break;
            }

            slice[indexOf] = newChar;
            slice = slice[(indexOf + 1)..];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(int startIndex, int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        if (length > _length - startIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if (startIndex == 0)
        {
            _chars = _chars[length..];
        }
        else if (startIndex + length == _length)
        {
            _chars = _chars[..startIndex];
        }
        else
        {
            // Somewhere in the middle, this will be slow
            _chars[(startIndex + length)..].CopyTo(_chars[startIndex..]);
        }

        _length -= length;
    }
}
