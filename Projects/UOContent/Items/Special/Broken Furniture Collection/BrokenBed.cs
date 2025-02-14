using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BrokenBedAddon : BaseAddon
{
    [Constructible]
    public BrokenBedAddon(bool east)
    {
        if (east) // east
        {
            AddComponent(new LocalizedAddonComponent(0x1895, 1076263), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(0x1894, 1076263), 0, 1, 0);
            AddComponent(new LocalizedAddonComponent(0x1897, 1076263), 1, 0, 0);
            AddComponent(new LocalizedAddonComponent(0x1896, 1076263), 1, 1, 0);
        }
        else // south
        {
            AddComponent(new LocalizedAddonComponent(0x1899, 1076263), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(0x1898, 1076263), 1, 0, 0);
            AddComponent(new LocalizedAddonComponent(0x189B, 1076263), 0, 1, 0);
            AddComponent(new LocalizedAddonComponent(0x189A, 1076263), 1, 1, 0);
        }
    }

    public override BaseAddonDeed Deed => new BrokenBedDeed();
}

[SerializationGenerator(0)]
public partial class BrokenBedDeed : BaseAddonDeed, IDirectionAddonDeed
{
    public bool East { get; set; }

    [Constructible]
    public BrokenBedDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new BrokenBedAddon(East);

    public override int LabelNumber => 1076263; // Broken Bed

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

        public override int SelectionNumber => 1076749; // Please select your broken bed position
    }
}
