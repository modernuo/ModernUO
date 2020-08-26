namespace Server.Items
{
    public class ThorvaldsMedallion : Item
    {
        [Constructible]
        public ThorvaldsMedallion() : base(0x2AAA)
        {
            LootType = LootType.Blessed;
            Hue = 0x47F; // TODO check
        }

        public ThorvaldsMedallion(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074232; // Thorvald's Medallion

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
