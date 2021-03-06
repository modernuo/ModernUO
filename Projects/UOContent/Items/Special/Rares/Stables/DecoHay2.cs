namespace Server.Items
{
    public class DecoHay2 : Item
    {
        [Constructible]
        public DecoHay2() : base(0xF34)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoHay2(Serial serial) : base(serial)
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
