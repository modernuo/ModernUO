using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class BrambleCoat : WoodlandChest
    {
        [Constructible]
        public BrambleCoat()
        {
            Hue = 0x1;

            ArmorAttributes.SelfRepair = 3;
            Attributes.BonusHits = 4;
            Attributes.Luck = 150;
            Attributes.ReflectPhysical = 25;
            Attributes.DefendChance = 15;
        }

        public override int LabelNumber => 1072925; // Bramble Coat

        public override int BasePhysicalResistance => 10;
        public override int BaseFireResistance => 8;
        public override int BaseColdResistance => 7;
        public override int BasePoisonResistance => 8;
        public override int BaseEnergyResistance => 7;
    }
}
