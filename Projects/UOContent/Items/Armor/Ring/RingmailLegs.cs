using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x13f0, 0x13f1)]
    public partial class RingmailLegs : BaseArmor
    {
        [Constructible]
        public RingmailLegs() : base(0x13F0) => Weight = 15.0;

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 1;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 3;

        public override int InitMinHits => 40;
        public override int InitMaxHits => 50;

        public override int AosStrReq => 40;
        public override int OldStrReq => 20;

        public override int OldDexBonus => -1;

        public override int ArmorBase => 22;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Ringmail;
    }
}
