namespace Server.Items
{
    public class TravestysSushiPreparations : Item
    {
        [Constructible]
        public TravestysSushiPreparations() : base(Utility.Random(0x1E15, 2))
        {
        }

        public TravestysSushiPreparations(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075093; // Travesty's Sushi Preparations

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
