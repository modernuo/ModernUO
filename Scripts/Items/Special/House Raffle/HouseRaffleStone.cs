using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Server;
using Server.Accounting;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Regions;

namespace Server.Items
{
	public class RaffleEntry
	{
		private Mobile m_From;
		private IPAddress m_Address;
		private DateTime m_Date;

		public Mobile From
		{
			get { return m_From; }
		}

		public IPAddress Address
		{
			get { return m_Address; }
		}

		public DateTime Date
		{
			get { return m_Date; }
		}

		public RaffleEntry( Mobile from )
		{
			m_From = from;

			if ( m_From.NetState != null )
				m_Address = m_From.NetState.Address;
			else
				m_Address = IPAddress.None;

			m_Date = DateTime.Now;
		}

		public void Serialize( GenericWriter writer )
		{
			writer.Write( m_From );
			writer.Write( m_Address );
			writer.Write( m_Date );
		}

		public RaffleEntry( GenericReader reader, int version )
		{
			switch ( version )
			{
				case 1:
				case 0:
				{
					m_From = reader.ReadMobile();
					m_Address = Utility.Intern( reader.ReadIPAddress() );
					m_Date = reader.ReadDateTime();

					break;
				}
			}
		}
	}

	public enum HouseRaffleState
	{
		Inactive,
		Active,
		Completed
	}

	[FlipableAttribute( 0xEDD, 0xEDE )]
	public class HouseRaffleStone : Item
	{
		private const int EntryLimitPerIP = 4;
		private const int DefaultTicketPrice = 5000;
		private const int MessageHue = 1153;

		private static readonly TimeSpan ExpirationTime = TimeSpan.FromDays( 30.0 );

		private HouseRaffleRegion m_Region;
		private Rectangle2D m_Bounds;
		private Map m_Facet;

		private Mobile m_Winner;
		private HouseRaffleDeed m_Deed;

		private bool m_Active;
		private DateTime m_Started;
		private TimeSpan m_Duration;
		private int m_TicketPrice;

		private List<RaffleEntry> m_Entries;

		[CommandProperty( AccessLevel.GameMaster )]
		public HouseRaffleState CurrentState
		{
			get
			{
				if ( !m_Active && m_Winner != null )
					return HouseRaffleState.Completed;
				else if ( m_Active )
					return HouseRaffleState.Active;
				else
					return HouseRaffleState.Inactive;
			}
		}

		public bool Active
		{
			get { return m_Active; }
			set
			{
				if ( m_Active != value )
				{
					if ( value )
					{
						m_Entries.Clear();
						m_Winner = null;
						m_Deed = null;
						m_Started = DateTime.Now;
					}

					m_Active = value;
					InvalidateProperties();
				}
			}
		}

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Seer )]
		public Rectangle2D PlotBounds
		{
			get { return m_Bounds; }
			set
			{
				m_Bounds = value;

				InvalidateRegion();
				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Seer )]
		public Map PlotFacet
		{
			get { return m_Facet; }
			set
			{
				m_Facet = value;

				InvalidateRegion();
				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Seer )]
		public Mobile Winner
		{
			get { return m_Winner; }
			set { m_Winner = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Seer )]
		public HouseRaffleDeed Deed
		{
			get { return m_Deed; }
			set { m_Deed = value; }
		}

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Seer )]
		public DateTime Started
		{
			get { return m_Started; }
			set { m_Started = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Seer )]
		public TimeSpan Duration
		{
			get { return m_Duration; }
			set { m_Duration = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Seer )]
		public bool IsExpired
		{
			get
			{
				if ( CurrentState != HouseRaffleState.Completed )
					return false;

				return ( m_Started + m_Duration + ExpirationTime <= DateTime.Now );
			}
		}

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Seer )]
		public int TicketPrice
		{
			get { return m_TicketPrice; }
			set
			{
				m_TicketPrice = Math.Max( 0, value );
				InvalidateProperties();
			}
		}

		public List<RaffleEntry> Entries
		{
			get { return m_Entries; }
		}

		public override string DefaultName
		{
			get { return "a house raffle stone"; }
		}

		public override bool DisplayWeight
		{
			get { return false; }
		}

		private static List<HouseRaffleStone> m_AllStones = new List<HouseRaffleStone>();

		public static void CheckEnd_OnTick()
		{
			for ( int i = 0; i < m_AllStones.Count; i++ )
				m_AllStones[i].CheckEnd();
		}

		public static void Initialize()
		{
			for ( int i = m_AllStones.Count - 1; i >= 0; i-- )
			{
				HouseRaffleStone stone = m_AllStones[i];

				if ( stone.IsExpired )
					stone.Delete();
			}

			Timer.DelayCall( TimeSpan.FromMinutes( 1.0 ), TimeSpan.FromMinutes( 1.0 ), new TimerCallback( CheckEnd_OnTick ) );
		}

		[Constructable]
		public HouseRaffleStone()
			: base( 0xEDD )
		{
			m_Region = null;
			m_Bounds = new Rectangle2D();
			m_Facet = null;

			m_Winner = null;
			m_Deed = null;

			m_Active = false;
			m_Started = DateTime.MinValue;
			m_Duration = TimeSpan.Zero;
			m_TicketPrice = DefaultTicketPrice;

			m_Entries = new List<RaffleEntry>();

			Movable = false;

			m_AllStones.Add( this );
		}

		public HouseRaffleStone( Serial serial )
			: base( serial )
		{
		}

		public bool ValidLocation()
		{
			return ( m_Bounds.Start != Point2D.Zero && m_Bounds.End != Point2D.Zero && m_Facet != null && m_Facet != Map.Internal );
		}

		private void InvalidateRegion()
		{
			if ( m_Region != null )
			{
				m_Region.Unregister();
				m_Region = null;
			}

			if ( ValidLocation() )
			{
				m_Region = new HouseRaffleRegion( this );
				m_Region.Register();
			}
		}

		private bool HasEntered( Mobile from )
		{
			Account acc = from.Account as Account;

			if ( acc == null )
				return false;

			foreach ( RaffleEntry entry in m_Entries )
			{
				if ( entry.From != null )
				{
					Account entryAcc = entry.From.Account as Account;

					if ( entryAcc == acc )
						return true;
				}
			}

			return false;
		}

		private bool IsAtIPLimit( Mobile from )
		{
			if ( from.NetState == null )
				return false;

			IPAddress address = from.NetState.Address;
			int tickets = 0;

			foreach ( RaffleEntry entry in m_Entries )
			{
				if ( Utility.IPMatchClassC( entry.Address, address ) )
				{
					if ( ++tickets >= EntryLimitPerIP )
						return true;
				}
			}

			return false;
		}

		public static string FormatLocation( Point3D loc, Map map, bool displayMap )
		{
			StringBuilder result = new StringBuilder();

			int xLong = 0, yLat = 0;
			int xMins = 0, yMins = 0;
			bool xEast = false, ySouth = false;

			if ( Sextant.Format( loc, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth ) )
				result.AppendFormat( "{0}°{1}'{2},{3}°{4}'{5}", yLat, yMins, ySouth ? "S" : "N", xLong, xMins, xEast ? "E" : "W" );
			else
				result.AppendFormat( "{0},{1}", loc.X, loc.Y );

			if ( displayMap )
				result.AppendFormat( " ({0})", map );

			return result.ToString();
		}

		private Point3D GetPlotCenter()
		{
			int x = m_Bounds.X + m_Bounds.Width / 2;
			int y = m_Bounds.Y + m_Bounds.Height / 2;
			int z = ( m_Facet == null ) ? 0 : m_Facet.GetAverageZ( x, y );

			return new Point3D( x, y, z );
		}

		public string FormatLocation()
		{
			if ( !ValidLocation() )
				return "no location set";

			return FormatLocation( GetPlotCenter(), m_Facet, true );
		}

		public string FormatPrice()
		{
			if ( m_TicketPrice == 0 )
				return "FREE";
			else
				return String.Format( "{0} gold", m_TicketPrice );
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( ValidLocation() )
				list.Add( FormatLocation() );

			if ( m_Active )
			{
				list.Add( 1060658, "ticket price\t{0}", FormatPrice() ); // ~1_val~: ~2_val~
				list.Add( 1060659, "ends\t{0}", m_Started + m_Duration ); // ~1_val~: ~2_val~
			}
			else if ( m_Winner != null )
			{
				list.Add( 1060658, "won by\t{0}", m_Winner.Name ); // ~1_val~: ~2_val~
			}
		}

		public override void GetContextMenuEntries( Mobile from, List<ContextMenuEntry> list )
		{
			base.GetContextMenuEntries( from, list );

			if ( from.AccessLevel >= AccessLevel.Seer )
			{
				list.Add( new EditEntry( from, this ) );

				if ( !m_Active && m_Winner == null )
					list.Add( new ActivateEntry( from, this ) );
				else
					list.Add( new ManagementEntry( from, this ) );
			}
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !m_Active || !from.CheckAlive() )
				return;

			if ( !from.InRange( GetWorldLocation(), 2 ) )
			{
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
				return;
			}

			if ( HasEntered( from ) )
			{
				from.SendMessage( MessageHue, "You have already entered this plot's raffle." );
			}
			else if ( IsAtIPLimit( from ) )
			{
				from.SendMessage( MessageHue, "You may not enter this plot's raffle." );
			}
			else
			{
				from.SendGump( new WarningGump( 1150470, 0x7F00, String.Format( "You are about to purchase a raffle ticket for the house plot located at {0}.  The ticket price is {1}.  Tickets are non-refundable and you can only purchase one ticket per account.  Do you wish to continue?", FormatLocation(), FormatPrice() ), 0xFFFFFF, 420, 280, new WarningGumpCallback( Purchase_Callback ), null ) ); // CONFIRM TICKET PURCHASE
			}
		}

		public void Purchase_Callback( Mobile from, bool okay, object state )
		{
			if ( Deleted || !from.CheckAlive() || HasEntered( from ) || IsAtIPLimit( from ) )
				return;

			Account acc = from.Account as Account;

			if ( acc == null )
				return;

			if ( okay )
			{
				Container bank = from.FindBankNoCreate();

				if ( m_TicketPrice == 0 || ( from.Backpack != null && from.Backpack.ConsumeTotal( typeof( Gold ), m_TicketPrice ) ) || ( bank != null && bank.ConsumeTotal( typeof( Gold ), m_TicketPrice ) ) )
				{
					m_Entries.Add( new RaffleEntry( from ) );

					from.SendMessage( MessageHue, "You have successfully entered the plot's raffle." );
				}
				else
				{
					from.SendMessage( MessageHue, "You do not have the {0} required to enter the raffle.", FormatPrice() );
				}
			}
			else
			{
				from.SendMessage( MessageHue, "You have chosen not to enter the raffle." );
			}
		}

		public void CheckEnd()
		{
			if ( !m_Active || m_Started + m_Duration > DateTime.Now )
				return;

			m_Active = false;

			if ( m_Region != null && m_Entries.Count != 0 )
			{
				int winner = Utility.Random( m_Entries.Count );

				m_Winner = m_Entries[winner].From;

				if ( m_Winner != null )
				{
					m_Deed = new HouseRaffleDeed( GetPlotCenter(), m_Facet, m_Winner );

					m_Winner.SendMessage( MessageHue, "Congratulations, {0}!  You have won the raffle for the plot located at {1}.", m_Entries[winner].From.Name, FormatLocation() );

					if ( m_Winner.AddToBackpack( m_Deed ) )
					{
						m_Winner.SendMessage( MessageHue, "The writ of lease has been placed in your backpack." );
					}
					else
					{
						BankBox box = m_Winner.BankBox;

						if ( box.TryDropItem( m_Winner, m_Deed, false ) )
						{
							m_Winner.SendMessage( MessageHue, "As your backpack is full, the writ of lease has been placed in your bank box." );
						}
						else
						{
							// Item is already at the mobile's feet

							m_Winner.SendMessage( MessageHue, "As both your backpack and bank box are full, the writ of lease has been placed at your feet." );
						}
					}
				}
			}

			InvalidateProperties();
		}

		public override void OnDelete()
		{
			if ( m_Region != null )
			{
				m_Region.Unregister();
				m_Region = null;
			}

			m_AllStones.Remove( this );

			base.OnDelete();
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version

			writer.Write( m_Deed );

			writer.Write( m_Active );

			writer.Write( m_Bounds );
			writer.Write( m_Facet );

			writer.Write( m_Winner );

			writer.Write( m_TicketPrice );
			writer.Write( m_Started );
			writer.Write( m_Duration );

			writer.Write( m_Entries.Count );

			foreach ( RaffleEntry entry in m_Entries )
				entry.Serialize( writer );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 1:
				{
					m_Deed = reader.ReadItem<HouseRaffleDeed>();

					goto case 0;
				}
				case 0:
				{
					m_Active = reader.ReadBool();

					m_Bounds = reader.ReadRect2D();
					m_Facet = reader.ReadMap();

					m_Winner = reader.ReadMobile();

					m_TicketPrice = reader.ReadInt();
					m_Started = reader.ReadDateTime();
					m_Duration = reader.ReadTimeSpan();

					int entryCount = reader.ReadInt();
					m_Entries = new List<RaffleEntry>( entryCount );

					for ( int i = 0; i < entryCount; i++ )
					{
						RaffleEntry entry = new RaffleEntry( reader, version );

						if ( entry.From == null )
							continue; // Character was deleted

						m_Entries.Add( entry );
					}

					InvalidateRegion();

					m_AllStones.Add( this );

					break;
				}
			}
		}

		private class RaffleContextMenuEntry : ContextMenuEntry
		{
			protected Mobile m_From;
			protected HouseRaffleStone m_Stone;

			public RaffleContextMenuEntry( Mobile from, HouseRaffleStone stone, int label )
				: base( label )
			{
				m_From = from;
				m_Stone = stone;
			}
		}

		private class EditEntry : RaffleContextMenuEntry
		{
			public EditEntry( Mobile from, HouseRaffleStone stone )
				: base( from, stone, 5101 ) // Edit
			{
			}

			public override void OnClick()
			{
				if ( m_Stone.Deleted || m_From.AccessLevel < AccessLevel.Seer )
					return;

				m_From.SendGump( new PropertiesGump( m_From, m_Stone ) );
			}
		}

		private class ActivateEntry : RaffleContextMenuEntry
		{
			public ActivateEntry( Mobile from, HouseRaffleStone stone )
				: base( from, stone, 5113 ) // Start
			{
				if ( !stone.ValidLocation() )
					Flags |= Network.CMEFlags.Disabled;
			}

			public override void OnClick()
			{
				if ( m_Stone.Deleted || m_From.AccessLevel < AccessLevel.Seer || m_Stone.Active || !m_Stone.ValidLocation() )
					return;

				m_Stone.Active = true;
			}
		}

		private class ManagementEntry : RaffleContextMenuEntry
		{
			public ManagementEntry( Mobile from, HouseRaffleStone stone)
				: base( from, stone, 5032 ) // Game Monitor
			{
			}

			public override void OnClick()
			{
				if ( m_Stone.Deleted || m_From.AccessLevel < AccessLevel.Seer )
					return;

				m_From.SendGump( new HouseRaffleManagementGump( m_Stone ) );
			}
		}
	}
}
