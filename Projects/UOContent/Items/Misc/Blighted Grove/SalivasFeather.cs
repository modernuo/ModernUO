namespace Server.Items
{
    public class SalivasFeather : Item
    {
        [Constructible]
        public SalivasFeather() : base(0x1020)
        {
            LootType = LootType.Blessed;
            Hue = 0x5C;
        }

        public SalivasFeather(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074234; // Saliva's Feather

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
