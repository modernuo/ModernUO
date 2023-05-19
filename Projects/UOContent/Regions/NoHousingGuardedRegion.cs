using System;
using System.Text.Json.Serialization;

namespace Server.Regions;

public class NoHousingGuardedRegion : GuardedRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public NoHousingGuardedRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public NoHousingGuardedRegion(string name, Map map, int priority, params Rectangle2D[] area) : base(name, map, priority, area)
    {
    }

    public NoHousingGuardedRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public NoHousingGuardedRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }

    public NoHousingGuardedRegion(string name, Map map, Region parent, int priority, Type guardType, params Rectangle3D[] area) : base(name, map, parent, priority, guardType, area)
    {
    }
}
