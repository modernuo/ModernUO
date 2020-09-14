using System;
using Server.Factions;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public enum MoonstoneType
    {
        Felucca,
        Trammel
    }

    public class Moonstone : Item
    {
        private MoonstoneType m_Type;

        [Constructible]
        public Moonstone(MoonstoneType type) : base(0xF8B)
        {
            Weight = 1.0;
            m_Type = type;
        }

        public Moonstone(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MoonstoneType Type
        {
            get => m_Type;
            set
            {
                m_Type = value;
                InvalidateProperties();
            }
        }

        public override int LabelNumber => 1041490 + (int)m_Type;

        public override void OnSingleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                Hue = Utility.RandomBirdHue();
                ProcessDelta();
                from.SendLocalizedMessage(1005398); // The stone's substance shifts as you examine it.
            }

            base.OnSingleClick(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (from.Mounted)
            {
                from.SendLocalizedMessage(1005399); // You can not bury a stone while you sit on a mount.
            }
            else if (!from.Body.IsHuman)
            {
                from.SendLocalizedMessage(1005400); // You can not bury a stone in this form.
            }
            else if (Sigil.ExistsOn(from))
            {
                from.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
            }
            else if (from.Map == GetTargetMap() || from.Map != Map.Trammel && from.Map != Map.Felucca)
            {
                from.SendLocalizedMessage(1005401); // You cannot bury the stone here.
            }
            else if (from is PlayerMobile mobile && mobile.Young)
            {
                mobile.SendLocalizedMessage(1049543); // You decide against traveling to Felucca while you are still young.
            }
            else if (from.Kills >= 5)
            {
                from.SendLocalizedMessage(
                    1005402
                ); // The magic of the stone cannot be evoked by someone with blood on their hands.
            }
            else if (from.Criminal)
            {
                from.SendLocalizedMessage(1005403); // The magic of the stone cannot be evoked by the lawless.
            }
            else if (!Region.Find(from.Location, from.Map).IsDefault ||
                     !Region.Find(from.Location, GetTargetMap()).IsDefault)
            {
                from.SendLocalizedMessage(1005401); // You cannot bury the stone here.
            }
            else if (!GetTargetMap().CanFit(from.Location, 16))
            {
                from.SendLocalizedMessage(1005408); // Something is blocking the facet gate exit.
            }
            else
            {
                Movable = false;
                MoveToWorld(from.Location, from.Map);

                from.Animate(32, 5, 1, true, false, 0);

                new SettleTimer(this, from.Location, from.Map, GetTargetMap(), from).Start();
            }
        }

        public Map GetTargetMap() => m_Type == MoonstoneType.Felucca ? Map.Felucca : Map.Trammel;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write((int)m_Type);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Type = (MoonstoneType)reader.ReadInt();

                        break;
                    }
            }
        }

        private class SettleTimer : Timer
        {
            private readonly Mobile m_Caster;
            private readonly Point3D m_Location;
            private readonly Map m_Map;
            private readonly Item m_Stone;
            private readonly Map m_TargetMap;
            private int m_Count;

            public SettleTimer(Item stone, Point3D loc, Map map, Map targetMap, Mobile caster) : base(
                TimeSpan.FromSeconds(2.5),
                TimeSpan.FromSeconds(1.0)
            )
            {
                m_Stone = stone;

                m_Location = loc;
                m_Map = map;
                m_TargetMap = targetMap;

                m_Caster = caster;
            }

            protected override void OnTick()
            {
                ++m_Count;

                if (m_Count == 1)
                {
                    m_Stone.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1005414); // The stone settles into the ground.
                }
                else if (m_Count >= 10)
                {
                    m_Stone.Location = new Point3D(m_Stone.X, m_Stone.Y, m_Stone.Z - 1);

                    if (m_Count == 16)
                    {
                        if (!Region.Find(m_Location, m_Map).IsDefault || !Region.Find(m_Location, m_TargetMap).IsDefault)
                        {
                            m_Stone.Movable = true;
                            m_Caster.AddToBackpack(m_Stone);
                            Stop();
                            return;
                        }

                        if (!m_TargetMap.CanFit(m_Location, 16))
                        {
                            m_Stone.Movable = true;
                            m_Caster.AddToBackpack(m_Stone);
                            Stop();
                            return;
                        }

                        var hue = m_Stone.Hue;

                        if (hue == 0)
                        {
                            hue = Utility.RandomBirdHue();
                        }

                        new MoonstoneGate(m_Location, m_TargetMap, m_Map, m_Caster, hue);
                        new MoonstoneGate(m_Location, m_Map, m_TargetMap, m_Caster, hue);

                        m_Stone.Delete();
                        Stop();
                    }
                }
            }
        }
    }
}
