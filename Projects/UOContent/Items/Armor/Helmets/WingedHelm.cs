using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x2B73, 0x316A)]
    [SerializationGenerator(0)]
    public partial class WingedHelm : BaseArmor
    {
        [Constructible]
        public WingedHelm() : base(0x2B73) => Weight = 5.0;

        public override int RequiredRaces => Race.AllowElvesOnly;

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 1;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 2;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 45;
        public override int InitMaxHits => 55;

        public override int AosStrReq => 25;
        public override int OldStrReq => 25;

        public override int ArmorBase => 40;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
    }
}
