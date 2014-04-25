using System;
using System.Collections;
using System.Text;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using Server.Gumps;
using System.Collections.Generic;

namespace Server.Engines.ConPVP
{
	public sealed class DDBoard : Item
	{
		public DDTeamInfo m_TeamInfo;

		public override string DefaultName
		{
			get { return "scoreboard"; }
		}

		[Constructable]
		public DDBoard()
			: base( 7774 )
		{
			Movable = false;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( m_TeamInfo != null && m_TeamInfo.Game != null )
			{
				from.CloseGump( typeof( DDBoardGump ) );
				from.SendGump( new DDBoardGump( from, m_TeamInfo.Game ) );
			}
		}

		public DDBoard( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

	public class DDBoardGump : Gump
	{
		public string Center( string text )
		{
			return String.Format( "<CENTER>{0}</CENTER>", text );
		}

		public string Color( string text, int color )
		{
			return String.Format( "<BASEFONT COLOR=#{0:X6}>{1}</BASEFONT>", color, text );
		}

		private void AddBorderedText( int x, int y, int width, int height, string text, int color, int borderColor )
		{
			AddColoredText( x - 1, y - 1, width, height, text, borderColor );
			AddColoredText( x - 1, y + 1, width, height, text, borderColor );
			AddColoredText( x + 1, y - 1, width, height, text, borderColor );
			AddColoredText( x + 1, y + 1, width, height, text, borderColor );
			AddColoredText( x, y, width, height, text, color );
		}

		private void AddColoredText( int x, int y, int width, int height, string text, int color )
		{
			if ( color == 0 )
				AddHtml( x, y, width, height, text, false, false );
			else
				AddHtml( x, y, width, height, Color( text, color ), false, false );
		}

		private const int LabelColor32 = 0xFFFFFF;
		private const int BlackColor32 = 0x000000;

		private DDGame m_Game;

		public DDBoardGump( Mobile mob, DDGame game )
			: this( mob, game, null )
		{
		}

		public DDBoardGump( Mobile mob, DDGame game, DDTeamInfo section )
			: base( 60, 60 )
		{
			m_Game = game;

			DDTeamInfo ourTeam = game.GetTeamInfo( mob );

			List<IRankedCTF> entries = new List<IRankedCTF>();

			if ( section == null )
			{
				for ( int i = 0; i < game.Context.Participants.Count; ++i )
				{
					DDTeamInfo teamInfo = game.Controller.TeamInfo[i % game.Controller.TeamInfo.Length];

					if ( teamInfo != null )
						entries.Add( teamInfo );
				}
			}
			else
			{
				foreach ( DDPlayerInfo player in section.Players.Values )
				{
					if ( player.Score > 0 )
						entries.Add( player );
				}
			}

			entries.Sort( delegate( IRankedCTF a, IRankedCTF b )
			{
				return b.Score - a.Score;
			} );

			int height = 0;

			if ( section == null )
				height = 73 + ( entries.Count * 75 ) + 28;

			Closable = false;

			AddPage( 0 );

			AddBackground( 1, 1, 398, height, 3600 );

			AddImageTiled( 16, 15, 369, height - 29, 3604 );

			for ( int i = 0; i < entries.Count; i += 1 )
				AddImageTiled( 22, 58 + ( i * 75 ), 357, 70, 0x2430 );

			AddAlphaRegion( 16, 15, 369, height - 29 );

			AddImage( 215, -45, 0xEE40 );
			//AddImage( 330, 141, 0x8BA );

			AddBorderedText( 22, 22, 294, 20, Center( "DD Scoreboard" ), LabelColor32, BlackColor32 );

			AddImageTiled( 32, 50, 264, 1, 9107 );
			AddImageTiled( 42, 52, 264, 1, 9157 );

			if ( section == null )
			{
				for ( int i = 0; i < entries.Count; ++i )
				{
					DDTeamInfo teamInfo = entries[i] as DDTeamInfo;

					AddImage( 30, 70 + ( i * 75 ), 10152 );
					AddImage( 30, 85 + ( i * 75 ), 10151 );
					AddImage( 30, 100 + ( i * 75 ), 10151 );
					AddImage( 30, 106 + ( i * 75 ), 10154 );

					AddImage( 24, 60 + ( i * 75 ), teamInfo == ourTeam ? 9730 : 9727, teamInfo.Color - 1 );

					int nameColor = LabelColor32;
					int borderColor = BlackColor32;

					switch ( teamInfo.Color )
					{
						case 0x47E:
							nameColor = 0xFFFFFF;
							break;

						case 0x4F2:
							nameColor = 0x3399FF;
							break;

						case 0x4F7:
							nameColor = 0x33FF33;
							break;

						case 0x4FC:
							nameColor = 0xFF00FF;
							break;

						case 0x021:
							nameColor = 0xFF3333;
							break;

						case 0x01A:
							nameColor = 0xFF66FF;
							break;

						case 0x455:
							nameColor = 0x333333;
							borderColor = 0xFFFFFF;
							break;
					}

					AddBorderedText( 60, 65 + ( i * 75 ), 250, 20, String.Format( "{0}: {1}", LadderGump.Rank( 1 + i ), teamInfo.Name ), nameColor, borderColor );

					AddBorderedText( 50 + 10, 85 + ( i * 75 ), 100, 20, "Score:", 0xFFC000, BlackColor32 );
					AddBorderedText( 50 + 15, 105 + ( i * 75 ), 100, 20, teamInfo.Score.ToString( "N0" ), 0xFFC000, BlackColor32 );

					AddBorderedText( 110 + 10, 85 + ( i * 75 ), 100, 20, "Kills:", 0xFFC000, BlackColor32 );
					AddBorderedText( 110 + 15, 105 + ( i * 75 ), 100, 20, teamInfo.Kills.ToString( "N0" ), 0xFFC000, BlackColor32 );

					AddBorderedText( 160 + 10, 85 + ( i * 75 ), 100, 20, "Captures:", 0xFFC000, BlackColor32 );
					AddBorderedText( 160 + 15, 105 + ( i * 75 ), 100, 20, teamInfo.Captures.ToString( "N0" ), 0xFFC000, BlackColor32 );

					DDPlayerInfo pl = teamInfo.Leader;

					AddBorderedText( 235 + 10, 85 + ( i * 75 ), 250, 20, "Leader:", 0xFFC000, BlackColor32 );

					if ( pl != null )
						AddBorderedText( 235 + 15, 105 + ( i * 75 ), 250, 20, pl.Player.Name, 0xFFC000, BlackColor32 );
				}
			}
			else
			{
			}

			AddButton( 314, height - 42, 247, 248, 1, GumpButtonType.Reply, 0 );
		}
	}

	public sealed class DDPlayerInfo : IRankedCTF
	{
		private DDTeamInfo m_TeamInfo;

		private Mobile m_Player;

		private int m_Kills;
		private int m_Captures;

		private int m_Score;

		public Mobile Player { get { return m_Player; } }

		public string Name
		{
			get { return m_Player.Name; }
		}

		public int Kills
		{
			get
			{
				return m_Kills;
			}
			set
			{
				m_TeamInfo.Kills += ( value - m_Kills );
				m_Kills = value;
			}
		}

		public int Captures
		{
			get
			{
				return m_Captures;
			}
			set
			{
				m_TeamInfo.Captures += ( value - m_Captures );
				m_Captures = value;
			}
		}

		public int Score
		{
			get
			{
				return m_Score;
			}
			set
			{
				m_TeamInfo.Score += ( value - m_Score );
				m_Score = value;

				if ( m_TeamInfo.Leader == null || m_Score > m_TeamInfo.Leader.Score )
					m_TeamInfo.Leader = this;
			}
		}

		public DDPlayerInfo( DDTeamInfo teamInfo, Mobile player )
		{
			m_TeamInfo = teamInfo;
			m_Player = player;
		}
	}

	[PropertyObject]
	public sealed class DDTeamInfo : IRankedCTF
	{
		private DDGame m_Game;
		private int m_TeamID;

		private int m_Color;
		private string m_Name;

		private DDBoard m_Board;

		private Point3D m_Origin;

		private int m_Kills;
		private int m_Captures;

		private int m_Score;

		private Dictionary<Mobile, DDPlayerInfo> m_Players;

		public string Name
		{
			get { return String.Format( "{0} Team", m_Name ); }
		}

		public DDGame Game { get { return m_Game; } set { m_Game = value; } }
		public int TeamID { get { return m_TeamID; } }

		public int Kills { get { return m_Kills; } set { m_Kills = value; } }
		public int Captures { get { return m_Captures; } set { m_Captures = value; } }

		public int Score { get { return m_Score; } set { m_Score = value; } }

		private DDPlayerInfo m_Leader;

		public DDPlayerInfo Leader
		{
			get { return m_Leader; }
			set { m_Leader = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public DDBoard Board
		{
			get { return m_Board; }
			set { m_Board = value; }
		}

		public Dictionary<Mobile, DDPlayerInfo> Players
		{
			get { return m_Players; }
		}

		public DDPlayerInfo this[Mobile mob]
		{
			get
			{
				if ( mob == null )
					return null;
	
				 DDPlayerInfo val;

				if ( !m_Players.TryGetValue( mob, out val ) )
					m_Players[mob] = val = new DDPlayerInfo( this, mob );

				return val;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int Color
		{
			get { return m_Color; }
			set { m_Color = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public string TeamName
		{
			get { return m_Name; }
			set { m_Name = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D Origin
		{
			get { return m_Origin; }
			set { m_Origin = value; }
		}

		public DDTeamInfo( int teamID )
		{
			m_TeamID = teamID;
			m_Players = new Dictionary<Mobile, DDPlayerInfo>();
		}

		public void Reset()
		{
			m_Kills = 0;
			m_Captures = 0;

			m_Score = 0;

			m_Leader = null;

			m_Players.Clear();

			if ( m_Board != null )
				m_Board.m_TeamInfo = this;
		}

		public DDTeamInfo( int teamID, GenericReader ip )
		{
			m_TeamID = teamID;
			m_Players = new Dictionary<Mobile, DDPlayerInfo>();

			int version = ip.ReadEncodedInt();

			switch ( version )
			{
				case 0:
				{
					m_Board = ip.ReadItem() as DDBoard;
					m_Name = ip.ReadString();
					m_Color = ip.ReadEncodedInt();
					m_Origin = ip.ReadPoint3D();
					break;
				}
			}
		}

		public void Serialize( GenericWriter op )
		{
			op.WriteEncodedInt( 0 ); // version

			op.Write( m_Board );
			op.Write( m_Name );
			op.WriteEncodedInt( m_Color );
			op.Write( m_Origin );
		}

		public override string ToString()
		{
			return "...";
		}
	}

	public sealed class DDController : EventController
	{
		private DDTeamInfo[] m_TeamInfo;

		private TimeSpan m_Duration;

		public DDTeamInfo[] TeamInfo { get { return m_TeamInfo; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public DDTeamInfo Team1 { get { return m_TeamInfo[0]; } set { } }

		[CommandProperty( AccessLevel.GameMaster )]
		public DDTeamInfo Team2 { get { return m_TeamInfo[1]; } set { } }

		[CommandProperty( AccessLevel.GameMaster )]
		public DDWayPoint PointA { get { return m_PointA; } set { m_PointA = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public DDWayPoint PointB { get { return m_PointB; } set { m_PointB = value; } }

		private DDWayPoint m_PointA, m_PointB;

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan Duration
		{
			get { return m_Duration; }
			set { m_Duration = value; }
		}

		public override string Title
		{
			get { return "DoubleDom"; }
		}

		public override string GetTeamName( int teamID )
		{
			return m_TeamInfo[teamID % m_TeamInfo.Length].Name;
		}

		public override EventGame Construct( DuelContext context )
		{
			return new DDGame( this, context );
		}

		public override string DefaultName { get { return "DD Controller"; } }

		[Constructable]
		public DDController()
		{
			Visible = false;
			Movable = false;

			m_Duration = TimeSpan.FromMinutes( 30.0 );

			m_TeamInfo = new DDTeamInfo[2];

			for ( int i = 0; i < m_TeamInfo.Length; ++i )
				m_TeamInfo[i] = new DDTeamInfo( i );
		}

		public DDController( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );

			writer.Write( m_Duration );

			writer.WriteEncodedInt( m_TeamInfo.Length );

			for ( int i = 0; i < m_TeamInfo.Length; ++i )
				m_TeamInfo[i].Serialize( writer );

			writer.Write( m_PointA );
			writer.Write( m_PointB );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_Duration = reader.ReadTimeSpan();
					m_TeamInfo = new DDTeamInfo[reader.ReadEncodedInt()];

					for ( int i = 0; i < m_TeamInfo.Length; ++i )
						m_TeamInfo[i] = new DDTeamInfo( i, reader );

					m_PointA = reader.ReadItem() as DDWayPoint;
					m_PointB = reader.ReadItem() as DDWayPoint;

					break;
				}
			}
		}
	}

	public sealed class DDGame : EventGame
	{
		private DDController m_Controller;

		public DDController Controller { get { return m_Controller; } }

		public void Alert( string text )
		{
			if ( m_Context.m_Tournament != null )
				m_Context.m_Tournament.Alert( text );

			for ( int i = 0; i < m_Context.Participants.Count; ++i )
			{
				Participant p = m_Context.Participants[i] as Participant;

				for ( int j = 0; j < p.Players.Length; ++j )
				{
					if ( p.Players[j] != null )
						p.Players[j].Mobile.SendMessage( 0x35, text );
				}
			}
		}

		public void Alert( string format, params object[] args )
		{
			Alert( String.Format( format, args ) );
		}

		public DDGame( DDController controller, DuelContext context ) : base( context )
		{
			m_Controller = controller;
		}

		public Map Facet
		{
			get
			{
				if ( m_Context.Arena != null )
					return m_Context.Arena.Facet;

				return m_Controller.Map;
			}
		}

		public DDTeamInfo GetTeamInfo( Mobile mob )
		{
			int teamID = GetTeamID( mob );

			if ( teamID >= 0 )
				return m_Controller.TeamInfo[teamID % m_Controller.TeamInfo.Length];

			return null;
		}

		public int GetTeamID( Mobile mob )
		{
			PlayerMobile pm = mob as PlayerMobile;

			if ( pm == null )
				return -1;

			if ( pm.DuelContext == null || pm.DuelContext != m_Context )
				return -1;

			if ( pm.DuelPlayer == null || pm.DuelPlayer.Eliminated )
				return -1;

			return pm.DuelContext.Participants.IndexOf( pm.DuelPlayer.Participant );
		}

		public int GetColor( Mobile mob )
		{
			DDTeamInfo teamInfo = GetTeamInfo( mob );

			if ( teamInfo != null )
				return teamInfo.Color;

			return -1;
		}

		private void ApplyHues( Participant p, int hueOverride )
		{
			for ( int i = 0; i < p.Players.Length; ++i )
			{
				if ( p.Players[i] != null )
					p.Players[i].Mobile.SolidHueOverride = hueOverride;
			}
		}

		public void DelayBounce( TimeSpan ts, Mobile mob, Container corpse )
		{
			Timer.DelayCall( ts, new TimerStateCallback( DelayBounce_Callback ), new object[] { mob, corpse } );
		}

		private void DelayBounce_Callback( object state )
		{
			object[] states = (object[]) state;
			Mobile mob = (Mobile) states[0];
			Container corpse = (Container) states[1];

			DuelPlayer dp = null;

			if ( mob is PlayerMobile )
				dp = ( mob as PlayerMobile ).DuelPlayer;

			m_Context.RemoveAggressions( mob );

			if ( dp != null && !dp.Eliminated )
				mob.MoveToWorld( m_Context.Arena.GetBaseStartPoint( GetTeamID( mob ) ), Facet );
			else
				m_Context.SendOutside( mob );

			m_Context.Refresh( mob, corpse );
			DuelContext.Debuff( mob );
			DuelContext.CancelSpell( mob );
			mob.Frozen = false;
		}

		public override bool OnDeath( Mobile mob, Container corpse )
		{
			Mobile killer = mob.FindMostRecentDamager( false );

			if ( killer != null && killer.Player )
			{
				DDTeamInfo teamInfo = GetTeamInfo( killer );
				DDTeamInfo victInfo = GetTeamInfo( mob );

				if ( teamInfo != null && teamInfo != victInfo )
				{
					DDPlayerInfo playerInfo = teamInfo[killer];

					if ( playerInfo != null )
					{
						playerInfo.Kills += 1;
						playerInfo.Score += 1; // base frag

						// extra points for killing someone on the waypoint
						if ( this.Controller.PointA != null )
						{
							if ( mob.InRange( this.Controller.PointA, 2 ) )
								playerInfo.Score += 1;
						}

						if ( this.Controller.PointB != null )
						{
							if ( mob.InRange( this.Controller.PointB, 2 ) )
								playerInfo.Score += 1;
						}
					}

					playerInfo = victInfo[mob];
					if ( playerInfo != null )
						playerInfo.Score -= 1;
				}
			}

			mob.CloseGump( typeof( DDBoardGump ) );
			mob.SendGump( new DDBoardGump( mob, this ) );

			m_Context.Requip( mob, corpse );
			DelayBounce( TimeSpan.FromSeconds( 30.0 ), mob, corpse );

			return false;
		}

		private Timer m_FinishTimer;

		public override void OnStart()
		{
			m_Capturable = true;

			if ( m_CaptureTimer != null )
			{
				m_CaptureTimer.Stop();
				m_CaptureTimer = null;
			}

			if ( m_UncaptureTimer != null )
			{
				m_UncaptureTimer.Stop();
				m_UncaptureTimer = null;
			}

			for ( int i = 0; i < m_Controller.TeamInfo.Length; ++i )
			{
				DDTeamInfo teamInfo = m_Controller.TeamInfo[i];

				teamInfo.Game = this;
				teamInfo.Reset();
			}

			if ( m_Controller.PointA != null )
				m_Controller.PointA.Game = this;

			if ( m_Controller.PointB != null )
				m_Controller.PointB.Game = this;

			for ( int i = 0; i < m_Context.Participants.Count; ++i )
				ApplyHues( m_Context.Participants[i] as Participant, m_Controller.TeamInfo[i % m_Controller.TeamInfo.Length].Color );

			if ( m_FinishTimer != null )
				m_FinishTimer.Stop();

			m_FinishTimer = Timer.DelayCall( m_Controller.Duration, new TimerCallback( Finish_Callback ) );
		}

		private void Finish_Callback()
		{
			List<DDTeamInfo> teams = new List<DDTeamInfo>();

			for ( int i = 0; i < m_Context.Participants.Count; ++i )
			{
				DDTeamInfo teamInfo = m_Controller.TeamInfo[i % m_Controller.TeamInfo.Length];

				if ( teamInfo != null )
					teams.Add( teamInfo );
			}

			teams.Sort( delegate( DDTeamInfo a, DDTeamInfo b )
			{
				return b.Score - a.Score;
			} );

			Tournament tourny = m_Context.m_Tournament;

			StringBuilder sb = new StringBuilder();

			if ( tourny != null && tourny.TournyType == TournyType.FreeForAll )
			{
				sb.Append( m_Context.Participants.Count * tourny.PlayersPerParticipant );
				sb.Append( "-man FFA" );
			}
			else if ( tourny != null && tourny.TournyType == TournyType.RandomTeam )
			{
				sb.Append( tourny.ParticipantsPerMatch );
				sb.Append( "-team" );
			}
			else if ( tourny != null && tourny.TournyType == TournyType.RedVsBlue )
			{
				sb.Append( "Red v Blue" );
			}
			else if ( tourny != null && tourny.TournyType == TournyType.Faction )
			{
				sb.Append( tourny.ParticipantsPerMatch );
				sb.Append( "-team Faction" );
			}
			else if ( tourny != null )
			{
				for ( int i = 0; i < tourny.ParticipantsPerMatch; ++i )
				{
					if ( sb.Length > 0 )
						sb.Append( 'v' );

					sb.Append( tourny.PlayersPerParticipant );
				}
			}

			if ( m_Controller != null )
				sb.Append( ' ' ).Append( m_Controller.Title );

			string title = sb.ToString();

			DDTeamInfo winner = (DDTeamInfo)( teams.Count > 0 ? teams[0] : null );

			for ( int i = 0; i < teams.Count; ++i )
			{
				TrophyRank rank = TrophyRank.Bronze;

				if ( i == 0 )
					rank = TrophyRank.Gold;
				else if ( i == 1 )
					rank = TrophyRank.Silver;

				DDPlayerInfo leader = ((DDTeamInfo)teams[i]).Leader;

				foreach ( DDPlayerInfo pl in ((DDTeamInfo)teams[i]).Players.Values )
				{
					Mobile mob = pl.Player;

					if ( mob == null )
						continue;

					//"Red v Blue DD Champion"

					sb = new StringBuilder();

					sb.Append( title );

					if ( pl == leader )
						sb.Append( " Leader" );

					if ( pl.Score > 0 )
					{
						sb.Append( ": " );

						sb.Append( pl.Score.ToString( "N0" ) );
						sb.Append( pl.Score == 1 ? " point" : " points" );

						if ( pl.Kills > 0 )
						{
							sb.Append( ", " );
							sb.Append( pl.Kills.ToString( "N0" ) );
							sb.Append( pl.Kills == 1 ? " kill" : " kills" );
						}

						if ( pl.Captures > 0 )
						{
							sb.Append( ", " );
							sb.Append( pl.Captures.ToString( "N0" ) );
							sb.Append( pl.Captures == 1 ? " capture" : " captures" );
						}
					}

					Item item = new Trophy( sb.ToString(), rank );

					if ( pl == leader )
						item.ItemID = 4810;

					item.Name = String.Format( "{0}, {1} team", item.Name, ((DDTeamInfo)teams[i]).Name.ToLower() );

					if ( !mob.PlaceInBackpack( item ) )
						mob.BankBox.DropItem( item );

					int cash = pl.Score * 250;

					if ( cash > 0 )
					{
						item = new BankCheck( cash );

						if ( !mob.PlaceInBackpack( item ) )
							mob.BankBox.DropItem( item );

						mob.SendMessage( "You have been awarded a {0} trophy and {1:N0}gp for your participation in this tournament.", rank.ToString().ToLower(), cash );
					}
					else
					{
						mob.SendMessage( "You have been awarded a {0} trophy for your participation in this tournament.", rank.ToString().ToLower() );
					}
				}
			}

			for ( int i = 0; i < m_Context.Participants.Count; ++i )
			{
				Participant p = m_Context.Participants[i] as Participant;

				for ( int j = 0; j < p.Players.Length; ++j )
				{
					DuelPlayer dp = p.Players[j];

					if ( dp != null && dp.Mobile != null )
					{
						dp.Mobile.CloseGump( typeof( DDBoardGump ) );
						dp.Mobile.SendGump( new DDBoardGump( dp.Mobile, this ) );
					}
				}

				if ( i == winner.TeamID )
					continue;

				for ( int j = 0; j < p.Players.Length; ++j )
				{
					if ( p.Players[j] != null )
						p.Players[j].Eliminated = true;
				}
			}

			m_Context.Finish( m_Context.Participants[winner.TeamID] as Participant );
		}

		public override void OnStop()
		{
			for ( int i = 0; i < m_Controller.TeamInfo.Length; ++i )
			{
				DDTeamInfo teamInfo = m_Controller.TeamInfo[i];

				if ( teamInfo.Board != null )
					teamInfo.Board.m_TeamInfo = null;

				teamInfo.Game = null;
			}

			if ( m_Controller.PointA != null )
				m_Controller.PointA.Game = null;

			if ( m_Controller.PointB != null )
				m_Controller.PointB.Game = null;

			m_Capturable = false;

			if ( m_CaptureTimer != null )
			{
				m_CaptureTimer.Stop();
				m_CaptureTimer = null;
			}

			if ( m_UncaptureTimer != null )
			{
				m_UncaptureTimer.Stop();
				m_UncaptureTimer = null;
			}

			for ( int i = 0; i < m_Context.Participants.Count; ++i )
				ApplyHues( m_Context.Participants[i] as Participant, -1 );

			if ( m_FinishTimer != null )
				m_FinishTimer.Stop();

			m_FinishTimer = null;
		}

		private bool m_Capturable = true;
		private Timer m_CaptureTimer = null;
		private Timer m_UncaptureTimer = null;
		private int m_CapStage = 0;

		public void Dominate( DDWayPoint point, Mobile from, DDTeamInfo team )
		{
			if ( point == null || from == null || team == null || !m_Capturable )
				return;

			bool wasDom = ( m_Controller.PointA != null && m_Controller.PointB != null && 
				m_Controller.PointA.TeamOwner == m_Controller.PointB.TeamOwner && m_Controller.PointA.TeamOwner != null );

			point.TeamOwner = team;
			Alert( "{0} has captured {1}!", team.Name, point.Name );

			bool isDom = ( m_Controller.PointA != null && m_Controller.PointB != null && 
				m_Controller.PointA.TeamOwner == m_Controller.PointB.TeamOwner && m_Controller.PointA.TeamOwner != null );

			if ( wasDom && !isDom )
			{
				Alert( "Domination averted!" );

				if ( m_Controller.PointA != null )
					m_Controller.PointA.SetNonCaptureHue();

				if ( m_Controller.PointB != null )
					m_Controller.PointB.SetNonCaptureHue();

				if ( m_CaptureTimer != null )
					m_CaptureTimer.Stop();
				m_CaptureTimer = null;
			}
			
			if ( !wasDom && isDom )
			{
				m_CapStage = 0;
				m_CaptureTimer = Timer.DelayCall( TimeSpan.Zero, TimeSpan.FromSeconds( 1.0 ), new TimerCallback( CaptureTick ) );
				m_CaptureTimer.Start();
			}
		}

		private void CaptureTick()
		{
			DDTeamInfo team = null;

			if ( m_Controller.PointA != null && m_Controller.PointA.TeamOwner != null )
				team = m_Controller.PointA.TeamOwner;
			else if ( m_Controller.PointB != null && m_Controller.PointB.TeamOwner != null )
				team = m_Controller.PointB.TeamOwner;
			
			if ( team == null )
			{
				m_Capturable = true;
				if ( m_CaptureTimer != null )
					m_CaptureTimer.Stop();
				m_CaptureTimer = null;
				return;
			}

			if ( ++m_CapStage < 10 )
			{
				Alert( "{0} is dominating... {1}", team.Name, 10 - m_CapStage );

				if ( m_Controller.PointA != null )
					m_Controller.PointA.SetCaptureHue( m_CapStage );

				if ( m_Controller.PointB != null )
					m_Controller.PointB.SetCaptureHue( m_CapStage );
			}
			else
			{
				Alert( "{0} has scored!", team.Name );

				team.Score += 100;
				team.Captures += 1;

				m_Capturable = false;
				m_CapStage = 0;
				m_CaptureTimer.Stop();
				m_CaptureTimer = null;

				if ( m_Controller.PointA != null )
				{
					m_Controller.PointA.TeamOwner = null;
					m_Controller.PointA.SetUncapturableHue();
				}

				if ( m_Controller.PointB != null )
				{
					m_Controller.PointB.TeamOwner = null;
					m_Controller.PointB.SetUncapturableHue();
				}

				m_UncaptureTimer = Timer.DelayCall( TimeSpan.FromSeconds( 30.0 ), new TimerCallback( UncaptureTick ) );
				m_UncaptureTimer.Start();
			}
		}

		private void UncaptureTick()
		{			
			m_Capturable = true;

			if ( m_CaptureTimer != null )
			{
				m_CaptureTimer.Stop();
				m_CaptureTimer = null;
			}

			if ( m_UncaptureTimer != null )
			{
				m_UncaptureTimer.Stop();
				m_UncaptureTimer = null;
			}

			if ( m_Controller.PointA != null )
			{
				m_Controller.PointA.TeamOwner = null;
				m_Controller.PointA.SetNonCaptureHue();
			}

			if ( m_Controller.PointB != null )
			{
				m_Controller.PointB.TeamOwner = null;
				m_Controller.PointB.SetNonCaptureHue();
			}
		}
	}

	public class DDWayPoint : BaseAddon
	{
		private DDTeamInfo m_TeamOwner;
		private DDGame m_Game;

		[Constructable]
		public DDWayPoint()
		{
			this.ItemID = 0x519;
			this.Visible = true;
			this.Name = "SET MY NAME";

			AddComponent( new DDStep( 0x7A8 ), -1, -1, -5 );
			AddComponent( new DDStep( 0x7A6 ),  0, -1, -5 );
			AddComponent( new DDStep( 0x7AA ),  1, -1, -5 );

			AddComponent( new DDStep( 0x7A5 ),  1,  0, -5 );

			AddComponent( new DDStep( 0x7A9 ),  1,  1, -5 );
			AddComponent( new DDStep( 0x7A4 ),  0,  1, -5 );
			AddComponent( new DDStep( 0x7AB ), -1,  1, -5 );

			AddComponent( new DDStep( 0x7A7 ), -1,  0, -5 );

			SetUncapturableHue();
		}

		public DDWayPoint( Serial serial ) : base(serial)
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize(reader);

			int version = reader.ReadInt();
		}

		public override bool ShareHue{ get{ return false; } }

		public DDGame Game
		{ 
			get{ return m_Game; } 
			set
			{ 
				m_Game = value; 
				m_TeamOwner = null;
				
				if ( m_Game != null )
					SetNonCaptureHue(); 
				else
					SetUncapturableHue();
			}
		}

		public DDTeamInfo TeamOwner
		{ 
			get{ return m_TeamOwner; } 
			set
			{ 
				m_TeamOwner = value; 

				SetNonCaptureHue();
			} 
		}

		public const int UncapturableHue = 0x497;
		public const int NonCapturedHue = 0x38A;

		public void SetUncapturableHue()
		{
			for (int i=0;i<Components.Count;i++)
				((Item)Components[i]).Hue = UncapturableHue;
			this.Hue = UncapturableHue;
		}

		public void SetNonCaptureHue()
		{
			for (int i=0;i<Components.Count;i++)
				((Item)Components[i]).Hue = NonCapturedHue;

			if ( m_TeamOwner != null )
				this.Hue = m_TeamOwner.Color;
			else
				this.Hue = NonCapturedHue;
		}

		public void SetCaptureHue( int stage )
		{
			if ( m_TeamOwner == null )
				return;

			this.Hue = m_TeamOwner.Color;

			for (int i=0;i<Components.Count;i++)
			{
				if ( i < stage )
					((Item)Components[i]).Hue = m_TeamOwner.Color;
				else
					((Item)Components[i]).Hue = NonCapturedHue;
			}
		}

		public override bool OnMoveOver( Mobile from )
		{
			if ( m_Game == null )
			{
				SetUncapturableHue();
			}
			else if ( from.Alive )
			{
				DDTeamInfo team = m_Game.GetTeamInfo( from );

				if ( team != null && team != TeamOwner )
					m_Game.Dominate( this, from, team );
			}

			return true;
		}

		public class DDStep : AddonComponent
		{
			public DDStep( int itemID ) : base( itemID )
			{
				this.Visible = true;
			}

			public DDStep( Serial serial ) : base( serial )
			{
			}

			public override void Deserialize( GenericReader reader )
			{
				base.Deserialize( reader );

				int version = reader.ReadInt();
			}

			public override void Serialize( GenericWriter writer )
			{
				base.Serialize( writer );

				writer.Write( (int)0 );//version
			}

			public override bool OnMoveOver( Mobile m )
			{
				return Addon.OnMoveOver( m );
			}
		}
	}
}
