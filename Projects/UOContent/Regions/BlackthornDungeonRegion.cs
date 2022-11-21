namespace Server.Regions;

// TODO: Implement
public class BlackthornDungeonRegion : DungeonRegion
{
    public BlackthornDungeonRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public BlackthornDungeonRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }
}
