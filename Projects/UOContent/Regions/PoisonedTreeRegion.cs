namespace Server.Regions;

// TODO: Implement damage boost regions
public class PoisonedTreeRegion : MondainRegion
{
    public PoisonedTreeRegion(string name, Map map, Region parent, params Rectangle3D[] area): base(name, map, parent, area)
    {
    }

    public PoisonedTreeRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }
}
