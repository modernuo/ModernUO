using System.Collections.Generic;

namespace Server.Gumps;

public class LocationTree
{
    public LocationTree(Map map, Dictionary<Mobile, GoCategory> lastBranch, GoCategory root)
    {
        LastBranch = lastBranch;
        Map = map;
        Root = root;

        if (root != null)
        {
            SetParents(root);
        }
    }

    public Dictionary<Mobile, GoCategory> LastBranch { get; }

    public Map Map { get; }

    public GoCategory Root { get; }

    private static void SetParents(GoCategory parent)
    {
        // Deserialization may leave these null
        parent.Categories ??= [];
        parent.Locations ??= [];

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
