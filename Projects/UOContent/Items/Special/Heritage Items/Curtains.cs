using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class CurtainsComponent : AddonComponent, IDyable
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _closedId;

    public CurtainsComponent(int itemID, int closedID) : base(itemID) => _closedId = closedID;

    public override int LabelNumber => 1076280; // Curtains
    public override bool DisplayWeight => false;

    public virtual bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        Hue = sender.DyedHue;
        return true;
    }

    public override void OnDoubleClick(Mobile from)
    {
        base.OnDoubleClick(from);

        if (Addon == null || !from.InRange(Location, 1))
        {
            return;
        }

        foreach (var c in Addon.Components)
        {
            if (c is CurtainsComponent curtain)
            {
                (curtain.ItemID, curtain.ClosedId) = (curtain.ClosedId, curtain.ItemID);
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
        }
    }
}

[SerializationGenerator(0)]
public partial class CurtainsAddon : BaseAddon
{
    [Constructible]
    public CurtainsAddon(bool east)
    {
        if (east) // east
        {
            AddComponent(new CurtainsComponent(0x3D9E, 0x3DA8), 0, -1, 0);
            AddComponent(new CurtainsComponent(0x3DAC, 0x3DAE), 0, 0, 0);
            AddComponent(new CurtainsComponent(0x3DA0, 0x3DA6), 0, 2, 0);
            AddComponent(new CurtainsComponent(0x3D9F, 0x3DA7), 0, 1, 0);
        }
        else // south
        {
            AddComponent(new CurtainsComponent(0x3D9C, 0x3DAD), 0, 0, 0);
            AddComponent(new CurtainsComponent(0x3D9D, 0x3DA3), -1, 0, 0);
            AddComponent(new CurtainsComponent(0x3DA1, 0x3DA5), 2, 0, 0);
            AddComponent(new CurtainsComponent(0x3DAB, 0x3DA4), 1, 0, 0);
        }
    }

    public override BaseAddonDeed Deed => new CurtainsDeed();
    public override bool RetainDeedHue => true;
}

[SerializationGenerator(0)]
public partial class CurtainsDeed : BaseAddonDeed, IDirectionAddonDeed
{
    public bool East { get; set; }

    [Constructible]
    public CurtainsDeed() => LootType = LootType.Blessed;

    public override BaseAddon Addon => new CurtainsAddon(East);
    public override int LabelNumber => 1076280; // Curtains

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

        public override int SelectionNumber => 1076581; // Please select your curtain position
    }
}
