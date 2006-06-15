using System;
using Server.Mobiles;

namespace Server.ContextMenus
{
	public class TeachEntry : ContextMenuEntry
	{
		private SkillName m_Skill;
		private BaseCreature m_Mobile;
		private Mobile m_From;

		public TeachEntry( SkillName skill, BaseCreature m, Mobile from, bool enabled ) : base( 6000 + (int)skill, 4 )
		{
			m_Skill = skill;
			m_Mobile = m;
			m_From = from;

			if ( !enabled )
				Flags |= Network.CMEFlags.Disabled;
		}

		public override void OnClick()
		{
			if ( !m_From.CheckAlive() )
				return;

			m_Mobile.Teach( m_Skill, m_From, 0, false );
		}
	}
}