using System;
using Server.Mobiles;
using Server.Engines.PartySystem;

namespace Server.ContextMenus
{
	public class AddToPartyEntry : ContextMenuEntry
	{
		private Mobile m_From;
		private Mobile m_Target;
		
		public AddToPartyEntry( Mobile from, Mobile target ) : base( 0197, 12 )
		{
			m_From = from;
			m_Target = target;
		}

		public override void OnClick()
		{			
			Party.Invite( m_From, m_Target );
		}
	}
}
