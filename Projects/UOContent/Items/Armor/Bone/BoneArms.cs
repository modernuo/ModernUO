using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x144e, 0x1453)]
    public partial class BoneArms : BaseArmor
    {
        [Constructible]
        public BoneArms() : base(0x144E) => Weight = 2.0;

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 4;
        public override int BasePoisonResistance => 2;
        public override int BaseEnergyResistance => 4;

        public override int InitMinHits => 25;
        public override int InitMaxHits => 30;

        public override int AosStrReq => 55;
        public override int OldStrReq => 40;

        public override int OldDexBonus => -2;

        public override int ArmorBase => 30;
        public override int RevertArmorBase => 4;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Bone;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;
    }
}
