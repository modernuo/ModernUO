using System;

namespace Server.Engines.Spawners
{
  public class SpawnerType
  {
    public static Type GetType(string name) => AssemblyHandler.FindFirstTypeForName(name);
  }
}
