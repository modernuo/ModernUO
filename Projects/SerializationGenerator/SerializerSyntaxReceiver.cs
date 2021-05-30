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
#pragma warning disable RS1024
        public Dictionary<INamedTypeSymbol, List<ISymbol>> ClassAndFields { get; } = new(SymbolEqualityComparer.Default);
#pragma warning restore RS1024

        public static HashSet<string> AttributeTypes { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode node, SemanticModel semanticModel)
        {
            if (node is ClassDeclarationSyntax { AttributeLists: { Count: > 0 } } classDeclarationSyntax)
            {
                if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
                {
                    return;
                }

                if (classSymbol.GetAttributes().Any(ad => AttributeTypes.Contains(ad.AttributeClass?.ToDisplayString()) && !ClassAndFields.ContainsKey(classSymbol)))
                {
                    ClassAndFields.Add(classSymbol, new List<ISymbol>());
                }

                return;
            }

            if (node is FieldDeclarationSyntax { AttributeLists: { Count: > 0 } } fieldDeclarationSyntax)
            {
                foreach (var variable in fieldDeclarationSyntax.Declaration.Variables)
                {
                    if (semanticModel.GetDeclaredSymbol(variable) is IFieldSymbol fieldSymbol)
                    {
                        AddFieldOrProperty(fieldSymbol);
                    }
                }
            }
            else if (node is PropertyDeclarationSyntax { AttributeLists: { Count: > 0 } } propertyDeclarationSyntax)
            {
                if (semanticModel.GetDeclaredSymbol(propertyDeclarationSyntax) is IPropertySymbol propertySymbol)
                {
                    AddFieldOrProperty(propertySymbol);
                }
            }
        }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context) =>
            OnVisitSyntaxNode(context.Node, context.SemanticModel);

        private void AddFieldOrProperty(ISymbol symbol)
        {
            if (symbol.GetAttributes().Any(ad => AttributeTypes.Contains(ad.AttributeClass?.ToDisplayString())))
            {
                var classSymbol = symbol.ContainingType;
                if (ClassAndFields.TryGetValue(classSymbol, out var fieldsList))
                {
                    fieldsList.Add(symbol);
                }
                else
                {
                    ClassAndFields.Add(classSymbol, new List<ISymbol> { symbol });
                }
            }
        }
    }
}
