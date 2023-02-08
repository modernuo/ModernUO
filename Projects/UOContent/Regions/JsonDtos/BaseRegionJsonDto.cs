namespace Server.Regions;

public class BaseRegionJsonDto<TRegion> : GenericRegionJsonDto<TRegion> where TRegion : BaseRegion
{
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
