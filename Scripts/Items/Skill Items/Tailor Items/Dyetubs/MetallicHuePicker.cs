using Server;
using Server.Gumps;
using Server.Network;
using System;

namespace Server.Items
{
	public class MetallicHuePicker : Gump
	{
		public delegate void MetallicHuePickerCallback( Mobile from, object state, int hue );

		private Mobile m_From;
		private MetallicHuePickerCallback m_Callback;
		private object m_State;

		public void Render()
		{
			AddPage( 0 );

			AddBackground( 0, 0, 450, 450, 0x13BE );
			AddBackground( 10, 10, 430, 430, 0xBB8 );

			AddHtmlLocalized( 55, 400, 200, 25, 1011036, false, false ); // OKAY

			AddButton( 20, 400, 4005, 4007, 1, GumpButtonType.Reply, 0 );
			AddButton( 200, 400, 4005, 4007, 2, GumpButtonType.Reply, 0 );
			AddLabel( 235, 400, 0, "DEFAULT" );

			AddHtmlLocalized( 55, 25, 200, 25, 1150063, false, false ); // Base/Shadow Color
			AddHtmlLocalized( 260, 25, 200, 25, 1150064, false, false ); // Highlight Color

			for( int row = 0; row < 13; row++ )
			{
				AddButton( 30, ( 65 + ( row * 25 ) ), 0x1467, 0x1468, row + 1, GumpButtonType.Page, row + 1 );
				AddItem( 50, ( 65 + ( row * 25 ) ), 0x1412, 2501 + ( row * 12 ) + ( ( row == 12 ) ? 6 : 0 ) );
			}

			for( int page = 1; page < 14; page++ )
			{
				AddPage( page );

				for( int row = 0; row < 12; row++ )
				{
					int hue = ( 2501 + ( ( page == 13 ) ? 6 : 0 ) + ( row + ( 12 * ( page - 1 ) ) ) ); /* OSI just had to skip 6 unused hues, didnt they */
					AddRadio( 260, ( 65 + ( row * 25 ) ), 0xd2, 0xd3, false, hue );
					AddItem( 280, ( 65 + ( row * 25 ) ), 0x1412, hue );
				}
			}
		}

		public MetallicHuePicker( Mobile from, MetallicHuePickerCallback callback, object state )
			: base( 450, 450 )
		{
			m_From = from;
			m_Callback = callback;
			m_State = state;

			Render();
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			switch( info.ButtonID )
			{
				case 1: // Okay
				{
					if( info.Switches.Length > 0 )
					{
						m_Callback( m_From, m_State, info.Switches[ 0 ] );
					}
					break;
				}
				case 2: // Default
				{
					m_Callback( m_From, m_State, 0 );

					break;
				}
			}
		}
	}
}