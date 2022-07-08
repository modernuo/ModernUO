using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class OrcChieftainHelm : OrcHelm
    {
        [Constructible]
        public OrcChieftainHelm()
        {
            Hue = 0x2a3;

            Attributes.Luck = 100;
            Attributes.RegenHits = 3;

            if (Utility.RandomBool())
            {
                Attributes.BonusHits = 30;
            }
            else
            {
                Attributes.AttackChance = 30;
            }
        }

        public override int LabelNumber => 1094924; // Orc Chieftain Helm [Replica]

        public override int BasePhysicalResistance => 23;
        public override int BaseFireResistance => 1;
        public override int BaseColdResistance => 23;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;
    }
}
