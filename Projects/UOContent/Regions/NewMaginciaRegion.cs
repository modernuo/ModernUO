using System.Text.Json.Serialization;

namespace Server.Regions;

public class NewMaginciaRegion : TownRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public NewMaginciaRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public NewMaginciaRegion(string name, Map map, Region parent, params Rectangle3D[] area)
        : base(name, map, parent, area)
    {
    }

    public NewMaginciaRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }
}
