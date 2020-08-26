namespace Server.Items
{
    public class DecoMandrake3 : Item
    {
        [Constructible]
        public DecoMandrake3() : base(0x18DF)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoMandrake3(Serial serial) : base(serial)
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
