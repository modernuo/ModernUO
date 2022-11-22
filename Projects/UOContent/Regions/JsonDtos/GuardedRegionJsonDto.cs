using System;

namespace Server.Regions;

public class GuardedRegionJsonDto<TRegion> : BaseRegionJsonDto<TRegion> where TRegion : GuardedRegion
{
    public bool GuardsDisabled { get; set; }
    public Type? GuardType { get; set; }

    public override void FromRegion(Region region)
    {
        base.FromRegion(region);

        if (region is GuardedRegion guardedRegion)
        {
            GuardsDisabled = guardedRegion.GuardsDisabled;
            GuardType = guardedRegion.GuardType == guardedRegion.DefaultGuardType ? null : guardedRegion.GuardType;
        }
    }

    protected override void HydrateRegion(Region region)
    {
        base.HydrateRegion(region);
        if (region is GuardedRegion guardedRegion)
        {
            guardedRegion.GuardsDisabled = GuardsDisabled;
            guardedRegion.GuardType = GuardType ?? guardedRegion.DefaultGuardType;
        }
    }
}
