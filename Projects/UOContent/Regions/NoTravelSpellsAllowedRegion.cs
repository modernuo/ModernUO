using Server;
using Server.Regions;
using Server.Spells;

public class NoTravelSpellsAllowedRegion : DungeonRegion
{
    public NoTravelSpellsAllowedRegion(string name, Map map, Region parent, params Rectangle3D[] area)
        : base(name, map, parent, area)
    {
    }

    public NoTravelSpellsAllowedRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    public override bool CheckTravel(Mobile m, Point3D newLocation, TravelCheckType travelType, out TextDefinition message)
    {
        message = null; // Use default message
        return m.AccessLevel > AccessLevel.Player ||
               travelType is not TravelCheckType.TeleportFrom or TravelCheckType.TeleportTo;
    }
}
