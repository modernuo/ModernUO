using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x2B70, 0x3167)]
    [SerializationGenerator(0)]
    public partial class GemmedCirclet : BaseArmor
    {
        [Constructible]
        public GemmedCirclet() : base(0x2B70) => Weight = 2.0;

        public override int RequiredRaces => Race.AllowElvesOnly;

        public override int BasePhysicalResistance => 1;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 2;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 20;
        public override int InitMaxHits => 35;

        public override int AosStrReq => 10;
        public override int OldStrReq => 10;

        public override int ArmorBase => 30;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;
    }
}
