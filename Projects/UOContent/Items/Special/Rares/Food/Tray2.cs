namespace Server.Items
{
    public class DecoTray2 : Item
    {
        [Constructible]
        public DecoTray2() : base(0x991)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoTray2(Serial serial) : base(serial)
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
