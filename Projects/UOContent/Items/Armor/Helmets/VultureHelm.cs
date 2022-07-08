using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x2B72, 0x3169)]
    [SerializationGenerator(0)]
    public partial class VultureHelm : BaseArmor
    {
        [Constructible]
        public VultureHelm() : base(0x2B72) => Weight = 5.0;

        public override int RequiredRaces => Race.AllowElvesOnly;

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 1;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 2;
        public override int BaseEnergyResistance => 5;

        public override int InitMinHits => 50;
        public override int InitMaxHits => 65;

        public override int AosStrReq => 25;
        public override int OldStrReq => 25;

        public override int ArmorBase => 40;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
    }
}
