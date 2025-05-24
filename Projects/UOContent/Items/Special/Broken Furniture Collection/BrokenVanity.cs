using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class BrokenVanityAddon : BaseAddon
{
    [Constructible]
    public BrokenVanityAddon(bool east)
    {
        if (east) // east
        {
            AddComponent(new LocalizedAddonComponent(0xC20, 1076260), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xC21, 1076260), 0, -1, 0);
        }
        else // south
        {
            AddComponent(new LocalizedAddonComponent(0xC22, 1076260), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(0xC23, 1076260), -1, 0, 0);
        }
    }

    public override BaseAddonDeed Deed => new BrokenVanityDeed();
}

[SerializationGenerator(0)]
public partial class BrokenVanityDeed : BaseAddonDeed, IDirectionAddonDeed
{
    public bool East { get; set; }

    [Constructible]
    public BrokenVanityDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new BrokenVanityAddon(East);

    public override int LabelNumber => 1076260; // Broken Vanity

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

        public override int SelectionNumber => 1076747; // Please select your broken vanity position
    }
}
