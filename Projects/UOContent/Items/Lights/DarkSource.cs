namespace Server.Items
{
    public class DarkSource : Item
    {
        [Constructible]
        public DarkSource() : base(0x1646)
        {
            Layer = Layer.TwoHanded;
            Movable = false;
        }

        public DarkSource(Serial serial) : base(serial)
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
