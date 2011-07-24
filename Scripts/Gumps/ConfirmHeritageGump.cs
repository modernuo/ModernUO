using System;
using Server;
using Server.Items;
using Server.Network;

namespace Server.Gumps
{
	public class ConfirmHeritageGump : Gump
	{
		private HeritageToken m_Token;
		private Type[] m_Selected;

		private enum Buttons
		{
			Cancel,
			Okay
		}

		public ConfirmHeritageGump( HeritageToken token, Type[] selected, int cliloc ) : base( 60, 36 )
		{
			m_Token = token;
			m_Selected = selected;

			AddPage( 0 );

			AddBackground( 0, 0, 291, 99, 0x13BE );
			AddImageTiled( 5, 6, 280, 20, 0xA40 );
			AddHtmlLocalized( 9, 8, 280, 20, 1070972, 0x7FFF, false, false ); // Click "OKAY" to redeem the following promotional item:
			AddImageTiled( 5, 31, 280, 40, 0xA40 );
			AddHtmlLocalized( 9, 35, 272, 40, cliloc, 0x7FFF, false, false );
			AddButton( 180, 73, 0xFB7, 0xFB8, (int) Buttons.Okay, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 215, 75, 100, 20, 1011036, 0x7FFF, false, false ); // OKAY
			AddButton( 5, 73, 0xFB1, 0xFB2, (int) Buttons.Cancel, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 40, 75, 100, 20, 1060051, 0x7FFF, false, false ); // CANCEL
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			if ( m_Token == null || m_Token.Deleted )
				return;

			switch ( info.ButtonID )
			{
				case (int) Buttons.Okay:
					
					Item item = null;

					foreach ( Type type in m_Selected )
					{
						try
						{
							item = Activator.CreateInstance( type ) as Item;
						}
						catch ( Exception ex )
						{
							Console.WriteLine( ex.Message );
							Console.WriteLine( ex.StackTrace );
						}				

						if ( item != null )
						{
							m_Token.Delete();
							sender.Mobile.AddToBackpack( item );
						}
					}

					break;
				case (int) Buttons.Cancel:
					sender.Mobile.SendGump( new HeritageTokenGump( m_Token ) );
					break;
			}
		}
	}
}
