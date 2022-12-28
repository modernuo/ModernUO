using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class BardGuildmaster : BaseGuildmaster
    {
        [Constructible]
        public BardGuildmaster() : base("bard")
        {
            SetSkill(SkillName.Archery, 80.0, 100.0);
            SetSkill(SkillName.Discordance, 80.0, 100.0);
            SetSkill(SkillName.Musicianship, 80.0, 100.0);
            SetSkill(SkillName.Peacemaking, 80.0, 100.0);
            SetSkill(SkillName.Provocation, 80.0, 100.0);
            SetSkill(SkillName.Swords, 80.0, 100.0);
        }

        public override NpcGuild NpcGuild => NpcGuild.BardsGuild;

    }
}
