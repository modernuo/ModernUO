using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class WarriorGuildmaster : BaseGuildmaster
    {
        [Constructible]
        public WarriorGuildmaster() : base("warrior")
        {
            SetSkill(SkillName.ArmsLore, 75.0, 98.0);
            SetSkill(SkillName.Parry, 85.0, 100.0);
            SetSkill(SkillName.MagicResist, 60.0, 83.0);
            SetSkill(SkillName.Tactics, 85.0, 100.0);
            SetSkill(SkillName.Swords, 90.0, 100.0);
            SetSkill(SkillName.Macing, 60.0, 83.0);
            SetSkill(SkillName.Fencing, 60.0, 83.0);
        }

        public override NpcGuild NpcGuild => NpcGuild.WarriorsGuild;
    }
}
