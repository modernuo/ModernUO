using System.Text.Json;
using Server;
using Server.Json;
using Server.Regions;
using Server.Spells;

public class NoTravelSpellsAllowedRegion : DungeonRegion
{
  public NoTravelSpellsAllowedRegion(DynamicJson json, JsonSerializerOptions options) : base(json, options)
  {
  }

  public override bool CheckTravel(Mobile m, Point3D newLocation, TravelCheckType travelType) =>
    m.AccessLevel == AccessLevel.Player;
}
