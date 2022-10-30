/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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

namespace Server;

[Parsable]
[PropertyObject]
public class TextDefinition
{
    public TextDefinition(string text) : this(0, text)
    {
    }

    public TextDefinition(int number = 0, string text = null)
    {
        Number = number;
        String = text;
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

    public string GetValue() => Number > 0 ? Number.ToString() : String ?? "";

    public static implicit operator TextDefinition(int v) => new(v);

    public static implicit operator TextDefinition(string s) => new(s);

    public static implicit operator int(TextDefinition m) => m?.Number ?? 0;

    public static implicit operator string(TextDefinition m) => m?.String;

    public static TextDefinition Parse(string value)
    {
        if (value == null)
        {
            return null;
        }

        return Utility.ToInt32(value, out var i) ? new TextDefinition(i) : new TextDefinition(value);
    }
}
