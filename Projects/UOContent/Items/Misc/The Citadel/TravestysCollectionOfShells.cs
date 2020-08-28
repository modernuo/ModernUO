namespace Server.Items
{
    public class TravestysCollectionOfShells : Item
    {
        [Constructible]
        public TravestysCollectionOfShells() : base(0xFD3)
        {
        }

        public TravestysCollectionOfShells(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1072090; // Travesty's Collection of Shells

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
