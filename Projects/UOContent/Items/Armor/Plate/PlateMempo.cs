using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class PlateMempo : BaseArmor
    {
        [Constructible]
        public PlateMempo() : base(0x2779) => Weight = 3.0;

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 60;
        public override int InitMaxHits => 70;

        public override int AosStrReq => 50;
        public override int OldStrReq => 50;

        public override int ArmorBase => 4;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
    }
}
