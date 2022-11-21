namespace Server.Regions;

public class TombOfKingsRegion : BaseRegion
{
    public TombOfKingsRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }

    public TombOfKingsRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public override void AlterLightLevel(Mobile m, ref int global, ref int personal)
    {
        global = 0;
    }
}
