using System;
using System.Text.Json.Serialization;

namespace Server.Regions;

public class GenericRegionJsonDto<TRegion> : RegionJsonDto where TRegion : BaseRegion
{
    [JsonIgnore]
    public override Type RegionType => typeof(TRegion);
    public string RuneName { get; set; }
    public bool NoLogoutDelay { get; set; }

    public override void FromRegion(Region region)
    {
        base.FromRegion(region);

        if (region is BaseRegion baseRegion)
        {
            RuneName = baseRegion.RuneName;
            NoLogoutDelay = baseRegion.NoLogoutDelay;
        }
    }

    protected override void HydrateRegion(Region region)
    {
        base.HydrateRegion(region);

        if (region is BaseRegion baseRegion)
        {
            baseRegion.RuneName = RuneName;
            baseRegion.NoLogoutDelay = NoLogoutDelay;
        }
    }
}
