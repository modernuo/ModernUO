using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x1451, 0x1456)]
    public partial class BoneHelm : BaseArmor
    {
        [Constructible]
        public BoneHelm() : base(0x1451)
        {
        }

        public override double DefaultWeight => 3.0;

        public override int BasePhysicalResistance => 3;
        public override int BaseFireResistance => 3;
        public override int BaseColdResistance => 4;
        public override int BasePoisonResistance => 2;
        public override int BaseEnergyResistance => 4;

        public override int InitMinHits => 25;
        public override int InitMaxHits => 30;

        public override int AosStrReq => 20;
        public override int OldStrReq => 40;

        public override int ArmorBase => 30;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Bone;
    }
}
