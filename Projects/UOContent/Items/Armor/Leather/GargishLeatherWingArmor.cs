using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    [Flippable(0x457E, 0x457F)]
    public partial class GargishLeatherWingArmor : BaseArmor
    {
        [Constructible]
        public GargishLeatherWingArmor() : base(0x457E) => Weight = 2.0;

        public override int RequiredRaces => Race.AllowGargoylesOnly;
        public override int PhysicalResistance => 0;
        public override int FireResistance => 0;
        public override int ColdResistance => 0;
        public override int PoisonResistance => 0;
        public override int EnergyResistance => 0;

        public override int AosStrReq => 10;
        public override int OldStrReq => 10;
        public override int ArmorBase => 0;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;
    }
}
