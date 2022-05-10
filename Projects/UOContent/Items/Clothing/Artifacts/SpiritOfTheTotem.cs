using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class SpiritOfTheTotem : BearMask
    {
        [Constructible]
        public SpiritOfTheTotem()
        {
            Hue = 0x455;

            Attributes.BonusStr = 20;
            Attributes.ReflectPhysical = 15;
            Attributes.AttackChance = 15;
        }

        public override int LabelNumber => 1061599; // Spirit of the Totem

        public override int ArtifactRarity => 11;

        public override int BasePhysicalResistance => 20;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
