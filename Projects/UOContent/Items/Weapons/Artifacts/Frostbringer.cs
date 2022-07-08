using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Frostbringer : Bow
    {
        [Constructible]
        public Frostbringer()
        {
            Hue = 0x4F2;
            WeaponAttributes.HitDispel = 50;
            Attributes.RegenStam = 10;
            Attributes.WeaponDamage = 50;
        }

        public override int LabelNumber => 1061111; // Frostbringer
        public override int ArtifactRarity => 11;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override void GetDamageTypes(
            Mobile wielder, out int phys, out int fire, out int cold, out int pois,
            out int nrgy, out int chaos, out int direct
        )
        {
            phys = fire = pois = nrgy = chaos = direct = 0;
            cold = 100;
        }
    }
}
