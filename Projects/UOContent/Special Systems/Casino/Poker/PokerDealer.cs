using System;
using System.Collections.Generic;

using Server;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Poker
{
	public class PokerDealer : Mobile
	{
		public static void Initialize()
		{
			CommandSystem.Register( "AddPokerSeat", AccessLevel.Administrator, new CommandEventHandler( AddPokerSeat_OnCommand ) );
			CommandSystem.Register( "PokerKick", AccessLevel.Seer, new CommandEventHandler( PokerKick_OnCommand ) );

            //EventSink.Disconnected += new DisconnectedEventHandler( EventSink_Disconnected );
            EventSink.Disconnected += EventSink_Disconnected;

        }

        

        private double m_Rake;
		private int m_RakeMax;
		private int m_MinBuyIn;
		private int m_MaxBuyIn;
		private int m_SmallBlind;
		private int m_BigBlind;
		private int m_MaxPlayers;
		private bool m_Active;
		private bool m_TournamentMode;
		private PokerGame m_Game;
		private List<Point3D> m_Seats;
		private Point3D m_ExitLocation;
		private Map m_ExitMap;

		private static int m_Jackpot;
		public static int Jackpot { get { return m_Jackpot; } set { m_Jackpot = value; } }

		[CommandProperty( AccessLevel.Seer )]
		public bool TournamentMode { get { return m_TournamentMode; } set { m_TournamentMode = value; } }
		[CommandProperty( AccessLevel.Administrator )]
		public bool ClearSeats { get { return false; } set { m_Seats.Clear(); } }
		[CommandProperty( AccessLevel.Administrator )]
		public int RakeMax { get { return m_RakeMax; } set { m_RakeMax = value; } }
		[CommandProperty( AccessLevel.Seer )]
		public int MinBuyIn { get { return m_MinBuyIn; } set { m_MinBuyIn = value; } }
		[CommandProperty( AccessLevel.Seer )]
		public int MaxBuyIn { get { return m_MaxBuyIn; } set { m_MaxBuyIn = value; } }
		[CommandProperty( AccessLevel.Seer )]
		public int SmallBlind { get { return m_SmallBlind; } set { m_SmallBlind = value; } }
		[CommandProperty( AccessLevel.Seer )]
		public int BigBlind { get { return m_BigBlind; } set { m_BigBlind = value; } }
		[CommandProperty( AccessLevel.Administrator )]
		public Point3D ExitLocation { get { return m_ExitLocation; } set { m_ExitLocation = value; } }
		[CommandProperty( AccessLevel.Administrator )]
		public Map ExitMap { get { return m_ExitMap; } set { m_ExitMap = value; } }
		[CommandProperty( AccessLevel.Administrator )]
		public double Rake
		{
			get { return m_Rake; }
			set
			{
				if ( value > 1 )
					m_Rake = 1;
				else if ( value < 0 )
					m_Rake = 0;
				else
					m_Rake = value;
			}
		}
		[CommandProperty( AccessLevel.Seer )]
		public int MaxPlayers
		{
			get { return m_MaxPlayers; }
			set
			{
				if ( value > 22 ) m_MaxPlayers = 22;
				else if ( value < 0 ) m_MaxPlayers = 0; 
				else m_MaxPlayers = value;
			}
		}
		[CommandProperty( AccessLevel.Seer )]
		public bool Active
		{
			get { return m_Active; }
			set
			{
				List<PokerPlayer> toRemove = new List<PokerPlayer>();

				if ( !value )
					foreach ( PokerPlayer player in m_Game.Players.Players )
						if ( player.Mobile != null )
							toRemove.Add( player );

				for ( int i = 0; i < toRemove.Count; ++i )
				{
					toRemove[i].Mobile.SendMessage( 0x22, "The poker dealer has been set to inactive by a game master, and you are now being removed from the poker game and being refunded the money that you currently have." );
					m_Game.RemovePlayer( toRemove[i] );
				}

				m_Active = value;
			}
		}

		public PokerGame Game { get { return m_Game; } set { m_Game = value; } }
		public List<Point3D> Seats { get { return m_Seats; } set { m_Seats = value; } }

		[Constructible]
		public PokerDealer()
			: this( 10 )
		{
		}

		[Constructible]
		public PokerDealer( int maxPlayers )
		{
			Blessed = true;
			Frozen = true;
			InitStats( 100, 100, 100 );

			Title = "the poker dealer";
			NameHue = 0x35;

			if ( this.Female = Utility.RandomBool() )
			{
				this.Body = 0x191;
				this.Name = NameList.RandomName( "female" );
			}
			else
			{
				this.Body = 0x190;
				this.Name = NameList.RandomName( "male" );
			}

			Dress();

			MaxPlayers = maxPlayers;
			m_Seats = new List<Point3D>();
			m_Rake = 0.10;		//10% rake default
			m_RakeMax = 5000;	//5k maximum rake default
			m_Game = new PokerGame( this );
		}

		private void Dress()
		{
			AddItem( new FancyShirt( 0 ) );

			Item pants = new LongPants();
			pants.Hue = 1;
			AddItem( pants );

			Item shoes = new Shoes();
			shoes.Hue = 1;
			AddItem( shoes );

			Item sash = new BodySash();
			sash.Hue = 1;
			AddItem( sash );

			Utility.AssignRandomHair( this );
		}

		private static JackpotInfo m_JackpotWinners;
		public static JackpotInfo JackpotWinners { get { return m_JackpotWinners; } set { m_JackpotWinners = value; } }

		public static void AwardJackpot()
		{
			if ( m_JackpotWinners != null && m_JackpotWinners.Winners != null && m_JackpotWinners.Winners.Count > 0 )
			{
				int award = m_Jackpot / m_JackpotWinners.Winners.Count;

				if ( award <= 0 )
					return;

				foreach ( PokerPlayer m in m_JackpotWinners.Winners )
				{
					if ( m != null && m.Mobile != null && m.Mobile.BankBox != null )
					{
						m.Mobile.BankBox.DropItem( new BankCheck( award ) );
						World.Broadcast( 1161, true, "{0} has won the poker jackpot of {1} gold with {2}", m.Mobile.Name, award.ToString( "#,###" ), HandRanker.RankString( m_JackpotWinners.Hand ) );
					}
				}

				m_Jackpot = 0;
				m_JackpotWinners = null;
			}
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !m_Active )
				from.SendMessage( 0x9A, "This table is inactive" );
			else if ( !InRange( from.Location, 8 ) )
				from.PrivateOverheadMessage( Server.Network.MessageType.Regular, 0x22, true, "I am too far away to do that", from.NetState );
			else if ( m_MinBuyIn == 0 || m_MaxBuyIn == 0 )
				from.SendMessage( 0x9A, "This table is inactive" );
			else if ( m_MinBuyIn > m_MaxBuyIn )
				from.SendMessage( 0x9A, "This table is inactive" );
			else if ( m_Seats.Count < m_MaxPlayers )
				from.SendMessage( 0x9A, "This table is inactive" );
			else if ( m_Game.GetIndexFor( from ) != -1 )
				return; //TODO: Grab more chips from the player's bankbox
			else if ( m_Game.Players.Count >= m_MaxPlayers )
			{
				from.SendMessage( 0x22, "This table is full" );
				base.OnDoubleClick( from );
			}
			else if ( m_Game.Players.Count < m_MaxPlayers )
			{
				//TODO: Send player the poker join gump
				from.CloseGump<PokerJoinGump>();
				from.SendGump( new PokerJoinGump( from, m_Game ) );
			}
		}

		public override void OnDelete()
		{
			List<PokerPlayer> toRemove = new List<PokerPlayer>();

			foreach ( PokerPlayer player in m_Game.Players.Players )
				if ( player.Mobile != null )
					toRemove.Add( player );

			for ( int i = 0; i < toRemove.Count; ++i )
			{
				toRemove[i].Mobile.SendMessage( 0x22, "The poker dealer has been deleted, and you are now being removed from the poker game and being refunded the money that you currently have." );
				m_Game.RemovePlayer( toRemove[i] );
			}

			base.OnDelete();
		}

		public static void PokerKick_OnCommand( CommandEventArgs e )
		{
			Mobile from = e.Mobile;

			if ( from == null )
				return;

			foreach ( Mobile m in from.GetMobilesInRange( 0 ) )
			{
				if ( m is PlayerMobile )
				{
					PlayerMobile pm = (PlayerMobile)m;

					PokerGame game = pm.PokerGame;

					if ( game != null )
					{
						PokerPlayer player = game.GetPlayer( m );

						if ( player != null )
						{
							game.RemovePlayer( player );
							from.SendMessage( "They have been removed from the poker table" );
							return;
						}
					}
				}
			}

			from.SendMessage( "No one found to kick from a poker table. Make sure you are standing on top of them." );
		}
        private static void EventSink_Disconnected(Mobile obj)
        {
            Mobile from = obj;

            if (from == null)
                return;

            if (from is PlayerMobile)
            {
                PlayerMobile pm = (PlayerMobile)from;

                PokerGame game = pm.PokerGame;

                if (game != null)
                {
                    PokerPlayer player = game.GetPlayer(from);

                    if (player != null)
                        game.RemovePlayer(player);
                }
            }
        }
        
		public static void AddPokerSeat_OnCommand( CommandEventArgs e )
		{
			Mobile from = e.Mobile;

			if ( from == null )
				return;

			string args = e.ArgString.ToLower();
			string[] argLines = args.Split( ' ' );
			int x = 0, y = 0, z = 0;

			try
			{
				x = Convert.ToInt32( argLines[0] );
				y = Convert.ToInt32( argLines[1] );
				z = Convert.ToInt32( argLines[2] );
			}
			catch { from.SendMessage( 0x22, "Usage: [AddPokerSeat <x> <y> <z>" ); return;  }

			bool success = false;
			foreach ( Mobile m in from.GetMobilesInRange( 0 ) )
			{
				if ( m is PokerDealer )
				{
					Point3D seat = new Point3D( x, y, z );

					if ( ((PokerDealer)m).AddPokerSeat( from, seat ) != -1 )
					{
						from.SendMessage( 0x22, "A new seat was successfully created." );
						success = true;
						break;
					}
					else
					{
						from.SendMessage( 0x22, "There is no more room at that table for another seat. Try increasing the value of MaxPlayers first." );
						success = true;
						break;
					}
				}
			}

			if ( !success )
				from.SendMessage( 0x22, "No poker dealers were found in range. (Try standing on top of the dealer)" );
		}

		public int AddPokerSeat( Mobile from, Point3D seat )
		{
			if ( m_Seats.Count >= m_MaxPlayers )
				return -1;

			m_Seats.Add( seat );
			return 0;
		}

		public bool SeatTaken( Point3D seat )
		{
			for ( int i = 0; i < m_Game.Players.Count; ++i )
				if ( m_Game.Players[i].Seat == seat )
					return true;

			return false;
		}

		public int RakeGold( int gold )
		{
			double amount = gold * m_Rake;
			return (int)( amount > m_RakeMax ? m_RakeMax : amount );
		}

		public PokerDealer( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( IGenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); //version

			writer.Write( m_Active );
			writer.Write( m_SmallBlind );
			writer.Write( m_BigBlind );
			writer.Write( m_MinBuyIn );
			writer.Write( m_MaxBuyIn );
			writer.Write( m_ExitLocation );
			writer.Write( m_ExitMap );
			writer.Write( m_Rake );
			writer.Write( m_RakeMax );
			writer.Write( m_MaxPlayers );

			writer.Write( m_Seats.Count );

			for ( int i = 0; i < m_Seats.Count; ++i )
				writer.Write( m_Seats[i] );
		}

		public override void Deserialize( IGenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
					m_Active = reader.ReadBool();
					m_SmallBlind = reader.ReadInt();
					m_BigBlind = reader.ReadInt();
					m_MinBuyIn = reader.ReadInt();
					m_MaxBuyIn = reader.ReadInt();
					m_ExitLocation = reader.ReadPoint3D();
					m_ExitMap = reader.ReadMap();
					m_Rake = reader.ReadDouble();
					m_RakeMax = reader.ReadInt();
					m_MaxPlayers = reader.ReadInt();

					int count = reader.ReadInt();
					m_Seats = new List<Point3D>();

					for ( int i = 0; i < count; ++i )
						m_Seats.Add( reader.ReadPoint3D() );

					break;
			}

			m_Game = new PokerGame( this );
		}

		public class JackpotInfo
		{
			private List<PokerPlayer> m_Winners;
			private ResultEntry m_Hand;
			private DateTime m_Date;
			
			public List<PokerPlayer> Winners { get { return m_Winners; } }
			public ResultEntry Hand { get { return m_Hand; } }
			public DateTime Date { get { return m_Date; } }

			public JackpotInfo( List<PokerPlayer> winners, ResultEntry hand, DateTime date )
			{
				m_Winners = winners;
				m_Hand = hand;
				m_Date = date;
			}
		}
	}
}
