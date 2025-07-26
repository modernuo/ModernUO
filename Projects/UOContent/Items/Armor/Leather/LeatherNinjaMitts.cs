using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LeatherNinjaMitts : BaseArmor
    {
        [Constructible]
        public LeatherNinjaMitts() : base(0x2792)
        {
        }

        public override double DefaultWeight => 2.0;

        public override int BasePhysicalResistance => 2;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 3;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 3;

        public override int InitMinHits => 25;
        public override int InitMaxHits => 25;

        public override int AosStrReq => 10;
        public override int OldStrReq => 10;

        public override int ArmorBase => 3;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;
    }
}
