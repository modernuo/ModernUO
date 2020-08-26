namespace Server.Items
{
    public class SnowPileDeco : Item
    {
        private static readonly int[] m_Types = { 0x8E2, 0x8E0, 0x8E6, 0x8E5, 0x8E3 };

        [Constructible]
        public SnowPileDeco()
            : this(m_Types.RandomElement())
        {
        }

        [Constructible]
        public SnowPileDeco(int itemid)
            : base(itemid) =>
            Hue = 0x481;

        public SnowPileDeco(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "Snow Pile";
        public override double DefaultWeight => 2.0;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
