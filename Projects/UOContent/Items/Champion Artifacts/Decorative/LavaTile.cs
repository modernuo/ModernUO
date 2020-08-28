namespace Server.Items
{
    public class LavaTile : Item
    {
        [Constructible]
        public LavaTile() : base(0x12EE)
        {
        }

        public LavaTile(Serial serial) : base(serial)
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
