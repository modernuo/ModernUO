namespace Server.Items
{
    public class HelmOfInsight : PlateHelm
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

        public HelmOfInsight(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061096; // Helm of Insight
        public override int ArtifactRarity => 11;

        public override int BaseEnergyResistance => 17;

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
                EnergyBonus = 0;
            }
        }
    }
}
