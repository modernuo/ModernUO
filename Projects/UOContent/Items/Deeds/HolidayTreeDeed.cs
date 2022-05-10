using ModernUO.Serialization;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HolidayTreeDeed : Item
{
    [Constructible]
    public HolidayTreeDeed() : base(0x14F0)
    {
        Hue = 0x488;
        Weight = 1.0;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1041116; // a deed for a holiday tree

    public bool ValidatePlacement(Mobile from, Point3D loc)
    {
        if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            return true;
        }

        if (!from.InRange(GetWorldLocation(), 1))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
            return false;
        }

        if (Core.Now.Month != 12)
        {
            from.SendLocalizedMessage(
                1005700
            ); // You will have to wait till next December to put your tree back up for display.
            return false;
        }

        var map = from.Map;

        if (map == null)
        {
            return false;
        }

        var house = BaseHouse.FindHouseAt(loc, map, 20);

        if (house?.IsFriend(from) != true)
        {
            from.SendLocalizedMessage(1005701); // The holiday tree can only be placed in your house.
            return false;
        }

        if (!map.CanFit(loc, 20))
        {
            from.SendLocalizedMessage(500269); // You cannot build that there.
            return false;
        }

        return true;
    }

    public void BeginPlace(Mobile from, HolidayTreeType type)
    {
        from.BeginTarget(-1, true, TargetFlags.None, Placement_OnTarget, type);
    }

    public void Placement_OnTarget(Mobile from, object targeted, HolidayTreeType type)
    {
        if (targeted is not IPoint3D p)
        {
            return;
        }

        var loc = new Point3D(p);

        if (p is StaticTarget target)
            /* NOTE: OSI does not properly normalize Z positioning here.
             * A side affect is that you can only place on floors (due to the CanFit call).
             * That functionality may be desired. And so, it's included in this script.
             */
        {
            loc.Z -= TileData.ItemTable[target.ItemID]
                .CalcHeight;
        }

        if (ValidatePlacement(from, loc))
        {
            EndPlace(from, type, loc);
        }
    }

    public void EndPlace(Mobile from, HolidayTreeType type, Point3D loc)
    {
        Delete();
        var tree = new HolidayTree(from, type, loc);
        BaseHouse.FindHouseAt(tree)?.Addons.Add(tree);
    }

    public override void OnDoubleClick(Mobile from)
    {
        from.CloseGump<HolidayTreeChoiceGump>();
        from.SendGump(new HolidayTreeChoiceGump(from, this));
    }
}

public class HolidayTreeChoiceGump : Gump
{
    private readonly HolidayTreeDeed m_Deed;
    private readonly Mobile m_From;

    public HolidayTreeChoiceGump(Mobile from, HolidayTreeDeed deed) : base(200, 200)
    {
        m_From = from;
        m_Deed = deed;

        AddPage(0);

        AddBackground(0, 0, 220, 120, 5054);
        AddBackground(10, 10, 200, 100, 3000);

        AddButton(20, 35, 4005, 4007, 1);
        AddHtmlLocalized(55, 35, 145, 25, 1018322); // Classic

        AddButton(20, 65, 4005, 4007, 2);
        AddHtmlLocalized(55, 65, 145, 25, 1018321); // Modern
    }

    public override void OnResponse(NetState sender, RelayInfo info)
    {
        if (m_Deed.Deleted)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1:
                {
                    m_Deed.BeginPlace(m_From, HolidayTreeType.Classic);
                    break;
                }
            case 2:
                {
                    m_Deed.BeginPlace(m_From, HolidayTreeType.Modern);
                    break;
                }
        }
    }
}
