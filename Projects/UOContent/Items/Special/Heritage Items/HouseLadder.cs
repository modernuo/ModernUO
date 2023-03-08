using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class HouseLadderAddon : BaseAddon
{
    [Constructible]
    public HouseLadderAddon(int type)
    {
        switch (type)
        {
            case 0: // castle south
                {
                    AddComponent(new LocalizedAddonComponent(0x3DB2, 1076791), 0, 0, 0);
                    AddComponent(new LocalizedAddonComponent(0x3F28, 1076791), 0, 1, 28);
                    AddComponent(new LocalizedAddonComponent(0x3DB4, 1076791), 0, 2, 20);
                    break;
                }
            case 1: // castle east
                {
                    AddComponent(new LocalizedAddonComponent(0x3DB3, 1076791), 0, 0, 0);
                    AddComponent(new LocalizedAddonComponent(0x3F28, 1076791), 1, 0, 28);
                    AddComponent(new LocalizedAddonComponent(0x3DB5, 1076791), 2, 0, 20);
                    break;
                }
            case 2: // castle north
                {
                    AddComponent(new LocalizedAddonComponent(0x2FDF, 1076791), 0, 0, 0);
                    AddComponent(new LocalizedAddonComponent(0x3F28, 1076791), 0, -1, 28);
                    AddComponent(new LocalizedAddonComponent(0x3DB6, 1076791), 0, -2, 20);
                    break;
                }
            case 3: // castle west
                {
                    AddComponent(new LocalizedAddonComponent(0x2FDE, 1076791), 0, 0, 0);
                    AddComponent(new LocalizedAddonComponent(0x3F28, 1076791), -1, 0, 28);
                    AddComponent(new LocalizedAddonComponent(0x3DB7, 1076791), -2, 0, 20);
                    break;
                }
            case 4: // south
                {
                    AddComponent(new LocalizedAddonComponent(0x3DB2, 1076287), 0, 0, 0);
                    break;
                }
            case 5: // east
                {
                    AddComponent(new LocalizedAddonComponent(0x3DB3, 1076287), 0, 0, 0);
                    break;
                }
            case 6: // north
                {
                    AddComponent(new LocalizedAddonComponent(0x2FDF, 1076287), 0, 0, 0);
                    break;
                }
            case 7: // west
                {
                    AddComponent(new LocalizedAddonComponent(0x2FDE, 1076287), 0, 0, 0);
                    break;
                }
        }
    }

    public override BaseAddonDeed Deed => new HouseLadderDeed();
}

[SerializationGenerator(0)]
public partial class HouseLadderDeed : BaseAddonDeed
{
    private int _type;

    [Constructible]
    public HouseLadderDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new HouseLadderAddon(_type);
    public override int LabelNumber => 1076287; // Ladder

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
        private readonly HouseLadderDeed _deed;

        public InternalGump(HouseLadderDeed deed) : base(60, 36)
        {
            _deed = deed;

            AddPage(0);

            AddBackground(0, 0, 273, 324, 0x13BE);
            AddImageTiled(10, 10, 253, 20, 0xA40);
            AddImageTiled(10, 40, 253, 244, 0xA40);
            AddImageTiled(10, 294, 253, 20, 0xA40);
            AddAlphaRegion(10, 10, 253, 304);
            AddButton(10, 294, 0xFB1, 0xFB2, 0);
            AddHtmlLocalized(45, 296, 450, 20, 1060051, 0x7FFF); // CANCEL
            // Please select your ladder position.  <br>Use the ladders marked (castle) <br> for accessing the tops of keeps <br> and castles.
            AddHtmlLocalized(14, 12, 273, 20, 1076780, 0x7FFF);

            AddPage(1);

            AddButton(19, 49, 0x845, 0x846, 1);
            AddHtmlLocalized(44, 47, 213, 20, 1076794, 0x7FFF); // South (Castle)
            AddButton(19, 73, 0x845, 0x846, 2);
            AddHtmlLocalized(44, 71, 213, 20, 1076795, 0x7FFF); // East (Castle)
            AddButton(19, 97, 0x845, 0x846, 3);
            AddHtmlLocalized(44, 95, 213, 20, 1076792, 0x7FFF); // North (Castle)
            AddButton(19, 121, 0x845, 0x846, 4);
            AddHtmlLocalized(44, 119, 213, 20, 1076793, 0x7FFF); // West (Castle)
            AddButton(19, 145, 0x845, 0x846, 5);
            AddHtmlLocalized(44, 143, 213, 20, 1075386, 0x7FFF); // South
            AddButton(19, 169, 0x845, 0x846, 6);
            AddHtmlLocalized(44, 167, 213, 20, 1075387, 0x7FFF); // East
            AddButton(19, 193, 0x845, 0x846, 7);
            AddHtmlLocalized(44, 191, 213, 20, 1075389, 0x7FFF); // North
            AddButton(19, 217, 0x845, 0x846, 8);
            AddHtmlLocalized(44, 215, 213, 20, 1075390, 0x7FFF); // West
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (_deed?.Deleted != false || info.ButtonID is 0 or < 1 or > 8)
            {
                return;
            }

            _deed._type = info.ButtonID - 1;
            _deed.SendTarget(sender.Mobile);
        }
    }
}
