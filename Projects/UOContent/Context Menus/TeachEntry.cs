using Server.Mobiles;
using Server.Network;

namespace Server.ContextMenus
{
    public class TeachEntry : ContextMenuEntry
    {
        private readonly Mobile m_From;
        private readonly BaseCreature m_Mobile;
        private readonly SkillName m_Skill;

        public TeachEntry(SkillName skill, BaseCreature m, Mobile from, bool enabled) : base(6000 + (int)skill)
        {
            m_Skill = skill;
            m_Mobile = m;
            m_From = from;

            if (!enabled)
            {
                Flags |= CMEFlags.Disabled;
            }
        }

        public override void OnClick()
        {
            if (!m_From.CheckAlive())
            {
                return;
            }

            m_Mobile.Teach(m_Skill, m_From, 0, false);
        }
    }
}
