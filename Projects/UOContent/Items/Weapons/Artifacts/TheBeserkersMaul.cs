using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class TheBeserkersMaul : Maul
    {
        [Constructible]
        public TheBeserkersMaul()
        {
            Hue = 0x21;
            Attributes.WeaponSpeed = 75;
            Attributes.WeaponDamage = 50;
        }

        public override int LabelNumber => 1061108; // The Berserker's Maul
        public override int ArtifactRarity => 11;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
