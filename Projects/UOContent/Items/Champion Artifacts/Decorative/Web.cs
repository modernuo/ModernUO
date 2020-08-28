namespace Server.Items
{
    public class Web : Item
    {
        private static readonly int[] m_itemids =
        {
            0x10d7, 0x10d8, 0x10dd
        };

        [Constructible]
        public Web()
            : base(m_itemids[Utility.Random(3)])
        {
        }

        [Constructible]
        public Web(int itemid) : base(itemid)
        {
        }

        public Web(Serial serial) : base(serial)
        {
        }

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
