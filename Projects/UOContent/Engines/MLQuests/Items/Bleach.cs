namespace Server.Items
{
    public class Bleach : PigmentsOfTokuno
    {
        [Constructible]
        public Bleach() => LootType = LootType.Blessed;

        public Bleach(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075375; // Bleach

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // Version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
