using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x4200, 0x4207)]
    [SerializationGenerator(0)]
    public partial class GargishWoodenShield : BaseShield
    {
        [Constructible]
        public GargishWoodenShield() : base(0x4200) => Weight = 5.0;

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 0;
        public override int BaseColdResistance => 0;
        public override int BasePoisonResistance => 0;
        public override int BaseEnergyResistance => 1;
        public override int InitMinHits => 20;
        public override int InitMaxHits => 25;
        public override int AosStrReq => 20;

        public override int RequiredRaces => Race.AllowGargoylesOnly;
    }
}
