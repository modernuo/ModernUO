using System.Text.Json.Serialization;

namespace Server.Regions;

// TODO: Implement
public class BlackthornDungeonRegion : DungeonRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public BlackthornDungeonRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public BlackthornDungeonRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public BlackthornDungeonRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }
}
