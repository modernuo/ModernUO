namespace Server.Items
{
    public class Cards3 : Item
    {
        [Constructible]
        public Cards3() : base(0xE15)
        {
            Movable = true;
            Stackable = false;
        }

        public Cards3(Serial serial) : base(serial)
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
