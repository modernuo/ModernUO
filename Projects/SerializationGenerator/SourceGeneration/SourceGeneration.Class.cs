/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: SourceGeneration.Class.cs                                       *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SerializationGenerator;

public static partial class SourceGeneration
{
    public static void GenerateClassStart(
        this StringBuilder source,
        INamedTypeSymbol classSymbol,
        string indent,
        ImmutableArray<ITypeSymbol> interfaces,
        bool isPartial = true
    )
    {
        var accessor = classSymbol.DeclaredAccessibility;
        source.Append($"{indent}{accessor.ToFriendlyString()} {(isPartial ? "partial " : "")}class {classSymbol.Name}");
        if (!interfaces.IsEmpty)
        {
            source.Append(" : ");
            for (var i = 0; i < interfaces.Length; i++)
            {
                source.Append(interfaces[i].ToDisplayString());
                if (i < interfaces.Length - 1)
                {
                    source.Append(", ");
                }
            }
        }

        source.AppendLine($"\n{indent}{{");
    }

    public static void GenerateClassEnd(this StringBuilder source, string indent)
    {
        source.AppendLine($"{indent}}}");
    }

    // TODO: Generalize this to any field using dynamic indentation
    public static void GenerateClassField(
        this StringBuilder source,
        string indent,
        Accessibility accessors,
        InstanceModifier instance,
        string type,
        string variableName,
        string value
    )
    {
        var instanceStr = instance == InstanceModifier.None ? "" : $"{instance.ToFriendlyString()} ";
        var accessorStr = accessors == Accessibility.NotApplicable ? "" : $"{accessors.ToFriendlyString()} ";
        var valueStr = value == null ? "" : $" = {value}";
        source.AppendLine($"{indent}{accessorStr}{instanceStr}{type} {variableName}{valueStr};");
    }
}