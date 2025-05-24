using ModernUO.Serialization;
using Server.Gumps;

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
public partial class HangingAxesDeed : BaseAddonDeed, IDirectionAddonDeed
{
    public bool East { get; set;  }

    [Constructible]
    public HangingAxesDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new HangingAxesAddon(East);
    public override int LabelNumber => 1076271; // Hanging Axes

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

        public override int SelectionNumber => 1076745; // Please select your hanging axe position
    }
}
