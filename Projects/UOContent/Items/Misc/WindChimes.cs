using ModernUO.Serialization;
using Server.Gumps;
using Server.Multis;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseWindChimes : Item, IGumpToggleItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _turnedOn;

    public BaseWindChimes(int itemID) : base(itemID)
    {
    }

    public static int[] Sounds { get; } = { 0x505, 0x506, 0x507 };

    public override bool HandlesOnMovement => _turnedOn && IsLockedDown;

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (_turnedOn && IsLockedDown && (!m.Hidden || m.AccessLevel == AccessLevel.Player) &&
            Utility.InRange(m.Location, Location, 2) && !Utility.InRange(oldLocation, Location, 2))
        {
            Effects.PlaySound(Location, Map, Sounds.RandomElement());
        }

        base.OnMovement(m, oldLocation);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_turnedOn)
        {
            list.Add(502695); // turned on
        }
        else
        {
            list.Add(502696); // turned off
        }
    }

    public bool HasAccess(Mobile mob) => mob.AccessLevel >= AccessLevel.GameMaster ||
                                         BaseHouse.FindHouseAt(this)?.IsOwner(mob) == true;

    public override void OnDoubleClick(Mobile from)
    {
        if (!HasAccess(from))
        {
            from.SendLocalizedMessage(502691); // You must be the owner to use this.
        }
        else if (TurnedOn)
        {
            from.SendGump(new TurnOffGump(this));
        }
        else
        {
            from.SendGump(new TurnOnGump(this));
        }
    }
}

[SerializationGenerator(0, false)]
public partial class WindChimes : BaseWindChimes
{
    [Constructible]
    public WindChimes() : base(0x2832)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class FancyWindChimes : BaseWindChimes
{
    [Constructible]
    public FancyWindChimes() : base(0x2833)
    {
    }

    public override int LabelNumber => 1030291;
}
