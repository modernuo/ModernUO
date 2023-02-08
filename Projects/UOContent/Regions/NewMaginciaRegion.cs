namespace Server.Regions;

public class NewMaginciaRegion : TownRegion
{
    public NewMaginciaRegion(string name, Map map, Region parent, params Rectangle3D[] area)
        : base(name, map, parent, area)
    {
    }

    public NewMaginciaRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }
}
