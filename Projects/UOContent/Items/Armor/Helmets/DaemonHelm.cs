using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x1451, 0x1456)]
    public partial class DaemonHelm : BaseArmor
    {
        [Constructible]
        public DaemonHelm() : base(0x1451)
        {
            Hue = 0x648;
            Weight = 3.0;

            ArmorAttributes.SelfRepair = 1;
        }

        public override int BasePhysicalResistance => 6;
        public override int BaseFireResistance => 6;
        public override int BaseColdResistance => 7;
        public override int BasePoisonResistance => 5;
        public override int BaseEnergyResistance => 7;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override int AosStrReq => 20;
        public override int OldStrReq => 40;

        public override int ArmorBase => 46;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Bone;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override int LabelNumber => 1041374; // daemon bone helmet
    }
}
