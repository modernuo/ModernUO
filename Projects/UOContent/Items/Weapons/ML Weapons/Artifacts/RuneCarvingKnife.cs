using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class RuneCarvingKnife : AssassinSpike
    {
        [Constructible]
        public RuneCarvingKnife()
        {
            Hue = 0x48D;

            WeaponAttributes.HitLeechMana = 40;
            Attributes.RegenStam = 2;
            Attributes.LowerManaCost = 10;
            Attributes.WeaponSpeed = 35;
            Attributes.WeaponDamage = 30;
        }

        public override int LabelNumber => 1072915; // Rune Carving Knife
    }
}
