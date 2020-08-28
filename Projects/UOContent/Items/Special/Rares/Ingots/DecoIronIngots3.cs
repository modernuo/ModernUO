namespace Server.Items
{
    public class DecoIronIngots3 : Item
    {
        [Constructible]
        public DecoIronIngots3() : base(0x1BF0)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoIronIngots3(Serial serial) : base(serial)
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
