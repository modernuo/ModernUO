namespace Server.Regions;

public class NoHousingRegion : BaseRegion
{
    public NoHousingRegion(string name, Map map, Region parent, params Rectangle3D[] area)
        : base(name, map, parent, area)
    {
    }

    public NoHousingRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    public override bool AllowHousing(Mobile from, Point3D p) => true;
}
