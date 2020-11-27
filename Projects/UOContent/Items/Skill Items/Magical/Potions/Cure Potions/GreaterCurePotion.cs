namespace Server.Items
{
    public class GreaterCurePotion : BaseCurePotion
    {
        private static readonly CureLevelInfo[] m_OldLevelInfo =
        {
            new(Poison.Lesser, 1.00),  // 100% chance to cure lesser poison
            new(Poison.Regular, 1.00), // 100% chance to cure regular poison
            new(Poison.Greater, 1.00), // 100% chance to cure greater poison
            new(Poison.Deadly, 0.75),  //  75% chance to cure deadly poison
            new(Poison.Lethal, 0.25)   //  25% chance to cure lethal poison
        };

        private static readonly CureLevelInfo[] m_AosLevelInfo =
        {
            new(Poison.Lesser, 1.00),
            new(Poison.Regular, 1.00),
            new(Poison.Greater, 1.00),
            new(Poison.Deadly, 0.95),
            new(Poison.Lethal, 0.75)
        };

        [Constructible]
        public GreaterCurePotion() : base(PotionEffect.CureGreater)
        {
        }

        public GreaterCurePotion(Serial serial) : base(serial)
        {
        }

        public override CureLevelInfo[] LevelInfo => Core.AOS ? m_AosLevelInfo : m_OldLevelInfo;

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
