using System;

namespace Server.Mobiles
{
  public class SpawnerType
  {
    public static Type GetType(string name) => ScriptCompiler.FindTypeByName(name);
  }
}