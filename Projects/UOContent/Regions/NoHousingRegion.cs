using System.Text.Json;
using Server.Json;

namespace Server.Regions
{
  public class NoHousingRegion : BaseRegion
  {
    /*  False: this uses 'stupid OSI' house placement checking: part of the house may be placed here provided that the center is not in the region
     *  True: this uses 'smart RunUO' house placement checking: no part of the house may be in the region
     */
    public bool SmartChecking { get; }

    public NoHousingRegion(DynamicJson json, JsonSerializerOptions options) : base(json, options) =>
      SmartChecking = json.GetProperty("smartNoHousing", options, out bool smartNoHousing) && smartNoHousing;

    public override bool AllowHousing(Mobile from, Point3D p) => SmartChecking;
  }
}
