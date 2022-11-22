namespace Server.Regions;

public class TownRegionJsonDto<TRegion> : BaseRegionJsonDto<TRegion> where TRegion : TownRegion
{
    public Point3D Entrance { get; set; }

    public override void FromRegion(Region region)
    {
        base.FromRegion(region);

        if (region is TownRegion townRegion)
        {
            Entrance = townRegion.Entrance;
        }
    }

    protected override void HydrateRegion(Region region)
    {
        base.HydrateRegion(region);

        if (region is TownRegion townRegion)
        {
            townRegion.Entrance = Entrance;
        }
    }
}
