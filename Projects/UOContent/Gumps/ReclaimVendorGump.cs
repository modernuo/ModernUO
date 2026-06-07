using Server.Multis;
using Server.Network;

namespace Server.Gumps;

public class ReclaimVendorGump : DynamicGump
{
    private readonly BaseHouse _house;
    private readonly Mobile[] _vendors;

    public override bool Singleton => true;

    private ReclaimVendorGump(BaseHouse house) : base(50, 50)
    {
        _house = house;
        _vendors = house.InternalizedVendors.ToArray();
    }

    public static void DisplayTo(Mobile from, BaseHouse house)
    {
        if (from?.NetState != null && house?.Deleted == false && house.InternalizedVendors.Count != 0)
        {
            from.SendGump(new ReclaimVendorGump(house));
        }
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 170, 50 + _vendors.Length * 20, 0x13BE);

        builder.AddImageTiled(10, 10, 150, 20, 0xA40);
        builder.AddHtmlLocalized(10, 10, 150, 20, 1061827, 0x7FFF); // <CENTER>Reclaim Vendor</CENTER>

        builder.AddImageTiled(10, 40, 150, _vendors.Length * 20, 0xA40);

        for (var i = 0; i < _vendors.Length; i++)
        {
            var m = _vendors[i];

            var y = 40 + i * 20;

            builder.AddButton(10, y, 0xFA5, 0xFA7, i + 1);
            builder.AddLabel(45, y, 0x481, m.Name);
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (info.ButtonID == 0 || !_house.IsActive || !_house.IsInside(from) || !_house.IsOwner(from) || !from.CheckAlive())
        {
            return;
        }

        var index = info.ButtonID - 1;

        if (index < 0 || index >= _vendors.Length)
        {
            return;
        }

        var mob = _vendors[index];

        if (!_house.InternalizedVendors.Contains(mob))
        {
            return;
        }

        if (mob.Deleted)
        {
            _house.InternalizedVendors.Remove(mob);
        }
        else
        {
            BaseHouse.IsThereVendor(from.Location, from.Map, out var vendor, out var contract);

            if (vendor)
            {
                from.SendLocalizedMessage(1062677); // You cannot place a vendor or barkeep at this location.
            }
            else if (contract)
            {
                from.SendLocalizedMessage(1062678); // You cannot place a vendor or barkeep on top of a rental contract!
            }
            else
            {
                _house.InternalizedVendors.Remove(mob);
                mob.MoveToWorld(from.Location, from.Map);
            }
        }
    }
}
