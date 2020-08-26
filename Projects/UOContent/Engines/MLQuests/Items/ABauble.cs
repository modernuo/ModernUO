namespace Server.Items
{
    public class ABauble : Item
    {
        [Constructible]
        public ABauble() : base(0x23B) => LootType = LootType.Blessed;

        public ABauble(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073137; // A bauble

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
