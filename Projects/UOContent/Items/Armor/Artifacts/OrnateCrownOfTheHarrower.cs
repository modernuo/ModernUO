using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class OrnateCrownOfTheHarrower : BoneHelm
    {
        [Constructible]
        public OrnateCrownOfTheHarrower()
        {
            Hue = 0x4F6;
            Attributes.RegenHits = 2;
            Attributes.RegenStam = 3;
            Attributes.WeaponDamage = 25;
        }

        public override int LabelNumber => 1061095; // Ornate Crown of the Harrower
        public override int ArtifactRarity => 11;

        public override int BasePoisonResistance => 17;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
