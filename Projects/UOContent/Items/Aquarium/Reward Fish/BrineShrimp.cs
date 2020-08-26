namespace Server.Items
{
    public class BrineShrimp : BaseFish
    {
        [Constructible]
        public BrineShrimp() : base(0x3B11)
        {
        }

        public BrineShrimp(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074415; // Brine shrimp

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
