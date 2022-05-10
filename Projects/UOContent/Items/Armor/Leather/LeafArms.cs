using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    [Flippable(0x2FC8, 0x317E)]
    public partial class LeafArms : BaseArmor
    {
        [Constructible]
        public LeafArms() : base(0x2FC8) => Weight = 2.0;

        public override int RequiredRaces => Race.AllowElvesOnly;
        public override int BasePhysicalResistance => 2;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 4;
        public override int BaseEnergyResistance => 4;

        public override int InitMinHits => 30;
        public override int InitMaxHits => 40;

        public override int AosStrReq => 15;
        public override int OldStrReq => 15;

        public override int ArmorBase => 13;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;
    }
}
