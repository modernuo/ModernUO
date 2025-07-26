using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    [Flippable(0x2B77, 0x316E)]
    public partial class HidePauldrons : BaseArmor
    {
        [Constructible]
        public HidePauldrons() : base(0x2B77)
        {
        }

        public override double DefaultWeight => 4.0;

        public override int RequiredRaces => Race.AllowElvesOnly;

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 4;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 35;
        public override int InitMaxHits => 45;

        public override int AosStrReq => 20;
        public override int OldStrReq => 20;

        public override int ArmorBase => 15;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Studded;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.Half;
    }
}
