using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Bascinet : BaseArmor
    {
        [Constructible]
        public Bascinet() : base(0x140C) => Weight = 5.0;

        public override int BasePhysicalResistance => 7;
        public override int BaseFireResistance => 2;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 2;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 40;
        public override int InitMaxHits => 50;

        public override int AosStrReq => 40;
        public override int OldStrReq => 10;

        public override int ArmorBase => 18;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;
    }
}
