/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: SyntaxReceiver.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SerializationGenerator
{
    public class SerializerSyntaxReceiver : ISyntaxContextReceiver
    {
        public List<IFieldSymbol> Fields { get; } = new();

        public static HashSet<string> AttributeTypes { get; } = new();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax
                && fieldDeclarationSyntax.AttributeLists.Count > 0)
            {
                foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
                {
                    if (context.SemanticModel.GetDeclaredSymbol(variable) is not IFieldSymbol fieldSymbol)
                    {
                        return;
                    }

                    if (fieldSymbol.GetAttributes().Any(ad => AttributeTypes.Contains(ad.AttributeClass?.ToDisplayString())))
                    {
                        Fields.Add(fieldSymbol);
                    }
                }
            }
        }
    }
}
