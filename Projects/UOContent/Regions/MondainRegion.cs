namespace Server.Regions;

public class MondainRegion : NoTravelSpellsAllowedRegion
{
    public MondainRegion(string name, Map map, Region parent, params Rectangle3D[] area)
        : base(name, map, parent, area)
    {
    }

    public MondainRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }
}
