using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class BreathOfTheDead : BoneHarvester
    {
        [Constructible]
        public BreathOfTheDead()
        {
            Hue = 0x455;
            WeaponAttributes.HitLeechHits = 100;
            WeaponAttributes.HitHarm = 25;
            Attributes.SpellDamage = 5;
            Attributes.WeaponDamage = 50;
        }

        public override int LabelNumber => 1061109; // Breath of the Dead
        public override int ArtifactRarity => 11;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
