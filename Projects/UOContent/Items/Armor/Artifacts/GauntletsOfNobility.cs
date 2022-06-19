using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class GauntletsOfNobility : RingmailGloves
    {
        [Constructible]
        public GauntletsOfNobility()
        {
            Hue = 0x4FE;
            Attributes.BonusStr = 8;
            Attributes.Luck = 100;
            Attributes.WeaponDamage = 20;
        }

        public override int LabelNumber => 1061092; // Gauntlets of Nobility
        public override int ArtifactRarity => 11;

        public override int BasePhysicalResistance => 18;
        public override int BasePoisonResistance => 20;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
