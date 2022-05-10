using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class FangOfRactus : Kryss
    {
        [Constructible]
        public FangOfRactus()
        {
            Hue = 0x117;

            Attributes.SpellChanneling = 1;
            Attributes.AttackChance = 5;
            Attributes.DefendChance = 5;
            Attributes.WeaponDamage = 35;

            WeaponAttributes.HitPoisonArea = 20;
            WeaponAttributes.ResistPoisonBonus = 15;
        }

        public override int LabelNumber => 1094892; // Fang of Ractus [Replica]

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;
    }
}
