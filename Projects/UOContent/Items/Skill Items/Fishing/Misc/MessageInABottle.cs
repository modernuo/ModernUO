using System;
using Server.Network;

namespace Server.Items
{
    public class MessageInABottle : Item
    {
        private int m_Level;

        [Constructible]
        public MessageInABottle(Map map = null) : this(map, GetRandomLevel())
        {
        }

        [Constructible]
        public MessageInABottle(Map map, int level) : base(0x099F)
        {
            Weight = 1.0;
            TargetMap = map ?? Map.Trammel;
            m_Level = level;
        }

        public MessageInABottle(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041080; // a message in a bottle

        [CommandProperty(AccessLevel.GameMaster)]
        public Map TargetMap { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level
        {
            get => m_Level;
            set => m_Level = Math.Max(1, Math.Min(value, 4));
        }

        public static int GetRandomLevel()
        {
            if (Core.AOS && Utility.Random(25) < 1)
            {
                return 4; // ancient
            }

            return Utility.RandomMinMax(1, 3);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(3); // version

            writer.Write(m_Level);

            writer.Write(TargetMap);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 3:
                case 2:
                    {
                        m_Level = reader.ReadInt();
                        goto case 1;
                    }
                case 1:
                    {
                        TargetMap = reader.ReadMap();
                        break;
                    }
                case 0:
                    {
                        TargetMap = Map.Trammel;
                        break;
                    }
            }

            if (version < 2)
            {
                m_Level = GetRandomLevel();
            }

            if (version < 3 && TargetMap == Map.Tokuno)
            {
                TargetMap = Map.Trammel;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                ReplaceWith(new SOS(TargetMap, m_Level));
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 501891); // You extract the message from the bottle.
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }
    }
}
