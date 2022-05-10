using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x1411, 0x141a)]
    public partial class PlateLegs : BaseArmor
    {
        [Constructible]
        public PlateLegs() : base(0x1411) => Weight = 7.0;

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 50;
        public override int InitMaxHits => 65;

        public override int AosStrReq => 90;

        public override int OldStrReq => 60;
        public override int OldDexBonus => -6;

        public override int ArmorBase => 40;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
    }
}
