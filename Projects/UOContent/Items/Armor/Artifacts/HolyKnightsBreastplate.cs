namespace Server.Items
{
    [Serializable(1, false)]
    public partial class HolyKnightsBreastplate : PlateChest
    {
        [Constructible]
        public HolyKnightsBreastplate()
        {
            Hue = 0x47E;
            Attributes.BonusHits = 10;
            Attributes.ReflectPhysical = 15;
        }

        public override int LabelNumber => 1061097; // Holy Knight's Breastplate
        public override int ArtifactRarity => 11;

        public override int BasePhysicalResistance => 35;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        private void Deserialize(IGenericReader reader, int version)
        {
        }
    }
}
