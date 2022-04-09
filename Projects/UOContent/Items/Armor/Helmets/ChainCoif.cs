using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x13BB, 0x13C0)]
    public partial class ChainCoif : BaseArmor
    {
        [Constructible]
        public ChainCoif() : base(0x13BB) => Weight = 1.0;

        public override int BasePhysicalResistance => 4;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 4;
        public override int BasePoisonResistance => 1;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 35;
        public override int InitMaxHits => 60;

        public override int AosStrReq => 60;
        public override int OldStrReq => 20;

        public override int ArmorBase => 28;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Chainmail;
    }
}
