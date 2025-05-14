using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class StoneStatueAddon : BaseAddon
{
    [Constructible]
    public StoneStatueAddon(bool east)
    {
        if (east) // east
        {
            AddComponent(new LocalizedAddonComponent(0x139E, 1076284), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(0x139F, 1076284), -1, 0, 0);
            AddComponent(new LocalizedAddonComponent(0x13A0, 1076284), 0, -1, 0);
        }
        else // south
        {
            AddComponent(new LocalizedAddonComponent(0x129F, 1076284), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(0x12A0, 1076284), 0, -1, 0);
            AddComponent(new LocalizedAddonComponent(0x12A1, 1076284), -1, 0, 0);
        }
    }

    public override BaseAddonDeed Deed => new StoneStatueDeed();
}

[SerializationGenerator(0)]
public partial class StoneStatueDeed : BaseAddonDeed, IDirectionAddonDeed
{
    public bool East { get; set; }

    [Constructible]
    public StoneStatueDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new StoneStatueAddon(East);
    public override int LabelNumber => 1076284; // Statue

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

        public override int SelectionNumber => 1076579; // Please select your statue position
    }
}
