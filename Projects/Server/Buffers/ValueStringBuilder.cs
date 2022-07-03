// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server.Buffers;

public ref struct ValueStringBuilder
{
    private char[] _arrayToReturnToPool;
    private Span<char> _chars;
    private int _length;
    private bool _mt;

    private ArrayPool<char> ArrayPool
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _mt ? ArrayPool<char>.Shared : STArrayPool<char>.Shared;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueStringBuilder Create(int capacity = 64, bool mt = false) => new(capacity, mt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueStringBuilder CreateMT(int capacity = 64) => new(capacity, true);

    // If this ctor is used, you cannot pass in stackalloc ROS for append/replace.
    public ValueStringBuilder(ReadOnlySpan<char> initialString, bool mt = false) : this(initialString.Length, mt)
    {
        Append(initialString);
    }

    public ValueStringBuilder(ReadOnlySpan<char> initialString, Span<char> initialBuffer, bool mt = false) : this(initialBuffer, mt)
    {
        Append(initialString);
    }

    public ValueStringBuilder(Span<char> initialBuffer, bool mt = false)
    {
        _mt = mt;
        _arrayToReturnToPool = null;
        _chars = initialBuffer;
        _length = 0;
    }

    // If this ctor is used, you cannot pass in stackalloc ROS for append/replace.
    public ValueStringBuilder(int initialCapacity, bool mt = false)
    {
        _mt = mt;
        _length = 0;
        _arrayToReturnToPool = (_mt ? ArrayPool<char>.Shared : STArrayPool<char>.Shared).Rent(initialCapacity);
        _chars = _arrayToReturnToPool;
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
    public void Insert(int index, string? s)
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

    public void Append<T>(T value, string? format = null)
    {
        if (value is IFormattable)
        {
            if (value is ISpanFormattable)
            {
                Span<char> destination = _chars[_length..];
                int charsWritten;
                while (!((ISpanFormattable)value).TryFormat(destination, out charsWritten, format, default))
                {
                    Grow(1);
                }

                if ((uint)charsWritten > (uint)destination.Length)
                {
                    throw new FormatException("Invalid string");
                }

                _length += charsWritten;
            }
            else
            {
                Append(((IFormattable)value).ToString(format, default)); // constrained call avoiding boxing for value types
            }
        }
        else if (value is not null)
        {
            Append(value.ToString());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string? s)
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
    public void AppendLine(string? s)
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
    private void AppendSlow(string? s)
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
        char[] poolArray = ArrayPool.Rent(Math.Max(_length + additionalCapacityBeyondPos, _chars.Length * 2));

        _chars[.._length].CopyTo(poolArray);

        char[] toReturn = _arrayToReturnToPool;
        _chars = _arrayToReturnToPool = poolArray;
        if (toReturn != null)
        {
            ArrayPool.Return(toReturn);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_arrayToReturnToPool != null)
        {
            ArrayPool.Return(_arrayToReturnToPool);
        }

        this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
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

    /// <summary>Provides a handler used by the language compiler to append interpolated strings into <see cref="ValueStringBuilder"/> instances.</summary>
    [InterpolatedStringHandler]
    public ref struct AppendInterpolatedStringHandler
    {
        // Implementation note:
        // As this type is only intended to be targeted by the compiler, public APIs eschew argument validation logic
        // in a variety of places, e.g. allowing a null input when one isn't expected to produce a NullReferenceException rather
        // than an ArgumentNullException.

        /// <summary>The associated StringBuilder to which to append.</summary>
        internal ValueStringBuilder _stringBuilder;

        /// <summary>Creates a handler used to append an interpolated string into a <see cref="ValueStringBuilder"/>.</summary>
        /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
        /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
        /// <param name="stringBuilder">The associated StringBuilder to which to append.</param>
        /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
        public AppendInterpolatedStringHandler(int literalLength, int formattedCount, ValueStringBuilder stringBuilder)
        {
            _stringBuilder = stringBuilder;
        }

        /// <summary>Writes the specified string to the handler.</summary>
        /// <param name="value">The string to write.</param>
        public void AppendLiteral(string value) => _stringBuilder.Append(value);

        // Design note:
        // This provides the same set of overloads and semantics as DefaultInterpolatedStringHandler.

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        public void AppendFormatted<T>(T value) => _stringBuilder.Append(value);

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        /// <param name="format">The format string.</param>
        public void AppendFormatted<T>(T value, string? format) => _stringBuilder.Append(value, format);

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        public void AppendFormatted<T>(T value, int alignment) =>
            AppendFormatted(value, alignment, format: null);

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        /// <param name="format">The format string.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        public void AppendFormatted<T>(T value, int alignment, string? format)
        {
            if (alignment == 0)
            {
                // This overload is used as a fallback from several disambiguation overloads, so special-case 0.
                AppendFormatted(value, format);
            }
            else if (alignment < 0)
            {
                // Left aligned: format into the handler, then append any additional padding required.
                int start = _stringBuilder.Length;
                AppendFormatted(value, format);
                int paddingRequired = -alignment - (_stringBuilder.Length - start);
                if (paddingRequired > 0)
                {
                    _stringBuilder.Append(' ', paddingRequired);
                }
            }
            else
            {
                var startingPos = _stringBuilder._length;
                AppendFormatted(value, format);

                InsertAlignment(startingPos, alignment);
            }
        }

        /// <summary>Writes the specified character span to the handler.</summary>
        /// <param name="value">The span to write.</param>
        public void AppendFormatted(ReadOnlySpan<char> value) => _stringBuilder.Append(value);

        /// <summary>Writes the specified string of chars to the handler.</summary>
        /// <param name="value">The span to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <param name="format">The format string.</param>
        public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
        {
            if (alignment == 0)
            {
                _stringBuilder.Append(value);
            }
            else
            {
                bool leftAlign = false;
                if (alignment < 0)
                {
                    leftAlign = true;
                    alignment = -alignment;
                }

                int paddingRequired = alignment - value.Length;
                if (paddingRequired <= 0)
                {
                    _stringBuilder.Append(value);
                }
                else if (leftAlign)
                {
                    _stringBuilder.Append(value);
                    _stringBuilder.Append(' ', paddingRequired);
                }
                else
                {
                    _stringBuilder.Append(' ', paddingRequired);
                    _stringBuilder.Append(value);
                }
            }
        }

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        public void AppendFormatted(string? value) => _stringBuilder.Append(value);

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <param name="format">The format string.</param>
        public void AppendFormatted(string? value, int alignment = 0, string? format = null) =>
            // Format is meaningless for strings and doesn't make sense for someone to specify.  We have the overload
            // simply to disambiguate between ROS<char> and object, just in case someone does specify a format, as
            // string is implicitly convertible to both. Just delegate to the T-based implementation.
            AppendFormatted<string?>(value, alignment, format);

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <param name="format">The format string.</param>
        public void AppendFormatted(object? value, int alignment = 0, string? format = null) =>
            // This overload is expected to be used rarely, only if either a) something strongly typed as object is
            // formatted with both an alignment and a format, or b) the compiler is unable to target type to T. It
            // exists purely to help make cases from (b) compile. Just delegate to the T-based implementation.
            AppendFormatted<object?>(value, alignment, format);

        private void InsertAlignment(int startingPos, int alignment)
        {
            var charsWritten = _stringBuilder._length - startingPos;

            var paddingNeeded = alignment - charsWritten;
            if (paddingNeeded > 0)
            {
                var chars = _stringBuilder._chars;
                if (chars.Length - _stringBuilder._length < paddingNeeded)
                {
                    _stringBuilder.Grow(paddingNeeded);
                }

                chars.Slice(startingPos, charsWritten).CopyTo(chars[(startingPos + paddingNeeded)..]);
                chars.Slice(startingPos, paddingNeeded).Fill(' ');

                _stringBuilder._length += paddingNeeded;
            }
        }
    }
}
