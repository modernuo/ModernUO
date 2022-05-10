using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x13bf, 0x13c4)]
    public partial class ChainChest : BaseArmor
    {
        [Constructible]
        public ChainChest() : base(0x13BF) => Weight = 7.0;

        public override int BasePhysicalResistance => 4;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 4;
        public override int BasePoisonResistance => 1;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 45;
        public override int InitMaxHits => 60;

        public override int AosStrReq => 60;
        public override int OldStrReq => 20;

        public override int OldDexBonus => -5;

        public override int ArmorBase => 28;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Chainmail;
    }
}
