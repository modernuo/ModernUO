using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class BladeOfInsanity : Katana
    {
        [Constructible]
        public BladeOfInsanity()
        {
            Hue = 0x76D;
            WeaponAttributes.HitLeechStam = 100;
            Attributes.RegenStam = 2;
            Attributes.WeaponSpeed = 30;
            Attributes.WeaponDamage = 50;
        }

        public override int LabelNumber => 1061088; // Blade of Insanity
        public override int ArtifactRarity => 11;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
