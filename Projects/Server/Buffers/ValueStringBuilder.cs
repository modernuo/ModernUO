// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Server.Network;

namespace Server.Buffers
{
    public ref struct ValueStringBuilder
    {
        private char[] _arrayToReturnToPool;
        private Span<char> _chars;

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
            Length = 0;
        }

        // If this ctor is used, you cannot pass in stackalloc ROS for append/replace.
        public ValueStringBuilder(int initialCapacity)
        {
            _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialCapacity);
            _chars = _arrayToReturnToPool;
            Length = 0;
        }

        public int Length { get; set; }

        public int Capacity => _chars.Length;

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
                EnsureCapacity(Length + 1);
                _chars[Length] = '\0';
            }
            return ref MemoryMarshal.GetReference(_chars);
        }

        public ref char this[int index] => ref _chars[index];

        public override string ToString() => _chars.SliceToLength(Length).ToString();

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
                EnsureCapacity(Length + 1);
                _chars[Length] = '\0';
            }
            return _chars.SliceToLength(Length);
        }

        public ReadOnlySpan<char> AsSpan() => _chars.SliceToLength(Length);
        public ReadOnlySpan<char> AsSpan(int start) => _chars.Slice(start, Length - start);
        public ReadOnlySpan<char> AsSpan(int start, int length) => _chars.Slice(start, length);

        public bool TryCopyTo(Span<char> destination, out int charsWritten)
        {
            if (_chars.SliceToLength(Length).TryCopyTo(destination))
            {
                charsWritten = Length;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        public void Insert(int index, char value, int count)
        {
            if (Length > _chars.Length - count)
            {
                Grow(count);
            }

            int remaining = Length - index;
            _chars.Slice(index, remaining).CopyTo(_chars.Slice(index + count));
            _chars.Slice(index, count).Fill(value);
            Length += count;
        }

        public void Insert(int index, string s)
        {
            if (s == null)
            {
                return;
            }

            int count = s.Length;

            if (Length > _chars.Length - count)
            {
                Grow(count);
            }

            int remaining = Length - index;
            _chars.Slice(index, remaining).CopyTo(_chars.Slice(index + count));
            s.AsSpan().CopyTo(_chars.Slice(index));
            Length += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(char c)
        {
            int pos = Length;
            if ((uint)pos < (uint)_chars.Length)
            {
                _chars[pos] = c;
                Length = pos + 1;
            }
            else
            {
                GrowAndAppend(c);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(string s)
        {
            if (s == null)
            {
                return;
            }

            int pos = Length;
            if (s.Length == 1 && (uint)pos < (uint)_chars.Length) // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
            {
                _chars[pos] = s[0];
                Length = pos + 1;
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

        private void AppendSlow(string s)
        {
            int pos = Length;
            if (pos > _chars.Length - s.Length)
            {
                Grow(s.Length);
            }

            s.AsSpan().CopyTo(_chars.Slice(pos));
            Length += s.Length;
        }

        public void Append(char c, int count)
        {
            if (Length > _chars.Length - count)
            {
                Grow(count);
            }

            Span<char> dst = _chars.Slice(Length, count);
            for (int i = 0; i < dst.Length; i++)
            {
                dst[i] = c;
            }
            Length += count;
        }

        public unsafe void Append(char* value, int length)
        {
            int pos = Length;
            if (pos > _chars.Length - length)
            {
                Grow(length);
            }

            Span<char> dst = _chars.Slice(Length, length);
            for (int i = 0; i < dst.Length; i++)
            {
                dst[i] = *value++;
            }
            Length += length;
        }

        public void Append(ReadOnlySpan<char> value)
        {
            int pos = Length;
            if (pos > _chars.Length - value.Length)
            {
                Grow(value.Length);
            }

            value.CopyTo(_chars.Slice(Length));
            Length += value.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<char> AppendSpan(int length)
        {
            int origPos = Length;
            if (origPos > _chars.Length - length)
            {
                Grow(length);
            }

            Length = origPos + length;
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
            char[] poolArray = ArrayPool<char>.Shared.Rent(Math.Max(Length + additionalCapacityBeyondPos, _chars.Length * 2));

            _chars.SliceToLength(Length).CopyTo(poolArray);

            char[] toReturn = _arrayToReturnToPool;
            _chars = _arrayToReturnToPool = poolArray;
            if (toReturn != null)
            {
                ArrayPool<char>.Shared.Return(toReturn);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            char[] toReturn = _arrayToReturnToPool;
            this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
            if (toReturn != null)
            {
                ArrayPool<char>.Shared.Return(toReturn);
            }
        }
#nullable disable

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReplaceAny(ReadOnlySpan<char> oldChars, ReadOnlySpan<char> newChars, int startIndex, int count)
        {
            int currentLength = Length;
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
                slice = slice.Slice(indexOf + 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Replace(char oldChar, char newChar, int startIndex, int count)
        {
            int currentLength = Length;
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
                slice = slice.Slice(indexOf + 1);
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

            if (length > Length - startIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (startIndex == 0)
            {
                _chars = _chars.Slice(length);
            }
            else if (startIndex + length == Length)
            {
                _chars = _chars.SliceToLength(startIndex);
            }
            else
            {
                // Somewhere in the middle, this will be slow
                _chars.Slice(startIndex + length).CopyTo(_chars.Slice(startIndex));
            }

            Length -= length;
        }
    }
}
