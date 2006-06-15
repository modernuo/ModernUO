using System;
using System.Collections;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server
{
	public delegate void OnVirtueUsed( Mobile from );

	public class VirtueGump : Gump
	{
		private static Hashtable m_Callbacks = new Hashtable();

		public static void Initialize()
		{
			EventSink.VirtueGumpRequest += new VirtueGumpRequestEventHandler( EventSink_VirtueGumpRequest );
			EventSink.VirtueItemRequest += new VirtueItemRequestEventHandler( EventSink_VirtueItemRequest );
			EventSink.VirtueMacroRequest += new VirtueMacroRequestEventHandler( EventSink_VirtueMacroRequest );
		}

		public static void Register( int gumpID, OnVirtueUsed callback )
		{
			m_Callbacks[gumpID] = callback;
		}

		private static void EventSink_VirtueItemRequest( VirtueItemRequestEventArgs e )
		{
			if ( e.Beholder != e.Beheld )
				return;

			e.Beholder.CloseGump( typeof( VirtueGump ) );

			if ( e.Beholder.Kills >= 5 )
			{
				e.Beholder.SendLocalizedMessage( 1049609 ); // Murderers cannot invoke this virtue.
				return;
			}

			OnVirtueUsed callback = (OnVirtueUsed)m_Callbacks[e.GumpID];

			if ( callback != null )
				callback( e.Beholder );
			else
				e.Beholder.SendLocalizedMessage( 1052066 ); // That virtue is not active yet.
		}


		private static void EventSink_VirtueMacroRequest( VirtueMacroRequestEventArgs e )
		{
			int virtueID = 0;

			switch ( e.VirtueID )
			{
				case 0:	// Honor
					virtueID = 107;	break;
				case 1:	// Sacrifice
					virtueID = 110; break;
				case 2:	// Valor;
					virtueID = 112;	break;
			}

			EventSink_VirtueItemRequest( new VirtueItemRequestEventArgs( e.Mobile, e.Mobile, virtueID ) );
		}

		private static void EventSink_VirtueGumpRequest( VirtueGumpRequestEventArgs e )
		{
			Mobile beholder = e.Beholder;
			Mobile beheld = e.Beheld;

			if ( beholder == beheld && beholder.Kills >= 5 )
			{
				beholder.SendLocalizedMessage( 1049609 ); // Murderers cannot invoke this virtue.
			}
			else if ( beholder.Map == beheld.Map && beholder.InRange( beheld, 12 ) )
			{
				beholder.CloseGump( typeof( VirtueGump ) );
				beholder.SendGump( new VirtueGump( beholder, beheld ) );
			}
		}

		private Mobile m_Beholder, m_Beheld;

		public VirtueGump( Mobile beholder, Mobile beheld ) : base( 0, 0 )
		{
			m_Beholder = beholder;
			m_Beheld = beheld;

			Serial = beheld.Serial;

			AddPage( 0 );

			AddImage( 30, 40, 104 );

			AddPage( 1 );

			Add( new InternalEntry( 61, 71, 108, GetHueFor( 0 ) ) ); // Humility
			Add( new InternalEntry( 123, 46, 112, GetHueFor( 4 ) ) ); // Valor
			Add( new InternalEntry( 187, 70, 107, GetHueFor( 5 ) ) ); // Honor
			Add( new InternalEntry( 35, 135, 110, GetHueFor( 1 ) ) ); // Sacrifice
			Add( new InternalEntry( 211, 133, 105, GetHueFor( 2 ) ) ); // Compassion
			Add( new InternalEntry( 61, 195, 111, GetHueFor( 3 ) ) ); // Spiritulaity
			Add( new InternalEntry( 186, 195, 109, GetHueFor( 6 ) ) ); // Justice
			Add( new InternalEntry( 121, 221, 106, GetHueFor( 7 ) ) ); // Honesty

			if ( m_Beholder == m_Beheld )
			{
				AddButton( 57, 269, 2027, 2027, 1, GumpButtonType.Reply, 0 );
				AddButton( 186, 269, 2071, 2071, 2, GumpButtonType.Reply, 0 );
			}
		}

		private static int[] m_Table = new int[24]
			{
				0x0481, 0x0963, 0x0965,
				0x060A, 0x060F, 0x002A,
				0x08A4, 0x08A7, 0x0034,
				0x0965, 0x08FD, 0x0480,
				0x00EA, 0x0845, 0x0020,
				0x0011, 0x0269, 0x013D,
				0x08A1, 0x08A3, 0x0042,
				0x0543, 0x0547, 0x0061
			};

		private int GetHueFor( int index )
		{
			if ( m_Beheld.Virtues.GetValue( index ) == 0 )
				return 2402;

			int value = m_Beheld.Virtues.GetValue( index );

			if ( value < 4000 )
				return 2402;

			if( value >= 30000 )
				value = 20000;	//Sanity
				
			/*
			int vl;
			
			vl = (value/10000)

			if( value < 10000 )
				vl = 0;
			else if( value > 20000 )
				vl = 2;
			else
				vl = 1;
			*/

			return m_Table[(index * 3) + (int)(value/10000)];
		}

		private class InternalEntry : GumpImage
		{
			public InternalEntry( int x, int y, int gumpID, int hue ) : base( x, y, gumpID, hue )
			{
			}

			public override string Compile()
			{
				return String.Format( "{{ gumppic {0} {1} {2} hue={3} class=VirtueGumpItem }}", X, Y, GumpID, Hue );
			}

			private static byte[] m_Class = Gump.StringToBuffer( " class=VirtueGumpItem" );

			public override void AppendTo( IGumpWriter disp )
			{
				base.AppendTo( disp );

				disp.AppendLayout( m_Class );
			}
		}

		public override void OnResponse( NetState state, RelayInfo info )
		{
			if ( info.ButtonID == 1 && m_Beholder == m_Beheld )
				m_Beholder.SendGump( new VirtueStatusGump( m_Beholder ) );
		}
	}
}