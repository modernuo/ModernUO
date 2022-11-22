namespace Server.Regions;

// TODO: Implement jail
public class WrongLevel3Region : DungeonRegion
{
    public WrongLevel3Region(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public WrongLevel3Region(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }
}
