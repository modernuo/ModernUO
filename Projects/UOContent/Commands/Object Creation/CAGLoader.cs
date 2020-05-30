using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Server.Json;

namespace Server.Commands
{
  public static class CAGLoader
  {
    public static CAGCategory Load()
    {
      var root = new CAGCategory("Add Menu");
      var path = Path.Combine(Core.BaseDirectory, "Data/objects.json");

      List<CAGJson> list = JsonConfig.Deserialize<List<CAGJson>>(path);

      // Not an optimized solution
      foreach (var cag in list)
      {
        var parent = root;
        var categories = cag.Category.Split(".");
        for (int i = 0; i < categories.Length; i++)
        {
          var category = categories[i];

          for (int j = 0; j < parent.Nodes.Length; j++)
          {
            var node = parent.Nodes[i];
            if (category == node.Title && node is CAGCategory cat)
            {
              parent = cat;
              break;
            }

            if (j == parent.Nodes.Length - 1)
              parent = new CAGCategory(category, parent);
          }
        }

        parent.Nodes = new CAGNode[cag.Objects.Length];
        for (int i = 0; i < cag.Objects.Length; i++)
        {
          var obj = cag.Objects[i];
          obj.Parent = parent;
          parent.Nodes[i] = obj;
        }
      }

      return root;
    }
  }

  public class CAGJson
  {
    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("objects")]
    public CAGObject[] Objects { get; set; }
  }
}
