namespace Server.Items
{
    public class DecoMandrake : Item
    {
        [Constructible]
        public DecoMandrake() : base(0x18DF)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoMandrake(Serial serial) : base(serial)
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
