using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items
{
    public abstract class BaseWindChimes : Item
    {
        private bool m_TurnedOn;

        public BaseWindChimes(int itemID) : base(itemID)
        {
        }

        public BaseWindChimes(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool TurnedOn
        {
            get => m_TurnedOn;
            set
            {
                m_TurnedOn = value;
                InvalidateProperties();
            }
        }

        public static int[] Sounds { get; } = { 0x505, 0x506, 0x507 };

        public override bool HandlesOnMovement => m_TurnedOn && IsLockedDown;

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m_TurnedOn && IsLockedDown && (!m.Hidden || m.AccessLevel == AccessLevel.Player) &&
                Utility.InRange(m.Location, Location, 2) && !Utility.InRange(oldLocation, Location, 2))
            {
                Effects.PlaySound(Location, Map, Sounds.RandomElement());
            }

            base.OnMovement(m, oldLocation);
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_TurnedOn)
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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_TurnedOn);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_TurnedOn = reader.ReadBool();
                        break;
                    }
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

    public class WindChimes : BaseWindChimes
    {
        [Constructible]
        public WindChimes() : base(0x2832)
        {
        }

        public WindChimes(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1030290;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class FancyWindChimes : BaseWindChimes
    {
        [Constructible]
        public FancyWindChimes() : base(0x2833)
        {
        }

        public FancyWindChimes(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1030291;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
