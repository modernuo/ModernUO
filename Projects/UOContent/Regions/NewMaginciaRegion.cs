using System.Text.Json;
using Server.Json;
using Server.Regions;

namespace Server.Engines.NewMagincia
{
  public class NewMaginciaRegion : TownRegion
  {
    public NewMaginciaRegion(DynamicJson json, JsonSerializerOptions options) : base(json, options)
    {
    }
  }
}
