using System.Text.Json.Serialization;

namespace Server.Regions;

public class TownRegion : GuardedRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public TownRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public TownRegion(string name, Map map, Region parent, params Rectangle3D[] area)
        : base(name, map, parent, area)
    {
    }

    public TownRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    public Point3D Entrance { get; set; }
}
