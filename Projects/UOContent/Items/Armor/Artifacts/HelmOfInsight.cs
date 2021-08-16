namespace Server.Items
{
    [Serializable(1, false)]
    public partial class HelmOfInsight : PlateHelm
    {
        [Constructible]
        public HelmOfInsight()
        {
            Hue = 0x554;
            Attributes.BonusInt = 8;
            Attributes.BonusMana = 15;
            Attributes.RegenMana = 2;
            Attributes.LowerManaCost = 8;
        }

        public override int LabelNumber => 1061096; // Helm of Insight
        public override int ArtifactRarity => 11;

        public override int BaseEnergyResistance => 17;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        private void Deserialize(IGenericReader reader, int version)
        {
        }
    }
}
