namespace Server.Regions;

// TODO: Implement
public class ExodusDungeonRegion : DungeonRegion
{
    public ExodusDungeonRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public ExodusDungeonRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }
}
