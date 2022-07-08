using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class SingingAxe : OrnateAxe
    {
        [Constructible]
        public SingingAxe()
        {
            SkillBonuses.SetValues(0, SkillName.Musicianship, 5);
        }

        public override int LabelNumber => 1073546; // singing axe
    }
}
