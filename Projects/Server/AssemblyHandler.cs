/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AssemblyHandler.cs                                              *
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Server
{
    public static class AssemblyHandler
    {
        private static readonly Dictionary<Assembly, TypeCache> m_TypeCaches = new();
        private static TypeCache m_NullCache;
        public static Assembly[] Assemblies { get; set; }

        public static void LoadScripts(string[] files)
        {
            var assemblies = new Assembly[files.Length];

            for (var i = 0; i < files.Length; i++)
            {
                assemblies[i] = AssemblyLoadContext.Default.LoadFromAssemblyPath(files[i]);
            }

            Assemblies = assemblies;
        }

        public static void Invoke(string method)
        {
            var invoke = new List<MethodInfo>();

            Core.Assembly.AddMethods(method, invoke);

            for (var i = 0; i < Assemblies.Length; i++)
            {
                Assemblies[i].AddMethods(method, invoke);
            }

            invoke.Sort(new CallPriorityComparer());

            for (var i = 0; i < invoke.Count; ++i)
            {
                invoke[i].Invoke(null, null);
            }
        }

        private static void AddMethods(this Assembly assembly, string method, List<MethodInfo> list)
        {
            var types = assembly.GetTypes();

            for (int i = 0; i < types.Length; i++)
            {
                var m = types[i].GetMethod(method, BindingFlags.Static | BindingFlags.Public);
                if (m != null)
                {
                    list.Add(m);
                }
            }
        }


        public static TypeCache GetTypeCache(Assembly asm)
        {
            if (asm == null)
            {
                return m_NullCache ??= new TypeCache(null);
            }

            if (m_TypeCaches.TryGetValue(asm, out var c))
            {
                return c;
            }

            return m_TypeCaches[asm] = new TypeCache(asm);
        }

        private static bool IgnoreCaseTypeComparer(string name, Type type) =>
            type.FullName.InsensitiveEquals(name) || type.Name.InsensitiveEquals(name);

        private static bool CaseTypeComparer(string name, Type type) =>
            type.FullName.EqualsOrdinal(name) || type.Name.EqualsOrdinal(name);

        public static Type FindFirstTypeForName(string name, bool ignoreCase = false, Func<string, Type, bool> predicate = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var types = FindTypesByName(name, ignoreCase);

            if (types.Count == 0)
            {
                return null;
            }

            // Try to find the closest match if there is no predicate.
            // Check for exact match of the FullName or Name
            // Then check for case-insensitive match of FullName or Name
            // Otherwise just return the first entry
            predicate ??= ignoreCase ? IgnoreCaseTypeComparer : CaseTypeComparer;

            foreach (var type in types)
            {
                if (predicate(name, type))
                {
                    return type;
                }
            }

            return null;
        }

        public static List<Type> FindTypesByName(string name, bool ignoreCase = false)
        {
            var types = new List<Type>();

            if (ignoreCase)
            {
                name = name.ToLower();
            }

            for (var i = 0; i < Assemblies.Length; i++)
            {
                foreach (var type in GetTypeCache(Assemblies[i])[name])
                {
                    types.Add(type);
                }
            }

            if (types.Count == 0)
            {
                foreach(var type in GetTypeCache(Core.Assembly)[name])
                {
                    types.Add(type);
                }
            }

            return types;
        }

        public static string EnsureDirectory(string dir)
        {
            var path = Path.Combine(Core.BaseDirectory, dir);
            Directory.CreateDirectory(path);

            return path;
        }
    }

    public class TypeCache
    {
        private readonly Dictionary<string, int[]> m_NameMap = new();
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
                {
                    for (var j = 0; j < alias.Aliases.Length; j++)
                    {
                        addToRefs(i, alias.Aliases[j]);
                        addToRefs(i, alias.Aliases[j].ToLower());
                    }
                }
            }

            foreach (var (key, value) in nameMap)
            {
                m_NameMap[key] = value.ToArray();
            }
        }

        public Enumerator this[string name] => new(name, this);

        public IEnumerable<Type> Types => m_Types;

        public struct Enumerator : IEnumerable<Type>, IEnumerator<Type>
        {
            private readonly TypeCache _cache;
            private readonly int[] _values;
            private int _index;
            private Type _current;

            internal Enumerator(string name, TypeCache cache)
            {
                _cache = cache;
                _values = !cache.m_NameMap.TryGetValue(name, out var values) ? Array.Empty<int>() : values;
                _index = 0;
                _current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                int[] localList = _values;

                while ((uint)_index < (uint)localList.Length)
                {
                    _current = _cache.m_Types[_values[_index++]];

                    if (_current != null)
                    {
                        return true;
                    }
                }

                return false;
            }

            public Type Current => _current!;

            object IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _values.Length + 1)
                    {
                        throw new InvalidOperationException(nameof(_index));
                    }

                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                _index = 0;
                _current = default;
            }

            public IEnumerator<Type> GetEnumerator() => this;

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
