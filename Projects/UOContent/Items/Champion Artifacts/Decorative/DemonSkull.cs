namespace Server.Items
{
    public class DemonSkull : Item
    {
        [Constructible]
        public DemonSkull() : base(0x224e + Utility.Random(4))
        {
        }

        public DemonSkull(Serial serial) : base(serial)
        {
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
        }
    }
}
