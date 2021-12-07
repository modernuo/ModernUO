/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SourceGeneration.Enum.cs                                        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Text;
using Microsoft.CodeAnalysis;

namespace SerializationGenerator;

public static partial class SourceGeneration
{
    public static void GenerateEnumStart(
        this StringBuilder source,
        string enumName,
        string indent,
        bool useFlags,
        Accessibility accessor = Accessibility.Public
    )
    {
        if (useFlags)
        {
            source.AppendLine($"{indent}[System.Flags]");
        }
        source.AppendLine($"{indent}{accessor.ToFriendlyString()} enum {enumName}\n{indent}{{");
    }

    public static void GenerateEnumValue(this StringBuilder source, string indent, bool isFlag, string name, int value)
    {
        var number = value < 0 ? 0 : 1 << value;
        var valueStr = isFlag ? $"0x{number:X8}" : value.ToString();
        source.AppendLine($"{indent}{name} = {valueStr},");
    }

    public static void GenerateEnumEnd(this StringBuilder source, string indent)
    {
        source.AppendLine($"{indent}}}");
    }
}