/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: SourceGeneration.Attribute.cs                                   *
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
    public static void GenerateAttribute(
        this StringBuilder source,
        string indent,
        string attrClassName,
        ImmutableArray<TypedConstant> args
    )
    {
        source.Append($"{indent}[{attrClassName}");
        var hasArgs = args.Length > 0;

        if (hasArgs)
        {
            source.Append("(");
        }

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            source.GenerateTypedConstant(arg);
            if (i < args.Length - 1)
            {
                source.Append(", ");
            }
        }

        if (hasArgs)
        {
            source.Append(")");
        }

        source.AppendLine("]");
    }

    public static void GenerateAttribute(this StringBuilder source, AttributeData attr)
    {
        source.Append($"        [{attr.AttributeClass?.Name}");
        var ctorArgs = attr.ConstructorArguments;
        var namedArgs = attr.NamedArguments;
        var hasArgs = ctorArgs.Length + namedArgs.Length > 0;

        if (hasArgs)
        {
            source.Append("(");
        }

        for (var i = 0; i < ctorArgs.Length; i++)
        {
            var arg = ctorArgs[i];
            source.GenerateTypedConstant(arg);
            if (i < ctorArgs.Length - 1)
            {
                source.Append(", ");
            }
        }

        for (var i = 0; i < namedArgs.Length; i++)
        {
            var arg = namedArgs[i];
            source.GenerateNamedArgument(arg);
            if (i < namedArgs.Length - 1)
            {
                source.Append(", ");
            }
        }

        if (hasArgs)
        {
            source.Append(")");
        }

        source.AppendLine("]");
    }

    public static void AggressiveInline(this StringBuilder source, string indent) =>
        source.AppendLine(
            $"{indent}[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]"
        );
}