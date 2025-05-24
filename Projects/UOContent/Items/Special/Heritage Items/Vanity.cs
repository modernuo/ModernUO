using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class VanityAddon : BaseAddonContainer
{
    [Constructible]
    public VanityAddon(bool east) : base(east ? 0xA44 : 0xA3C)
    {
        if (east) // east
        {
            AddComponent(new AddonContainerComponent(0xA45), 0, -1, 0);
        }
        else // south
        {
            AddComponent(new AddonContainerComponent(0xA3D), -1, 0, 0);
        }
    }

    public override BaseAddonContainerDeed Deed => new VanityDeed();
    public override int LabelNumber => 1074027; // Vanity
    public override int DefaultGumpID => 0x51;
    public override int DefaultDropSound => 0x42;
}

[SerializationGenerator(0)]
public partial class VanityDeed : BaseAddonContainerDeed, IDirectionAddonDeed
{
    public bool East { get; set; }

    [Constructible]
    public VanityDeed() => LootType = LootType.Blessed;

    public override BaseAddonContainer Addon => new VanityAddon(East);
    public override int LabelNumber => 1074027; // Vanity

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

        public override int SelectionNumber => 1076744; // Please select your vanity position.
    }
}
