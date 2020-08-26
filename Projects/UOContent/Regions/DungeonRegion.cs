using System.Text.Json;
using Server.Json;

namespace Server.Regions
{
  public class DungeonRegion : BaseRegion
  {
    public DungeonRegion(DynamicJson json, JsonSerializerOptions options) : base(json, options)
    {
      if (json.GetProperty("map", options, out Map map))
        EntranceMap = map;

      if (json.GetProperty("entrance", options, out Point3D entrance))
        EntranceLocation = entrance;
    }

    public override bool YoungProtected => false;

    public Point3D EntranceLocation { get; set; }

    public Map EntranceMap { get; set; }

    public override bool AllowHousing(Mobile from, Point3D p) => false;

    public override void AlterLightLevel(Mobile m, ref int global, ref int personal)
    {
      global = LightCycle.DungeonLevel;
    }

    public override bool CanUseStuckMenu(Mobile m) => Map != Map.Felucca && base.CanUseStuckMenu(m);
  }
}
