using System;
using System.Collections.Generic;
using System.Text;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using Server.Gumps;

namespace Server.Engines.ConPVP
{
	public sealed class CTFBoard : Item
	{
		public CTFTeamInfo m_TeamInfo;

		public override string DefaultName
		{
			get { return "scoreboard"; }
		}

		[Constructable]
		public CTFBoard()
			: base( 7774 )
		{
			Movable = false;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( m_TeamInfo != null && m_TeamInfo.Game != null )
			{
				from.CloseGump( typeof( CTFBoardGump ) );
				from.SendGump( new CTFBoardGump( from, m_TeamInfo.Game ) );
			}
		}

		public CTFBoard( Serial serial )
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

	public class CTFBoardGump : Gump
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

		private CTFGame m_Game;

		public CTFBoardGump( Mobile mob, CTFGame game )
			: this( mob, game, null )
		{
		}

		public CTFBoardGump( Mobile mob, CTFGame game, CTFTeamInfo section )
			: base( 60, 60 )
		{
			m_Game = game;

			CTFTeamInfo ourTeam = game.GetTeamInfo( mob );

			List<IRankedCTF> entries = new List<IRankedCTF>();

			if ( section == null )
			{
				for ( int i = 0; i < game.Context.Participants.Count; ++i )
				{
					CTFTeamInfo teamInfo = game.Controller.TeamInfo[i % 8];

					if ( teamInfo == null || teamInfo.Flag == null )
						continue;

					entries.Add( teamInfo );
				}
			}
			else
			{
				foreach ( CTFPlayerInfo player in section.Players.Values )
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

			AddBorderedText( 22, 22, 294, 20, Center( "CTF Scoreboard" ), LabelColor32, BlackColor32 );

			AddImageTiled( 32, 50, 264, 1, 9107 );
			AddImageTiled( 42, 52, 264, 1, 9157 );

			if ( section == null )
			{
				for ( int i = 0; i < entries.Count; ++i )
				{
					CTFTeamInfo teamInfo = entries[i] as CTFTeamInfo;

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

					AddBorderedText( 60, 65 + ( i * 75 ), 250, 20, String.Format( "{0}: {1} Team", LadderGump.Rank( 1 + i ), teamInfo.Name ), nameColor, borderColor );

					AddBorderedText( 50 + 10, 85 + ( i * 75 ), 100, 20, "Score:", 0xFFC000, BlackColor32 );
					AddBorderedText( 50 + 15, 105 + ( i * 75 ), 100, 20, teamInfo.Score.ToString( "N0" ), 0xFFC000, BlackColor32 );

					AddBorderedText( 110 + 10, 85 + ( i * 75 ), 100, 20, "Kills:", 0xFFC000, BlackColor32 );
					AddBorderedText( 110 + 15, 105 + ( i * 75 ), 100, 20, teamInfo.Kills.ToString( "N0" ), 0xFFC000, BlackColor32 );

					AddBorderedText( 160 + 10, 85 + ( i * 75 ), 100, 20, "Captures:", 0xFFC000, BlackColor32 );
					AddBorderedText( 160 + 15, 105 + ( i * 75 ), 100, 20, teamInfo.Captures.ToString( "N0" ), 0xFFC000, BlackColor32 );

					CTFPlayerInfo pl = teamInfo.Leader;

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

	public sealed class CTFFlag : Item
	{
		public CTFTeamInfo m_TeamInfo;

		public override string DefaultName
		{
			get { return "old people cookies"; }
		}

		public Mobile m_Fragger;
		public DateTime m_FragTime;

		public Mobile m_Returner;
		public DateTime m_ReturnTime;

		private int m_ReturnCount;
		private Timer m_ReturnTimer;

		[Constructable]
		public CTFFlag()
			: base( 5643 )
		{
			Movable = false;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( m_TeamInfo != null && m_TeamInfo.Game != null )
			{
				CTFTeamInfo ourTeam = m_TeamInfo;
				CTFTeamInfo useTeam = m_TeamInfo.Game.GetTeamInfo( from );

				if ( ourTeam == null || useTeam == null )
					return;

				if ( IsChildOf( from.Backpack ) )
				{
					from.BeginTarget( 1, false, TargetFlags.None, new TargetCallback( Flag_OnTarget ) );
				}
				else if ( !from.InRange( this, 1 ) || !from.InLOS( this ) )
				{
					from.LocalOverheadMessage( MessageType.Regular, 0x26, 1019045 ); // I can't reach that
				}
				else if ( ourTeam == useTeam )
				{
					if ( this.Location == m_TeamInfo.Origin && this.Map == m_TeamInfo.Game.Facet )
					{
						from.Send( new UnicodeMessage( this.Serial, this.ItemID, MessageType.Regular, 0x3B2, 3, "ENU", this.Name, "Touch me not for I am chaste." ) );
					}
					else
					{
						CTFPlayerInfo playerInfo = useTeam[from];

						if ( playerInfo != null )
							playerInfo.Score += 4; // return

						m_Returner = from;
						m_ReturnTime = DateTime.UtcNow;

						SendHome();

						from.LocalOverheadMessage( MessageType.Regular, 0x59, false, "You returned the cookies!" );
						m_TeamInfo.Game.Alert( "The {1} cookies have been returned by {0}.", from.Name, ourTeam.Name );
					}
				}
				else if ( !from.PlaceInBackpack( this ) )
				{
					from.LocalOverheadMessage( MessageType.Regular, 0x26, false, "I can't hold that." );
				}
				else
				{
					from.RevealingAction();

					from.LocalOverheadMessage( MessageType.Regular, 0x59, false, "You stole the cookies!" );
					m_TeamInfo.Game.Alert( "The {1} cookies have been stolen by {0} ({2}).", from.Name, ourTeam.Name, useTeam.Name );

					BeginCountdown( 120 );
				}
			}
		}

		public override void Delete()
		{
			if ( Parent != null )
			{
				SendHome();
				return;
			}

			base.Delete();
		}

		public void DropTo( Mobile mob, Mobile killer )
		{
			m_Fragger = killer;
			m_FragTime = DateTime.UtcNow;

			if ( mob != null )
			{
				MoveToWorld( new Point3D( mob.X, mob.Y, mob.Z + 2 ), mob.Map );

				m_ReturnCount = Math.Min( m_ReturnCount, 10 );
			}
			else
			{
				SendHome();
			}
		}

		private void StopCountdown()
		{
			if ( m_ReturnTimer != null )
				m_ReturnTimer.Stop();

			m_ReturnTimer = null;
		}

		private void BeginCountdown( int returnCount )
		{
			StopCountdown();

			m_ReturnTimer = Timer.DelayCall( TimeSpan.FromSeconds( 1.0 ), TimeSpan.FromSeconds( 1.0 ), new TimerCallback( Countdown_OnTick ) );
			m_ReturnCount = returnCount;
		}

		private void Countdown_OnTick()
		{
			Mobile owner = this.RootParent as Mobile;

			switch ( m_ReturnCount )
			{
				case 60:
				case 30:
				case 15:
				case 10:
				case 5:
				case 4:
				case 3:
				case 2:
				case 1:
				{
					if ( owner != null )
						owner.SendMessage( 0x26, "You have {0} {1} to capture the cookies!", m_ReturnCount, m_ReturnCount == 1 ? "second" : "seconds" );

					break;
				}

				case 0:
				{
					if ( owner != null )
					{
						owner.SendMessage( 0x26, "You have taken too long to capture the cookies!" );
						owner.Kill();
					}

					SendHome();

					if ( m_TeamInfo != null && m_TeamInfo.Game != null )
						m_TeamInfo.Game.Alert( "The {0} cookies have been returned.", m_TeamInfo.Name );

					return;
				}
			}

			--m_ReturnCount;
		}

		private void Flag_OnTarget( Mobile from, object obj )
		{
			if ( m_TeamInfo == null )
				return;
			
			if ( !IsChildOf( from.Backpack ) )
				return;

			CTFTeamInfo ourTeam = m_TeamInfo;
			CTFTeamInfo useTeam = m_TeamInfo.Game.GetTeamInfo( from );

			if ( obj is CTFFlag )
			{
				if ( obj == useTeam.Flag )
				{
					from.LocalOverheadMessage( MessageType.Regular, 0x59, false, "You captured the cookies!" );
					m_TeamInfo.Game.Alert( "{0} captured the {1} cookies!", from.Name, ourTeam.Name );

					SendHome();

					CTFPlayerInfo playerInfo = useTeam[from];

					if ( playerInfo != null )
					{
						playerInfo.Captures += 1;
						playerInfo.Score += 50; // capture

						CTFFlag teamFlag = useTeam.Flag;

						if ( teamFlag.m_Fragger != null && DateTime.UtcNow < ( teamFlag.m_FragTime + TimeSpan.FromSeconds( 5.0 ) ) && m_TeamInfo.Game.GetTeamInfo( teamFlag.m_Fragger ) == useTeam )
						{
							CTFPlayerInfo assistInfo = useTeam[teamFlag.m_Fragger];

							if ( assistInfo != null )
								assistInfo.Score += 6; // frag assist
						}

						if ( teamFlag.m_Returner != null && DateTime.UtcNow < ( teamFlag.m_ReturnTime + TimeSpan.FromSeconds( 5.0 ) ) )
						{
							CTFPlayerInfo assistInfo = useTeam[teamFlag.m_Returner];

							if ( assistInfo != null )
								assistInfo.Score += 4; // return assist
						}
					}
				}
				else
				{
					from.LocalOverheadMessage( MessageType.Regular, 0x26, false, "Those are not my cookies." );
				}
			}
			else if ( obj is Mobile )
			{
				Mobile passTo = obj as Mobile;

				CTFTeamInfo passTeam = m_TeamInfo.Game.GetTeamInfo( passTo );

				if ( passTo == from )
				{
					from.LocalOverheadMessage( MessageType.Regular, 0x26, false, "I can't pass to them." );
				}
				else if ( passTeam == useTeam && passTo.PlaceInBackpack( this ) )
				{
					passTo.LocalOverheadMessage( MessageType.Regular, 0x59, false, String.Format( "{0} has passed you the cookies!", from.Name ) );
				}
				else
				{
					from.LocalOverheadMessage( MessageType.Regular, 0x26, false, "I can't pass to them." );
				}
			}
		}

		public void SendHome()
		{
			StopCountdown();

			if ( m_TeamInfo == null )
				return;

			MoveToWorld( m_TeamInfo.Origin, m_TeamInfo.Game.Facet );
		}

		private Mobile FindOwner( object parent )
		{
			if ( parent is Item )
				return ( (Item) parent ).RootParent as Mobile;

			if ( parent is Mobile )
				return (Mobile) parent;

			return null;
		}

		public override void OnAdded(IEntity parent)
		{
			base.OnAdded( parent );

			Mobile mob = FindOwner( parent );

			if ( mob != null )
				mob.SolidHueOverride = 0x4001;
		}

		public override void OnRemoved(IEntity parent)
		{
			base.OnRemoved( parent );

			Mobile mob = FindOwner( parent );

			if ( mob != null )
				mob.SolidHueOverride = ( m_TeamInfo == null ? -1 : m_TeamInfo.Game.GetColor( mob ) );
		}

		public CTFFlag( Serial serial )
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

	public interface IRankedCTF
	{
		int Kills { get; }
		int Captures { get; }
		int Score { get; }
		string Name { get; }
	}

	public sealed class CTFPlayerInfo : IRankedCTF
	{
		private CTFTeamInfo m_TeamInfo;

		private Mobile m_Player;

		private int m_Kills;
		private int m_Captures;

		private int m_Score;

		public Mobile Player { get { return m_Player; } }

		string IRankedCTF.Name
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

		public CTFPlayerInfo( CTFTeamInfo teamInfo, Mobile player )
		{
			m_TeamInfo = teamInfo;
			m_Player = player;
		}
	}

	[PropertyObject]
	public sealed class CTFTeamInfo : IRankedCTF
	{
		private CTFGame m_Game;
		private int m_TeamID;

		private int m_Color;
		private string m_Name;

		private CTFBoard m_Board;

		private CTFFlag m_Flag;
		private Point3D m_Origin;

		private int m_Kills;
		private int m_Captures;

		private int m_Score;

		private Dictionary<Mobile, CTFPlayerInfo> m_Players;

		string IRankedCTF.Name
		{
			get { return String.Format( "{0} Team", m_Name ); }
		}

		public CTFGame Game { get { return m_Game; } set { m_Game = value; } }
		public int TeamID { get { return m_TeamID; } }

		public int Kills { get { return m_Kills; } set { m_Kills = value; } }
		public int Captures { get { return m_Captures; } set { m_Captures = value; } }

		public int Score { get { return m_Score; } set { m_Score = value; } }

		private CTFPlayerInfo m_Leader;

		public CTFPlayerInfo Leader
		{
			get { return m_Leader; }
			set { m_Leader = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public CTFBoard Board
		{
			get { return m_Board; }
			set { m_Board = value; }
		}

		public Dictionary<Mobile, CTFPlayerInfo> Players
		{
			get { return m_Players; }
		}

		public CTFPlayerInfo this[Mobile mob]
		{
			get
			{
				if ( mob == null )
					return null;

				CTFPlayerInfo val;

				if ( !m_Players.TryGetValue( mob, out val ) )
					m_Players[mob] = val = new CTFPlayerInfo( this, mob );

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
		public string Name
		{
			get { return m_Name; }
			set { m_Name = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public CTFFlag Flag
		{
			get { return m_Flag; }
			set { m_Flag = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Point3D Origin
		{
			get { return m_Origin; }
			set { m_Origin = value; }
		}

		public CTFTeamInfo( int teamID )
		{
			m_TeamID = teamID;
			m_Players = new Dictionary<Mobile, CTFPlayerInfo>();
		}

		public void Reset()
		{
			m_Kills = 0;
			m_Captures = 0;

			m_Score = 0;

			m_Leader = null;

			m_Players.Clear();

			if ( m_Flag != null )
			{
				m_Flag.m_TeamInfo = this;
				m_Flag.Hue = m_Color;
				m_Flag.SendHome();
			}

			if ( m_Board != null )
				m_Board.m_TeamInfo = this;
		}

		public CTFTeamInfo( int teamID, GenericReader ip )
		{
			m_TeamID = teamID;
			m_Players = new Dictionary<Mobile, CTFPlayerInfo>();

			int version = ip.ReadEncodedInt();

			switch ( version )
			{
				case 2:
				{
					m_Board = ip.ReadItem() as CTFBoard;

					goto case 1;
				}
				case 1:
				{
					m_Name = ip.ReadString();

					goto case 0;
				}
				case 0:
				{
					m_Color = ip.ReadEncodedInt();

					m_Flag = ip.ReadItem() as CTFFlag;
					m_Origin = ip.ReadPoint3D();
					break;
				}
			}
		}

		public void Serialize( GenericWriter op )
		{
			op.WriteEncodedInt( 2 ); // version

			op.Write( m_Board );

			op.Write( m_Name );

			op.WriteEncodedInt( m_Color );

			op.Write( m_Flag );
			op.Write( m_Origin );
		}

		public override string ToString()
		{
			return "...";
		}
	}

	public sealed class CTFController : EventController
	{
		private CTFTeamInfo[] m_TeamInfo;

		private TimeSpan m_Duration;

		public CTFTeamInfo[] TeamInfo { get { return m_TeamInfo; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public CTFTeamInfo Team1 { get { return m_TeamInfo[0]; } set { } }

		[CommandProperty( AccessLevel.GameMaster )]
		public CTFTeamInfo Team2 { get { return m_TeamInfo[1]; } set { } }

		[CommandProperty( AccessLevel.GameMaster )]
		public CTFTeamInfo Team3 { get { return m_TeamInfo[2]; } set { } }

		[CommandProperty( AccessLevel.GameMaster )]
		public CTFTeamInfo Team4 { get { return m_TeamInfo[3]; } set { } }

		[CommandProperty( AccessLevel.GameMaster )]
		public CTFTeamInfo Team5 { get { return m_TeamInfo[4]; } set { } }

		[CommandProperty( AccessLevel.GameMaster )]
		public CTFTeamInfo Team6 { get { return m_TeamInfo[5]; } set { } }

		[CommandProperty( AccessLevel.GameMaster )]
		public CTFTeamInfo Team7 { get { return m_TeamInfo[6]; } set { } }

		[CommandProperty( AccessLevel.GameMaster )]
		public CTFTeamInfo Team8 { get { return m_TeamInfo[7]; } set { } }

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan Duration
		{
			get { return m_Duration; }
			set { m_Duration = value; }
		}

		public override string Title
		{
			get { return "CTF"; }
		}

		public override string GetTeamName( int teamID )
		{
			return m_TeamInfo[teamID % m_TeamInfo.Length].Name;
		}

		public override EventGame Construct( DuelContext context )
		{
			return new CTFGame( this, context );
		}

		[Constructable]
		public CTFController()
		{
			Visible = false;
			Movable = false;

			m_Duration = TimeSpan.FromMinutes( 30.0 );

			m_TeamInfo = new CTFTeamInfo[8];

			for ( int i = 0; i < m_TeamInfo.Length; ++i )
				m_TeamInfo[i] = new CTFTeamInfo( i );
		}

		public CTFController( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 2 );

			writer.Write( m_Duration );

			writer.WriteEncodedInt( m_TeamInfo.Length );

			for ( int i = 0; i < m_TeamInfo.Length; ++i )
				m_TeamInfo[i].Serialize( writer );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 2:
				{
					m_Duration = reader.ReadTimeSpan();

					goto case 1;
				}
				case 1:
				{
					m_TeamInfo = new CTFTeamInfo[reader.ReadEncodedInt()];

					for ( int i = 0; i < m_TeamInfo.Length; ++i )
						m_TeamInfo[i] = new CTFTeamInfo( i, reader );

					break;
				}
				case 0:
				{
					m_TeamInfo = new CTFTeamInfo[8];

					for ( int i = 0; i < m_TeamInfo.Length; ++i )
						m_TeamInfo[i] = new CTFTeamInfo( i );

					break;
				}
			}

			if ( version < 2 )
				m_Duration = TimeSpan.FromMinutes( 30.0 );
		}
	}

	public sealed class CTFGame : EventGame
	{
		public static void Initialize()
		{
			for ( int i = 0x7C9; i <= 0x7D0; ++i )
				TileData.ItemTable[i].Flags |= TileFlag.NoShoot;
		}

		private CTFController m_Controller;

		public CTFController Controller { get { return m_Controller; } }

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

		public CTFGame( CTFController controller, DuelContext context ) : base( context )
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

		public CTFTeamInfo GetTeamInfo( Mobile mob )
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
			CTFTeamInfo teamInfo = GetTeamInfo( mob );

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

			bool hadFlag = false;

			Item[] flags = corpse.FindItemsByType( typeof( CTFFlag ), false );

			for ( int i = 0; i < flags.Length; ++i )
				( flags[i] as CTFFlag ).DropTo( mob, killer );

			hadFlag = ( hadFlag || flags.Length > 0 );

			if ( mob.Backpack != null )
			{
				flags = mob.Backpack.FindItemsByType( typeof( CTFFlag ), false );

				for ( int i = 0; i < flags.Length; ++i )
					( flags[i] as CTFFlag ).DropTo( mob, killer );

				hadFlag = ( hadFlag || flags.Length > 0 );
			}

			if ( killer != null && killer.Player )
			{
				CTFTeamInfo teamInfo = GetTeamInfo( killer );
				CTFTeamInfo victInfo = GetTeamInfo( mob );

				if ( teamInfo != null && teamInfo != victInfo )
				{
					CTFPlayerInfo playerInfo = teamInfo[killer];

					if ( playerInfo != null )
					{
						playerInfo.Kills += 1;
						playerInfo.Score += 1; // base frag

						if ( hadFlag )
							playerInfo.Score += 4; // fragged flag carrier

						if ( mob.InRange( teamInfo.Origin, 24 ) && mob.Map == this.Facet )
							playerInfo.Score += 1; // fragged in base -- guarding

						for ( int i = 0; i < m_Controller.TeamInfo.Length; ++i )
						{
							if ( m_Controller.TeamInfo[i] == teamInfo )
								continue;

							Mobile ourFlagCarrier = null;

							if ( m_Controller.TeamInfo[i].Flag != null )
								ourFlagCarrier = m_Controller.TeamInfo[i].Flag.RootParent as Mobile;

							if ( ourFlagCarrier != null && GetTeamInfo( ourFlagCarrier ) == teamInfo )
							{
								for ( int j = 0; j < ourFlagCarrier.Aggressors.Count; ++j )
								{
									AggressorInfo aggr = ourFlagCarrier.Aggressors[j] as AggressorInfo;

									if ( aggr == null || aggr.Defender != ourFlagCarrier || aggr.Attacker != mob )
										continue;

									playerInfo.Score += 2; // helped defend guy capturing enemy flag
									break;
								}

								if ( mob.Map == ourFlagCarrier.Map && ourFlagCarrier.InRange( mob, 12 ) )
									playerInfo.Score += 1; // helped defend guy capturing enemy flag
							}
						}
					}
				}
			}

			mob.CloseGump( typeof( CTFBoardGump ) );
			mob.SendGump( new CTFBoardGump( mob, this ) );

			m_Context.Requip( mob, corpse );
			DelayBounce( TimeSpan.FromSeconds( 30.0 ), mob, corpse );

			return false;
		}

		private Timer m_FinishTimer;

		public override void OnStart()
		{
			for ( int i = 0; i < m_Controller.TeamInfo.Length; ++i )
			{
				CTFTeamInfo teamInfo = m_Controller.TeamInfo[i];

				teamInfo.Game = this;
				teamInfo.Reset();
			}

			for ( int i = 0; i < m_Context.Participants.Count; ++i )
				ApplyHues( m_Context.Participants[i] as Participant, m_Controller.TeamInfo[i % 8].Color );

			if ( m_FinishTimer != null )
				m_FinishTimer.Stop();

			m_FinishTimer = Timer.DelayCall( m_Controller.Duration, new TimerCallback( Finish_Callback ) );
		}

		private void Finish_Callback()
		{
			List<CTFTeamInfo> teams = new List<CTFTeamInfo>();

			for ( int i = 0; i < m_Context.Participants.Count; ++i )
			{
				CTFTeamInfo teamInfo = m_Controller.TeamInfo[i % 8];

				if ( teamInfo == null || teamInfo.Flag == null )
					continue;

				teams.Add( teamInfo );
			}

			teams.Sort( delegate( CTFTeamInfo a, CTFTeamInfo b )
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

			CTFTeamInfo winner = ( teams.Count > 0 ? teams[0] : null );

			for ( int i = 0; i < teams.Count; ++i )
			{
				TrophyRank rank = TrophyRank.Bronze;

				if ( i == 0 )
					rank = TrophyRank.Gold;
				else if ( i == 1 )
					rank = TrophyRank.Silver;

				CTFPlayerInfo leader = teams[i].Leader;

				foreach ( CTFPlayerInfo pl in teams[i].Players.Values )
				{
					Mobile mob = pl.Player;

					if ( mob == null )
						continue;

					//"Red v Blue CTF Champion"

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

					item.Name = String.Format( "{0}, {1} team", item.Name, teams[i].Name.ToLower() );

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
						dp.Mobile.CloseGump( typeof( CTFBoardGump ) );
						dp.Mobile.SendGump( new CTFBoardGump( dp.Mobile, this ) );
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
				CTFTeamInfo teamInfo = m_Controller.TeamInfo[i];

				if ( teamInfo.Flag != null )
				{
					teamInfo.Flag.SendHome();
					teamInfo.Flag.m_TeamInfo = null;
				}

				if ( teamInfo.Board != null )
					teamInfo.Board.m_TeamInfo = null;

				teamInfo.Game = null;
			}

			for ( int i = 0; i < m_Context.Participants.Count; ++i )
				ApplyHues( m_Context.Participants[i] as Participant, -1 );

			if ( m_FinishTimer != null )
				m_FinishTimer.Stop();

			m_FinishTimer = null;
		}
	}
}