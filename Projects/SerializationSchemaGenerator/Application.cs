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
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SerializationGenerator;

namespace SerializationSchemaGenerator
{
    public static class Application
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                throw new ArgumentException("Usage: dotnet SerializationSchemaGenerator.dll <path to solution>");
            }

            var solutionPath = args[0];

            Parallel.ForEach(
                SourceCodeAnalysis.GetCompilation(solutionPath),
                (projectCompilation) =>
                {
                    var (project, compilation) = projectCompilation;
                    if (project.Name.EndsWith(".Tests", StringComparison.Ordinal) || project.Name == "Benchmarks")
                    {
                        return;
                    }

                    var projectFile = new FileInfo(project.FilePath!);
                    var projectPath = projectFile.Directory?.FullName;
                    var migrationPath = Path.Join(projectPath, "Migrations");
                    Directory.CreateDirectory(migrationPath);

                    var syntaxReceiver = new SerializerSyntaxReceiver();

                    foreach (var syntaxTree in compilation.SyntaxTrees)
                    {
                        var root = syntaxTree.GetRoot();
                        var syntaxVisitor = new SyntaxVisitor(compilation.GetSemanticModel(syntaxTree), syntaxReceiver);
                        syntaxVisitor.Visit(root);
                    }

                    var jsonOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        AllowTrailingCommas = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };

                    var serializableTypes = syntaxReceiver.SerializableList;
                    var embeddedSerializableTypes = syntaxReceiver.EmbeddedSerializableList;

                    foreach (var (classSymbol, (attributeData, fieldsList)) in syntaxReceiver.ClassAndFields)
                    {
                        var source = compilation.GenerateSerializationPartialClass(
                            classSymbol,
                            attributeData,
                            migrationPath,
                            false,
                            jsonOptions,
                            fieldsList.ToImmutableArray(),
                            serializableTypes,
                            embeddedSerializableTypes
                        );
                    }

                    foreach (var (classSymbol, (attributeData, fieldsList)) in syntaxReceiver.EmbeddedClassAndFields)
                    {
                        var source = compilation.GenerateSerializationPartialClass(
                            classSymbol,
                            attributeData,
                            migrationPath,
                            true,
                            jsonOptions,
                            fieldsList.ToImmutableArray(),
                            serializableTypes,
                            embeddedSerializableTypes
                        );
                    }
                }
            );
        }
    }
}
