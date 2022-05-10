using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x1c04, 0x1c05)]
    public partial class FemalePlateChest : BaseArmor
    {
        [Constructible]
        public FemalePlateChest() : base(0x1C04) => Weight = 4.0;

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 50;
        public override int InitMaxHits => 65;

        public override int AosStrReq => 95;
        public override int OldStrReq => 45;

        public override int OldDexBonus => -5;

        public override bool AllowMaleWearer => false;

        public override int ArmorBase => 30;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
    }
}
