using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    [Flippable(0x2B6A, 0x3161)]
    public partial class WoodlandGloves : BaseArmor
    {
        [Constructible]
        public WoodlandGloves() : base(0x2B6A) => Weight = 2.0;

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 2;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 2;

        public override int InitMinHits => 50;
        public override int InitMaxHits => 65;

        public override int AosStrReq => 70;
        public override int OldStrReq => 70;

        public override int ArmorBase => 40;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Wood;
        public override int RequiredRaces => Race.AllowElvesOnly;
    }
}
