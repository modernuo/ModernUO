using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class NorseHelm : BaseArmor
    {
        [Constructible]
        public NorseHelm() : base(0x140E) => Weight = 5.0;

        public override int BasePhysicalResistance => 4;
        public override int BaseFireResistance => 1;
        public override int BaseColdResistance => 4;
        public override int BasePoisonResistance => 4;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 45;
        public override int InitMaxHits => 60;

        public override int AosStrReq => 55;
        public override int OldStrReq => 40;

        public override int ArmorBase => 30;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
    }
}
