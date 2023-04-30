using System.Text.Json.Serialization;

namespace Server.Regions;

// TODO: Implement jail safe zone
public class WrongJailRegion : DungeonRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public WrongJailRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public WrongJailRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public WrongJailRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }
}
