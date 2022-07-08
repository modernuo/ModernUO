using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LegacyOfTheDreadLord : Bardiche
    {
        [Constructible]
        public LegacyOfTheDreadLord()
        {
            Hue = 0x676;
            Attributes.SpellChanneling = 1;
            Attributes.CastRecovery = 3;
            Attributes.WeaponSpeed = 30;
            Attributes.WeaponDamage = 50;
        }

        public override int LabelNumber => 1060860; // Legacy of the Dread Lord
        public override int ArtifactRarity => 10;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
