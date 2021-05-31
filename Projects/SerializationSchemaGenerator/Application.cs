/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Application.cs                                                  *
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
using System.IO;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using SerializationGenerator;

namespace SerializationSchemaGenerator
{
    public static class Application
    {
        public static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Must specify the path to the solution, project name, and path to migrations");
            }

            var solutionPath = args[0];
            var projectName = args[1];
            var migrationPath = args[2];

            var compilation = SourceCodeAnalysis.GetCompilation(solutionPath, projectName);
            if (compilation == null)
            {
                throw new FileLoadException("Unable to load solution and project.");
            }

            Directory.CreateDirectory(migrationPath);

            var syntaxReceiver = new SerializerSyntaxReceiver();

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = syntaxTree.GetRoot();

                void VisitSyntaxNode(SyntaxNode node)
                {
                    syntaxReceiver.OnVisitSyntaxNode(node, semanticModel);
                }

                var syntaxVisitor = new SyntaxVisitor(VisitSyntaxNode);
                syntaxVisitor.Visit(root);
            }

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                AllowTrailingCommas = true,
                IgnoreNullValues = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var serializableTypes = syntaxReceiver.SerializableList;

            foreach (var (classSymbol, (attributeData, fieldsList)) in syntaxReceiver.ClassAndFields)
            {
                compilation.GenerateSchema(
                    classSymbol,
                    attributeData,
                    fieldsList.ToImmutableArray(),
                    migrationPath,
                    jsonOptions,
                    serializableTypes
                );
            }
        }
    }
}
