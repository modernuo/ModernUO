namespace Server.Items
{
    public class CrystallineFragments : Item
    {
        [Constructible]
        public CrystallineFragments() : base(0x223B)
        {
            LootType = LootType.Blessed;
            Hue = 0x47E;
        }

        public CrystallineFragments(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073160; // Crystalline Fragments

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
