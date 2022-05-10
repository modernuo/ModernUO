using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x144f, 0x1454)]
    public partial class DaemonChest : BaseArmor
    {
        [Constructible]
        public DaemonChest() : base(0x144F)
        {
            Weight = 6.0;
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

        public override int AosStrReq => 60;
        public override int OldStrReq => 40;

        public override int OldDexBonus => -6;

        public override int ArmorBase => 46;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Bone;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override int LabelNumber => 1041372; // daemon bone armor
    }
}
