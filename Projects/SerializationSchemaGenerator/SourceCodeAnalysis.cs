/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SourceCodeAnalysis.cs                                           *
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace SerializationSchemaGenerator
{
    public static class SourceCodeAnalysis
    {
        public static List<(Project, Compilation)> GetCompilation(string solutionPath)
        {
            if (!File.Exists(solutionPath) || !solutionPath.EndsWith(".sln", StringComparison.Ordinal))
            {
                throw new FileNotFoundException($"Could not open a valid solution at location {solutionPath}");
            }

            MSBuildLocator.RegisterDefaults();

            var workspace = MSBuildWorkspace.Create();

            var solutionToAnalyze = workspace.OpenSolutionAsync(solutionPath).Result;

            var results = solutionToAnalyze.Projects.AsParallel()
                .Select((project) => (project, project?.GetCompilationAsync().Result))
                .Where((value) => value.Result != null)
                .ToList();

            return results;
        }
    }
}
