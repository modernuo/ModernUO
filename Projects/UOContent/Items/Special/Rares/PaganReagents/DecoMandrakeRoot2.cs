namespace Server.Items
{
    public class DecoMandrakeRoot2 : Item
    {
        [Constructible]
        public DecoMandrakeRoot2() : base(0x18DD)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoMandrakeRoot2(Serial serial) : base(serial)
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
