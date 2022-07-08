using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LeatherJingasa : BaseArmor
    {
        [Constructible]
        public LeatherJingasa() : base(0x2776) => Weight = 3.0;

        public override int BasePhysicalResistance => 4;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 3;
        public override int BasePoisonResistance => 2;
        public override int BaseEnergyResistance => 3;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 30;

        public override int AosStrReq => 25;
        public override int OldStrReq => 25;

        public override int ArmorBase => 3;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;
    }
}
