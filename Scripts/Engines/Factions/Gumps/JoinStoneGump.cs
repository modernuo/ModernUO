using System;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Factions
{
	public class JoinStoneGump : FactionGump
	{
		private PlayerMobile m_From;
		private Faction m_Faction;

		public JoinStoneGump( PlayerMobile from, Faction faction ) : base( 20, 30 )
		{
			m_From = from;
			m_Faction = faction;

			AddPage( 0 );

			AddBackground( 0, 0, 550, 440, 5054 );
			AddBackground( 10, 10, 530, 420, 3000 );


			AddHtmlText( 20, 30, 510, 20, faction.Definition.Header, false, false );
			AddHtmlText( 20, 130, 510, 100, faction.Definition.About, true, true );


			AddHtmlLocalized( 20, 60, 100, 20, 1011429, false, false ); // Led By : 
			AddHtml( 125, 60, 200, 20, faction.Commander != null ? faction.Commander.Name : "Nobody", false, false );

			AddHtmlLocalized( 20, 80, 100, 20, 1011457, false, false ); // Tithe rate : 
			if ( faction.Tithe >= 0 && faction.Tithe <= 100 && (faction.Tithe % 10) == 0 )
				AddHtmlLocalized( 125, 80, 350, 20, 1011480 + (faction.Tithe / 10), false, false );
			else
				AddHtml( 125, 80, 350, 20, faction.Tithe + "%", false, false );


			AddButton( 20, 400, 4005, 4007, 1, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 400, 200, 20, 1011425, false, false ); // JOIN THIS FACTION

			AddButton( 300, 400, 4005, 4007, 0, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 335, 400, 200, 20, 1011012, false, false ); // CANCEL
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			if ( info.ButtonID == 1 )
				m_Faction.OnJoinAccepted( m_From );
		}
	}
}