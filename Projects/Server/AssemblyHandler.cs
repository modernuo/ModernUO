/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: AssemblyHandler.cs - Created: 2019/08/02 - Updated: 2020/01/19  *
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
    private static Dictionary<Assembly, TypeCache> m_TypeCaches = new Dictionary<Assembly, TypeCache>();
    private static TypeCache m_NullCache;
    public static Assembly[] Assemblies { get; set; }

    public static string AssembliesPath = EnsureDirectory("Assemblies");

    public static void LoadScripts(string path = null) =>
      Assemblies = Directory.GetFiles(path ?? AssembliesPath, "*.dll")
        .Select(t => AssemblyLoadContext.Default.LoadFromAssemblyPath(t)).ToArray();

    public static void Invoke(string method)
    {
      List<MethodInfo> invoke = new List<MethodInfo>();

      for (int a = 0; a < Assemblies.Length; ++a)
        invoke.AddRange(Assemblies[a].GetTypes()
          .Select(t => t.GetMethod(method, BindingFlags.Static | BindingFlags.Public)).Where(m => m != null));

      invoke.Sort(new CallPriorityComparer());

      for (int i = 0; i < invoke.Count; ++i)
        invoke[i].Invoke(null, null);
    }

    public static TypeCache GetTypeCache(Assembly asm)
    {
      if (asm == null) return m_NullCache ??= new TypeCache(null);

      if (m_TypeCaches.TryGetValue(asm, out TypeCache c))
        return c;

      return m_TypeCaches[asm] = new TypeCache(asm);
    }

    public static Type FindTypeByFullName(string fullName) => FindTypeByFullName(fullName, true);

    public static Type FindTypeByFullName(string fullName, bool ignoreCase)
    {
      Type type = null;

      for (int i = 0; type == null && i < Assemblies.Length; ++i)
        type = GetTypeCache(Assemblies[i]).GetTypeByFullName(fullName, ignoreCase);

      return type ?? GetTypeCache(Core.Assembly).GetTypeByFullName(fullName, ignoreCase);
    }

    public static Type FindTypeByName(string name) => FindTypeByName(name, true);

    public static Type FindTypeByName(string name, bool ignoreCase)
    {
      Type type = null;

      for (int i = 0; type == null && i < Assemblies.Length; ++i)
        type = GetTypeCache(Assemblies[i]).GetTypeByName(name, ignoreCase);

      return type ?? GetTypeCache(Core.Assembly).GetTypeByName(name, ignoreCase);
    }

    public static string EnsureDirectory(string dir)
    {
      string path = Path.Combine(Core.BaseDirectory, dir);

      if (!Directory.Exists(path))
        Directory.CreateDirectory(path);

      return path;
    }
  }

  public class TypeCache
  {
    public TypeCache(Assembly asm)
    {
      Types = asm?.GetTypes() ?? Type.EmptyTypes;

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

    public Type GetTypeByName(string name, bool ignoreCase) => Names.Get(name, ignoreCase);

    public Type GetTypeByFullName(string fullName, bool ignoreCase) => FullNames.Get(fullName, ignoreCase);
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
