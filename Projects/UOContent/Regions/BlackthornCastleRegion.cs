namespace Server.Regions;

// TODO: Implement
public class BlackthornCastleRegion : GuardedRegion
{
    public BlackthornCastleRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public BlackthornCastleRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public BlackthornCastleRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }
}
