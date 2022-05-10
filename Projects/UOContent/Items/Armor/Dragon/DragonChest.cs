using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x2641, 0x2642)]
    public partial class DragonChest : BaseArmor
    {
        [Constructible]
        public DragonChest() : base(0x2641) => Weight = 10.0;

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 3;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 3;

        public override int InitMinHits => 55;
        public override int InitMaxHits => 75;

        public override int AosStrReq => 75;
        public override int OldStrReq => 60;

        public override int OldDexBonus => -8;

        public override int ArmorBase => 40;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Dragon;
        public override CraftResource DefaultResource => CraftResource.RedScales;
    }
}
