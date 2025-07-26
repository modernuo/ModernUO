using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    [TypeAlias("Server.Items.GargishClothArms", "Server.Items.GargishClothArmsArmor")]
    public partial class GargishClothArmsType1 : BaseArmor
    {
        [Constructible]
        public GargishClothArmsType1() : base(0x404)
        {
        }

        public override double DefaultWeight => 2.0;

        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 7;
        public override int BaseColdResistance => 6;
        public override int BasePoisonResistance => 6;
        public override int BaseEnergyResistance => 6;
        public override int InitMinHits => 40;
        public override int InitMaxHits => 50;
        public override int AosStrReq => 20;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Cloth;
        public override CraftResource DefaultResource => CraftResource.None;
        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;

        public override int RequiredRaces => Race.AllowGargoylesOnly;
    }
}
