namespace Server.Items
{
    public class CoilsFang : Item
    {
        [Constructible]
        public CoilsFang() : base(0x10E8)
        {
            LootType = LootType.Blessed;
            Hue = 0x487;
        }

        public CoilsFang(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074229; // Coil's Fang

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
