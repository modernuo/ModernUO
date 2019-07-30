/***************************************************************************
 *                             ScriptCompiler.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
#if NETCORE
using System.Runtime.Loader;
#endif

namespace Server
{
  public static class ScriptCompiler
  {
    private static Dictionary<Assembly, TypeCache> m_TypeCaches = new Dictionary<Assembly, TypeCache>();
    private static TypeCache m_NullCache;
    public static Assembly[] Assemblies { get; set; }

    // TODO: Make this more robust
#if NETCORE
    public static string ScriptsPath = "Scripts.CS.NETCORE.dll";
#else
    public static string ScriptsPath = "Scripts.CS.NETFULL.dll";
#endif

    public static bool LoadScripts()
    {
      string scriptsPath = Path.Combine(Core.BaseDirectory, ScriptsPath);
      if (File.Exists(ScriptsPath))
      {
#if NETCORE
        Assembly scripts = AssemblyLoadContext.Default.LoadFromAssemblyPath(scriptsPath);
#else
        Assembly scripts = Assembly.LoadFrom(scriptsPath);
#endif
        Assemblies = new Assembly[] { scripts };
        return true;
      }

      return false;
    }

    public static void Invoke(string method)
    {
      List<MethodInfo> invoke = new List<MethodInfo>();

      for (int a = 0; a < Assemblies.Length; ++a)
      {
        Type[] types = Assemblies[a].GetTypes();

        for (int i = 0; i < types.Length; ++i)
        {
          MethodInfo m = types[i].GetMethod(method, BindingFlags.Static | BindingFlags.Public);

          if (m != null)
            invoke.Add(m);
        }
      }

      invoke.Sort(new CallPriorityComparer());

      for (int i = 0; i < invoke.Count; ++i)
        invoke[i].Invoke(null, null);
    }

    public static TypeCache GetTypeCache(Assembly asm)
    {
      if (asm == null)
      {
        return m_NullCache ?? (m_NullCache = new TypeCache(null));
      }
      if (!m_TypeCaches.TryGetValue(asm, out TypeCache c))
        m_TypeCaches[asm] = c = new TypeCache(asm);

      return c;
    }

    public static Type FindTypeByFullName(string fullName)
    {
      return FindTypeByFullName(fullName, true);
    }

    public static Type FindTypeByFullName(string fullName, bool ignoreCase)
    {
      Type type = null;

      for (int i = 0; type == null && i < Assemblies.Length; ++i)
        type = GetTypeCache(Assemblies[i]).GetTypeByFullName(fullName, ignoreCase);

      return type ?? GetTypeCache(Core.Assembly).GetTypeByFullName(fullName, ignoreCase);
    }

    public static Type FindTypeByName(string name)
    {
      return FindTypeByName(name, true);
    }

    public static Type FindTypeByName(string name, bool ignoreCase)
    {
      Type type = null;

      for (int i = 0; type == null && i < Assemblies.Length; ++i)
        type = GetTypeCache(Assemblies[i]).GetTypeByName(name, ignoreCase);

      return type ?? GetTypeCache(Core.Assembly).GetTypeByName(name, ignoreCase);
    }

    public static void EnsureDirectory(string dir)
    {
      string path = Path.Combine(Core.BaseDirectory, dir);

      if (!Directory.Exists(path))
        Directory.CreateDirectory(path);
    }
  }

  public class TypeCache
  {
    public TypeCache(Assembly asm)
    {
      Types = asm == null ? Type.EmptyTypes : asm.GetTypes();

      Names = new TypeTable(Types.Length);
      FullNames = new TypeTable(Types.Length);

      Type typeofTypeAliasAttribute = typeof(TypeAliasAttribute);

      for (int i = 0; i < Types.Length; ++i)
      {
        Type type = Types[i];

        Names.Add(type.Name, type);
        FullNames.Add(type.FullName, type);

        if (type.IsDefined(typeofTypeAliasAttribute, false))
        {
          object[] attrs = type.GetCustomAttributes(typeofTypeAliasAttribute, false);

          if (attrs.Length > 0 && attrs[0] is TypeAliasAttribute attr)
            for (int j = 0; j < attr.Aliases.Length; ++j)
              FullNames.Add(attr.Aliases[j], type);
        }
      }
    }

    public Type[] Types{ get; }

    public TypeTable Names{ get; }

    public TypeTable FullNames{ get; }

    public Type GetTypeByName(string name, bool ignoreCase)
    {
      return Names.Get(name, ignoreCase);
    }

    public Type GetTypeByFullName(string fullName, bool ignoreCase)
    {
      return FullNames.Get(fullName, ignoreCase);
    }
  }

  public class TypeTable
  {
    private Dictionary<string, Type> m_Sensitive, m_Insensitive;

    public TypeTable(int capacity)
    {
      m_Sensitive = new Dictionary<string, Type>(capacity);
      m_Insensitive = new Dictionary<string, Type>(capacity, StringComparer.OrdinalIgnoreCase);
    }

    public void Add(string key, Type type)
    {
      m_Sensitive[key] = type;
      m_Insensitive[key] = type;
    }

    public Type Get(string key, bool ignoreCase)
    {
      Type t;

      if (ignoreCase)
        m_Insensitive.TryGetValue(key, out t);
      else
        m_Sensitive.TryGetValue(key, out t);

      return t;
    }
  }
}
