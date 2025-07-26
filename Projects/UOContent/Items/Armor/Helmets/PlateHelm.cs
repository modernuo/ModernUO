using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class PlateHelm : BaseArmor
    {
        [Constructible]
        public PlateHelm() : base(0x1412)
        {
        }

        public override double DefaultWeight => 5.0;

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 50;
        public override int InitMaxHits => 65;

        public override int AosStrReq => 80;
        public override int OldStrReq => 40;

        public override int OldDexBonus => -1;

        public override int ArmorBase => 40;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
    }
}
