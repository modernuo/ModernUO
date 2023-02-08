namespace Server.Regions;

public class TownRegion : GuardedRegion
{
    public Point3D Entrance { get; set; }

    public TownRegion(string name, Map map, Region parent, params Rectangle3D[] area)
        : base(name, map, parent, area)
    {
    }

    public TownRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }
}
