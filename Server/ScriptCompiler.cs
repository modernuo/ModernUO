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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Server
{
  public static class ScriptCompiler
  {
    private static List<string> m_AdditionalReferences = new List<string>();

    private static Dictionary<Assembly, TypeCache> m_TypeCaches = new Dictionary<Assembly, TypeCache>();
    private static TypeCache m_NullCache;
    public static Assembly[] Assemblies{ get; set; }

    public static string[] GetReferenceAssemblies()
    {
      List<string> list = new List<string>();

      string path = Path.Combine(Core.BaseDirectory, "Data/Assemblies.cfg");

      if (File.Exists(path))
        using (StreamReader ip = new StreamReader(path))
        {
          string line;

          while ((line = ip.ReadLine()) != null)
            if (line.Length > 0 && !line.StartsWith("#"))
              list.Add(line);
        }

      list.Add(Core.ExePath);

      list.AddRange(m_AdditionalReferences);

      return list.ToArray();
    }

    public static string GetCompilerOptions(bool debug)
    {
      StringBuilder sb = null;

      AppendCompilerOption(ref sb, "/unsafe");

      if (!debug)
        AppendCompilerOption(ref sb, "/optimize");

#if MONO
			AppendCompilerOption( ref sb, "/d:MONO" );
#endif

      if (Core.Is64Bit)
        AppendCompilerOption(ref sb, "/d:x64");

#if NEWTIMERS
      AppendCompilerOption(ref sb, "/d:NEWTIMERS");
#endif

      return sb?.ToString();
    }

    private static void AppendCompilerOption(ref StringBuilder sb, string define)
    {
      if (sb == null)
        sb = new StringBuilder();
      else
        sb.Append(' ');

      sb.Append(define);
    }

    private static byte[] GetHashCode(string compiledFile, string[] scriptFiles, bool debug)
    {
      using (MemoryStream ms = new MemoryStream())
      {
        using (BinaryWriter bin = new BinaryWriter(ms))
        {
          FileInfo fileInfo = new FileInfo(compiledFile);

          bin.Write(fileInfo.LastWriteTimeUtc.Ticks);

          foreach (string scriptFile in scriptFiles)
          {
            fileInfo = new FileInfo(scriptFile);

            bin.Write(fileInfo.LastWriteTimeUtc.Ticks);
          }

          bin.Write(debug);
          bin.Write(Core.Version.ToString());

          ms.Position = 0;

          using (SHA1 sha1 = SHA1.Create())
          {
            return sha1.ComputeHash(ms);
          }
        }
      }
    }

    public static bool CompileCSScripts(out Assembly assembly, bool cache = true, bool debug = false)
    {
      Console.Write("Scripts: Compiling C# scripts...");
      string[] files = GetScripts("*.cs");

      if (files.Length == 0)
      {
        Console.WriteLine("no files found.");
        assembly = null;
        return true;
      }

      if (File.Exists("Scripts/Output/Scripts.CS.dll"))
        if (cache && File.Exists("Scripts/Output/Scripts.CS.hash"))
          try
          {
            byte[] hashCode = GetHashCode("Scripts/Output/Scripts.CS.dll", files, debug);

            using (FileStream fs = new FileStream("Scripts/Output/Scripts.CS.hash", FileMode.Open,
              FileAccess.Read, FileShare.Read))
            {
              using (BinaryReader bin = new BinaryReader(fs))
              {
                byte[] bytes = bin.ReadBytes(hashCode.Length);

                if (bytes.Length == hashCode.Length)
                {
                  bool valid = true;

                  for (int i = 0; i < bytes.Length; ++i)
                    if (bytes[i] != hashCode[i])
                    {
                      valid = false;
                      break;
                    }

                  if (valid)
                  {
                    assembly = Assembly.LoadFrom("Scripts/Output/Scripts.CS.dll");

                    if (!m_AdditionalReferences.Contains(assembly.Location))
                      m_AdditionalReferences.Add(assembly.Location);

                    Console.WriteLine("done (cached)");

                    return true;
                  }
                }
              }
            }
          }
          catch
          {
            // ignored
          }

      DeleteFiles("Scripts.CS*.dll");

      using (CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp"))
      {
        string path = GetUnusedPath("Scripts.CS");

        CompilerParameters parms = new CompilerParameters(GetReferenceAssemblies(), path, debug);

        string options = GetCompilerOptions(debug);

        if (options != null)
          parms.CompilerOptions = options;

        if (Core.HaltOnWarning)
          parms.WarningLevel = 4;

        if (Core.Unix)
        {
          parms.CompilerOptions = $"{parms.CompilerOptions} /nowarn:169,219,414 /recurse:Scripts/*.cs";
          files = new string[0];
        }

        CompilerResults results = provider.CompileAssemblyFromFile(parms, files);
        m_AdditionalReferences.Add(path);

        Display(results);

        if (results.Errors.Count > 0)
        {
          if (!Core.Unix)
          {
            assembly = null;
            return false;
          }

          foreach (CompilerError err in results.Errors)
            if (!err.IsWarning)
            {
              assembly = null;
              return false;
            }
        }


        if (cache && Path.GetFileName(path) == "Scripts.CS.dll")
          try
          {
            byte[] hashCode = GetHashCode(path, files, debug);

            using (FileStream fs = new FileStream("Scripts/Output/Scripts.CS.hash", FileMode.Create,
              FileAccess.Write, FileShare.None))
            {
              using (BinaryWriter bin = new BinaryWriter(fs))
              {
                bin.Write(hashCode, 0, hashCode.Length);
              }
            }
          }
          catch
          {
            // ignored
          }

        assembly = results.CompiledAssembly;
        return true;
      }
    }

    public static void Display(CompilerResults results)
    {
      if (results.Errors.Count > 0)
      {
        Dictionary<string, List<CompilerError>> errors =
          new Dictionary<string, List<CompilerError>>(results.Errors.Count, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, List<CompilerError>> warnings =
          new Dictionary<string, List<CompilerError>>(results.Errors.Count, StringComparer.OrdinalIgnoreCase);

        foreach (CompilerError e in results.Errors)
        {
          string file = e.FileName;

          // Ridiculous. FileName is null if the warning/error is internally generated in csc.
          if (string.IsNullOrEmpty(file))
          {
            Console.WriteLine("ScriptCompiler: {0}: {1}", e.ErrorNumber, e.ErrorText);
            continue;
          }

          Dictionary<string, List<CompilerError>> table = e.IsWarning ? warnings : errors;

          if (!table.TryGetValue(file, out List<CompilerError> list))
            table[file] = list = new List<CompilerError>();

          list.Add(e);
        }

        Console.WriteLine(errors.Count > 0 ? "failed ({0} errors, {1} warnings)" : "done ({0} errors, {1} warnings)",
          errors.Count, warnings.Count);

        string scriptRoot =
          Path.GetFullPath(Path.Combine(Core.BaseDirectory, "Scripts" + Path.DirectorySeparatorChar));
        Uri scriptRootUri = new Uri(scriptRoot);

        Utility.PushColor(ConsoleColor.Yellow);

        if (warnings.Count > 0)
          Console.WriteLine("Warnings:");

        foreach (KeyValuePair<string, List<CompilerError>> kvp in warnings)
        {
          string fileName = kvp.Key;
          List<CompilerError> list = kvp.Value;

          string fullPath = Path.GetFullPath(fileName);
          string usedPath =
            Uri.UnescapeDataString(scriptRootUri.MakeRelativeUri(new Uri(fullPath)).OriginalString);

          Console.WriteLine(" + {0}:", usedPath);

          Utility.PushColor(ConsoleColor.DarkYellow);

          foreach (CompilerError e in list)
            Console.WriteLine("    {0}: Line {1}: {2}", e.ErrorNumber, e.Line, e.ErrorText);

          Utility.PopColor();
        }

        Utility.PopColor();

        Utility.PushColor(ConsoleColor.Red);

        if (errors.Count > 0)
          Console.WriteLine("Errors:");

        foreach (KeyValuePair<string, List<CompilerError>> kvp in errors)
        {
          string fileName = kvp.Key;
          List<CompilerError> list = kvp.Value;

          string fullPath = Path.GetFullPath(fileName);
          string usedPath =
            Uri.UnescapeDataString(scriptRootUri.MakeRelativeUri(new Uri(fullPath)).OriginalString);

          Console.WriteLine(" + {0}:", usedPath);

          Utility.PushColor(ConsoleColor.DarkRed);

          foreach (CompilerError e in list)
            Console.WriteLine("    {0}: Line {1}: {2}", e.ErrorNumber, e.Line, e.ErrorText);

          Utility.PopColor();
        }

        Utility.PopColor();
      }
      else
      {
        Console.WriteLine("done (0 errors, 0 warnings)");
      }
    }

    public static string GetUnusedPath(string name)
    {
      string path = Path.Combine(Core.BaseDirectory, $"Scripts/Output/{name}.dll");

      for (int i = 2; File.Exists(path) && i <= 1000; ++i)
        path = Path.Combine(Core.BaseDirectory, $"Scripts/Output/{name}.{i}.dll");

      return path;
    }

    public static void DeleteFiles(string mask)
    {
      try
      {
        string[] files = Directory.GetFiles(Path.Combine(Core.BaseDirectory, "Scripts/Output"), mask);

        foreach (string file in files)
          try
          {
            File.Delete(file);
          }
          catch
          {
            // ignored
          }
      }
      catch
      {
        // ignored
      }
    }

    public static bool Compile(bool debug, bool cache = true)
    {
      EnsureDirectory("Scripts/");
      EnsureDirectory("Scripts/Output/");

      if (m_AdditionalReferences.Count > 0)
        m_AdditionalReferences.Clear();

      List<Assembly> assemblies = new List<Assembly>();

      if (CompileCSScripts(out Assembly assembly, cache, debug))
      {
        if (assembly != null)
          assemblies.Add(assembly);
      }
      else
      {
        return false;
      }

      if (assemblies.Count == 0)
        return false;

      Assemblies = assemblies.ToArray();

      Console.Write("Scripts: Verifying...");

      Stopwatch watch = Stopwatch.StartNew();

      Core.VerifySerialization();

      watch.Stop();

      Console.WriteLine("done ({0} items, {1} mobiles) ({2:F2} seconds)", Core.ScriptItems, Core.ScriptMobiles,
        watch.Elapsed.TotalSeconds);

      return true;
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

    public static string[] GetScripts(string filter)
    {
      List<string> list = new List<string>();

      GetScripts(list, Path.Combine(Core.BaseDirectory, "Scripts"), filter);

      return list.ToArray();
    }

    public static void GetScripts(List<string> list, string path, string filter)
    {
      foreach (string dir in Directory.GetDirectories(path))
        GetScripts(list, dir, filter);

      list.AddRange(Directory.GetFiles(path, filter));
    }

    private delegate CompilerResults Compiler(bool debug);
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
