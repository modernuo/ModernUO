using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class TheNightReaper : RepeatingCrossbow
    {
        [Constructible]
        public TheNightReaper()
        {
            ItemID = 0x26CD;
            Hue = 0x41C;

            Slayer = SlayerName.Exorcism;
            Attributes.NightSight = 1;
            Attributes.WeaponSpeed = 25;
            Attributes.WeaponDamage = 55;
        }

        public override int LabelNumber => 1072912; // The Night Reaper
    }
}
