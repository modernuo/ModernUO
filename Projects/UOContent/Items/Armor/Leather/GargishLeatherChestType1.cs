using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [TypeAlias("Server.Items.GargishLeatherChest")]
    public partial class GargishLeatherChestType1 : BaseArmor
    {
        [Constructible]
        public GargishLeatherChestType1() : base(0x0304) => Weight = 4.0;

        public override int RequiredRaces => Race.AllowGargoylesOnly;
        public override int BasePhysicalResistance => 5;
        public override int BaseFireResistance => 6;
        public override int BaseColdResistance => 7;
        public override int BasePoisonResistance => 6;
        public override int BaseEnergyResistance => 6;

        public override int InitMinHits => 30;
        public override int InitMaxHits => 50;

        public override int AosStrReq => 25;
        public override int OldStrReq => 25;
        public override int ArmorBase => 13;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;
    }
}
