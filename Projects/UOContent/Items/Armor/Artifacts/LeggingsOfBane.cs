using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class LeggingsOfBane : ChainLegs
    {
        [Constructible]
        public LeggingsOfBane()
        {
            Hue = 0x4F5;
            ArmorAttributes.DurabilityBonus = 100;
            HitPoints = MaxHitPoints =
                255; // Cause the Durability bonus and such and the min/max hits as well as all other hits being whole #'s...
            Attributes.BonusStam = 8;
            Attributes.AttackChance = 20;
        }

        public override int LabelNumber => 1061100; // Leggings of Bane
        public override int ArtifactRarity => 11;

        public override int BasePoisonResistance => 36;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
