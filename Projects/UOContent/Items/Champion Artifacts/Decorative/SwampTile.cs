namespace Server.Items
{
    public class SwampTile : Item
    {
        [Constructible]
        public SwampTile() : base(0x320D)
        {
        }

        public SwampTile(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
