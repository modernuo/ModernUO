using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class HangingAxesAddon : BaseAddon
{
    [Constructible]
    public HangingAxesAddon(bool east)
    {
        if (east) // east
        {
            AddComponent(new LocalizedAddonComponent(0x156A, 1076271), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(0x156B, 1076271), 0, -1, 0);
        }
        else // south
        {
            AddComponent(new LocalizedAddonComponent(0x1568, 1076271), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(0x1569, 1076271), 1, 0, 0);
        }
    }

    public override BaseAddonDeed Deed => new HangingAxesDeed();
}

[SerializationGenerator(0)]
public partial class HangingAxesDeed : BaseAddonDeed
{
    private bool m_East;

    [Constructible]
    public HangingAxesDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new HangingAxesAddon(m_East);
    public override int LabelNumber => 1076271; // Hanging Axes

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.CloseGump<InternalGump>();
            from.SendGump(new InternalGump(this));
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
    }

    private void SendTarget(Mobile m)
    {
        base.OnDoubleClick(m);
    }

    private class InternalGump : Gump
    {
        private readonly HangingAxesDeed m_Deed;

        public InternalGump(HangingAxesDeed deed) : base(60, 36)
        {
            m_Deed = deed;

            AddPage(0);

            AddBackground(0, 0, 273, 324, 0x13BE);
            AddImageTiled(10, 10, 253, 20, 0xA40);
            AddImageTiled(10, 40, 253, 244, 0xA40);
            AddImageTiled(10, 294, 253, 20, 0xA40);
            AddAlphaRegion(10, 10, 253, 304);
            AddButton(10, 294, 0xFB1, 0xFB2, 0);
            AddHtmlLocalized(45, 296, 450, 20, 1060051, 0x7FFF); // CANCEL
            AddHtmlLocalized(14, 12, 273, 20, 1076745, 0x7FFF);  // Please select your hanging axe position

            AddPage(1);

            AddButton(19, 49, 0x845, 0x846, 1);
            AddHtmlLocalized(44, 47, 213, 20, 1075386, 0x7FFF); // South
            AddButton(19, 73, 0x845, 0x846, 2);
            AddHtmlLocalized(44, 71, 213, 20, 1075387, 0x7FFF); // East
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Deed?.Deleted != false || info.ButtonID == 0)
            {
                return;
            }

            m_Deed.m_East = info.ButtonID != 1;
            m_Deed.SendTarget(sender.Mobile);
        }
    }
}
