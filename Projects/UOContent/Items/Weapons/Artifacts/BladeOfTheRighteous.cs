using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class BladeOfTheRighteous : Longsword
    {
        [Constructible]
        public BladeOfTheRighteous()
        {
            Hue = 0x47E;
            // Slayer = SlayerName.DaemonDismissal;
            Slayer = SlayerName.Exorcism;
            WeaponAttributes.HitLeechHits = 50;
            WeaponAttributes.UseBestSkill = 1;
            Attributes.BonusHits = 10;
            Attributes.WeaponDamage = 50;
        }

        public override int LabelNumber => 1061107; // Blade of the Righteous
        public override int ArtifactRarity => 10;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
