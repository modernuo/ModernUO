using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class HangingSwordsAddon : BaseAddon
{
    [Constructible]
    public HangingSwordsAddon(bool east)
    {
        if (east) // east
        {
            AddComponent(new LocalizedAddonComponent(0x1566, 1076272), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(0x1567, 1076272), 0, -1, 0);
        }
        else // south
        {
            AddComponent(new LocalizedAddonComponent(0x1564, 1076272), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(0x1565, 1076272), 1, 0, 0);
        }
    }

    public override BaseAddonDeed Deed => new HangingSwordsDeed();
}

[SerializationGenerator(0)]
public partial class HangingSwordsDeed : BaseAddonDeed, IDirectionAddonDeed
{
    public bool East { get; set; }

    [Constructible]
    public HangingSwordsDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new HangingSwordsAddon(East);
    public override int LabelNumber => 1076272; // Hanging Swords

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.SendGump(new InternalGump(this));
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
    }

    public void SendTarget(Mobile m)
    {
        base.OnDoubleClick(m);
    }

    private class InternalGump : SelectAddonDirectionGump<InternalGump>
    {
        public InternalGump(IDirectionAddonDeed deed) : base(deed)
        {
        }

        public override int SelectionNumber => 1076746; // Please select your hanging sword position
    }
}
