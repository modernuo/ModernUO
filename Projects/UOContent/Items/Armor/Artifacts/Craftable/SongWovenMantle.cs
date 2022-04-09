using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class SongWovenMantle : LeafArms
    {
        [Constructible]
        public SongWovenMantle()
        {
            Hue = 0x493;

            SkillBonuses.SetValues(0, SkillName.Musicianship, 10.0);

            Attributes.Luck = 100;
            Attributes.DefendChance = 5;
        }

        public override int LabelNumber => 1072931; // Song Woven Mantle

        public override int BasePhysicalResistance => 14;
        public override int BaseColdResistance => 14;
        public override int BaseEnergyResistance => 16;
    }
}
