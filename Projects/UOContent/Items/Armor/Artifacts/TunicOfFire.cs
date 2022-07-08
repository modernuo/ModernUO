using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class TunicOfFire : ChainChest
    {
        [Constructible]
        public TunicOfFire()
        {
            Hue = 0x54F;
            ArmorAttributes.SelfRepair = 5;
            Attributes.NightSight = 1;
            Attributes.ReflectPhysical = 15;
        }

        public override int LabelNumber => 1061099; // Tunic of Fire
        public override int ArtifactRarity => 11;

        public override int BasePhysicalResistance => 24;
        public override int BaseFireResistance => 34;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
