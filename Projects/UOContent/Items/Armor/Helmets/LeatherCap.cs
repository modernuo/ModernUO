using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x1db9, 0x1dba)]
    [SerializationGenerator(0, false)]
    public partial class LeatherCap : BaseArmor
    {
        [Constructible]
        public LeatherCap() : base(0x1DB9) => Weight = 2.0;

        public override int BasePhysicalResistance => 2;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 3;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 3;

        public override int InitMinHits => 30;
        public override int InitMaxHits => 40;

        public override int AosStrReq => 20;
        public override int OldStrReq => 15;

        public override int ArmorBase => 13;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;
    }
}
