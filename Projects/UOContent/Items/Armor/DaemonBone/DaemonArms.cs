using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x144e, 0x1453)]
    public partial class DaemonArms : BaseArmor
    {
        [Constructible]
        public DaemonArms() : base(0x144E)
        {
            Weight = 2.0;
            Hue = 0x648;

            ArmorAttributes.SelfRepair = 1;
        }

        public override int BasePhysicalResistance => 6;
        public override int BaseFireResistance => 6;
        public override int BaseColdResistance => 7;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 7;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override int AosStrReq => 55;
        public override int OldStrReq => 40;

        public override int OldDexBonus => -2;

        public override int ArmorBase => 46;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Bone;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override int LabelNumber => 1041371; // daemon bone arms
    }
}
