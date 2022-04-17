using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    [Flippable(0x2B6D, 0x3164)]
    public partial class FemaleElvenPlateChest : BaseArmor
    {
        [Constructible]
        public FemaleElvenPlateChest() : base(0x2B6D) => Weight = 8.0;

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 50;
        public override int InitMaxHits => 65;

        public override int AosStrReq => 95;
        public override int OldStrReq => 95;

        public override bool AllowMaleWearer => false;

        public override int ArmorBase => 30;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Wood;
    }
}
