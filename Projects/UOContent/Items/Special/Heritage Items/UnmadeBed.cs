using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class UnmadeBedAddon : BaseAddon
{
    [Constructible]
    public UnmadeBedAddon(bool east)
    {
        if (east) // east
        {
            AddComponent(new LocalizedAddonComponent(0xA8C, 1076279), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xA8D, 1076279), 0, -1, 0);
            AddComponent(new LocalizedAddonComponent(0xA90, 1076279), -1, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xA91, 1076279), -1, -1, 0);
        }
        else // south
        {
            AddComponent(new LocalizedAddonComponent(0xDB0, 1076279), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xDB1, 1076279), -1, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xDB4, 1076279), 0, -1, 0);
            AddComponent(new LocalizedAddonComponent(0xDB5, 1076279), -1, -1, 0);
        }
    }

    public override BaseAddonDeed Deed => new UnmadeBedDeed();
}

[SerializationGenerator(0)]
public partial class UnmadeBedDeed : BaseAddonDeed, IDirectionAddonDeed
{
    public bool East { get; set; }

    [Constructible]
    public UnmadeBedDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new UnmadeBedAddon(East);
    public override int LabelNumber => 1076279; // Unmade Bed

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

        // Misspelled in cliloc
        public override int SelectionNumber => 1076580; // Pleae select your unmade bed position
    }
}
