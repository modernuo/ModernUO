namespace Server.Items
{
    public class GoldBricks : Item
    {
        [Constructible]
        public GoldBricks() : base(0x1BEB)
        {
        }

        public GoldBricks(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1063489;

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
