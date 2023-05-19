using System.Text.Json.Serialization;
using Server;
using Server.Regions;
using Server.Spells;

public class NoTravelSpellsAllowedRegion : DungeonRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public NoTravelSpellsAllowedRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

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
        return m.AccessLevel > AccessLevel.Player;
    }
}
