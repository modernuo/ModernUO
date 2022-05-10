using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Pacify : Pike
    {
        [Constructible]
        public Pacify()
        {
            Hue = 0x835;

            Attributes.SpellChanneling = 1;
            Attributes.AttackChance = 10;
            Attributes.WeaponSpeed = 20;
            Attributes.WeaponDamage = 50;

            WeaponAttributes.HitLeechMana = 100;
            WeaponAttributes.UseBestSkill = 1;
        }

        public override int LabelNumber => 1094929; // Pacify [Replica]

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;
    }
}
