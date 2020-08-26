using System.Text.Json;
using Server.Json;

namespace Server.Regions
{
  public class TownRegion : GuardedRegion
  {
    public TownRegion(DynamicJson json, JsonSerializerOptions options) : base(json, options)
    {
    }
  }
}
