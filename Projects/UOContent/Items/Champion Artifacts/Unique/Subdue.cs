using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Subdue : Scythe
    {
        [Constructible]
        public Subdue()
        {
            Hue = 0x2cb;

            Attributes.SpellChanneling = 1;
            Attributes.WeaponSpeed = 20;
            Attributes.WeaponDamage = 50;
            Attributes.AttackChance = 10;

            WeaponAttributes.HitLeechMana = 100;
            WeaponAttributes.UseBestSkill = 1;
        }

        public override int LabelNumber => 1094930; // Subdue [Replica]

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;
    }
}
