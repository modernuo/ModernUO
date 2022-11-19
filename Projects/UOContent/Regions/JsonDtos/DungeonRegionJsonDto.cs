namespace Server.Regions;

public class DungeonRegionJsonDto<TRegion> : BaseRegionJsonDto<TRegion> where TRegion : DungeonRegion
{
    public Point3D Entrance { get; set; }

    public override void FromRegion(Region region)
    {
        base.FromRegion(region);

        if (region is DungeonRegion dungeonRegion)
        {
            Entrance = dungeonRegion.Entrance;
        }
    }

    protected override void HydrateRegion(Region region)
    {
        base.HydrateRegion(region);

        if (region is DungeonRegion dungeonRegion)
        {
            dungeonRegion.Entrance = Entrance;
        }
    }
}
