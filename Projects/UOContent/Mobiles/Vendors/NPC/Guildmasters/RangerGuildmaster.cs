using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class RangerGuildmaster : BaseGuildmaster
    {
        [Constructible]
        public RangerGuildmaster() : base("ranger")
        {
            SetSkill(SkillName.AnimalLore, 64.0, 100.0);
            SetSkill(SkillName.Camping, 75.0, 98.0);
            SetSkill(SkillName.Hiding, 75.0, 98.0);
            SetSkill(SkillName.MagicResist, 75.0, 98.0);
            SetSkill(SkillName.Tactics, 65.0, 88.0);
            SetSkill(SkillName.Archery, 90.0, 100.0);
            SetSkill(SkillName.Tracking, 90.0, 100.0);
            SetSkill(SkillName.Stealth, 60.0, 83.0);
            SetSkill(SkillName.Fencing, 36.0, 68.0);
            SetSkill(SkillName.Herding, 36.0, 68.0);
            SetSkill(SkillName.Swords, 45.0, 68.0);
        }

        public override NpcGuild NpcGuild => NpcGuild.RangersGuild;
    }
}
