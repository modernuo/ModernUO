using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class TailorGuildmaster : BaseGuildmaster
    {
        [Constructible]
        public TailorGuildmaster() : base("tailor")
        {
            SetSkill(SkillName.Tailoring, 90.0, 100.0);
        }

        public override NpcGuild NpcGuild => NpcGuild.TailorsGuild;
    }
}
