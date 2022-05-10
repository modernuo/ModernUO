using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class ChainHatsuburi : BaseArmor
    {
        [Constructible]
        public ChainHatsuburi() : base(0x2774) => Weight = 7.0;

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 2;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 2;
        public override int BaseEnergyResistance => 4;

        public override int InitMinHits => 55;
        public override int InitMaxHits => 75;

        public override int AosStrReq => 50;
        public override int OldStrReq => 50;

        public override int ArmorBase => 3;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Chainmail;
    }
}
