namespace Server.Items
{
    public class WaterTile : Item
    {
        [Constructible]
        public WaterTile() : base(0x346E)
        {
        }

        public WaterTile(Serial serial) : base(serial)
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
