using System;
using Server;
using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
	public class DeclareFealtyGump : GuildMobileListGump
	{
		public DeclareFealtyGump( Mobile from, Guild guild ) : base( from, guild, true, guild.Members )
		{
		}

		protected override void Design()
		{
			AddHtmlLocalized( 20, 10, 400, 35, 1011097, false, false ); // Declare your fealty

			AddButton( 20, 400, 4005, 4007, 1, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 55, 400, 250, 35, 1011098, false, false ); // I have selected my new lord.

			AddButton( 300, 400, 4005, 4007, 0, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 335, 400, 100, 35, 1011012, false, false ); // CANCEL
		}

		public override void OnResponse( NetState state, RelayInfo info )
		{
			if ( GuildGump.BadMember( m_Mobile, m_Guild ) )
				return;

			if ( info.ButtonID == 1 )
			{
				int[] switches = info.Switches;

				if ( switches.Length > 0 )
				{
					int index = switches[0];

					if ( index >= 0 && index < m_List.Count )
					{
						Mobile m = (Mobile)m_List[index];

						if ( m != null && !m.Deleted )
						{
							state.Mobile.GuildFealty = m;
						}
					}
				}
			}

			GuildGump.EnsureClosed( m_Mobile );
			m_Mobile.SendGump( new GuildGump( m_Mobile, m_Guild ) );
		}
	}
}
