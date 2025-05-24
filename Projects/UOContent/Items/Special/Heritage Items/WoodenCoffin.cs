using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class WoodenCoffinComponent : AddonComponent
{
    public WoodenCoffinComponent(int itemID) : base(itemID)
    {
    }

    public override int LabelNumber => 1076274; // Coffin
}

[SerializationGenerator(0)]
public partial class WoodenCoffinAddon : BaseAddon
{
    [Constructible]
    public WoodenCoffinAddon(bool east)
    {
        if (east) // east
        {
            AddComponent(new WoodenCoffinComponent(0x1C41), 0, 0, 0);
            AddComponent(new WoodenCoffinComponent(0x1C42), 1, 0, 0);
            AddComponent(new WoodenCoffinComponent(0x1C43), 2, 0, 0);
        }
        else // south
        {
            AddComponent(new WoodenCoffinComponent(0x1C4F), 0, 0, 0);
            AddComponent(new WoodenCoffinComponent(0x1C50), 0, 1, 0);
            AddComponent(new WoodenCoffinComponent(0x1C51), 0, 2, 0);
        }
    }

    public override BaseAddonDeed Deed => new WoodenCoffinDeed();
}

[SerializationGenerator(0)]
public partial class WoodenCoffinDeed : BaseAddonDeed, IDirectionAddonDeed
{
    public bool East { get; set; }

    [Constructible]
    public WoodenCoffinDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new WoodenCoffinAddon(East);
    public override int LabelNumber => 1076274; // Coffin

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

        public override int SelectionNumber => 1076748; // Please select your coffin position
    }
}
