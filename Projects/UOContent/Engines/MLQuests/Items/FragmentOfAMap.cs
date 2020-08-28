namespace Server.Items
{
    public class FragmentOfAMap : Item
    {
        [Constructible]
        public FragmentOfAMap() : base(0x14ED) => LootType = LootType.Blessed;

        public FragmentOfAMap(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074533; // Fragment of a Map

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
