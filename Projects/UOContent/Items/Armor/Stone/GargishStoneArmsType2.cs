using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [TypeAlias("Server.Items.FemaleGargishStoneArms")]
    public partial class GargishStoneArmsType2 : BaseArmor
    {
        [Constructible]
        public GargishStoneArmsType2() : base(0x283)
        {
        }

        public override double DefaultWeight => 10.0;

        public override int RequiredRaces => Race.AllowGargoylesOnly;
        public override int BasePhysicalResistance => 6;
        public override int BaseFireResistance => 6;
        public override int BaseColdResistance => 4;
        public override int BasePoisonResistance => 8;
        public override int BaseEnergyResistance => 6;

        public override int InitMinHits => 40;
        public override int InitMaxHits => 50;

        public override int AosStrReq => 40;
        public override int OldStrReq => 40;
        public override int ArmorBase => 40;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Stone;
    }
}
