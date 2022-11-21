namespace Server.Regions;

// TODO: Implement
public class ExploringDeepCreaturesRegion : DungeonRegion
{
    public ExploringDeepCreaturesRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public ExploringDeepCreaturesRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }
}
