using System;
using Server.Items;
using Server.Regions;

namespace Server.Engines.Quests
{
    public class HornOfRetreat : Item
    {
        private int m_Charges;

        private TimerExecutionToken _timerToken;

        [Constructible]
        public HornOfRetreat() : base(0xFC4)
        {
            Hue = 0x482;
            Weight = 1.0;
            Charges = 10;
        }

        public HornOfRetreat(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D DestLoc { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map DestMap { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges
        {
            get => m_Charges;
            set
            {
                m_Charges = value;
                InvalidateProperties();
            }
        }

        public override int LabelNumber => 1049117; // Horn of Retreat

        public virtual bool ValidateUse(Mobile from) => true;

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060741, m_Charges); // charges: ~1_val~
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                if (!ValidateUse(from))
                {
                    SendLocalizedMessageTo(from, 500309); // Nothing Happens.
                }
                else if (Core.ML && from.Map != Map.Trammel && from.Map != Map.Malas)
                {
                    from.SendLocalizedMessage(1076154); // You can only use this in Trammel and Malas.
                }
                else if (_timerToken.Running)
                {
                    SendLocalizedMessageTo(from, 1042144); // This is currently in use.
                }
                else if (Charges > 0)
                {
                    from.Animate(34, 7, 1, true, false, 0);
                    from.PlaySound(0xFF);
                    from.SendLocalizedMessage(1049115); // You play the horn and a sense of peace overcomes you...

                    --Charges;

                    Timer.StartTimer(TimeSpan.FromSeconds(5.0), () => PlayTimer_Callback(from), out _timerToken);
                }
                else
                {
                    SendLocalizedMessageTo(from, 1042544); // This item is out of charges.
                }
            }
            else
            {
                SendLocalizedMessageTo(from, 1042001); // That must be in your pack for you to use it.
            }
        }

        public virtual void PlayTimer_Callback(Mobile from)
        {
            _timerToken.Cancel();

            var gate = new HornOfRetreatMoongate(DestLoc, DestMap, from, Hue);

            gate.MoveToWorld(from.Location, from.Map);

            from.PlaySound(0x20E);

            gate.SendLocalizedMessageTo(from, 1049102, from.Name); // Quickly ~1_NAME~! Onward through the gate!
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(DestLoc);
            writer.Write(DestMap);
            writer.Write(m_Charges);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        DestLoc = reader.ReadPoint3D();
                        DestMap = reader.ReadMap();
                        m_Charges = reader.ReadInt();
                        break;
                    }
            }
        }
    }

    public class HornOfRetreatMoongate : Moongate
    {
        private readonly Mobile m_Caster;

        public HornOfRetreatMoongate(Point3D destLoc, Map destMap, Mobile caster, int hue)
        {
            m_Caster = caster;

            Target = destLoc;
            TargetMap = destMap;

            Hue = hue;
            Light = LightType.Circle300;

            Dispellable = false;

            Timer.StartTimer(TimeSpan.FromSeconds(10.0), Delete);
        }

        public HornOfRetreatMoongate(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1049114; // Sanctuary Gate

        public override void BeginConfirmation(Mobile from)
        {
            EndConfirmation(from);
        }

        public override void UseGate(Mobile m)
        {
            if (m.Region.IsPartOf<JailRegion>())
            {
                m.SendLocalizedMessage(1114345); // You'll need a better jailbreak plan than that!
            }
            else if (m == m_Caster)
            {
                base.UseGate(m);
                Delete();
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Delete();
        }
    }
}
