using System;
using System.Collections;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.ConPVP
{
	public class ReadyGump : Gump
	{
		private Mobile m_From;
		private DuelContext m_Context;
		private int m_Count;

		public string Center( string text )
		{
			return String.Format( "<CENTER>{0}</CENTER>", text );
		}

		public ReadyGump( Mobile from, DuelContext context, int count ) : base( 50, 50 )
		{
			m_From = from;
			m_Context = context;
			m_Count = count;

			ArrayList parts = context.Participants;

			int height = 25 + 20;

			for ( int i = 0; i < parts.Count; ++i )
			{
				Participant p = (Participant)parts[i];

				height += 4;

				if ( p.Players.Length > 1 )
					height += 22;

				height += (p.Players.Length * 22);
			}

			height += 25;

			Closable = false;
			Dragable = false;

			AddPage( 0 );

			AddBackground( 0, 0, 260, height, 9250 );
			AddBackground( 10, 10, 240, height - 20, 0xDAC );

			if ( count == -1 )
			{
				AddHtml( 35, 25, 190, 20, Center( "Ready" ), false, false );
			}
			else
			{
				AddHtml( 35, 25, 190, 20, Center( "Starting" ), false, false );
				AddHtml( 35, 25, 190, 20, "<DIV ALIGN=RIGHT>" + count.ToString(), false, false );
			}

			int y = 25 + 20;

			for ( int i = 0; i < parts.Count; ++i )
			{
				Participant p = (Participant)parts[i];

				y += 4;

				bool isAllReady = true;
				int yStore = y;
				int offset = 0;

				if ( p.Players.Length > 1 )
				{
					AddHtml( 35 + 14, y, 176, 20, String.Format( "Participant #{0}", i + 1 ), false, false );
					y += 22;
					offset = 10;
				}

				for ( int j = 0; j < p.Players.Length; ++j )
				{
					DuelPlayer pl = p.Players[j];

					if ( pl != null && pl.Ready )
					{
						AddImage( 35 + offset, y + 4, 0x939 );
					}
					else
					{
						AddImage( 35 + offset, y + 4, 0x938 );
						isAllReady = false;
					}

					string name = ( pl == null ? "(Empty)" : pl.Mobile.Name );

					AddHtml( 35 + offset + 14, y, 166, 20, name, false, false );

					y += 22;
				}

				if ( p.Players.Length > 1 )
					AddImage( 35, yStore + 4, isAllReady ? 0x939 : 0x938 );
			}
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
		}
	}
}