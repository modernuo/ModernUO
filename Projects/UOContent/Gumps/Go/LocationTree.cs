using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Server.Json;

namespace Server.Gumps
{
    public class LocationTree
    {
        public LocationTree(string fileName, Map map)
        {
            LastBranch = new Dictionary<Mobile, GoCategory>();
            Map = map;

            var path = Path.Combine($"Data/Locations/{fileName}.json");

            if (!File.Exists(path))
            {
                Console.WriteLine("Go Locations: {0} does not exist", path);
                return;
            }

            try
            {
                Root = JsonConfig.Deserialize<GoCategory>(path);
                if (Root == null)
                {
                    throw new JsonException($"Failed to deserialize {path}.");
                }
                SetParents(Root);
            }
            catch (Exception e)
            {
                Console.WriteLine("Go Locations: Error in deserializing {0}", path);
                Console.WriteLine(e);
            }
        }

        public Dictionary<Mobile, GoCategory> LastBranch { get; }

        public Map Map { get; }

        public GoCategory Root { get; }

        private static void SetParents(GoCategory parent)
        {
            // Deserialization may leave these null
            parent.Categories ??= Array.Empty<GoCategory>();
            parent.Locations ??= Array.Empty<GoLocation>();

            for (var i = 0; i < parent.Categories.Length; i++)
            {
                var category = parent.Categories[i];
                category.Parent = parent;
                SetParents(category);
            }

            for (var j = 0; j < parent.Locations.Length; j++)
            {
                parent.Locations[j].Parent = parent;
            }
        }
    }
}
