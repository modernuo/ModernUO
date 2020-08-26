namespace Server.Items
{
    public class DecoNightshade3 : Item
    {
        [Constructible]
        public DecoNightshade3() : base(0x18E6)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoNightshade3(Serial serial) : base(serial)
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
