namespace Server.Items
{
    public class NougatSwirl : CandyCane
    {
        [Constructible]
        public NougatSwirl(int amount = 1)
            : base(0x4690) =>
            Stackable = true;

        public NougatSwirl(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1096936; /* nougat swirl */

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
