/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Server.Logging;

namespace Server;

public static class AssemblyHandler
{
    private static readonly Dictionary<Assembly, TypeCache> m_TypeCaches = new();
    private static TypeCache m_NullCache;

    public static Assembly[] Assemblies { get; set; }

    internal static Assembly AssemblyResolver(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name);
        var assembly = LoadAssemblyByAssemblyName(assemblyName);
        if (assembly == null)
        {
            throw new FileNotFoundException(
                $"Could not load file or assembly {assemblyName}. The system cannot find the file specified. Review the assemblyDirectories field in {ServerConfiguration.ConfigurationFilePath}",
                $"{assemblyName.Name}.dll"
            );
        }

        return assembly;
    }

    private static void EnsureAssemblyDirectories()
    {
        if (ServerConfiguration.AssemblyDirectories.Count == 0)
        {
            ServerConfiguration.AssemblyDirectories.Add("./Assemblies");
            ServerConfiguration.Save();
        }
    }

    public static Assembly LoadAssemblyByAssemblyName(AssemblyName assemblyName)
    {
        if (assemblyName?.Name == null)
        {
            return null;
        }

        var fullName = assemblyName.FullName;
        var fileName = $"{assemblyName.Name}.dll";

        EnsureAssemblyDirectories();
        var assemblyDirectories = ServerConfiguration.AssemblyDirectories;

        Assembly assembly = null;

        foreach (var assemblyDir in assemblyDirectories)
        {
            var assemblyPath = PathUtility.GetFullPath(Path.Combine(assemblyDir, fileName), Core.BaseDirectory);
            if (File.Exists(assemblyPath))
            {
                var assemblyNameCheck = AssemblyName.GetAssemblyName(assemblyPath);
                if (assemblyNameCheck.FullName == fullName)
                {
                    assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
                    break;
                }
            }
        }

        // This forces the type caching to be generated.
        // We need this for world loading to find types by hash.
        GetTypeCache(assembly);

        return assembly;
    }

    public static Assembly LoadAssemblyByFileName(string assemblyFile)
    {
        EnsureAssemblyDirectories();
        var assemblyDirectories = ServerConfiguration.AssemblyDirectories;

        foreach (var assemblyDir in assemblyDirectories)
        {
            var assemblyPath = Path.Combine(assemblyDir, assemblyFile);
            if (File.Exists(assemblyPath))
            {
                return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
            }
        }

        return null;
    }

    public static void LoadAssemblies(string[] files)
    {
        var assemblies = new Assembly[files.Length];

        for (var i = 0; i < files.Length; i++)
        {
            var assemblyFile = files[i];
            var assembly = LoadAssemblyByFileName(assemblyFile);
            if (assembly == null)
            {
                throw new FileNotFoundException(
                    $"Could not load file or assembly {assemblyFile}. The system cannot find the file specified. Review the assemblyDirectories field in {ServerConfiguration.ConfigurationFilePath}",
                    assemblyFile
                );
            }

            assemblies[i] = assembly;
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
            if (m?.GetParameters().Length == 0)
            {
                list.Add(m);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetTypeHash(Type type) => GetTypeHash(type.FullName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetTypeHash(string key) => key == null ? 0 : HashUtility.ComputeHash64(key);

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

    public static Type FindTypeByFullName(string name, bool ignoreCase = true) =>
        FindTypeByName(name, true, ignoreCase);

    public static Type FindTypeByName(string name, bool fullName = false, bool ignoreCase = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        for (var i = 0; i < Assemblies.Length; i++)
        {
            foreach (var type in GetTypeCache(Assemblies[i]).GetTypesByName(name, fullName, ignoreCase))
            {
                return type;
            }
        }

        foreach(var type in GetTypeCache(Core.Assembly).GetTypesByName(name, fullName, ignoreCase))
        {
            return type;
        }

        return null;
    }

    public static Type FindTypeByHash(ulong hash)
    {
        for (var i = 0; i < Assemblies.Length; i++)
        {
            foreach (var type in GetTypeCache(Assemblies[i]).GetTypesByHash(hash, true, false))
            {
                return type;
            }
        }

        foreach(var type in GetTypeCache(Core.Assembly).GetTypesByHash(hash, true, false))
        {
            return type;
        }

        return null;
    }
}

public class TypeCache
{
    private static ILogger logger = LogFactory.GetLogger(typeof(TypeCache));

    private Dictionary<ulong, Type[]> _nameMap = new();
    private Dictionary<ulong, Type[]> _nameMapInsensitive = new();
    private Dictionary<ulong, Type[]> _fullNameMap = new();
    private Dictionary<ulong, Type[]> _fullNameMapInsensitive = new();

    public TypeCache(Assembly asm)
    {
        Types = asm?.GetTypes() ?? Type.EmptyTypes;

        var nameMap = new Dictionary<string, HashSet<Type>>();
        var nameMapInsensitive = new Dictionary<string, HashSet<Type>>();
        var fullNameMap = new Dictionary<string, HashSet<Type>>();
        var fullNameMapInsensitive = new Dictionary<string, HashSet<Type>>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void addTypeToRefs(Type type, string typeName, string fullTypeName)
        {
            AddToRefs(type, typeName, nameMap);
            AddToRefs(type, typeName.ToLower(), nameMapInsensitive);
            AddToRefs(type, fullTypeName, fullNameMap);
            AddToRefs(type, fullTypeName.ToLower(), fullNameMapInsensitive);
        }

        var aliasType = typeof(TypeAliasAttribute);
        for (var i = 0; i < Types.Length; i++)
        {
            var current = Types[i];
            addTypeToRefs(current, current.Name, current.FullName ?? "");
            if (current.GetCustomAttribute(aliasType, false) is TypeAliasAttribute alias)
            {
                for (var j = 0; j < alias.Aliases.Length; j++)
                {
                    var fullTypeName = alias.Aliases[j];
                    var typeName = fullTypeName[(fullTypeName.LastIndexOf('.')+1)..];
                    addTypeToRefs(current, typeName, fullTypeName);
                }
            }
        }

        foreach (var (key, value) in nameMap)
        {
            _nameMap[HashUtility.ComputeHash64(key)] = value.ToArray();
        }

        foreach (var (key, value) in nameMapInsensitive)
        {
            _nameMapInsensitive[HashUtility.ComputeHash64(key)] = value.ToArray();
        }

        foreach (var (key, value) in fullNameMap)
        {
            var values = value.ToArray();
            _fullNameMap[HashUtility.ComputeHash64(key)] = value.ToArray();
#if DEBUG
            if (values.Length > 1)
            {
                for (var i = 0; i < values.Length; i++)
                {
                    var type = values[i];
                    logger.Warning(
                        "Duplicate type {Type1} for {Name}.",
                        type,
                        key
                    );
                }
            }
#endif
        }

        foreach (var (key, value) in fullNameMapInsensitive)
        {
            _fullNameMapInsensitive[HashUtility.ComputeHash64(key)] = value.ToArray();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddToRefs(Type type, string key, Dictionary<string, HashSet<Type>> map)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (map.TryGetValue(key, out var refs))
        {
            refs.Add(type);
        }
        else
        {
            refs = new HashSet<Type> { type };
            map.Add(key, refs);
        }
    }

    public Type[] Types { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TypeEnumerator GetTypesByName(string name, bool full, bool ignoreCase) => new(name, this, full, ignoreCase);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TypeEnumerator GetTypesByHash(ulong hash, bool full, bool ignoreCase) => new(hash, this, full, ignoreCase);

    public ref struct TypeEnumerator
    {
        private Type[] _values;
        private int _index;
        private Type _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TypeEnumerator(string name, TypeCache cache, bool full, bool ignoreCase)
            : this(HashUtility.ComputeHash64(ignoreCase ? name.ToLower() : name), cache, full, ignoreCase)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TypeEnumerator(ulong hash, TypeCache cache, bool full, bool ignoreCase)
        {
            if (ignoreCase)
            {
                var map = full ? cache._fullNameMapInsensitive : cache._nameMapInsensitive;
                _values = map.TryGetValue(hash, out var values) ? values : Array.Empty<Type>();
            }
            else
            {
                var map = full ? cache._fullNameMap : cache._nameMap;
                _values = map.TryGetValue(hash, out var values) ? values : Array.Empty<Type>();
            }

            _index = 0;
            _current = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TypeEnumerator GetEnumerator() => this;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if ((uint)_index < (uint)_values.Length)
            {
                _current = _values[_index++];
                return true;
            }

            return false;
        }

        public Type Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }
}
