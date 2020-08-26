namespace Server.Items
{
    public class DecoBrimstone : Item
    {
        [Constructible]
        public DecoBrimstone() : base(0xF7F)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoBrimstone(Serial serial) : base(serial)
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
