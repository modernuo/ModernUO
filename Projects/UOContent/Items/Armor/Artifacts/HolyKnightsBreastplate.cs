namespace Server.Items
{
    public class HolyKnightsBreastplate : PlateChest
    {
        [Constructible]
        public HolyKnightsBreastplate()
        {
            Hue = 0x47E;
            Attributes.BonusHits = 10;
            Attributes.ReflectPhysical = 15;
        }

        public HolyKnightsBreastplate(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061097; // Holy Knight's Breastplate
        public override int ArtifactRarity => 11;

        public override int BasePhysicalResistance => 35;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version < 1)
            {
                PhysicalBonus = 0;
            }
        }
    }
}
