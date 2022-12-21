using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class FisherGuildmaster : BaseGuildmaster
    {
        [Constructible]
        public FisherGuildmaster() : base("fisher")
        {
            SetSkill(SkillName.Fishing, 80.0, 100.0);
        }

        public override NpcGuild NpcGuild => NpcGuild.FishermensGuild;
    }
}
