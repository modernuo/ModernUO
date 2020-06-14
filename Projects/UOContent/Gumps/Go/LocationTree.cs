using System.Collections.Generic;
using System.IO;
using Server.Json;

namespace Server.Gumps
{
  public class LocationTree
  {
    public LocationTree(string fileName, Map map)
    {
      LastBranch = new Dictionary<Mobile, GoCategory>();
      Map = map;

      string path = Path.Combine("Data/Locations/", fileName);

      Root = File.Exists(path) ? JsonConfig.Deserialize<GoCategory>(path) : null;
      SetParents(Root);
    }

    public Dictionary<Mobile, GoCategory> LastBranch { get; }

    public Map Map { get; }

    public GoCategory Root { get; }

    private static void SetParents(GoCategory parent)
    {
      for (int i = 0; i < parent.Categories.Length; i++)
      {
        GoCategory category = parent.Categories[i];
        category.Parent = parent;
        SetParents(category);
      }

      for (int j = 0; j < parent.Locations.Length; j++)
        parent.Locations[j].Parent = parent;
    }
  }
}
