using System.Text.Json.Serialization;

namespace Server.Regions;

// TODO: Implement jail
public class WrongLevel3Region : DungeonRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public WrongLevel3Region(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public WrongLevel3Region(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public WrongLevel3Region(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }
}
