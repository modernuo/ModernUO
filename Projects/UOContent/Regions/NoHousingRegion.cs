using System.Text.Json.Serialization;

namespace Server.Regions;

public class NoHousingRegion : BaseRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public NoHousingRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public NoHousingRegion(string name, Map map, Region parent, params Rectangle3D[] area)
        : base(name, map, parent, area)
    {
    }

    public NoHousingRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    // We don't set AllowHousing to false because it is manually checked so that a part of a house cannot overlap
    public override bool AllowHousing(Mobile from, Point3D p) => true;
}
