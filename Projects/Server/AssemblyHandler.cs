/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: AssemblyHandler.cs - Created: 2019/08/02 - Updated: 2020/05/09  *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Server
{
    public static class AssemblyHandler
    {
        private static readonly Dictionary<Assembly, TypeCache> m_TypeCaches = new Dictionary<Assembly, TypeCache>();
        private static TypeCache m_NullCache;
        public static Assembly[] Assemblies { get; set; }

        public static void LoadScripts(string[] files)
        {
            var assemblies = new Assembly[files.Length];

            for (var i = 0; i < files.Length; i++)
                assemblies[i] = AssemblyLoadContext.Default.LoadFromAssemblyPath(files[i]);

            Assemblies = assemblies;
        }

        public static void Invoke(string method)
        {
            var invoke = new List<MethodInfo>();

            for (var a = 0; a < Assemblies.Length; ++a)
                invoke.AddRange(
                    Assemblies[a]
                        .GetTypes()
                        .Select(t => t.GetMethod(method, BindingFlags.Static | BindingFlags.Public))
                        .Where(m => m != null)
                );

            invoke.Sort(new CallPriorityComparer());

            for (var i = 0; i < invoke.Count; ++i)
                invoke[i].Invoke(null, null);
        }

        public static TypeCache GetTypeCache(Assembly asm)
        {
            if (asm == null)
                return m_NullCache ??= new TypeCache(null);

            if (m_TypeCaches.TryGetValue(asm, out var c))
                return c;

            return m_TypeCaches[asm] = new TypeCache(asm);
        }

        public static Type FindFirstTypeForName(string name, bool ignoreCase = false, Func<Type, bool> predicate = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            var types = FindTypesByName(name, ignoreCase).ToList();
            if (types.Count == 0)
                return null;

            if (predicate != null)
                return types.FirstOrDefault(predicate);
            if (types.Count == 1)
                return types[0];
            // Try to find the closest match if there is no predicate.
            // Check for exact match of the FullName or Name
            // Then check for case-insensitive match of FullName or Name
            // Otherwise just return the first entry
            var stringComparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

            return types.FirstOrDefault(
                       x =>
                           stringComparer.Equals(x.FullName, name) || stringComparer.Equals(x.Name, name)
                   ) ??
                   types[0];
        }

        public static List<Type> FindTypesByName(string name, bool ignoreCase = false)
        {
            var types = new List<Type>();

            if (ignoreCase)
                name = name.ToLower();

            for (var i = 0; i < Assemblies.Length; i++)
                types.AddRange(GetTypeCache(Assemblies[i])[name]);

            if (types.Count == 0)
                types.AddRange(GetTypeCache(Core.Assembly)[name]);

            return types;
        }

        public static string EnsureDirectory(string dir)
        {
            var path = Path.Combine(Core.BaseDirectory, dir);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }
    }

    public class TypeCache
    {
        private readonly Dictionary<string, int[]> m_NameMap = new Dictionary<string, int[]>();
        private readonly Type[] m_Types;

        public TypeCache(Assembly asm)
        {
            m_Types = asm?.GetTypes() ?? Type.EmptyTypes;

            var nameMap = new Dictionary<string, HashSet<int>>();
            HashSet<int> refs;
            Action<int, string> addToRefs = (index, key) =>
            {
                if (nameMap.TryGetValue(key, out refs))
                {
                    refs.Add(index);
                }
                else
                {
                    refs = new HashSet<int> { index };
                    nameMap.Add(key, refs);
                }
            };

            var aliasType = typeof(TypeAliasAttribute);
            for (var i = 0; i < m_Types.Length; i++)
            {
                var current = m_Types[i];
                addToRefs(i, current.Name);
                addToRefs(i, current.Name.ToLower());
                addToRefs(i, current.FullName);
                addToRefs(i, current.FullName?.ToLower());
                if (current.GetCustomAttribute(aliasType, false) is TypeAliasAttribute alias)
                    for (var j = 0; j < alias.Aliases.Length; j++)
                    {
                        addToRefs(i, alias.Aliases[j]);
                        addToRefs(i, alias.Aliases[j].ToLower());
                    }
            }

            foreach (var (key, value) in nameMap)
                m_NameMap[key] = value.ToArray();
        }

        public IEnumerable<Type> Types => m_Types;
        public IEnumerable<string> Names => m_NameMap.Keys;

        public IEnumerable<Type> this[string name] =>
            m_NameMap.TryGetValue(name, out var value) ? value.Select(x => m_Types[x]) : Array.Empty<Type>();
    }
}
