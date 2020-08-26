namespace Server.Items
{
    public class LightSource : Item
    {
        [Constructible]
        public LightSource() : base(0x1647)
        {
            Layer = Layer.TwoHanded;
            Movable = false;
        }

        public LightSource(Serial serial) : base(serial)
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
