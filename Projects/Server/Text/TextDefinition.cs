/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TextDefinition.cs                                               *
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

namespace Server;

[PropertyObject]
public class TextDefinition : IEquatable<object>, IEquatable<TextDefinition>, ISpanParsable<TextDefinition>
{
    public static readonly TextDefinition Empty = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TextDefinition Of(int number) => Of(number, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TextDefinition Of(string text) => Of(0, text);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TextDefinition Of(ReadOnlySpan<char> text) => Of(0, text);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TextDefinition Of(int number, string text) => new(number, text);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TextDefinition Of(int number, ReadOnlySpan<char> text) => new(number, text);

    private TextDefinition()
    {
    }

    private TextDefinition(int number, string text)
    {
        Number = number;
        String = text;
    }

    private TextDefinition(int number, ReadOnlySpan<char> text)
    {
        Number = number;
        String = text.ToString();
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Number { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public string String { get; }

    public bool IsEmpty => Number <= 0 && String == null;

    public override string ToString() => Number > 0 ? $"#{Number}" : String ?? "";

    public string Format() =>
        Number > 0 ? $"{Number} (0x{Number:X})" :
        String != null ? $"\"{String}\"" : null;

    /// <summary>
    /// The editable text form. Quotes a string that <c>TryParse</c> would not read back unchanged;
    /// the check is a real round trip so it cannot drift from the parser.
    /// </summary>
    public string GetValue()
    {
        if (Number > 0)
        {
            return Number.ToString();
        }

        if (String == null)
        {
            return "";
        }

        return TryParse(String, null, out var parsed) && parsed.Number == 0 && parsed.String == String
            ? String
            : $"@\"{String}\"";
    }

    public static implicit operator TextDefinition(int v) => Of(v);

    public static implicit operator TextDefinition(string s) => Of(s);

    public static implicit operator int(TextDefinition m) => m?.Number ?? 0;

    public static implicit operator string(TextDefinition m) => m?.String;

    public void Deconstruct(out int number, out string s)
    {
        if (Number > 0)
        {
            number = Number;
            s = null;
        }
        else
        {
            number = 0;
            s = String;
        }
    }

    public override bool Equals(object obj) => Equals(obj as TextDefinition);

    public bool Equals(TextDefinition other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Number > 0 || other.Number > 0)
        {
            return Number == other.Number;
        }

        return String == other.String;
    }

    public override int GetHashCode() => Number > 0 ? HashCode.Combine(Number) : HashCode.Combine(String);

    public static bool operator ==(TextDefinition left, TextDefinition right) => Equals(left, right);

    public static bool operator !=(TextDefinition left, TextDefinition right) => !Equals(left, right);

    public static TextDefinition Parse(string value) => value == null ? null : Parse(value.AsSpan(), null);

    public static TextDefinition Parse(string s, IFormatProvider provider) => Parse(s.AsSpan(), provider);

    public static bool TryParse(string s, IFormatProvider provider, out TextDefinition result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static TextDefinition Parse(ReadOnlySpan<char> s, IFormatProvider provider)
    {
        TryParse(s, provider, out var result);
        return result;
    }

    /// <summary>
    /// <c>#1234</c> or a bare <c>1234</c>/<c>0x4D2</c> is a cliloc; <c>@"1234"</c> is the literal
    /// text. Always succeeds -- anything that is not a cliloc is a string.
    /// See <c>dev-docs/generic-commands.md</c>.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out TextDefinition result)
    {
        if (TryGetQuotedLiteral(s, out var literal))
        {
            result = Of(literal);
            return true;
        }

        if (s.Length > 1 && s[0] == '#' && Utility.ToInt32(s[1..], out var marked))
        {
            result = Of(marked);
            return true;
        }

        if (Utility.ToInt32(s, out var label))
        {
            result = Of(label);
            return true;
        }

        // We don't trim
        result = Of(s);
        return true;
    }

    private static bool TryGetQuotedLiteral(ReadOnlySpan<char> s, out ReadOnlySpan<char> literal)
    {
        if (s.Length >= 3 && s[0] == '@' && s[1] == '"' && s[^1] == '"')
        {
            literal = s[2..^1];
            return true;
        }

        literal = default;
        return false;
    }
}
