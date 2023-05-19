using System;
using System.Text.Json.Serialization;

namespace Server.Regions;

public class CousteauPerronHouseRegion : GuardedRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public CousteauPerronHouseRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public CousteauPerronHouseRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public CousteauPerronHouseRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }

    public CousteauPerronHouseRegion(string name, Map map, Region parent, int priority, Type guardType, params Rectangle3D[] area) : base(name, map, parent, priority, guardType, area)
    {
    }
}
