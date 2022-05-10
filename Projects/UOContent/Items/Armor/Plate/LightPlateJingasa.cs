using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LightPlateJingasa : BaseArmor
    {
        [Constructible]
        public LightPlateJingasa() : base(0x2781) => Weight = 5.0;

        public override int BasePhysicalResistance => 7;
        public override int BaseFireResistance => 2;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 2;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 55;
        public override int InitMaxHits => 60;

        public override int AosStrReq => 55;
        public override int OldStrReq => 55;

        public override int ArmorBase => 4;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
    }
}
