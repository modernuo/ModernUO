using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class VoiceOfTheFallenKing : LeatherGorget
    {
        [Constructible]
        public VoiceOfTheFallenKing()
        {
            Hue = 0x76D;
            Attributes.BonusStr = 8;
            Attributes.RegenHits = 5;
            Attributes.RegenStam = 3;
        }

        public override int LabelNumber => 1061094; // Voice of the Fallen King
        public override int ArtifactRarity => 11;

        public override int BaseColdResistance => 18;
        public override int BaseEnergyResistance => 18;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
