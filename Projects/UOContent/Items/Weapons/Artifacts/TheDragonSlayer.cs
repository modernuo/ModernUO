using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class TheDragonSlayer : Lance
    {
        [Constructible]
        public TheDragonSlayer()
        {
            Hue = 0x530;
            Slayer = SlayerName.DragonSlaying;
            Attributes.Luck = 110;
            Attributes.WeaponDamage = 50;
            WeaponAttributes.ResistFireBonus = 20;
            WeaponAttributes.UseBestSkill = 1;
        }

        public override int LabelNumber => 1061248; // The Dragon Slayer
        public override int ArtifactRarity => 11;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override void GetDamageTypes(
            Mobile wielder, out int phys, out int fire, out int cold, out int pois,
            out int nrgy, out int chaos, out int direct
        )
        {
            phys = fire = cold = pois = chaos = direct = 0;
            nrgy = 100;
        }
    }
}
