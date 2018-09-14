using System;

namespace Server.Gumps
{
	public delegate void NoticeGumpCallback( Mobile from, object state );

	public class NoticeGump : Gump
	{
		private NoticeGumpCallback m_Callback;
		private object m_State;

		public NoticeGump( int header, int headerColor, object content, int contentColor, int width, int height, NoticeGumpCallback callback, object state ) : base( (640 - width) / 2, (480 - height) / 2 )
		{
			m_Callback = callback;
			m_State = state;

			Closable = false;

			AddPage( 0 );

			AddBackground( 0, 0, width, height, 5054 );

			AddImageTiled( 10, 10, width - 20, 20, 2624 );
			AddAlphaRegion( 10, 10, width - 20, 20 );
			AddHtmlLocalized( 10, 10, width - 20, 20, header, headerColor, false, false );

			AddImageTiled( 10, 40, width - 20, height - 80, 2624 );
			AddAlphaRegion( 10, 40, width - 20, height - 80 );

			if ( content is int i )
				AddHtmlLocalized( 10, 40, width - 20, height - 80, i, contentColor, false, true );
			else if ( content is string )
				AddHtml( 10, 40, width - 20, height - 80, $"<BASEFONT COLOR=#{contentColor:X6}>{content}</BASEFONT>", false, true );

			AddImageTiled( 10, height - 30, width - 20, 20, 2624 );
			AddAlphaRegion( 10, height - 30, width - 20, 20 );
			AddButton( 10, height - 30, 4005, 4007, 1, GumpButtonType.Reply, 0 );
			AddHtmlLocalized( 40, height - 30, 120, 20, 1011036, 32767, false, false ); // OKAY
		}

		public override void OnResponse( Network.NetState sender, RelayInfo info )
		{
			if ( info.ButtonID == 1 )
				m_Callback?.Invoke( sender.Mobile, m_State );
		}
	}
}
