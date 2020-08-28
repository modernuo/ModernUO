namespace Server.Items
{
    public class UnfinishedBarrel : Item
    {
        [Constructible]
        public UnfinishedBarrel() : base(0x1EB5)
        {
            Movable = true;
            Stackable = false;
        }

        public UnfinishedBarrel(Serial serial) : base(serial)
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
