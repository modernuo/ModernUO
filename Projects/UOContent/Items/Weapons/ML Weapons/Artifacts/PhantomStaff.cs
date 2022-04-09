using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class PhantomStaff : WildStaff
    {
        [Constructible]
        public PhantomStaff()
        {
            Hue = 0x1;
            Attributes.RegenHits = 2;
            Attributes.NightSight = 1;
            Attributes.WeaponSpeed = 20;
            Attributes.WeaponDamage = 60;
        }

        public override int LabelNumber => 1072919; // Phantom Staff

        public override void GetDamageTypes(
            Mobile wielder, out int phys, out int fire, out int cold, out int pois,
            out int nrgy, out int chaos, out int direct
        )
        {
            phys = fire = nrgy = chaos = direct = 0;
            cold = pois = 50;
        }
    }
}
