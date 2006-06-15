using System;
using Server;
using Server.Mobiles;

namespace Server.Gumps
{
	public class ConfirmReleaseGump : Gump
	{
		private Mobile m_From;
		private BaseCreature m_Pet;

		public ConfirmReleaseGump( Mobile from, BaseCreature pet ) : base( 50, 50 )
		{
			m_From = from;
			m_Pet = pet;

			m_From.CloseGump( typeof( ConfirmReleaseGump ) );

			AddPage( 0 );

			AddBackground( 0, 0, 270, 120, 5054 );
			AddBackground( 10, 10, 250, 100, 3000 );

			AddHtmlLocalized( 20, 15, 230, 60, 1046257, true, true ); // Are you sure you want to release your pet?

			AddButton( 20, 80, 4005, 4007, 2, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 80, 75, 20, 1011011, false, false ); // CONTINUE

			AddButton( 135, 80, 4005, 4007, 1, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 170, 80, 75, 20, 1011012, false, false ); // CANCEL
		}

		public override void OnResponse( Server.Network.NetState sender, RelayInfo info )
		{
			if ( info.ButtonID == 2 )
			{
				if ( !m_Pet.Deleted && m_Pet.Controlled && m_From == m_Pet.ControlMaster && m_From.CheckAlive() /*&& m_Pet.CheckControlChance( m_From )*/ )
				{
					if ( m_Pet.Map == m_From.Map && m_Pet.InRange( m_From, 14 ) )
					{
						m_Pet.ControlTarget = null;
						m_Pet.ControlOrder = OrderType.Release;
					}
				}
			}
		}
	}
}