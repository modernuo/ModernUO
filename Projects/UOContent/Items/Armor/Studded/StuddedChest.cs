using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x13db, 0x13e2)]
    public partial class StuddedChest : BaseArmor
    {
        [Constructible]
        public StuddedChest() : base(0x13DB) => Weight = 8.0;

        public override int BasePhysicalResistance => 2;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 3;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 4;

        public override int InitMinHits => 35;
        public override int InitMaxHits => 45;

        public override int AosStrReq => 35;
        public override int OldStrReq => 35;

        public override int ArmorBase => 16;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Studded;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.Half;
    }
}
