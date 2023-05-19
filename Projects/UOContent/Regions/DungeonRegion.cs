using System.Text.Json.Serialization;

namespace Server.Regions;

public class DungeonRegion : BaseRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public DungeonRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public DungeonRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public DungeonRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    public override bool YoungProtected => false;

    public Point3D Entrance { get; set; }

    public override bool AllowHousing(Mobile from, Point3D p) => false;

    public override void AlterLightLevel(Mobile m, ref int global, ref int personal)
    {
        global = LightCycle.DungeonLevel;
    }

    public override bool CanUseStuckMenu(Mobile m) => Map != Map.Felucca && base.CanUseStuckMenu(m);
}
