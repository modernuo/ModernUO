using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class OrcHelm : BaseArmor
    {
        [Constructible]
        public OrcHelm() : base(0x1F0B)
        {
        }

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 1;
        public override int BaseColdResistance => 3;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 30;
        public override int InitMaxHits => 50;

        public override int AosStrReq => 30;
        public override int OldStrReq => 10;

        public override int ArmorBase => 20;

        public override double DefaultWeight => 5;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.None;
    }
}
