using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class BoneCrusher : WarMace
    {
        [Constructible]
        public BoneCrusher()
        {
            ItemID = 0x1406;
            Hue = 0x60C;
            WeaponAttributes.HitLowerDefend = 50;
            Attributes.BonusStr = 10;
            Attributes.WeaponDamage = 75;
        }

        public override int LabelNumber => 1061596; // Bone Crusher
        public override int ArtifactRarity => 11;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
