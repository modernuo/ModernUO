namespace Server.Items
{
    public class EvilIdolSkull : Item
    {
        [Constructible]
        public EvilIdolSkull() : base(0x1F18)
        {
        }

        public EvilIdolSkull(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1095237; // Evil Idol

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
