namespace Server.Items
{
    public class DecoTray : Item
    {
        [Constructible]
        public DecoTray() : base(Utility.Random(2) + 0x991)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoTray(Serial serial) : base(serial)
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
