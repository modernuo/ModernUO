/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: LocalizationEntry.cs                                            *
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
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Server.Buffers;
using Server.Collections;

namespace Server;

public class LocalizationEntry
{
    private static readonly Regex _textRegex = new(
        @"~(\d+)[_\w]+~",
        RegexOptions.Compiled |
        RegexOptions.IgnoreCase |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant
    );

    public string Language { get; }
    public int Number { get; }
    public string Text { get; }
    public string[] TextSlices { get; }
    public string StringFormatter { get; }

    public LocalizationEntry(string lang, int number, string text)
    {
        Language = lang;
        Number = number;
        Text = text;

        ParseText(text, out var textSlices, out var stringFormatter);
        TextSlices = textSlices;
        StringFormatter = stringFormatter;
    }

    private static void ParseText(string text, out string[] textSlices, out string stringFormatter)
    {
        bool hasMatch = false;
        var prevIndex = 0;
        var builder = new ValueStringBuilder(stackalloc char[256]);
        using var queue = PooledRefQueue<string>.Create();
        foreach (Match match in _textRegex.Matches(text))
        {
            if (prevIndex < match.Index)
            {
                var substr = text[prevIndex..match.Index];
                builder.Append(substr);

                queue.Enqueue(substr);
            }

            queue.Enqueue(null);
            hasMatch = true;
            builder.Append($"{{{int.Parse(match.Groups[1].Value) - 1}}}");
            prevIndex = match.Index + match.Length;
        }

        if (prevIndex < text.Length - 1)
        {
            var substr = prevIndex == 0 ? text : text[prevIndex..];
            builder.Append(substr);
            queue.Enqueue(substr);
        }

        textSlices = queue.ToArray();
        stringFormatter = hasMatch ? builder.ToString() : null;

        builder.Dispose();
    }

    /// <summary>
    /// Creates a formatted string of the localization entry.
    /// Uses string interpolation under the hood. This method is preferably relative to the object array method signature.
    /// Example:
    /// Format($"{totalItems}{maxItems}{totalWeight}");
    /// </summary>
    /// <param name="handler">interpolated string handler used by the compiler as a string builder during compilation</param>
    /// <returns>A copy of the localization text where the placeholder arguments have been replaced with string representations of the provided interpolation arguments</returns>
    public PooledArraySpanFormattable Format(
        [InterpolatedStringHandlerArgument("")]
        ref LocalizationInterpolationHandler handler
    )
    {
        var chars = handler.ToPooledArray(out var length);
        handler = default; // Defensive clear
        return new PooledArraySpanFormattable(chars, length);
    }

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
        private string _lang;

        public LocalizationInterpolationHandler(int literalLength, int formattedCount, LocalizationEntry entry, out bool isValid)
        {
            _slices = entry.TextSlices;
            _chars = _arrayToReturnToPool = STArrayPool<char>.Shared.Rent(256);
            isValid = true;

            _pos = 0;
            _index = 0;
            _current = null;
            _lang = entry.Language;
        }

        public LocalizationInterpolationHandler(
            int literalLength, int formattedCount, int number, out bool isValid
        ) : this(literalLength, formattedCount, number, Localization.FallbackLanguage, out isValid)
        {
        }

        public LocalizationInterpolationHandler(
            int literalLength, int formattedCount, int number, string lang, out bool isValid
        )
        {
            if (Localization.TryGetLocalization(lang, number, out var entry))
            {
                _slices = entry.TextSlices;
                _chars = _arrayToReturnToPool = STArrayPool<char>.Shared.Rent(256);
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
            _lang = lang;
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

        // Each numeric needs its own override
        public void AppendFormatted(int value, string? format)
        {
            if (!ReadyToAppend())
            {
                return;
            }

            if (!TryAppendCliloc(value, format))
            {
                AppendFormattedDirect(value, format);
            }
        }

        public void AppendFormatted(uint value, string? format)
        {
            if (!ReadyToAppend())
            {
                return;
            }

            if (!TryAppendCliloc((int)value, format))
            {
                AppendFormattedDirect(value, format);
            }
        }

        public void AppendFormatted(long value, string? format)
        {
            if (!ReadyToAppend())
            {
                return;
            }

            if (!TryAppendCliloc((int)value, format))
            {
                AppendFormattedDirect(value, format);
            }
        }

        public void AppendFormatted(ulong value, string? format)
        {
            if (!ReadyToAppend())
            {
                return;
            }

            if (!TryAppendCliloc((int)value, format))
            {
                AppendFormattedDirect(value, format);
            }
        }

        public void AppendFormatted<T>(T value, string? format)
        {
            if (ReadyToAppend())
            {
                AppendFormattedDirect(value, format);
            }
        }

        private void AppendFormattedDirect<T>(T value, string? format)
        {
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

        // Each numeric needs its own override
        public void AppendFormatted(int value, int alignment, string? format)
        {
            if (!ReadyToAppend())
            {
                return;
            }

            var startingPos = _pos;
            if (TryAppendCliloc(value, format))
            {
                AppendFormatted(value, format);
                if (alignment != 0)
                {
                    AppendOrInsertAlignmentIfNeeded(startingPos, alignment);
                }
            }
            else
            {
                AppendFormattedDirect(value, alignment, format);
            }
        }

        public void AppendFormatted(uint value, int alignment, string? format)
        {
            if (!ReadyToAppend())
            {
                return;
            }

            var startingPos = _pos;
            if (TryAppendCliloc((int)value, format))
            {
                AppendFormatted(value, format);
                if (alignment != 0)
                {
                    AppendOrInsertAlignmentIfNeeded(startingPos, alignment);
                }
            }
            else
            {
                AppendFormattedDirect(value, alignment, format);
            }
        }

        public void AppendFormatted(long value, int alignment, string? format)
        {
            if (!ReadyToAppend())
            {
                return;
            }

            var startingPos = _pos;
            if (TryAppendCliloc((int)value, format))
            {
                AppendFormatted(value, format);
                if (alignment != 0)
                {
                    AppendOrInsertAlignmentIfNeeded(startingPos, alignment);
                }
            }
            else
            {
                AppendFormattedDirect(value, alignment, format);
            }
        }

        public void AppendFormatted(ulong value, int alignment, string? format)
        {
            if (!ReadyToAppend())
            {
                return;
            }

            var startingPos = _pos;
            if (TryAppendCliloc((int)value, format))
            {
                AppendFormatted(value, format);
                if (alignment != 0)
                {
                    AppendOrInsertAlignmentIfNeeded(startingPos, alignment);
                }
            }
            else
            {
                AppendFormattedDirect(value, alignment, format);
            }
        }

        public void AppendFormatted<T>(T value, int alignment, string? format)
        {
            if (!ReadyToAppend())
            {
                return;
            }

            AppendFormattedDirect(value, alignment, format);
        }

        private void AppendFormattedDirect<T>(T value, int alignment, string? format)
        {
            var startingPos = _pos;
            AppendFormatted(value, format);
            if (alignment != 0)
            {
                AppendOrInsertAlignmentIfNeeded(startingPos, alignment);
            }
        }

        public void AppendFormatted(ReadOnlySpan<char> value)
        {
            if (!ReadyToAppend() || TryAppendClilocByNumericString(value))
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

        public void AppendFormatted(object? value, int alignment = 0, string? format = null)
        {
            if (value is int i)
            {
                AppendFormatted(i, alignment, format);
            }
            else if (value is uint ui)
            {
                AppendFormatted(ui, alignment, format);
            }
            else if (value is long l)
            {
                AppendFormatted(l, alignment, format);
            }
            else if (value is ulong ul)
            {
                AppendFormatted(ul, alignment, format);
            }
            else
            {
                AppendFormatted<object?>(value, alignment, format);
            }
        }

        public void AppendFormatted(string? value)
        {
            if (!ReadyToAppend() || TryAppendClilocByNumericString(value))
            {
                return;
            }

            if (value?.TryCopyTo(_chars[_pos..]) == true)
            {
                _pos += value.Length;
            }
            else
            {
                AppendFormattedSlow(value);
            }
        }

        public void AppendFormatted(string? value, int alignment, string? format = null) =>
            AppendFormatted<string?>(value, alignment, format);

        public bool TryAppendCliloc(int number, string? format)
        {
            if (format != "#" || !Localization.TryGetLocalization(_lang, number, out var entry))
            {
                return false;
            }

            var text = entry.Text;

            if (text.TryCopyTo(_chars[_pos..]))
            {
                _pos += text.Length;
            }
            else
            {
                AppendFormattedSlow(text);
            }

            return true;
        }

        private bool TryAppendClilocByNumericString(ReadOnlySpan<char> value) =>
            value[0] == '#' && long.TryParse(value[1..], out var number) && TryAppendCliloc((int)number, "#");

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
            var newCapacity = Math.Max(requiredMinCapacity, Math.Min((uint)_chars.Length * 2, 0x3FFFFFDF));
            var arraySize = (int)Math.Clamp(newCapacity, 256, int.MaxValue);

            var newArray = STArrayPool<char>.Shared.Rent(arraySize);
            _chars[.._pos].CopyTo(newArray);

            var toReturn = _arrayToReturnToPool;
            _chars = _arrayToReturnToPool = newArray;

            if (toReturn is not null)
            {
                STArrayPool<char>.Shared.Return(toReturn);
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
                STArrayPool<char>.Shared.Return(toReturn);
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
}
