namespace Server.Items
{
    public class DecoBridle2 : Item
    {
        [Constructible]
        public DecoBridle2() : base(0x1375)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoBridle2(Serial serial) : base(serial)
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
