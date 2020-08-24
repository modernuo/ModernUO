using Server.Engines.CannedEvil;

namespace Server.Items
{
    public class ChampionSkull : Item
    {
        private ChampionSkullType m_Type;

        [Constructible]
        public ChampionSkull(ChampionSkullType type) : base(0x1AE1)
        {
            m_Type = type;
            LootType = LootType.Cursed;

            // TODO: All hue values
            Hue = type switch
            {
                ChampionSkullType.Power => 0x159,
                ChampionSkullType.Venom => 0x172,
                ChampionSkullType.Greed => 0x1EE,
                ChampionSkullType.Death => 0x025,
                ChampionSkullType.Pain => 0x035,
                _ => Hue
            };
        }

        public ChampionSkull(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ChampionSkullType Type
        {
            get => m_Type;
            set
            {
                m_Type = value;
                InvalidateProperties();
            }
        }

        public override int LabelNumber => 1049479 + (int)m_Type;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write((int)m_Type);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        m_Type = (ChampionSkullType)reader.ReadInt();

                        break;
                    }
            }

            if (version == 0)
            {
                if (LootType != LootType.Cursed)
                    LootType = LootType.Cursed;

                if (Insured)
                    Insured = false;
            }
        }
    }
}
