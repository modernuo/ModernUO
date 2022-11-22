namespace Server.Regions;

// TODO: Implement
public class SeaMarketRegion : BaseRegion
{
    public SeaMarketRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }

    public SeaMarketRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }
}
