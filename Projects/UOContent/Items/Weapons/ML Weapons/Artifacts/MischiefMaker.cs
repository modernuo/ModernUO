using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class MischiefMaker : MagicalShortbow
    {
        [Constructible]
        public MischiefMaker()
        {
            Hue = 0x8AB;
            Slayer = SlayerName.Silver;
            Balanced = true;
            Attributes.WeaponSpeed = 35;
            Attributes.WeaponDamage = 45;
        }

        public override int LabelNumber => 1072910; // Mischief Maker

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
