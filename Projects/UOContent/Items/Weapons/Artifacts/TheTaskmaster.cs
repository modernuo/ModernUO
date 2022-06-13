using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class TheTaskmaster : WarFork
    {
        [Constructible]
        public TheTaskmaster()
        {
            Hue = 0x4F8;
            WeaponAttributes.HitPoisonArea = 100;
            Attributes.BonusDex = 5;
            Attributes.AttackChance = 15;
            Attributes.WeaponDamage = 50;
        }

        public override int LabelNumber => 1061110; // The Taskmaster
        public override int ArtifactRarity => 10;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override void GetDamageTypes(
            Mobile wielder, out int phys, out int fire, out int cold, out int pois,
            out int nrgy, out int chaos, out int direct
        )
        {
            phys = fire = cold = nrgy = chaos = direct = 0;
            pois = 100;
        }
    }
}
