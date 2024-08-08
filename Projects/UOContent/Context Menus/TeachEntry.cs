using Server.Mobiles;

namespace Server.ContextMenus
{
    public class TeachEntry : ContextMenuEntry
    {
        private readonly SkillName _skill;

        public TeachEntry(SkillName skill, bool enabled) : base(6000 + (int)skill)
        {
            _skill = skill;
            Enabled = enabled;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (!from.CheckAlive() || target is not BaseCreature bc)
            {
                return;
            }

            bc.Teach(_skill, from, 0, false);
        }
    }
}
