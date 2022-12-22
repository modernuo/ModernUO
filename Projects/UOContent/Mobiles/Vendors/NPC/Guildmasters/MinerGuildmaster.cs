using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class MinerGuildmaster : BaseGuildmaster
    {
        [Constructible]
        public MinerGuildmaster() : base("miner")
        {
            SetSkill(SkillName.ItemID, 60.0, 83.0);
            SetSkill(SkillName.Mining, 90.0, 100.0);
        }
    }
}
