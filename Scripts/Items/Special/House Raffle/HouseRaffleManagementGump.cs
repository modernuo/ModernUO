using System;
using Server;
using Server.Accounting;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Gumps
{
	public class HouseRaffleManagementGump : Gump
	{
		public string Right( string text )
		{
			return String.Format( "<DIV ALIGN=RIGHT>{0}</DIV>", text );
		}

		public string Center( string text )
		{
			return String.Format( "<CENTER>{0}</CENTER>", text );
		}

		public string Color( string text, int color )
		{
			return String.Format( "<BASEFONT COLOR=#{0:X6}>{1}</BASEFONT>", color, text );
		}

		public const int LabelColor = 0xFFFFFF;

		private HouseRaffleStone m_Stone;
		private int m_Page;

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			Mobile from = sender.Mobile;
			int buttonId = info.ButtonID;

			if ( buttonId == 2 && m_Page > 0 )
			{
				from.SendGump( new HouseRaffleManagementGump( m_Stone, m_Page - 1 ) );
			}
			else if ( buttonId == 3 && (m_Page + 1) * 10 < m_Stone.Entries.Count )
			{
				from.SendGump( new HouseRaffleManagementGump( m_Stone, m_Page + 1 ) );
			}
			else
			{
				buttonId -= 4;

				if ( buttonId >= 0 && buttonId < m_Stone.Entries.Count )
				{
					m_Stone.Entries.RemoveAt( buttonId );
					from.SendGump( new HouseRaffleManagementGump( m_Stone, m_Page ) );
				}
			}
		}

		public HouseRaffleManagementGump( HouseRaffleStone stone )
			: this( stone, 0 )
		{
		}

		public HouseRaffleManagementGump( HouseRaffleStone stone, int page ) : base( 40, 40 )
		{
			m_Stone = stone;
			m_Page = page;

			AddPage( 0 );

			AddBackground( 0, 0, 618, 354, 9270 );
			AddAlphaRegion( 10, 10, 598, 334 );

			AddHtml( 10, 10, 598, 20, Color( Center( "Raffle Management" ), LabelColor ), false, false );

			AddHtml(  45, 35, 100, 20, Color( "Location:", LabelColor ), false, false );
			AddHtml( 145, 35, 250, 20, Color( m_Stone.FormatLocation(), LabelColor ), false, false );

			AddHtml(  45, 55, 100, 20, Color( "Ticket Price:", LabelColor ), false, false );
			AddHtml( 145, 55, 250, 20, Color( m_Stone.FormatPrice(), LabelColor ), false, false );

			AddHtml(  45, 75, 100, 20, Color( "Total Entries:", LabelColor ), false, false );
			AddHtml( 145, 75, 250, 20, Color( m_Stone.Entries.Count.ToString(), LabelColor ), false, false );

			AddImageTiled( 13, 99, 592, 242, 9264 );
			AddImageTiled( 14, 100, 590, 240, 9274 );
			AddAlphaRegion( 14, 100, 590, 240 );

			AddHtml( 14, 100, 590, 20, Color( Center( "Entries" ), LabelColor ), false, false );

			if ( page > 0 )
				AddButton( 567, 104, 0x15E3, 0x15E7, 2, GumpButtonType.Reply, 0 );
			else
				AddImage( 567, 104, 0x25EA );

			if ( (page + 1) * 10 < m_Stone.Entries.Count )
				AddButton( 584, 104, 0x15E1, 0x15E5, 3, GumpButtonType.Reply, 0 );
			else
				AddImage( 584, 104, 0x25E6 );

			AddHtml( 14, 120, 30, 20, Color( Center( "DEL" ), LabelColor ), false, false );
			AddHtml( 47, 120, 250, 20, Color( "Name", LabelColor ), false, false );
			AddHtml( 295, 120, 100, 20, Color( Center( "Address" ), LabelColor ), false, false );
			AddHtml( 395, 120, 150, 20, Color( Center( "Date" ), LabelColor ), false, false );
			AddHtml( 545, 120, 60, 20, Color( Center( "Num" ), LabelColor ), false, false );

			int idx = 0;

			for ( int i = page * 10; i >= 0 && i < m_Stone.Entries.Count && i < (page + 1) * 10; ++i, ++idx )
			{
				RaffleEntry entry = m_Stone.Entries[i];

				AddButton( 13, 138 + (idx * 20), 4002, 4004, 4 + i, GumpButtonType.Reply, 0 );

				int x = 45;

				string name;
				Account acc = entry.From.Account as Account;

				if ( acc != null )
					name = String.Format( "{0} ({1})", entry.From.Name, acc );
				else
					name = entry.From.Name;

				AddHtml( x + 2, 140 + (idx * 20), 250, 20, Color( name, LabelColor ), false, false );
				x += 250;

				AddHtml( x, 140 + (idx * 20), 100, 20, Color( Center( entry.Address.ToString() ), LabelColor ), false, false );
				x += 100;

				AddHtml( x, 140 + (idx * 20), 150, 20, Color( Center( entry.Date.ToString() ), LabelColor ), false, false );
				x += 150;

				AddHtml( x, 140 + (idx * 20), 60, 20, Color( Center( "1" ), LabelColor ), false, false );
				x += 60;
			}
		}
	}
}
