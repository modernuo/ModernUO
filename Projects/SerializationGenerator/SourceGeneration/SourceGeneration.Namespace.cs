/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: SourceGeneration.Namespace.cs                                   *
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
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SerializationGenerator
{
    public static partial class SourceGeneration
    {
        public static void GenerateUsings(this StringBuilder source, IImmutableList<ITypeSymbol> typesUsed)
        {
            var enumerable = typesUsed
                .Select(t => t.ContainingNamespace.Name)
                .Distinct()
                .OrderByDescending(t => t);

            foreach (var t in enumerable)
            {
                source.Insert(0, $"using {t}{Environment.NewLine}");
            }
        }

        public static void GenerateNamespaceStart(this StringBuilder source, string namespaceName)
        {
            source.AppendLine($@"namespace {namespaceName}
{{");
        }

        public static void GenerateNamespaceEnd(this StringBuilder source)
        {
            source.AppendLine("}");
        }
    }
}
