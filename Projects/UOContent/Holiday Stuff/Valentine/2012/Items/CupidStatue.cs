namespace Server.Items
{
    [Flippable(0x4F7C, 0x4F7D)]
    public class CupidStatue : Item
    {
        [Constructible]
        public CupidStatue()
            : base(0x4F7D) =>
            LootType = LootType.Blessed;

        public CupidStatue(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1099220; // cupid statue

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
