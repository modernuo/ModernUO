using System.Text.Json.Serialization;

namespace Server.Regions;

// TODO: Implement
public class ExploringDeepCreaturesRegion : DungeonRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public ExploringDeepCreaturesRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public ExploringDeepCreaturesRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public ExploringDeepCreaturesRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }
}
