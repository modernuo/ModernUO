namespace Server.Regions;

// TODO: Implement
public class ApprenticeRegion : BaseRegion
{
    public ApprenticeRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }

    public ApprenticeRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }
}
