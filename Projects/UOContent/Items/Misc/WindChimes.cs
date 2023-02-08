using ModernUO.Serialization;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseWindChimes : Item
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

    public bool IsOwner(Mobile mob) => BaseHouse.FindHouseAt(this)?.IsOwner(mob) == true;

    public override void OnDoubleClick(Mobile from)
    {
        if (IsOwner(from))
        {
            from.SendGump(new OnOffGump(this));
        }
        else
        {
            from.SendLocalizedMessage(502691); // You must be the owner to use this.
        }
    }

    private class OnOffGump : Gump
    {
        private readonly BaseWindChimes m_Chimes;

        public OnOffGump(BaseWindChimes chimes) : base(150, 200)
        {
            m_Chimes = chimes;

            AddBackground(0, 0, 300, 150, 0xA28);
            AddHtmlLocalized(45, 20, 300, 35, chimes.TurnedOn ? 1011035 : 1011034); // [De]Activate this item
            AddButton(40, 53, 0xFA5, 0xFA7, 1);
            AddHtmlLocalized(80, 55, 65, 35, 1011036); // OKAY
            AddButton(150, 53, 0xFA5, 0xFA7, 0);
            AddHtmlLocalized(190, 55, 100, 35, 1011012); // CANCEL
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var from = sender.Mobile;

            if (info.ButtonID == 1)
            {
                var newValue = !m_Chimes.TurnedOn;

                m_Chimes.TurnedOn = newValue;

                if (newValue && !m_Chimes.IsLockedDown)
                {
                    from.SendLocalizedMessage(502693); // Remember, this only works when locked down.
                }
            }
            else
            {
                from.SendLocalizedMessage(502694); // Cancelled action.
            }
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
