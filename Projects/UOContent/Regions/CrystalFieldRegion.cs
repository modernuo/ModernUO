namespace Server.Regions;

// TODO: Implement damage boost regions
public class CrystalFieldRegion : MondainRegion
{
    public CrystalFieldRegion(string name, Map map, Region parent, params Rectangle3D[] area): base(name, map, parent, area)
    {
    }

    public CrystalFieldRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }
}
