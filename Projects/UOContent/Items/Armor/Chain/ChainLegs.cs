using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x13be, 0x13c3)]
    public partial class ChainLegs : BaseArmor
    {
        [Constructible]
        public ChainLegs() : base(0x13BE) => Weight = 7.0;

        public override int BasePhysicalResistance => 4;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 4;
        public override int BasePoisonResistance => 1;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 45;
        public override int InitMaxHits => 60;

        public override int AosStrReq => 60;
        public override int OldStrReq => 20;

        public override int OldDexBonus => -3;

        public override int ArmorBase => 28;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Chainmail;
    }
}
