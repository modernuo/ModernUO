using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class OblivionsNeedle : Dagger
    {
        [Constructible]
        public OblivionsNeedle()
        {
            Attributes.BonusStam = 20;
            Attributes.AttackChance = 20;
            Attributes.DefendChance = -20;
            Attributes.WeaponDamage = 40;

            WeaponAttributes.HitLeechStam = 50;
        }

        public override int LabelNumber => 1094916; // Oblivion's Needle [Replica]

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;
    }
}
