namespace Server.Items
{
    public class MiniatureMushroom : Food
    {
        [Constructible]
        public MiniatureMushroom() : base(0xD16) => LootType = LootType.Blessed;

        public MiniatureMushroom(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073138; // Miniature mushroom

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
