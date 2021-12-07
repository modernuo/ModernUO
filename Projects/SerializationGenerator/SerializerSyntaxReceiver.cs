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
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SerializationGenerator;

public class SerializerSyntaxReceiver : ISyntaxContextReceiver
{
#pragma warning disable RS1024
    public Dictionary<INamedTypeSymbol, (AttributeData?, List<ISymbol>)> ClassAndFields { get; } = new(SymbolEqualityComparer.Default);
    public Dictionary<INamedTypeSymbol, (AttributeData?, List<ISymbol>)> EmbeddedClassAndFields { get; } = new(SymbolEqualityComparer.Default);
#pragma warning restore RS1024

    public ImmutableArray<INamedTypeSymbol> SerializableList => ClassAndFields.Keys.ToImmutableArray();

    public ImmutableArray<INamedTypeSymbol> EmbeddedSerializableList => EmbeddedClassAndFields.Keys.ToImmutableArray();

    public void OnVisitSyntaxNode(SyntaxNode node, SemanticModel semanticModel)
    {
        var compilation = semanticModel.Compilation;

        if (node is ClassDeclarationSyntax { AttributeLists: { Count: > 0 } } classDeclarationSyntax)
        {
            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
            {
                return;
            }

            if (classSymbol.IsEmbeddedSerializable(compilation, out var attrData))
            {
                if (EmbeddedClassAndFields.TryGetValue(classSymbol, out var value))
                {
                    var (_, fieldsList) = value;
                    EmbeddedClassAndFields[classSymbol] = (attrData, fieldsList);
                }
                else
                {
                    EmbeddedClassAndFields.Add(classSymbol, (attrData, new List<ISymbol>()));
                }
            }
            else if (classSymbol.WillBeSerializable(compilation, out attrData))
            {
                if (ClassAndFields.TryGetValue(classSymbol, out var value))
                {
                    var (_, fieldsList) = value;
                    ClassAndFields[classSymbol] = (attrData, fieldsList);
                }
                else
                {
                    ClassAndFields.Add(classSymbol, (attrData, new List<ISymbol>()));
                }
            }

            return;
        }

        if (node is FieldDeclarationSyntax { AttributeLists: { Count: > 0 } } fieldDeclarationSyntax)
        {
            foreach (var variable in fieldDeclarationSyntax.Declaration.Variables)
            {
                if (semanticModel.GetDeclaredSymbol(variable) is IFieldSymbol fieldSymbol)
                {
                    AddFieldOrProperty(fieldSymbol, compilation);
                }
            }

            return;
        }

        if (node is PropertyDeclarationSyntax { AttributeLists: { Count: > 0 } } propertyDeclarationSyntax)
        {
            if (semanticModel.GetDeclaredSymbol(propertyDeclarationSyntax) is IPropertySymbol propertySymbol)
            {
                AddFieldOrProperty(propertySymbol, compilation);
            }
        }
    }

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context) =>
        OnVisitSyntaxNode(context.Node, context.SemanticModel);

    private void AddFieldOrProperty(ISymbol symbol, Compilation compilation)
    {
        var serializableFieldAttr = compilation.GetTypeByMetadataName(SymbolMetadata.SERIALIZABLE_FIELD_ATTRIBUTE);
        var parentAttr = compilation.GetTypeByMetadataName(SymbolMetadata.SERIALIZABLE_PARENT_ATTRIBUTE);

        if (symbol.GetAttribute(serializableFieldAttr) == null && symbol.GetAttribute(parentAttr) == null)
        {
            return;
        }

        var classSymbol = symbol.ContainingType;
        if (ClassAndFields.TryGetValue(classSymbol, out var value))
        {
            var (_, fieldsList) = value;
            fieldsList.Add(symbol);
            return;
        }

        if (EmbeddedClassAndFields.TryGetValue(classSymbol, out value))
        {
            var (_, fieldsList) = value;
            fieldsList.Add(symbol);
            return;
        }

        if (classSymbol.WillBeSerializable(compilation, out var attrData))
        {
            ClassAndFields.Add(classSymbol, (attrData, new List<ISymbol> { symbol }));
        }
        else if (classSymbol.IsEmbeddedSerializable(compilation, out attrData))
        {
            EmbeddedClassAndFields.Add(classSymbol, (attrData, new List<ISymbol> { symbol }));
        }
    }
}