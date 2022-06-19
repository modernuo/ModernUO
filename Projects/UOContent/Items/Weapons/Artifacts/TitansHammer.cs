using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class TitansHammer : WarHammer
    {
        [Constructible]
        public TitansHammer()
        {
            Hue = 0x482;
            WeaponAttributes.HitEnergyArea = 100;
            Attributes.BonusStr = 15;
            Attributes.AttackChance = 15;
            Attributes.WeaponDamage = 50;
        }

        public override int LabelNumber => 1060024; // Titan's Hammer
        public override int ArtifactRarity => 10;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;
    }
}
