using Server.Regions;
using Server.Targeting;

namespace Server.Multis.Deeds;

public class HousePlacementTarget : MultiTarget
{
    private readonly HouseDeed m_Deed;

    public HousePlacementTarget(HouseDeed deed) : base(deed.MultiID, deed.Offset) => m_Deed = deed;

    protected override void OnTarget(Mobile from, object o)
    {
        if (o is not IPoint3D ip)
        {
            return;
        }

        Point3D p = ip switch
        {
            Item item => item.GetWorldTop(),
            Mobile m  => m.Location,
            _         => new Point3D(ip)
        };

        var reg = Region.Find(p, from.Map);

        if (from.AccessLevel >= AccessLevel.GameMaster || reg.AllowHousing(from, p))
        {
            m_Deed.OnPlacement(from, p);
        }
        else if (reg.IsPartOf<TempNoHousingRegion>())
        {
            // Lord British has decreed a 'no build' period, thus you cannot build this house at this time.
            from.SendLocalizedMessage(501270);
        }
        else if (reg.IsPartOf<TreasureRegion>() || reg.IsPartOf<HouseRegion>())
        {
            // The house could not be created here.  Either something is blocking the house, or the house would not be on valid terrain.
            from.SendLocalizedMessage(1043287);
        }
        else if (reg.IsPartOf<HouseRaffleRegion>())
        {
            from.SendLocalizedMessage(1150493); // You must have a deed for this plot of land in order to build here.
        }
        else
        {
            from.SendLocalizedMessage(501265); // Housing can not be created in this area.
        }
    }
}
