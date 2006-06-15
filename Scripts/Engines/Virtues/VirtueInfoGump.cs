using System;
using Server;
using Server.Gumps;
using Server.Network;

namespace Server
{
	public class VirtueInfoGump : Gump
	{
		private Mobile m_Beholder;
		private int m_Desc;
		private string m_Page;
		private VirtueName m_Virtue;

		public VirtueInfoGump( Mobile beholder, VirtueName virtue, int description ) : this( beholder, virtue, description, null )
		{
		}

		public VirtueInfoGump( Mobile beholder, VirtueName virtue, int description, string webPage ) : base( 0, 0 )
		{
			m_Beholder = beholder;
			m_Virtue = virtue;
			m_Desc = description;
			m_Page = webPage;

			int value = beholder.Virtues.GetValue( (int)virtue );

			AddPage( 0 );

			AddImage( 30, 40, 2080 );
			AddImage( 47, 77, 2081 );
			AddImage( 47, 147, 2081 );
			AddImage( 47, 217, 2081 );
			AddImage( 47, 267, 2083 );
			AddImage( 70, 213, 2091 );

			AddPage( 1 );

			int maxValue = VirtueHelper.GetMaxAmount( m_Virtue );

			int valueDesc;

			if( value < 1 )
				valueDesc = 1052044; // You have not started on the path of this Virtue.
			else if( value < maxValue/6 )
				valueDesc = 1052045; // You have barely begun your journey through the path of this Virtue.
			else if( value < maxValue/3 )
				valueDesc = 1052046; // You have progressed in this Virtue, but still have much to do.
			else if( value < maxValue/2 )
				valueDesc = 1052047; // Your journey through the path of this Virtue is going well.
			else if( value < 2*maxValue/3 )
				valueDesc = 1052048; // You feel very close to achieving your next path in this Virtue.
			else if( value < 5*maxValue/6 )
				valueDesc = 1052049; // You have achieved a path in this Virtue.
			else
				valueDesc = 1052050; // You have achieved the highest path in this Virtue.


			AddHtmlLocalized( 157, 73, 200, 40, 1051000 + (int)virtue, false, false );
			AddHtmlLocalized( 75, 95, 220, 140, description, false, false );
			AddHtmlLocalized( 70, 224, 229, 60, valueDesc, false, false );

			AddButton( 65, 277, 1209, 1209, 1, GumpButtonType.Reply, 0 );

			AddButton( 280, 43, 4014, 4014, 2, GumpButtonType.Reply, 0 );

			AddHtmlLocalized( 83, 275, 400, 40, (webPage == null) ? 1052055 : 1052052, false, false ); // This virtue is not yet defined. OR -click to learn more (opens webpage)

			int dots;

			if( value < 4000 )
				dots = value / 400;
			else if( value < 10000 )
				dots = (value - 4000) / 600;
			else if( value < maxValue )
				dots = (value - 10000) / ((maxValue-10000)/10);
			else
				dots = 10;

			for ( int i = 0; i < 10; ++i )
				AddImage( 95 + (i * 17), 50, i < dots ? 2362 : 2360 );
		}

		public override void OnResponse( NetState state, RelayInfo info )
		{
			switch ( info.ButtonID )
			{
				case 1:
				{
					m_Beholder.SendGump( new VirtueInfoGump( m_Beholder, m_Virtue, m_Desc, m_Page ) );

					if( m_Page != null )
						state.Send( new LaunchBrowser( m_Page ) ); //No message about web browser starting on OSI
					break;
				}
				case 2:
				{
					m_Beholder.SendGump( new VirtueStatusGump( m_Beholder ) );
					break;
				}
			}
		}
	}
}