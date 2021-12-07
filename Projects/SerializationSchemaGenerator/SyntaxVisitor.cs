/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SyntaxVisitor.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SerializationGenerator;

namespace SerializationSchemaGenerator;

public class SyntaxVisitor : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;
    private readonly SerializerSyntaxReceiver _syntaxReceiver;

    public SyntaxVisitor(SemanticModel semanticModel, SerializerSyntaxReceiver syntaxReceiver)
    {
        _semanticModel = semanticModel;
        _syntaxReceiver = syntaxReceiver;
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        base.VisitClassDeclaration(node);
        _syntaxReceiver.OnVisitSyntaxNode(node, _semanticModel);
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        base.VisitFieldDeclaration(node);
        _syntaxReceiver.OnVisitSyntaxNode(node, _semanticModel);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        base.VisitPropertyDeclaration(node);
        _syntaxReceiver.OnVisitSyntaxNode(node, _semanticModel);
    }
}