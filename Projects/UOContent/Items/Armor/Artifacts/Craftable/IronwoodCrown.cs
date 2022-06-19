using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class IronwoodCrown : RavenHelm
    {
        [Constructible]
        public IronwoodCrown()
        {
            Hue = 0x1;

            ArmorAttributes.SelfRepair = 3;

            Attributes.BonusStr = 5;
            Attributes.BonusDex = 5;
            Attributes.BonusInt = 5;
        }

        public override int LabelNumber => 1072924; // Ironwood Crown

        public override int BasePhysicalResistance => 10;
        public override int BaseFireResistance => 6;
        public override int BaseColdResistance => 7;
        public override int BasePoisonResistance => 7;
        public override int BaseEnergyResistance => 10;
    }
}
