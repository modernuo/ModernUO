namespace Server.Regions;

// TODO: Implement
public class ToKBridgeRegion : BaseRegion
{
    public ToKBridgeRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }

    public ToKBridgeRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }
}
