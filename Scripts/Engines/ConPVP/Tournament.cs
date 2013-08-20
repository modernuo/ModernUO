using System;
using System.Text;
using System.Collections;
using Server;
using Server.Items;
using Server.Gumps;
using Server.Network;
using Server.ContextMenus;
using Server.Mobiles;
using Server.Targeting;
using System.Collections.Generic;

namespace Server.Engines.ConPVP
{
	public enum TournamentStage
	{
		Inactive,
		Signup,
		Fighting
	}

	public enum GroupingType
	{
		HighVsLow,
		Nearest,
		Random
	}

	public enum TieType
	{
		Random,
		Highest,
		Lowest,
		FullElimination,
		FullAdvancement
	}

	public class TournamentRegistrar : Banker
	{
		private TournamentController m_Tournament;

		[CommandProperty( AccessLevel.GameMaster )]
		public TournamentController Tournament{ get{ return m_Tournament; } set{ m_Tournament = value; } }

		[Constructable]
		public TournamentRegistrar()
		{
			Timer.DelayCall( TimeSpan.FromSeconds( 30.0 ), TimeSpan.FromSeconds( 30.0 ), new TimerCallback( Announce_Callback ) );
		}

		private void Announce_Callback()
		{
			Tournament tourny = null;

			if ( m_Tournament != null )
				tourny = m_Tournament.Tournament;

			if ( tourny != null && tourny.Stage == TournamentStage.Signup )
				PublicOverheadMessage( MessageType.Regular, 0x35, false, "Come one, come all! Do you aspire to be a fighter of great renown? Join this tournament and show the world your abilities." );
		}

		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			base.OnMovement( m, oldLocation );

			Tournament tourny = null;

			if ( m_Tournament != null )
				tourny = m_Tournament.Tournament;

			if ( InRange( m, 4 ) && !InRange( oldLocation, 4 ) && tourny != null && tourny.Stage == TournamentStage.Signup && m.CanBeginAction( this ) )
			{
				Ladder ladder = Ladder.Instance;

				if ( ladder != null )
				{
					LadderEntry entry = ladder.Find( m );

					if ( entry != null && Ladder.GetLevel( entry.Experience ) < tourny.LevelRequirement )
						return;
				}

				if ( tourny.HasParticipant( m ) )
					return;

				PrivateOverheadMessage( MessageType.Regular, 0x35, false, String.Format( "Hello m'{0}. Dost thou wish to enter this tournament? You need only to write your name in this book.", m.Female ? "Lady" : "Lord" ), m.NetState );
				m.BeginAction( this );
				Timer.DelayCall( TimeSpan.FromSeconds( 10.0 ), new TimerStateCallback( ReleaseLock_Callback ), m );
			}
		}

		private void ReleaseLock_Callback( object obj )
		{
			((Mobile)obj).EndAction( this );
		}

		public TournamentRegistrar( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );

			writer.Write( (Item) m_Tournament );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_Tournament = reader.ReadItem() as TournamentController;
					break;
				}
			}

			Timer.DelayCall( TimeSpan.FromSeconds( 30.0 ), TimeSpan.FromSeconds( 30.0 ), new TimerCallback( Announce_Callback ) );
		}
	}

	public class TournamentSignupItem : Item
	{
		private TournamentController m_Tournament;
		private Mobile m_Registrar;

		[CommandProperty( AccessLevel.GameMaster )]
		public TournamentController Tournament{ get{ return m_Tournament; } set{ m_Tournament = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile Registrar{ get{ return m_Registrar; } set{ m_Registrar = value; } }

		public override string DefaultName
		{
			get { return "tournament signup book"; }
		}

		[Constructable]
		public TournamentSignupItem() : base( 4029 )
		{
			Movable = false;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !from.InRange( GetWorldLocation(), 2 ) )
			{
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that
			}
			else if ( m_Tournament != null )
			{
				Tournament tourny = m_Tournament.Tournament;

				if ( tourny != null )
				{
					if ( m_Registrar != null )
						m_Registrar.Direction = m_Registrar.GetDirectionTo( this );

					switch ( tourny.Stage )
					{
						case TournamentStage.Fighting:
						{
							if ( m_Registrar != null )
							{
								if ( tourny.HasParticipant( from ) )
								{
									m_Registrar.PrivateOverheadMessage( MessageType.Regular,
										0x35, false, "Excuse me? You are already signed up.", from.NetState );
								}
								else
								{
									m_Registrar.PrivateOverheadMessage( MessageType.Regular,
										0x22, false, "The tournament has already begun. You are too late to signup now.", from.NetState );
								}
							}

							break;
						}
						case TournamentStage.Inactive:
						{
							if ( m_Registrar != null )
								m_Registrar.PrivateOverheadMessage( MessageType.Regular,
									0x35, false, "The tournament is closed.", from.NetState );

							break;
						}
						case TournamentStage.Signup:
						{
							Ladder ladder = Ladder.Instance;

							if ( ladder != null )
							{
								LadderEntry entry = ladder.Find( from );

								if ( entry != null && Ladder.GetLevel( entry.Experience ) < tourny.LevelRequirement )
								{
									if ( m_Registrar != null )
									{
										m_Registrar.PrivateOverheadMessage( MessageType.Regular,
											0x35, false, "You have not yet proven yourself a worthy dueler.", from.NetState );
									}

									break;
								}
							}

							if ( from.HasGump( typeof( AcceptTeamGump ) ) )
							{
								if ( m_Registrar != null )
									m_Registrar.PrivateOverheadMessage( MessageType.Regular,
										0x22, false, "You must first respond to the offer I've given you.", from.NetState );
							}
							else if ( from.HasGump( typeof( AcceptDuelGump ) ) )
							{
								if ( m_Registrar != null )
									m_Registrar.PrivateOverheadMessage( MessageType.Regular,
										0x22, false, "You must first cancel your duel offer.", from.NetState );
							}
							else if ( from is PlayerMobile && ((PlayerMobile)from).DuelContext != null )
							{
								if ( m_Registrar != null )
									m_Registrar.PrivateOverheadMessage( MessageType.Regular,
										0x22, false, "You are already participating in a duel.", from.NetState );
							}
							else if ( !tourny.HasParticipant( from ) )
							{
								ArrayList players = new ArrayList();
								players.Add(from);
								from.CloseGump( typeof( ConfirmSignupGump ) );
								from.SendGump( new ConfirmSignupGump( from, m_Registrar, tourny, players ) );
							}
							else if ( m_Registrar != null )
							{
								m_Registrar.PrivateOverheadMessage( MessageType.Regular,
									0x35, false, "You have already entered this tournament.", from.NetState );
							}

							break;
						}
					}
				}
			}
		}

		public TournamentSignupItem( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );

			writer.Write( (Item) m_Tournament );
			writer.Write( (Mobile) m_Registrar );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_Tournament = reader.ReadItem() as TournamentController;
					m_Registrar = reader.ReadMobile();
					break;
				}
			}
		}
	}

	public class ConfirmSignupGump : Gump
	{
		private Mobile m_From;
		private Tournament m_Tournament;
		private ArrayList m_Players;
		private Mobile m_Registrar;

		private const int BlackColor32 = 0x000008;
		private const int LabelColor32 = 0xFFFFFF;

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

		public void AddGoldenButton( int x, int y, int bid )
		{
			AddButton( x  , y  , 0xD2, 0xD2, bid, GumpButtonType.Reply, 0 );
			AddButton( x+3, y+3, 0xD8, 0xD8, bid, GumpButtonType.Reply, 0 );
		}

		public ConfirmSignupGump( Mobile from, Mobile registrar, Tournament tourny, ArrayList players ) : base( 50, 50 )
		{
			m_From = from;
			m_Registrar = registrar;
			m_Tournament = tourny;
			m_Players = players;

			m_From.CloseGump( typeof( AcceptTeamGump ) );
			m_From.CloseGump( typeof( AcceptDuelGump ) );
			m_From.CloseGump( typeof( DuelContextGump ) );
			m_From.CloseGump( typeof( ConfirmSignupGump ) );

			#region Rules
			Ruleset ruleset = tourny.Ruleset;
			Ruleset basedef = ruleset.Base;

			int height = 185 + 60 + 12;

			int changes = 0;

			BitArray defs;

			if ( ruleset.Flavors.Count > 0 )
			{
				defs = new BitArray( basedef.Options );

				for ( int i = 0; i < ruleset.Flavors.Count; ++i )
					defs.Or( ((Ruleset)ruleset.Flavors[i]).Options );

				height += ruleset.Flavors.Count * 18;
			}
			else
			{
				defs = basedef.Options;
			}

			BitArray opts = ruleset.Options;

			for ( int i = 0; i < opts.Length; ++i )
			{
				if ( defs[i] != opts[i] )
					++changes;
			}

			height += (changes * 22);

			height += 10 + 22 + 25 + 25;

			if ( tourny.PlayersPerParticipant > 1 )
				height += 36 + (tourny.PlayersPerParticipant * 20);
			#endregion

			Closable = false;

			AddPage( 0 );

			//AddBackground( 0, 0, 400, 220, 9150 );
			AddBackground( 1, 1, 398, height, 3600 );
			//AddBackground( 16, 15, 369, 189, 9100 );

			AddImageTiled( 16, 15, 369, height - 29, 3604 );
			AddAlphaRegion( 16, 15, 369, height - 29 );

			AddImage( 215, -43, 0xEE40 );
			//AddImage( 330, 141, 0x8BA );

			StringBuilder sb = new StringBuilder();

			if ( tourny.TournyType == TournyType.FreeForAll )
			{
				sb.Append( "FFA" );
			}
			else if ( tourny.TournyType == TournyType.RandomTeam )
			{
				sb.Append( "Team" );
			}
			else if ( tourny.TournyType == TournyType.RedVsBlue )
			{
				sb.Append( "Red v Blue" );
			}
			else
			{
				for ( int i = 0; i < tourny.ParticipantsPerMatch; ++i )
				{
					if ( sb.Length > 0 )
						sb.Append( 'v' );

					sb.Append( tourny.PlayersPerParticipant );
				}
			}

			if ( tourny.EventController != null )
				sb.Append( ' ' ).Append( tourny.EventController.Title );

			sb.Append( " Tournament Signup" );

			AddBorderedText( 22, 22, 294, 20, Center( sb.ToString() ), LabelColor32, BlackColor32 );
			AddBorderedText( 22, 50, 294, 40, "You have requested to join the tournament. Do you accept the rules?", 0xB0C868, BlackColor32 );

			AddImageTiled( 32, 88, 264, 1, 9107 );
			AddImageTiled( 42, 90, 264, 1, 9157 );

			#region Rules
			int y = 100;

			string groupText = null;

			switch ( tourny.GroupType )
			{
				case GroupingType.HighVsLow: groupText = "High vs Low"; break;
				case GroupingType.Nearest: groupText = "Closest opponent"; break;
				case GroupingType.Random: groupText = "Random"; break;
			}

			AddBorderedText( 35, y, 190, 20, String.Format( "Grouping: {0}", groupText ), LabelColor32, BlackColor32 );
			y += 20;

			string tieText = null;

			switch ( tourny.TieType )
			{
				case TieType.Random: tieText = "Random"; break;
				case TieType.Highest: tieText = "Highest advances"; break;
				case TieType.Lowest: tieText = "Lowest advances"; break;
				case TieType.FullAdvancement: tieText = ( tourny.ParticipantsPerMatch == 2 ? "Both advance" : "Everyone advances" ); break;
				case TieType.FullElimination: tieText = ( tourny.ParticipantsPerMatch == 2 ? "Both eliminated" : "Everyone eliminated" ); break;
			}

			AddBorderedText( 35, y, 190, 20, String.Format( "Tiebreaker: {0}", tieText ), LabelColor32, BlackColor32 );
			y += 20;

			string sdText = "Off";

			if ( tourny.SuddenDeath > TimeSpan.Zero )
			{
				sdText = String.Format( "{0}:{1:D2}", (int) tourny.SuddenDeath.TotalMinutes, tourny.SuddenDeath.Seconds );

				if ( tourny.SuddenDeathRounds > 0 )
					sdText = String.Format( "{0} (first {1} rounds)", sdText, tourny.SuddenDeathRounds );
				else
					sdText = String.Format( "{0} (all rounds)", sdText );
			}

			AddBorderedText( 35, y, 240, 20, String.Format( "Sudden Death: {0}", sdText ), LabelColor32, BlackColor32 );
			y += 20;

			y += 6;
			AddImageTiled( 32, y-1, 264, 1, 9107 );
			AddImageTiled( 42, y+1, 264, 1, 9157 );
			y += 6;

			AddBorderedText( 35, y, 190, 20, String.Format( "Ruleset: {0}", basedef.Title ), LabelColor32, BlackColor32 );
			y += 20;

			for ( int i = 0; i < ruleset.Flavors.Count; ++i, y += 18 )
				AddBorderedText( 35, y, 190, 20, String.Format( " + {0}", ((Ruleset)ruleset.Flavors[i]).Title ), LabelColor32, BlackColor32 );

			y += 4;

			if ( changes > 0 )
			{
				AddBorderedText( 35, y, 190, 20, "Modifications:", LabelColor32, BlackColor32 );
				y += 20;

				for ( int i = 0; i < opts.Length; ++i )
				{
					if ( defs[i] != opts[i] )
					{
						string name = ruleset.Layout.FindByIndex( i );

						if ( name != null ) // sanity
						{
							AddImage( 35, y, opts[i] ? 0xD3 : 0xD2 );
							AddBorderedText( 60, y, 165, 22, name, LabelColor32, BlackColor32 );
						}

						y += 22;
					}
				}
			}
			else
			{
				AddBorderedText( 35, y, 190, 20, "Modifications: None", LabelColor32, BlackColor32 );
				y += 20;
			}
			#endregion

			#region Team
			if ( tourny.PlayersPerParticipant > 1 )
			{
				y += 8;
				AddImageTiled( 32, y-1, 264, 1, 9107 );
				AddImageTiled( 42, y+1, 264, 1, 9157 );
				y += 8;

				AddBorderedText( 35, y, 190, 20, "Your Team", LabelColor32, BlackColor32 );
				y += 20;

				for ( int i = 0; i < players.Count; ++i, y += 20 )
				{
					if ( i == 0 )
						AddImage( 35, y, 0xD2 );
					else
						AddGoldenButton( 35, y, 1 + i );

					AddBorderedText( 60, y, 200, 20, ((Mobile)players[i]).Name, LabelColor32, BlackColor32 );
				}

				for ( int i = players.Count; i < tourny.PlayersPerParticipant; ++i, y += 20 )
				{
					if ( i == 0 )
						AddImage( 35, y, 0xD2 );
					else
						AddGoldenButton( 35, y, 1 + i );

					AddBorderedText( 60, y, 200, 20, "(Empty)", LabelColor32, BlackColor32 );
				}
			}
			#endregion

			y += 8;
			AddImageTiled( 32, y-1, 264, 1, 9107 );
			AddImageTiled( 42, y+1, 264, 1, 9157 );
			y += 8;

			AddRadio( 24, y, 9727, 9730, true, 1 );
			AddBorderedText( 60, y+5, 250, 20, "Yes, I wish to join the tournament.", LabelColor32, BlackColor32 );
			y += 35;

			AddRadio( 24, y, 9727, 9730, false, 2 );
			AddBorderedText( 60, y+5, 250, 20, "No, I do not wish to join.", LabelColor32, BlackColor32 );
			y += 35;

			y -= 3;
			AddButton( 314, y, 247, 248, 1, GumpButtonType.Reply, 0 );
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			if ( info.ButtonID == 1 && info.IsSwitched( 1 ) )
			{
				Tournament tourny = m_Tournament;
				Mobile from = m_From;

				switch ( tourny.Stage )
				{
					case TournamentStage.Fighting:
					{
						if ( m_Registrar != null )
						{
							if ( m_Tournament.HasParticipant( from ) )
							{
								m_Registrar.PrivateOverheadMessage( MessageType.Regular,
									0x35, false, "Excuse me? You are already signed up.", from.NetState );
							}
							else
							{
								m_Registrar.PrivateOverheadMessage( MessageType.Regular,
									0x22, false, "The tournament has already begun. You are too late to signup now.", from.NetState );
							}
						}

						break;
					}
					case TournamentStage.Inactive:
					{
						if ( m_Registrar != null )
							m_Registrar.PrivateOverheadMessage( MessageType.Regular,
								0x35, false, "The tournament is closed.", from.NetState );

						break;
					}
					case TournamentStage.Signup:
					{
						if ( m_Players.Count != tourny.PlayersPerParticipant )
						{
							if ( m_Registrar != null )
							{
								m_Registrar.PrivateOverheadMessage( MessageType.Regular,
									0x35, false, "You have not yet chosen your team.", from.NetState );
							}

							m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );
							break;
						}

						Ladder ladder = Ladder.Instance;

						for ( int i = 0; i < m_Players.Count; ++i )
						{
							Mobile mob = (Mobile)m_Players[i];

							LadderEntry entry = ( ladder == null ? null : ladder.Find( mob ) );

							if ( entry != null && Ladder.GetLevel( entry.Experience ) < tourny.LevelRequirement )
							{
								if ( m_Registrar != null )
								{
									if ( mob == from )
									{
										m_Registrar.PrivateOverheadMessage( MessageType.Regular,
											0x35, false, "You have not yet proven yourself a worthy dueler.", from.NetState );
									}
									else
									{
										m_Registrar.PrivateOverheadMessage( MessageType.Regular,
											0x35, false, String.Format( "{0} has not yet proven themselves a worthy dueler.", mob.Name ), from.NetState );
									}
								}

								m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );
								return;
							}
							else if ( tourny.HasParticipant( mob ) )
							{
								if ( m_Registrar != null )
								{
									if ( mob == from )
									{
										m_Registrar.PrivateOverheadMessage( MessageType.Regular,
											0x35, false, "You have already entered this tournament.", from.NetState );
									}
									else
									{
										m_Registrar.PrivateOverheadMessage( MessageType.Regular,
											0x35, false, String.Format( "{0} has already entered this tournament.", mob.Name ), from.NetState );
									}
								}

								m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );
								return;
							}
							else if ( mob is PlayerMobile && ((PlayerMobile)mob).DuelContext != null )
							{
								if ( m_Registrar != null )
								{
									if ( mob == from )
									{
										m_Registrar.PrivateOverheadMessage( MessageType.Regular,
											0x35, false, "You are already assigned to a duel. You must yield it before joining this tournament.", from.NetState );
									}
									else
									{
										m_Registrar.PrivateOverheadMessage( MessageType.Regular,
											0x35, false, String.Format( "{0} is already assigned to a duel. They must yield it before joining this tournament.", mob.Name ), from.NetState );
									}
								}

								m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );
								return;
							}
						}

						if ( m_Registrar != null )
						{
							string fmt;

							if ( tourny.PlayersPerParticipant == 1 )
								fmt = "As you say m'{0}. I've written your name to the bracket. The tournament will begin {1}.";
							else if ( tourny.PlayersPerParticipant == 2 )
								fmt = "As you wish m'{0}. The tournament will begin {1}, but first you must name your partner.";
							else
								fmt = "As you wish m'{0}. The tournament will begin {1}, but first you must name your team.";

							string timeUntil;
							int minutesUntil = (int)Math.Round( ( (tourny.SignupStart + tourny.SignupPeriod) - DateTime.Now ).TotalMinutes );

							if ( minutesUntil == 0 )
								timeUntil = "momentarily";
							else
								timeUntil = String.Format( "in {0} minute{1}", minutesUntil, minutesUntil == 1 ? "" : "s" );

							m_Registrar.PrivateOverheadMessage( MessageType.Regular,
								0x35, false, String.Format( fmt, from.Female ? "Lady" : "Lord", timeUntil ), from.NetState );
						}

						TournyParticipant part = new TournyParticipant( from );
						part.Players.Clear();
						part.Players.AddRange( m_Players );

						tourny.Participants.Add( part );

						break;
					}
				}
			}
			else if ( info.ButtonID > 1 )
			{
				int index = info.ButtonID-1;

				if ( index > 0 && index < m_Players.Count )
				{
					m_Players.RemoveAt( index );
					m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );
				}
				else if ( m_Players.Count < m_Tournament.PlayersPerParticipant )
				{
					m_From.BeginTarget( 12, false, TargetFlags.None, new TargetCallback( AddPlayer_OnTarget ) );
					m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );
				}
			}
		}

		private void AddPlayer_OnTarget( Mobile from, object obj )
		{
			Mobile mob = obj as Mobile;

			if ( mob == null || mob == from )
			{
				m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

				if ( m_Registrar != null )
					m_Registrar.PrivateOverheadMessage( MessageType.Regular,
						0x22, false, "Excuse me?", from.NetState );
			}
			else if ( !mob.Player )
			{
				m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

				if ( mob.Body.IsHuman )
					mob.SayTo( from, 1005443 ); // Nay, I would rather stay here and watch a nail rust.
				else
					mob.SayTo( from, 1005444 ); // The creature ignores your offer.
			}
			else if ( AcceptDuelGump.IsIgnored( mob, from ) || mob.Blessed )
			{
				m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

				if ( m_Registrar != null )
					m_Registrar.PrivateOverheadMessage( MessageType.Regular,
						0x22, false, "They ignore your invitation.", from.NetState );
			}
			else
			{
				PlayerMobile pm = mob as PlayerMobile;

				if ( pm == null )
					return;

				if ( pm.DuelContext != null )
				{
					m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

					if ( m_Registrar != null )
						m_Registrar.PrivateOverheadMessage( MessageType.Regular,
							0x22, false, "They are already assigned to another duel.", from.NetState );
				}
				else if ( mob.HasGump( typeof( AcceptTeamGump ) ) )
				{
					m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

					if ( m_Registrar != null )
						m_Registrar.PrivateOverheadMessage( MessageType.Regular,
							0x22, false, "They have already been offered a partnership.", from.NetState );
				}
				else if ( mob.HasGump( typeof( ConfirmSignupGump ) ) )
				{
					m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

					if ( m_Registrar != null )
						m_Registrar.PrivateOverheadMessage( MessageType.Regular,
							0x22, false, "They are already trying to join this tournament.", from.NetState );
				}
				else if ( m_Players.Contains( mob ) )
				{
					m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

					if ( m_Registrar != null )
						m_Registrar.PrivateOverheadMessage( MessageType.Regular,
							0x22, false, "You have already named them as a team member.", from.NetState );
				}
				else if ( m_Tournament.HasParticipant( mob ) )
				{
					m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

					if ( m_Registrar != null )
						m_Registrar.PrivateOverheadMessage( MessageType.Regular,
							0x22, false, "They have already entered this tournament.", from.NetState );
				}
				else if ( m_Players.Count >= m_Tournament.PlayersPerParticipant )
				{
					m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

					if ( m_Registrar != null )
						m_Registrar.PrivateOverheadMessage( MessageType.Regular,
							0x22, false, "Your team is full.", from.NetState );
				}
				else
				{
					m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );
					mob.SendGump( new AcceptTeamGump( from, mob, m_Tournament, m_Registrar, m_Players ) );

					if ( m_Registrar != null )
						m_Registrar.PrivateOverheadMessage( MessageType.Regular,
							0x59, false, String.Format( "As you command m'{0}. I've given your offer to {1}.", from.Female ? "Lady" : "Lord", mob.Name ), from.NetState );
				}
			}
		}
	}

	public class AcceptTeamGump : Gump
	{
		private bool m_Active;

		private Mobile m_From;
		private Mobile m_Requested;
		private Tournament m_Tournament;
		private Mobile m_Registrar;
		private ArrayList m_Players;

		private const int BlackColor32 = 0x000008;
		private const int LabelColor32 = 0xFFFFFF;

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

		public AcceptTeamGump( Mobile from, Mobile requested, Tournament tourny, Mobile registrar, ArrayList players ) : base( 50, 50 )
		{
			m_From = from;
			m_Requested = requested;
			m_Tournament = tourny;
			m_Registrar = registrar;
			m_Players = players;

			m_Active = true;

			#region Rules
			Ruleset ruleset = tourny.Ruleset;
			Ruleset basedef = ruleset.Base;

			int height = 185 + 35 + 60 + 12;

			int changes = 0;

			BitArray defs;

			if ( ruleset.Flavors.Count > 0 )
			{
				defs = new BitArray( basedef.Options );

				for ( int i = 0; i < ruleset.Flavors.Count; ++i )
					defs.Or( ((Ruleset)ruleset.Flavors[i]).Options );

				height += ruleset.Flavors.Count * 18;
			}
			else
			{
				defs = basedef.Options;
			}

			BitArray opts = ruleset.Options;

			for ( int i = 0; i < opts.Length; ++i )
			{
				if ( defs[i] != opts[i] )
					++changes;
			}

			height += (changes * 22);

			height += 10 + 22 + 25 + 25;
			#endregion

			Closable = false;

			AddPage( 0 );

			AddBackground( 1, 1, 398, height, 3600 );

			AddImageTiled( 16, 15, 369, height - 29, 3604 );
			AddAlphaRegion( 16, 15, 369, height - 29 );

			AddImage( 215, -43, 0xEE40 );

			StringBuilder sb = new StringBuilder();

			if ( tourny.TournyType == TournyType.FreeForAll )
			{
				sb.Append( "FFA" );
			}
			else if ( tourny.TournyType == TournyType.RandomTeam )
			{
				sb.Append( tourny.ParticipantsPerMatch );
				sb.Append( "-Team" );
			}
			else if ( tourny.TournyType == TournyType.RedVsBlue )
			{
				sb.Append( "Red v Blue" );
			}
			else
			{
				for ( int i = 0; i < tourny.ParticipantsPerMatch; ++i )
				{
					if ( sb.Length > 0 )
						sb.Append( 'v' );

					sb.Append( tourny.PlayersPerParticipant );
				}
			}

			if ( tourny.EventController != null )
				sb.Append( ' ' ).Append( tourny.EventController.Title );

			sb.Append( " Tournament Invitation" );

			AddBorderedText( 22, 22, 294, 20, Center( sb.ToString() ), LabelColor32, BlackColor32 );

			AddBorderedText( 22, 50, 294, 40,
				String.Format( "You have been asked to partner with {0} in a tournament. Do you accept?", from.Name ),
				0xB0C868, BlackColor32 );

			AddImageTiled( 32, 88, 264, 1, 9107 );
			AddImageTiled( 42, 90, 264, 1, 9157 );

			#region Rules
			int y = 100;

			string groupText = null;

			switch ( tourny.GroupType )
			{
				case GroupingType.HighVsLow: groupText = "High vs Low"; break;
				case GroupingType.Nearest: groupText = "Closest opponent"; break;
				case GroupingType.Random: groupText = "Random"; break;
			}

			AddBorderedText( 35, y, 190, 20, String.Format( "Grouping: {0}", groupText ), LabelColor32, BlackColor32 );
			y += 20;

			string tieText = null;

			switch ( tourny.TieType )
			{
				case TieType.Random: tieText = "Random"; break;
				case TieType.Highest: tieText = "Highest advances"; break;
				case TieType.Lowest: tieText = "Lowest advances"; break;
				case TieType.FullAdvancement: tieText = ( tourny.ParticipantsPerMatch == 2 ? "Both advance" : "Everyone advances" ); break;
				case TieType.FullElimination: tieText = ( tourny.ParticipantsPerMatch == 2 ? "Both eliminated" : "Everyone eliminated" ); break;
			}

			AddBorderedText( 35, y, 190, 20, String.Format( "Tiebreaker: {0}", tieText ), LabelColor32, BlackColor32 );
			y += 20;

			string sdText = "Off";

			if ( tourny.SuddenDeath > TimeSpan.Zero )
			{
				sdText = String.Format( "{0}:{1:D2}", (int) tourny.SuddenDeath.TotalMinutes, tourny.SuddenDeath.Seconds );

				if ( tourny.SuddenDeathRounds > 0 )
					sdText = String.Format( "{0} (first {1} rounds)", sdText, tourny.SuddenDeathRounds );
				else
					sdText = String.Format( "{0} (all rounds)", sdText );
			}

			AddBorderedText( 35, y, 240, 20, String.Format( "Sudden Death: {0}", sdText ), LabelColor32, BlackColor32 );
			y += 20;

			y += 6;
			AddImageTiled( 32, y-1, 264, 1, 9107 );
			AddImageTiled( 42, y+1, 264, 1, 9157 );
			y += 6;

			AddBorderedText( 35, y, 190, 20, String.Format( "Ruleset: {0}", basedef.Title ), LabelColor32, BlackColor32 );
			y += 20;

			for ( int i = 0; i < ruleset.Flavors.Count; ++i, y += 18 )
				AddBorderedText( 35, y, 190, 20, String.Format( " + {0}", ((Ruleset)ruleset.Flavors[i]).Title ), LabelColor32, BlackColor32 );

			y += 4;

			if ( changes > 0 )
			{
				AddBorderedText( 35, y, 190, 20, "Modifications:", LabelColor32, BlackColor32 );
				y += 20;

				for ( int i = 0; i < opts.Length; ++i )
				{
					if ( defs[i] != opts[i] )
					{
						string name = ruleset.Layout.FindByIndex( i );

						if ( name != null ) // sanity
						{
							AddImage( 35, y, opts[i] ? 0xD3 : 0xD2 );
							AddBorderedText( 60, y, 165, 22, name, LabelColor32, BlackColor32 );
						}

						y += 22;
					}
				}
			}
			else
			{
				AddBorderedText( 35, y, 190, 20, "Modifications: None", LabelColor32, BlackColor32 );
				y += 20;
			}
			#endregion

			y += 8;
			AddImageTiled( 32, y-1, 264, 1, 9107 );
			AddImageTiled( 42, y+1, 264, 1, 9157 );
			y += 8;

			AddRadio( 24, y, 9727, 9730, true, 1 );
			AddBorderedText( 60, y+5, 250, 20, "Yes, I will join them.", LabelColor32, BlackColor32 );
			y += 35;

			AddRadio( 24, y, 9727, 9730, false, 2 );
			AddBorderedText( 60, y+5, 250, 20, "No, I do not wish to fight.", LabelColor32, BlackColor32 );
			y += 35;

			AddRadio( 24, y, 9727, 9730, false, 3 );
			AddBorderedText( 60, y+5, 270, 20, "No, most certainly not. Do not ask again.", LabelColor32, BlackColor32 );
			y += 35;

			y -= 3;
			AddButton( 314, y, 247, 248, 1, GumpButtonType.Reply, 0 );

			Timer.DelayCall( TimeSpan.FromSeconds( 15.0 ), new TimerCallback( AutoReject ) );
		}

		public void AutoReject()
		{
			if ( !m_Active )
				return;

			m_Active = false;

			m_Requested.CloseGump( typeof( AcceptTeamGump ) );
			m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

			if ( m_Registrar != null )
			{
				m_Registrar.PrivateOverheadMessage( MessageType.Regular,
					0x22, false, String.Format( "{0} seems unresponsive.", m_Requested.Name ), m_From.NetState );

				m_Registrar.PrivateOverheadMessage( MessageType.Regular,
					0x22, false, String.Format( "You have declined the partnership with {0}.", m_From.Name ), m_Requested.NetState );
			}
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			Mobile from = m_From;
			Mobile mob = m_Requested;

			if ( info.ButtonID != 1 || !m_Active )
				return;

			m_Active = false;

			if ( info.IsSwitched( 1 ) )
			{
				PlayerMobile pm = mob as PlayerMobile;

				if ( pm == null )
					return;

				if ( AcceptDuelGump.IsIgnored( mob, from ) || mob.Blessed )
				{
					m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

					if ( m_Registrar != null )
						m_Registrar.PrivateOverheadMessage( MessageType.Regular,
							0x22, false, "They ignore your invitation.", from.NetState );
				}
				else if ( pm.DuelContext != null )
				{
					m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

					if ( m_Registrar != null )
						m_Registrar.PrivateOverheadMessage( MessageType.Regular,
							0x22, false, "They are already assigned to another duel.", from.NetState );
				}
				else if ( m_Players.Contains( mob ) )
				{
					m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

					if ( m_Registrar != null )
						m_Registrar.PrivateOverheadMessage( MessageType.Regular,
							0x22, false, "You have already named them as a team member.", from.NetState );
				}
				else if ( m_Tournament.HasParticipant( mob ) )
				{
					m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

					if ( m_Registrar != null )
						m_Registrar.PrivateOverheadMessage( MessageType.Regular,
							0x22, false, "They have already entered this tournament.", from.NetState );
				}
				else if ( m_Players.Count >= m_Tournament.PlayersPerParticipant )
				{
					m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

					if ( m_Registrar != null )
						m_Registrar.PrivateOverheadMessage( MessageType.Regular,
							0x22, false, "Your team is full.", from.NetState );
				}
				else
				{
					m_Players.Add( mob );

					m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

					if ( m_Registrar != null )
					{
						m_Registrar.PrivateOverheadMessage( MessageType.Regular,
							0x59, false, String.Format( "{0} has accepted your offer of partnership.", mob.Name ), from.NetState );

						m_Registrar.PrivateOverheadMessage( MessageType.Regular,
							0x59, false, String.Format( "You have accepted the partnership with {0}.", from.Name ), mob.NetState );
					}
				}
			}
			else
			{
				if ( info.IsSwitched( 3 ) )
					AcceptDuelGump.BeginIgnore( m_Requested, m_From );

				m_From.SendGump( new ConfirmSignupGump( m_From, m_Registrar, m_Tournament, m_Players ) );

				if ( m_Registrar != null )
				{
					m_Registrar.PrivateOverheadMessage( MessageType.Regular,
						0x22, false, String.Format( "{0} has declined your offer of partnership.", mob.Name ), from.NetState );

					m_Registrar.PrivateOverheadMessage( MessageType.Regular,
						0x22, false, String.Format( "You have declined the partnership with {0}.", from.Name ), mob.NetState );
				}
			}
		}
	}

	public class TournamentController : Item
	{
		private Tournament m_Tournament;

		[CommandProperty( AccessLevel.GameMaster )]
		public Tournament Tournament{ get{ return m_Tournament; } set{} }

		private static ArrayList m_Instances = new ArrayList();

		public static bool IsActive
		{
			get
			{
				for ( int i = 0; i < m_Instances.Count; ++i )
				{
					TournamentController controller = (TournamentController)m_Instances[i];

					if ( controller != null && !controller.Deleted && controller.Tournament != null && controller.Tournament.Stage!=TournamentStage.Inactive )
						return true;
				}

				return false;
			}
		}

		public override string DefaultName
		{
			get { return "tournament controller"; }
		}

		[Constructable]
		public TournamentController() : base( 0x1B7A )
		{
			Visible = false;
			Movable = false;

			m_Tournament = new Tournament();
			m_Instances.Add( this );
		}

		public override void GetContextMenuEntries( Mobile from, List<ContextMenuEntry> list )
		{
			base.GetContextMenuEntries( from, list );

			if ( from.AccessLevel >= AccessLevel.GameMaster && m_Tournament != null )
			{
				list.Add( new EditEntry( m_Tournament ) );

				if ( m_Tournament.CurrentStage == TournamentStage.Inactive )
					list.Add( new StartEntry( m_Tournament ) );
			}
		}

		private class EditEntry : ContextMenuEntry
		{
			private Tournament m_Tournament;

			public EditEntry( Tournament tourny ) : base( 5101 )
			{
				m_Tournament = tourny;
			}

			public override void OnClick()
			{
				Owner.From.SendGump( new PropertiesGump( Owner.From, m_Tournament ) );
			}
		}

		private class StartEntry : ContextMenuEntry
		{
			private Tournament m_Tournament;

			public StartEntry( Tournament tourny ) : base( 5113 )
			{
				m_Tournament = tourny;
			}

			public override void OnClick()
			{
				if ( m_Tournament.Stage == TournamentStage.Inactive )
				{
					m_Tournament.SignupStart = DateTime.Now;
					m_Tournament.Stage = TournamentStage.Signup;
					m_Tournament.Participants.Clear();
					m_Tournament.Pyramid.Levels.Clear();
					m_Tournament.Alert( "Hear ye! Hear ye!", "Tournament signup has opened. You can enter by signing up with the registrar." );
				}
			}
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.AccessLevel >= AccessLevel.GameMaster && m_Tournament != null )
			{
				from.CloseGump( typeof( PickRulesetGump ) );
				from.CloseGump( typeof( RulesetGump ) );
				from.SendGump( new PickRulesetGump( from, null, m_Tournament.Ruleset ) );
			}
		}

		public TournamentController( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );

			m_Tournament.Serialize( writer );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_Tournament = new Tournament( reader );
					break;
				}
			}

			m_Instances.Add( this );
		}

		public override void OnDelete()
		{
			base.OnDelete();

			m_Instances.Remove( this );
		}
	}

	public enum TournyType
	{
		Standard,
		FreeForAll,
		RandomTeam,
		RedVsBlue
	}

	[PropertyObject]
	public class Tournament
	{
		private int m_ParticipantsPerMatch;
		private int m_PlayersPerParticipant;
		private int m_LevelRequirement;
		private TournyPyramid m_Pyramid;
		private Ruleset m_Ruleset;

		private ArrayList m_Arenas;
		private ArrayList m_Participants;
		private ArrayList m_Undefeated;

		private TimeSpan m_SignupPeriod;
		private DateTime m_SignupStart;

		private TournamentStage m_Stage;

		private GroupingType m_GroupType;
		private TieType m_TieType;
		private TimeSpan m_SuddenDeath;

		private TournyType m_TournyType;

		private int m_SuddenDeathRounds;

		private EventController m_EventController;

		public bool IsNotoRestricted { get { return ( m_TournyType != TournyType.Standard ); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public EventController EventController
		{
			get { return m_EventController; }
			set { m_EventController = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int SuddenDeathRounds
		{
			get{ return m_SuddenDeathRounds; }
			set{ m_SuddenDeathRounds = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TournyType TournyType
		{
			get{ return m_TournyType; }
			set{ m_TournyType = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public GroupingType GroupType
		{
			get{ return m_GroupType; }
			set{ m_GroupType = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TieType TieType
		{
			get{ return m_TieType; }
			set{ m_TieType = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan SuddenDeath
		{
			get{ return m_SuddenDeath; }
			set{ m_SuddenDeath = value; }
		}

		public Ruleset Ruleset
		{
			get{ return m_Ruleset; }
			set{ m_Ruleset = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int ParticipantsPerMatch
		{
			get{ return m_ParticipantsPerMatch; }
			set{ if ( value < 2 ) value = 2; else if ( value > 10 ) value = 10; m_ParticipantsPerMatch = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int PlayersPerParticipant
		{
			get{ return m_PlayersPerParticipant; }
			set{ if ( value < 1 ) value = 1; else if ( value > 10 ) value = 10; m_PlayersPerParticipant = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int LevelRequirement
		{
			get{ return m_LevelRequirement; }
			set{ m_LevelRequirement = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan SignupPeriod
		{
			get{ return m_SignupPeriod; }
			set{ m_SignupPeriod = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public DateTime SignupStart
		{
			get{ return m_SignupStart; }
			set{ m_SignupStart = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TournamentStage CurrentStage
		{
			get{ return m_Stage; }
		}

		public TournamentStage Stage
		{
			get{ return m_Stage; }
			set{ m_Stage = value; }
		}

		public TournyPyramid Pyramid
		{
			get{ return m_Pyramid; }
			set{ m_Pyramid = value; }
		}

		public ArrayList Arenas
		{
			get{ return m_Arenas; }
			set{ m_Arenas = value; }
		}

		public ArrayList Participants
		{
			get{ return m_Participants; }
			set{ m_Participants = value; }
		}

		public ArrayList Undefeated
		{
			get{ return m_Undefeated; }
			set{ m_Undefeated = value; }
		}

		public bool HasParticipant( Mobile mob )
		{
			for ( int i = 0; i < m_Participants.Count; ++i )
			{
				TournyParticipant part = (TournyParticipant)m_Participants[i];

				if ( part.Players.Contains( mob ) )
					return true;
			}

			return false;
		}

		public void Serialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 4 ); // version

			writer.Write( (Item) m_EventController );

			writer.WriteEncodedInt( (int) m_SuddenDeathRounds );

			writer.WriteEncodedInt( (int) m_TournyType );

			writer.WriteEncodedInt( (int) m_GroupType );
			writer.WriteEncodedInt( (int) m_TieType );
			writer.Write( (TimeSpan) m_SuddenDeath );

			writer.WriteEncodedInt( (int) m_ParticipantsPerMatch );
			writer.WriteEncodedInt( (int) m_PlayersPerParticipant );
			writer.Write( (TimeSpan) m_SignupPeriod );
		}

		public Tournament( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			switch ( version )
			{
				case 4:
				{
					m_EventController = reader.ReadItem() as EventController;

					goto case 3;
				}
				case 3:
				{
					m_SuddenDeathRounds = reader.ReadEncodedInt();

					goto case 2;
				}
				case 2:
				{
					m_TournyType = (TournyType)reader.ReadEncodedInt();

					goto case 1;
				}
				case 1:
				{
					m_GroupType = (GroupingType)reader.ReadEncodedInt();
					m_TieType = (TieType)reader.ReadEncodedInt();
					m_SignupPeriod = reader.ReadTimeSpan();

					goto case 0;
				}
				case 0:
				{
					if ( version < 3 )
						m_SuddenDeathRounds = 3;

					m_ParticipantsPerMatch = reader.ReadEncodedInt();
					m_PlayersPerParticipant = reader.ReadEncodedInt();
					m_SignupPeriod = reader.ReadTimeSpan();
					m_Stage = TournamentStage.Inactive;
					m_Pyramid = new TournyPyramid();
					m_Ruleset = new Ruleset( RulesetLayout.Root );
					m_Ruleset.ApplyDefault( m_Ruleset.Layout.Defaults[0] );
					m_Participants = new ArrayList();
					m_Undefeated = new ArrayList();
					m_Arenas = new ArrayList();

					break;
				}
			}

			Timer.DelayCall( SliceInterval, SliceInterval, new TimerCallback( Slice ) );
		}

		public Tournament()
		{
			m_ParticipantsPerMatch = 2;
			m_PlayersPerParticipant = 1;
			m_Pyramid = new TournyPyramid();
			m_Ruleset = new Ruleset( RulesetLayout.Root );
			m_Ruleset.ApplyDefault( m_Ruleset.Layout.Defaults[0] );
			m_Participants = new ArrayList();
			m_Undefeated = new ArrayList();
			m_Arenas = new ArrayList();
			m_SignupPeriod = TimeSpan.FromMinutes( 10.0 );

			Timer.DelayCall( SliceInterval, SliceInterval, new TimerCallback( Slice ) );
		}

		public void HandleTie( Arena arena, TournyMatch match, ArrayList remaining )
		{
			if ( remaining.Count == 1 )
				HandleWon( arena, match, (TournyParticipant)remaining[0] );

			if ( remaining.Count < 2 )
				return;

			StringBuilder sb = new StringBuilder();

			sb.Append( "The match has ended in a tie " );

			if ( remaining.Count == 2 )
				sb.Append( "between " );
			else
				sb.Append( "among " );

			sb.Append( remaining.Count );

			if ( ((TournyParticipant)remaining[0]).Players.Count == 1 )
				sb.Append( " players: " );
			else
				sb.Append( " teams: " );

			bool hasAppended = false;

			for ( int j = 0; j < match.Participants.Count; ++j )
			{
				TournyParticipant part = (TournyParticipant)match.Participants[j];

				if ( remaining.Contains( part ) )
				{
					if ( hasAppended )
						sb.Append( ", " );

					sb.Append( part.NameList );
					hasAppended = true;
				}
				else
				{
					m_Undefeated.Remove( part );
				}
			}

			sb.Append( ". " );

			string whole = ( remaining.Count == 2 ? "both" : "all" );

			TieType tieType = m_TieType;

			if ( tieType == TieType.FullElimination && remaining.Count >= m_Undefeated.Count )
				tieType = TieType.FullAdvancement;

			switch ( m_TieType )
			{
				case TieType.FullAdvancement:
				{
					sb.AppendFormat( "In accordance with the rules, {0} parties are advanced.", whole );
					break;
				}
				case TieType.FullElimination:
				{
					for ( int j = 0; j < remaining.Count; ++j )
						m_Undefeated.Remove( remaining[j] );

					sb.AppendFormat( "In accordance with the rules, {0} parties are eliminated.", whole );
					break;
				}
				case TieType.Random:
				{
					TournyParticipant advanced = (TournyParticipant)remaining[Utility.Random( remaining.Count )];

					for ( int i = 0; i < remaining.Count; ++i )
					{
						if ( remaining[i] != advanced )
							m_Undefeated.Remove( remaining[i] );
					}

					if ( advanced != null )
						sb.AppendFormat( "In accordance with the rules, {0} {1} advanced.", advanced.NameList, advanced.Players.Count == 1 ? "is" : "are" );

					break;
				}
				case TieType.Highest:
				{
					TournyParticipant advanced = null;

					for ( int i = 0; i < remaining.Count; ++i )
					{
						TournyParticipant part = (TournyParticipant)remaining[i];

						if ( advanced == null || part.TotalLadderXP > advanced.TotalLadderXP )
							advanced = part;
					}

					for ( int i = 0; i < remaining.Count; ++i )
					{
						if ( remaining[i] != advanced )
							m_Undefeated.Remove( remaining[i] );
					}

					if ( advanced != null )
						sb.AppendFormat( "In accordance with the rules, {0} {1} advanced.", advanced.NameList, advanced.Players.Count == 1 ? "is" : "are" );

					break;
				}
				case TieType.Lowest:
				{
					TournyParticipant advanced = null;

					for ( int i = 0; i < remaining.Count; ++i )
					{
						TournyParticipant part = (TournyParticipant)remaining[i];

						if ( advanced == null || part.TotalLadderXP < advanced.TotalLadderXP )
							advanced = part;
					}

					for ( int i = 0; i < remaining.Count; ++i )
					{
						if ( remaining[i] != advanced )
							m_Undefeated.Remove( remaining[i] );
					}

					if ( advanced != null )
						sb.AppendFormat( "In accordance with the rules, {0} {1} advanced.", advanced.NameList, advanced.Players.Count == 1 ? "is" : "are" );

					break;
				}
			}

			Alert( arena, sb.ToString() );
		}

		public void OnEliminated( DuelPlayer player )
		{
			Participant part = player.Participant;

			if ( !part.Eliminated )
				return;

			if ( m_TournyType == TournyType.FreeForAll )
			{
				int rem = 0;

				for ( int i = 0; i < part.Context.Participants.Count; ++i )
				{
					Participant check = (Participant)part.Context.Participants[i];

					if ( check != null && !check.Eliminated )
						++rem;
				}

				TournyParticipant tp = part.TournyPart;

				if ( tp == null )
					return;

				if ( rem == 1 )
					GiveAwards( tp.Players, TrophyRank.Silver, ComputeCashAward() / 2 );
				else if ( rem == 2 )
					GiveAwards( tp.Players, TrophyRank.Bronze, ComputeCashAward() / 4 );
			}
		}

		public void HandleWon( Arena arena, TournyMatch match, TournyParticipant winner )
		{
			StringBuilder sb = new StringBuilder();

			sb.Append( "The match is complete. " );
			sb.Append( winner.NameList );

			if ( winner.Players.Count > 1 )
				sb.Append( " have bested " );
			else
				sb.Append( " has bested " );

			if ( match.Participants.Count > 2 )
				sb.AppendFormat( "{0} other {1}: ", match.Participants.Count - 1, winner.Players.Count == 1 ? "players" : "teams" );

			bool hasAppended = false;

			for ( int j = 0; j < match.Participants.Count; ++j )
			{
				TournyParticipant part = (TournyParticipant)match.Participants[j];

				if ( part == winner )
					continue;

				m_Undefeated.Remove( part );

				if ( hasAppended )
					sb.Append( ", " );

				sb.Append( part.NameList );
				hasAppended = true;
			}

			sb.Append( "." );

			if ( m_TournyType == TournyType.Standard )
				Alert( arena, sb.ToString() );
		}

		private static readonly TimeSpan SliceInterval = TimeSpan.FromSeconds( 12.0 );

		private int ComputeCashAward()
		{
			return m_Participants.Count * m_PlayersPerParticipant * 2500;
		}

		private void GiveAwards()
		{
			switch ( m_TournyType )
			{
				case TournyType.FreeForAll:
				{
					if ( m_Pyramid.Levels.Count < 1 )
						break;

					PyramidLevel top = m_Pyramid.Levels[m_Pyramid.Levels.Count - 1] as PyramidLevel;

					if ( top.FreeAdvance != null || top.Matches.Count != 1 )
						break;

					TournyMatch match = top.Matches[0] as TournyMatch;
					TournyParticipant winner = match.Winner;

					if ( winner != null )
						GiveAwards( winner.Players, TrophyRank.Gold, ComputeCashAward() );

					break;
				}
				case TournyType.Standard:
				{
					if ( m_Pyramid.Levels.Count < 2 )
						break;

					PyramidLevel top = m_Pyramid.Levels[m_Pyramid.Levels.Count - 1] as PyramidLevel;

					if ( top.FreeAdvance != null || top.Matches.Count != 1 )
						break;

					int cash = ComputeCashAward();

					TournyMatch match = top.Matches[0] as TournyMatch;
					TournyParticipant winner = match.Winner;

					for ( int i = 0; i < match.Participants.Count; ++i )
					{
						TournyParticipant part = (TournyParticipant) match.Participants[i];

						if ( part == winner )
							GiveAwards( part.Players, TrophyRank.Gold, cash );
						else
							GiveAwards( part.Players, TrophyRank.Silver, cash / 2 );
					}

					PyramidLevel next = m_Pyramid.Levels[m_Pyramid.Levels.Count - 2] as PyramidLevel;

					if ( next.Matches.Count > 2 )
						break;

					for ( int i = 0; i < next.Matches.Count; ++i )
					{
						match = (TournyMatch)next.Matches[i];
						winner = match.Winner;

						for ( int j = 0; j < match.Participants.Count; ++j )
						{
							TournyParticipant part = (TournyParticipant) match.Participants[j];

							if ( part != winner )
								GiveAwards( part.Players, TrophyRank.Bronze, cash / 4 );
						}
					}

					break;
				}
			}
		}

		private void GiveAwards( ArrayList players, TrophyRank rank, int cash )
		{
			if ( players.Count == 0 )
				return;

			if ( players.Count > 1 )
				cash /= ( players.Count - 1 );

			cash +=  500;
			cash /= 1000;
			cash *= 1000;

			StringBuilder sb = new StringBuilder();

			if ( m_TournyType == TournyType.FreeForAll )
			{
				sb.Append( m_Participants.Count * m_PlayersPerParticipant );
				sb.Append( "-man FFA" );
			}
			else if ( m_TournyType == TournyType.RandomTeam )
			{
				sb.Append( m_ParticipantsPerMatch );
				sb.Append( "-team" );
			}
			else if ( m_TournyType == TournyType.RedVsBlue )
			{
				sb.Append( "Red v Blue" );
			}
			else
			{
				for ( int i = 0; i < m_ParticipantsPerMatch; ++i )
				{
					if ( sb.Length > 0 )
						sb.Append( 'v' );

					sb.Append( m_PlayersPerParticipant );
				}
			}

			if ( m_EventController != null )
				sb.Append( ' ' ).Append( m_EventController.Title );

			sb.Append( " Champion" );

			string title = sb.ToString();

			for ( int i = 0; i < players.Count; ++i )
			{
				Mobile mob = (Mobile) players[i];

				if ( mob == null || mob.Deleted )
					continue;

				Item item = new Trophy( title, rank );

				if ( !mob.PlaceInBackpack( item ) )
					mob.BankBox.DropItem( item );

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

		public void Slice()
		{
			if ( m_Stage == TournamentStage.Signup )
			{
				TimeSpan until = ( m_SignupStart + m_SignupPeriod ) - DateTime.Now;

				if ( until <= TimeSpan.Zero )
				{
					for ( int i = m_Participants.Count - 1; i >= 0; --i )
					{
						TournyParticipant part = (TournyParticipant)m_Participants[i];
						bool bad = false;

						for ( int j = 0; j < part.Players.Count; ++j )
						{
							Mobile check = (Mobile) part.Players[j];

							if ( check.Deleted || check.Map == null || check.Map == Map.Internal || !check.Alive || Factions.Sigil.ExistsOn( check ) || check.Region.IsPartOf( typeof( Regions.Jail ) ) )
							{
								bad = true;
								break;
							}
						}

						if ( bad )
						{
							for ( int j = 0; j < part.Players.Count; ++j )
								((Mobile)part.Players[j]).SendMessage( "You have been disqualified from the tournament." );

							m_Participants.RemoveAt( i );
						}
					}

					if ( m_Participants.Count >= 2 )
					{
						m_Stage = TournamentStage.Fighting;

						m_Undefeated.Clear();

						m_Pyramid.Levels.Clear();
						m_Pyramid.AddLevel( m_ParticipantsPerMatch, m_Participants, m_GroupType, m_TournyType );

						PyramidLevel level = (PyramidLevel)m_Pyramid.Levels[0];

						if ( level.FreeAdvance != null )
							m_Undefeated.Add( level.FreeAdvance );

						for ( int i = 0; i < level.Matches.Count; ++i )
						{
							TournyMatch match = (TournyMatch)level.Matches[i];

							m_Undefeated.AddRange( match.Participants );
						}

						Alert( "Hear ye! Hear ye!", "The tournament will begin shortly." );
					}
					else
					{
						Alert( "Is this all?", "Pitiful. Signup extended." );
						m_SignupStart = DateTime.Now;
					}
				}
				else if ( Math.Abs( until.TotalSeconds - TimeSpan.FromMinutes( 1.0 ).TotalSeconds ) < (SliceInterval.TotalSeconds/2) )
				{
					Alert( "Last call!", "If you wish to enter the tournament, sign up with the registrar now." );
				}
				else if ( Math.Abs( until.TotalSeconds - TimeSpan.FromMinutes( 5.0 ).TotalSeconds ) < (SliceInterval.TotalSeconds/2) )
				{
					Alert( "The tournament will begin in 5 minutes.", "Sign up now before it's too late." );
				}
			}
			else if ( m_Stage == TournamentStage.Fighting )
			{
				if ( m_Undefeated.Count == 1 )
				{
					TournyParticipant winner = (TournyParticipant)m_Undefeated[0];

					try
					{
						if ( m_EventController != null )
							Alert( "The tournament has completed!", String.Format( "Team {0} has won!", m_EventController.GetTeamName( ((TournyMatch)((PyramidLevel)m_Pyramid.Levels[0]).Matches[0]).Participants.IndexOf( winner ) ) ) );
						else if ( m_TournyType == TournyType.RandomTeam )
							Alert( "The tournament has completed!", String.Format( "Team {0} has won!", ((TournyMatch)((PyramidLevel)m_Pyramid.Levels[0]).Matches[0]).Participants.IndexOf( winner ) + 1 ) );
						else if ( m_TournyType == TournyType.RedVsBlue )
							Alert( "The tournament has completed!", String.Format( "Team {0} has won!", ((TournyMatch)((PyramidLevel)m_Pyramid.Levels[0]).Matches[0]).Participants.IndexOf( winner ) == 0 ? "Red" : "Blue" ) );
						else
							Alert( "The tournament has completed!", String.Format( "{0} {1} the champion{2}.", winner.NameList, winner.Players.Count > 1 ? "are" : "is", winner.Players.Count == 1 ? "" : "s" ) );
					}
					catch
					{
					}

					GiveAwards();

					m_Stage = TournamentStage.Inactive;
					m_Undefeated.Clear();
				}
				else if ( m_Pyramid.Levels.Count > 0 )
				{
					PyramidLevel activeLevel = (PyramidLevel)m_Pyramid.Levels[m_Pyramid.Levels.Count - 1];
					bool stillGoing = false;

					for ( int i = 0; i < activeLevel.Matches.Count; ++i )
					{
						TournyMatch match = (TournyMatch)activeLevel.Matches[i];

						if ( match.Winner == null )
						{
							stillGoing = true;

							if ( !match.InProgress )
							{
								for ( int j = 0; j < m_Arenas.Count; ++j )
								{
									Arena arena = (Arena)m_Arenas[j];

									if ( !arena.IsOccupied )
									{
										match.Start( arena, this );
										break;
									}
								}
							}
						}
					}

					if ( !stillGoing )
					{
						for ( int i = m_Undefeated.Count - 1; i >= 0; --i )
						{
							TournyParticipant part = (TournyParticipant)m_Undefeated[i];
							bool bad = false;

							for ( int j = 0; j < part.Players.Count; ++j )
							{
								Mobile check = (Mobile) part.Players[j];

								if ( check.Deleted || check.Map == null || check.Map == Map.Internal || !check.Alive || Factions.Sigil.ExistsOn( check ) || check.Region.IsPartOf( typeof( Regions.Jail ) ) )
								{
									bad = true;
									break;
								}
							}

							if ( bad )
							{
								for ( int j = 0; j < part.Players.Count; ++j )
									((Mobile)part.Players[j]).SendMessage( "You have been disqualified from the tournament." );

								m_Undefeated.RemoveAt( i );

								if ( m_Undefeated.Count == 1 )
								{
									TournyParticipant winner = (TournyParticipant)m_Undefeated[0];

									try
									{
										if ( m_EventController != null )
											Alert( "The tournament has completed!", String.Format( "Team {0} has won", m_EventController.GetTeamName( ( (TournyMatch) ( (PyramidLevel) m_Pyramid.Levels[0] ).Matches[0] ).Participants.IndexOf( winner ) ) ) );
										else if ( m_TournyType == TournyType.RandomTeam )
											Alert( "The tournament has completed!", String.Format( "Team {0} has won!", ((TournyMatch)((PyramidLevel)m_Pyramid.Levels[0]).Matches[0]).Participants.IndexOf( winner ) + 1 ) );
										else if ( m_TournyType == TournyType.RedVsBlue )
											Alert( "The tournament has completed!", String.Format( "Team {0} has won!", ((TournyMatch)((PyramidLevel)m_Pyramid.Levels[0]).Matches[0]).Participants.IndexOf( winner ) == 0 ? "Red" : "Blue" ) );
										else
											Alert( "The tournament has completed!", String.Format( "{0} {1} the champion{2}.", winner.NameList, winner.Players.Count > 1 ? "are" : "is", winner.Players.Count == 1 ? "" : "s" ) );
									}
									catch
									{
									}

									GiveAwards();

									m_Stage = TournamentStage.Inactive;
									m_Undefeated.Clear();
									break;
								}
							}
						}

						if ( m_Undefeated.Count > 1 )
							m_Pyramid.AddLevel( m_ParticipantsPerMatch, m_Undefeated, m_GroupType, m_TournyType );
					}
				}
			}
		}

		public void Alert( params string[] alerts )
		{
			for ( int i = 0; i < m_Arenas.Count; ++i )
				Alert( (Arena) m_Arenas[i], alerts );
		}

		public void Alert( Arena arena, params string[] alerts )
		{
			if ( arena != null && arena.Announcer != null )
			{
				for ( int j = 0; j < alerts.Length; ++j )
					Timer.DelayCall( TimeSpan.FromSeconds( Math.Max( j-0.5, 0.0 ) ), new TimerStateCallback( Alert_Callback ), new object[]{ arena.Announcer, alerts[j] } );
			}
		}

		private void Alert_Callback( object state )
		{
			object[] states = (object[])state;

			if ( states[0] != null )
				((Mobile)states[0]).PublicOverheadMessage( MessageType.Regular, 0x35, false, (string)states[1] );
		}
	}

	public class TournyPyramid
	{
		private ArrayList m_Levels;

		public ArrayList Levels
		{
			get{ return m_Levels; }
			set{ m_Levels = value; }
		}

		public TournyPyramid()
		{
			m_Levels = new ArrayList();
		}

		public void AddLevel( int partsPerMatch, ArrayList participants, GroupingType groupType, TournyType tournyType )
		{
			ArrayList copy = new ArrayList( participants );

			if ( groupType == GroupingType.Nearest || groupType == GroupingType.HighVsLow )
				copy.Sort();

			PyramidLevel level = new PyramidLevel();

			switch ( tournyType )
			{
				case TournyType.RedVsBlue:
				{
					TournyParticipant[] parts = new TournyParticipant[2];

					for ( int i = 0; i < parts.Length; ++i )
						parts[i] = new TournyParticipant( new ArrayList() );

					for ( int i = 0; i < copy.Count; ++i )
					{
						ArrayList players = ((TournyParticipant)copy[i]).Players;

						for ( int j = 0; j < players.Count; ++j )
						{
							Mobile mob = (Mobile) players[j];

							if ( mob.Kills >= 5 )
								parts[0].Players.Add( mob );
							else
								parts[1].Players.Add( mob );
						}
					}

					level.Matches.Add( new TournyMatch( new ArrayList( parts ) ) );
					break;
				}
				case TournyType.RandomTeam:
				{
					TournyParticipant[] parts = new TournyParticipant[partsPerMatch];

					for ( int i = 0; i < partsPerMatch; ++i )
						parts[i] = new TournyParticipant( new ArrayList() );

					for ( int i = 0; i < copy.Count; ++i )
						parts[i % parts.Length].Players.AddRange( ((TournyParticipant)copy[i]).Players );

					level.Matches.Add( new TournyMatch( new ArrayList( parts ) ) );
					break;
				}
				case TournyType.FreeForAll:
				{
					level.Matches.Add( new TournyMatch( copy ) );
					break;
				}
				case TournyType.Standard:
				{
					if ( partsPerMatch >= 2 && participants.Count % partsPerMatch == 1 )
					{
						int lowAdvances = int.MaxValue;

						for ( int i = 0; i < participants.Count; ++i )
						{
							TournyParticipant p = (TournyParticipant)participants[i];

							if ( p.FreeAdvances < lowAdvances )
								lowAdvances = p.FreeAdvances;
						}

						ArrayList toAdvance = new ArrayList();

						for ( int i = 0; i < participants.Count; ++i )
						{
							TournyParticipant p = (TournyParticipant)participants[i];

							if ( p.FreeAdvances == lowAdvances )
								toAdvance.Add( p );
						}

						if ( toAdvance.Count == 0 )
							toAdvance = copy; // sanity

						int idx = Utility.Random( toAdvance.Count );

						((TournyParticipant)toAdvance[idx]).AddLog( "Advanced automatically due to an odd number of challengers." );
						level.FreeAdvance = (TournyParticipant)toAdvance[idx];
						++level.FreeAdvance.FreeAdvances;
						copy.Remove( toAdvance[idx] );
					}

					while ( copy.Count >= partsPerMatch )
					{
						ArrayList thisMatch = new ArrayList();

						for ( int i = 0; i < partsPerMatch; ++i )
						{
							int idx = 0;

							switch ( groupType )
							{
								case GroupingType.HighVsLow: idx = (i * (copy.Count - 1)) / (partsPerMatch - 1); break;
								case GroupingType.Nearest: idx = 0; break;
								case GroupingType.Random: idx = Utility.Random( copy.Count ); break;
							}

							thisMatch.Add( copy[idx] );
							copy.RemoveAt( idx );
						}

						level.Matches.Add( new TournyMatch( thisMatch ) );
					}

					if ( copy.Count > 1 )
						level.Matches.Add( new TournyMatch( copy ) );

					break;
				}
			}

			m_Levels.Add( level );
		}
	}

	public class PyramidLevel
	{
		private ArrayList m_Matches;
		private TournyParticipant m_FreeAdvance;

		public ArrayList Matches
		{
			get{ return m_Matches; }
			set{ m_Matches = value; }
		}

		public TournyParticipant FreeAdvance
		{
			get{ return m_FreeAdvance; }
			set{ m_FreeAdvance = value; }
		}

		public PyramidLevel()
		{
			m_Matches = new ArrayList();
		}
	}

	public class TournyMatch
	{
		private ArrayList m_Participants;
		private TournyParticipant m_Winner;
		private DuelContext m_Context;

		public ArrayList Participants
		{
			get{ return m_Participants; }
			set{ m_Participants = value; }
		}

		public TournyParticipant Winner
		{
			get{ return m_Winner; }
			set{ m_Winner = value; }
		}

		public DuelContext Context
		{
			get{ return m_Context; }
			set{ m_Context = value; }
		}

		public bool InProgress
		{
			get{ return ( m_Context != null && m_Context.Registered ); }
		}

		public void Start( Arena arena, Tournament tourny )
		{
			TournyParticipant first = (TournyParticipant)m_Participants[0];

			DuelContext dc = new DuelContext( (Mobile)first.Players[0], tourny.Ruleset.Layout, false );
			dc.Ruleset.Options.SetAll(false);
			dc.Ruleset.Options.Or(tourny.Ruleset.Options);

			for ( int i = 0; i < m_Participants.Count; ++i )
			{
				TournyParticipant tournyPart = (TournyParticipant)m_Participants[i];
				Participant duelPart = new Participant( dc, tournyPart.Players.Count );

				duelPart.TournyPart = tournyPart;

				for ( int j = 0; j < tournyPart.Players.Count; ++j )
					duelPart.Add( (Mobile) tournyPart.Players[j] );

				for ( int j = 0; j < duelPart.Players.Length; ++j )
				{
					if ( duelPart.Players[j] != null )
						duelPart.Players[j].Ready = true;
				}

				dc.Participants.Add( duelPart );
			}

			if ( tourny.EventController != null )
				dc.m_EventGame = tourny.EventController.Construct( dc );

			dc.m_Tournament = tourny;
			dc.m_Match = this;

			dc.m_OverrideArena = arena;

			if ( tourny.SuddenDeath > TimeSpan.Zero && (tourny.SuddenDeathRounds == 0 || tourny.Pyramid.Levels.Count <= tourny.SuddenDeathRounds) )
				dc.StartSuddenDeath( tourny.SuddenDeath );

			dc.SendReadyGump( 0 );

			if ( dc.StartedBeginCountdown )
			{
				m_Context = dc;

				for ( int i = 0; i < m_Participants.Count; ++i )
				{
					TournyParticipant p = (TournyParticipant)m_Participants[i];

					for ( int j = 0; j < p.Players.Count; ++j )
					{
						Mobile mob = (Mobile)p.Players[j];

						foreach ( Mobile view in mob.GetMobilesInRange( 18 ) )
						{
							if ( !mob.CanSee( view ) )
								mob.Send( view.RemovePacket );
						}

						mob.LocalOverheadMessage( MessageType.Emote, 0x3B2, false, "* Your mind focuses intently on the fight and all other distractions fade away *" );
					}
				}
			}
			else
			{
				dc.Unregister();
				dc.StopCountdown();
			}
		}

		public TournyMatch( ArrayList participants )
		{
			m_Participants = participants;

			for ( int i = 0; i < participants.Count; ++i )
			{
				TournyParticipant part = (TournyParticipant)participants[i];

				StringBuilder sb = new StringBuilder();

				sb.Append( "Matched in a duel against " );

				if ( participants.Count > 2 )
					sb.AppendFormat( "{0} other {1}: ", participants.Count - 1, part.Players.Count == 1 ? "players" : "teams" );

				bool hasAppended = false;

				for ( int j = 0; j < participants.Count; ++j )
				{
					if ( i == j )
						continue;

					if ( hasAppended )
						sb.Append( ", " );

					sb.Append( ((TournyParticipant)participants[j]).NameList );
					hasAppended = true;
				}

				sb.Append( "." );

				part.AddLog( sb.ToString() );
			}
		}
	}

	public class TournyParticipant : IComparable
	{
		private ArrayList m_Players;
		private ArrayList m_Log;
		private int m_FreeAdvances;

		public ArrayList Players
		{
			get{ return m_Players; }
			set{ m_Players = value; }
		}

		public ArrayList Log
		{
			get{ return m_Log; }
			set{ m_Log = value; }
		}

		public int FreeAdvances
		{
			get{ return m_FreeAdvances; }
			set{ m_FreeAdvances = value; }
		}

		public int TotalLadderXP
		{
			get
			{
				Ladder ladder = Ladder.Instance;

				if ( ladder == null )
					return 0;

				int total = 0;

				for ( int i = 0; i < m_Players.Count; ++i )
				{
					Mobile mob = (Mobile)m_Players[i];
					LadderEntry entry = ladder.Find( mob );

					if ( entry != null )
						total += entry.Experience;
				}

				return total;
			}
		}

		public string NameList
		{
			get
			{
				StringBuilder sb = new StringBuilder();

				for ( int i = 0; i < m_Players.Count; ++i )
				{
					if ( m_Players[i] == null )
						continue;

					Mobile mob = (Mobile) m_Players[i];

					if ( sb.Length > 0 )
					{
						if ( m_Players.Count == 2 )
							sb.Append( " and " );
						else if ( (i+1) < m_Players.Count )
							sb.Append( ", " );
						else
							sb.Append( ", and " );
					}

					sb.Append( mob.Name );
				}

				if ( sb.Length == 0 )
					return "Empty";

				return sb.ToString();
			}
		}

		public void AddLog( string text )
		{
			m_Log.Add( text );
		}

		public void AddLog( string format, params object[] args )
		{
			AddLog( String.Format( format, args ) );
		}

		public void WonMatch( TournyMatch match )
		{
			AddLog( "Match won." );
		}

		public void LostMatch( TournyMatch match )
		{
			AddLog( "Match lost." );
		}

		public TournyParticipant( Mobile owner )
		{
			m_Log = new ArrayList();
			m_Players = new ArrayList();
			m_Players.Add( owner );
		}

		public TournyParticipant( ArrayList players )
		{
			m_Log = new ArrayList();
			m_Players = players;
		}

		public int CompareTo( object obj )
		{
			TournyParticipant p = (TournyParticipant)obj;

			return p.TotalLadderXP - this.TotalLadderXP;
		}
	}

	public enum TournyBracketGumpType
	{
		Index,
		Rules_Info,
		Participant_List,
		Participant_Info,
		Round_List,
		Round_Info,
		Match_Info,
		Player_Info
	}

	public class TournamentBracketGump : Gump
	{
		private Mobile m_From;
		private Tournament m_Tournament;
		private TournyBracketGumpType m_Type;
		private ArrayList m_List;
		private int m_Page;
		private int m_PerPage;
		private object m_Object;

		private const int BlackColor32 = 0x000008;
		private const int LabelColor32 = 0xFFFFFF;

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

		public void AddRightArrow( int x, int y, int bid, string text )
		{
			AddButton( x, y, 0x15E1, 0x15E5, bid, GumpButtonType.Reply, 0 );

			if ( text != null )
				AddHtml( x + 20, y - 1, 230, 20, text, false, false );
		}

		public void AddRightArrow( int x, int y, int bid )
		{
			AddRightArrow( x, y, bid, null );
		}

		public void AddLeftArrow( int x, int y, int bid, string text )
		{
			AddButton( x, y, 0x15E3, 0x15E7, bid, GumpButtonType.Reply, 0 );

			if ( text != null )
				AddHtml( x + 20, y - 1, 230, 20, text, false, false );
		}

		public void AddLeftArrow( int x, int y, int bid )
		{
			AddLeftArrow( x, y, bid, null );
		}

		public int ToButtonID( int type, int index )
		{
			return 1 + (index * 7) + type;
		}

		public bool FromButtonID( int bid, out int type, out int index )
		{
			type = ( bid - 1 ) % 7;
			index = ( bid - 1 ) / 7;
			return ( bid >= 1 );
		}

		public void StartPage( out int index, out int count, out int y, int perPage )
		{
			m_PerPage = perPage;

			index = Math.Max( m_Page * perPage, 0 );
			count = Math.Max( Math.Min( m_List.Count - index, perPage ), 0 );

			y = 53 + ((12 - perPage) * 18);

			if ( m_Page > 0 )
				AddLeftArrow( 242, 35, ToButtonID( 1, 0 ) );

			if ( (m_Page + 1) * perPage < m_List.Count )
				AddRightArrow( 260, 35, ToButtonID( 1, 1 ) );
		}

		public TournamentBracketGump( Mobile from, Tournament tourny, TournyBracketGumpType type, ArrayList list, int page, object obj ) : base( 50, 50 )
		{
			m_From = from;
			m_Tournament = tourny;
			m_Type = type;
			m_List = list;
			m_Page = page;
			m_Object = obj;
			m_PerPage = 12;

			switch ( type )
			{
				case TournyBracketGumpType.Index:
				{
					AddPage( 0 );
					AddBackground( 0, 0, 300, 300, 9380 );

					StringBuilder sb = new StringBuilder();

					if ( tourny.TournyType == TournyType.FreeForAll )
					{
						sb.Append( "FFA" );
					}
					else if ( tourny.TournyType == TournyType.RandomTeam )
					{
						sb.Append( "Team" );
					}
					else if ( tourny.TournyType == TournyType.RedVsBlue )
					{
						sb.Append( "Red v Blue" );
					}
					else
					{
						for ( int i = 0; i < tourny.ParticipantsPerMatch; ++i )
						{
							if ( sb.Length > 0 )
								sb.Append( 'v' );

							sb.Append( tourny.PlayersPerParticipant );
						}
					}

					if ( tourny.EventController != null )
						sb.Append( ' ' ).Append( tourny.EventController.Title );

					sb.Append( " Tournament Bracket" );

					AddHtml( 25, 35, 250, 20, Center( sb.ToString() ), false, false );

					AddRightArrow( 25, 53, ToButtonID( 0, 4 ), "Rules" );
					AddRightArrow( 25, 71, ToButtonID( 0, 1 ), "Participants" );

					if ( m_Tournament.Stage == TournamentStage.Signup )
					{
						TimeSpan until = ( m_Tournament.SignupStart + m_Tournament.SignupPeriod ) - DateTime.Now;
						string text;
						int secs = (int) until.TotalSeconds;

						if ( secs > 0 )
						{
							int mins = secs / 60;
							secs %= 60;

							if ( mins > 0 && secs > 0 )
								text = String.Format( "The tournament will begin in {0} minute{1} and {2} second{3}.", mins, mins==1?"":"s", secs, secs==1?"":"s" );
							else if ( mins > 0 )
								text = String.Format( "The tournament will begin in {0} minute{1}.", mins, mins==1?"":"s" );
							else if ( secs > 0 )
								text = String.Format( "The tournament will begin in {0} second{1}.", secs, secs==1?"":"s" );
							else
								text = "The tournament will begin shortly.";
						}
						else
						{
							text = "The tournament will begin shortly.";
						}

						AddHtml( 25, 92, 250, 40, text, false, false );
					}
					else
					{
						AddRightArrow( 25, 89, ToButtonID( 0, 2 ), "Rounds" );
					}

					break;
				}
				case TournyBracketGumpType.Rules_Info:
				{
					Ruleset ruleset = tourny.Ruleset;
					Ruleset basedef = ruleset.Base;

					BitArray defs;

					if ( ruleset.Flavors.Count > 0 )
					{
						defs = new BitArray( basedef.Options );

						for ( int i = 0; i < ruleset.Flavors.Count; ++i )
							defs.Or( ((Ruleset)ruleset.Flavors[i]).Options );
					}
					else
					{
						defs = basedef.Options;
					}

					int changes = 0;

					BitArray opts = ruleset.Options;

					for ( int i = 0; i < opts.Length; ++i )
					{
						if ( defs[i] != opts[i] )
							++changes;
					}

					AddPage( 0 );
					AddBackground( 0, 0, 300, 60 + 18 + 20 + 20 + 20 + 8 + 20 + (ruleset.Flavors.Count * 18) + 4 + 20 + (changes * 22) + 6, 9380 );

					AddLeftArrow( 25, 11, ToButtonID( 0, 0 ) );
					AddHtml( 25, 35, 250, 20, Center( "Rules" ), false, false );

					int y = 53;

					string groupText = null;

					switch ( tourny.GroupType )
					{
						case GroupingType.HighVsLow: groupText = "High vs Low"; break;
						case GroupingType.Nearest: groupText = "Closest opponent"; break;
						case GroupingType.Random: groupText = "Random"; break;
					}

					AddHtml( 35, y, 190, 20, String.Format( "Grouping: {0}", groupText ), false, false );
					y += 20;

					string tieText = null;

					switch ( tourny.TieType )
					{
						case TieType.Random: tieText = "Random"; break;
						case TieType.Highest: tieText = "Highest advances"; break;
						case TieType.Lowest: tieText = "Lowest advances"; break;
						case TieType.FullAdvancement: tieText = ( tourny.ParticipantsPerMatch == 2 ? "Both advance" : "Everyone advances" ); break;
						case TieType.FullElimination: tieText = ( tourny.ParticipantsPerMatch == 2 ? "Both eliminated" : "Everyone eliminated" ); break;
					}

					AddHtml( 35, y, 190, 20, String.Format( "Tiebreaker: {0}", tieText ), false, false );
					y += 20;

					string sdText = "Off";

					if ( tourny.SuddenDeath > TimeSpan.Zero )
					{
						sdText = String.Format( "{0}:{1:D2}", (int) tourny.SuddenDeath.TotalMinutes, tourny.SuddenDeath.Seconds );

						if ( tourny.SuddenDeathRounds > 0 )
							sdText = String.Format( "{0} (first {1} rounds)", sdText, tourny.SuddenDeathRounds );
						else
							sdText = String.Format( "{0} (all rounds)", sdText );
					}

					AddHtml( 35, y, 240, 20, String.Format( "Sudden Death: {0}", sdText ), false, false );
					y += 20;

					y += 8;

					AddHtml( 35, y, 190, 20, String.Format( "Ruleset: {0}", basedef.Title ), false, false );
					y += 20;

					for ( int i = 0; i < ruleset.Flavors.Count; ++i, y += 18 )
						AddHtml( 35, y, 190, 20, String.Format( " + {0}", ((Ruleset)ruleset.Flavors[i]).Title ), false, false );

					y += 4;

					if ( changes > 0 )
					{
						AddHtml( 35, y, 190, 20, "Modifications:", false, false );
						y += 20;

						for ( int i = 0; i < opts.Length; ++i )
						{
							if ( defs[i] != opts[i] )
							{
								string name = ruleset.Layout.FindByIndex( i );

								if ( name != null ) // sanity
								{
									AddImage( 35, y, opts[i] ? 0xD3 : 0xD2 );
									AddHtml( 60, y, 165, 22, name, false, false );
								}

								y += 22;
							}
						}
					}
					else
					{
						AddHtml( 35, y, 190, 20, "Modifications: None", false, false );
						y += 20;
					}

					break;
				}
				case TournyBracketGumpType.Participant_List:
				{
					AddPage( 0 );
					AddBackground( 0, 0, 300, 300, 9380 );

					if ( m_List == null )
						m_List = new ArrayList( tourny.Participants );

					AddLeftArrow( 25, 11, ToButtonID( 0, 0 ) );
					AddHtml( 25, 35, 250, 20, Center( String.Format( "{0} Participant{1}", m_List.Count, m_List.Count == 1 ? "" : "s" ) ), false, false );

					int index, count, y;
					StartPage( out index, out count, out y, 12 );

					for ( int i = 0; i < count; ++i, y += 18 )
					{
						TournyParticipant part = (TournyParticipant)m_List[index + i];
						string name = part.NameList;

						if ( m_Tournament.TournyType != TournyType.Standard && part.Players.Count == 1 )
						{
							PlayerMobile pm = part.Players[0] as PlayerMobile;

							if ( pm != null && pm.DuelPlayer != null )
								name = Color( name, pm.DuelPlayer.Eliminated ? 0x6633333 : 0x336666 );
						}

						AddRightArrow( 25, y, ToButtonID( 2, index + i ), name );
					}

					break;
				}
				case TournyBracketGumpType.Participant_Info:
				{
					TournyParticipant part = obj as TournyParticipant;

					if ( part == null )
						break;

					AddPage( 0 );
					AddBackground( 0, 0, 300, 60 + 18 + 20 + (part.Players.Count * 18) + 20 + 20 + 160, 9380 );

					AddLeftArrow( 25, 11, ToButtonID( 0, 1 ) );
					AddHtml( 25, 35, 250, 20, Center( "Participants" ), false, false );

					int y = 53;

					AddHtml( 25, y, 200, 20, part.Players.Count == 1 ? "Players" : "Team", false, false );
					y += 20;

					for ( int i = 0; i < part.Players.Count; ++i )
					{
						Mobile mob = (Mobile)part.Players[i];
						string name = mob.Name;

						if ( m_Tournament.TournyType != TournyType.Standard )
						{
							PlayerMobile pm = mob as PlayerMobile;

							if ( pm != null && pm.DuelPlayer != null )
								name = Color( name, pm.DuelPlayer.Eliminated ? 0x6633333 : 0x336666 );
						}

						AddRightArrow( 35, y, ToButtonID( 4, i ), name );
						y += 18;
					}

					AddHtml( 25, y, 200, 20, String.Format( "Free Advances: {0}", part.FreeAdvances == 0 ? "None" : part.FreeAdvances.ToString() ), false, false );
					y += 20;

					AddHtml( 25, y, 200, 20, "Log:", false, false );
					y += 20;

					StringBuilder sb = new StringBuilder();

					for ( int i = 0; i < part.Log.Count; ++i )
					{
						if ( sb.Length > 0 )
							sb.Append( "<br>" );

						sb.Append( part.Log[i] );
					}

					if ( sb.Length == 0 )
						sb.Append( "Nothing logged yet." );

					AddHtml( 25, y, 250, 150, Color( sb.ToString(), BlackColor32 ), false, true );

					break;
				}
				case TournyBracketGumpType.Player_Info:
				{
					AddPage( 0 );
					AddBackground( 0, 0, 300, 300, 9380 );

					AddLeftArrow( 25, 11, ToButtonID( 0, 3 ) );
					AddHtml( 25, 35, 250, 20, Center( "Participants" ), false, false );

					Mobile mob = obj as Mobile;

					if ( mob == null )
						break;

					Ladder ladder = Ladder.Instance;
					LadderEntry entry = ( ladder == null ? null : ladder.Find( mob ) );

					AddHtml( 25, 53, 250, 20, String.Format( "Name: {0}", mob.Name ), false, false );
					AddHtml( 25, 73, 250, 20, String.Format( "Guild: {0}", mob.Guild == null ? "None" : mob.Guild.Name + " [" + mob.Guild.Abbreviation + "]" ), false, false );
					AddHtml( 25, 93, 250, 20, String.Format( "Rank: {0}", entry == null ? "N/A" : LadderGump.Rank( entry.Index + 1 ) ), false, false );
					AddHtml( 25, 113, 250, 20, String.Format( "Level: {0}", entry == null ? 0 : Ladder.GetLevel( entry.Experience ) ), false, false );
					AddHtml( 25, 133, 250, 20, String.Format( "Wins: {0:N0}", entry == null ? 0 : entry.Wins ), false, false );
					AddHtml( 25, 153, 250, 20, String.Format( "Losses: {0:N0}", entry == null ? 0 : entry.Losses ), false, false );

					break;
				}
				case TournyBracketGumpType.Round_List:
				{
					AddPage( 0 );
					AddBackground( 0, 0, 300, 300, 9380 );

					AddLeftArrow( 25, 11, ToButtonID( 0, 0 ) );
					AddHtml( 25, 35, 250, 20, Center( "Rounds" ), false, false );

					if ( m_List == null )
						m_List = new ArrayList( tourny.Pyramid.Levels );

					int index, count, y;
					StartPage( out index, out count, out y, 12 );

					for ( int i = 0; i < count; ++i, y += 18 )
					{
						PyramidLevel level = (PyramidLevel)m_List[index + i];

						AddRightArrow( 25, y, ToButtonID( 3, index + i ), "Round #" + (index + i + 1) );
					}

					break;
				}
				case TournyBracketGumpType.Round_Info:
				{
					AddPage( 0 );
					AddBackground( 0, 0, 300, 300, 9380 );

					AddLeftArrow( 25, 11, ToButtonID( 0, 2 ) );
					AddHtml( 25, 35, 250, 20, Center( "Rounds" ), false, false );

					PyramidLevel level = m_Object as PyramidLevel;

					if ( level == null )
						break;

					if ( m_List == null )
						m_List = new ArrayList( level.Matches );

					AddRightArrow( 25, 53, ToButtonID( 5, 0 ), String.Format( "Free Advance: {0}", level.FreeAdvance == null ? "None" : level.FreeAdvance.NameList ) );

					AddHtml( 25, 73, 200, 20, String.Format( "{0} Match{1}", m_List.Count, m_List.Count == 1 ? "" : "es" ), false, false );

					int index, count, y;
					StartPage( out index, out count, out y, 10 );

					for ( int i = 0; i < count; ++i, y += 18 )
					{
						TournyMatch match = (TournyMatch)m_List[index + i];

						int color = -1;

						if ( match.InProgress )
							color = 0x336666;
						else if ( match.Context != null && match.Winner == null )
							color = 0x666666;

						StringBuilder sb = new StringBuilder();

						if ( m_Tournament.TournyType == TournyType.Standard )
						{
							for ( int j = 0; j < match.Participants.Count; ++j )
							{
								if ( sb.Length > 0 )
									sb.Append( " vs " );

								TournyParticipant part = (TournyParticipant)match.Participants[j];
								string txt = part.NameList;

								if ( color == -1 && match.Context != null && match.Winner == part )
									txt = Color( txt, 0x336633 );
								else if ( color == -1 && match.Context != null )
									txt = Color( txt, 0x663333 );

								sb.Append( txt );
							}
						}
						else if ( m_Tournament.EventController != null || m_Tournament.TournyType == TournyType.RandomTeam || m_Tournament.TournyType == TournyType.RedVsBlue )
						{
							for ( int j = 0; j < match.Participants.Count; ++j )
							{
								if ( sb.Length > 0 )
									sb.Append( " vs " );

								TournyParticipant part = (TournyParticipant)match.Participants[j];
								string txt;

								if ( m_Tournament.EventController != null )
									txt = String.Format( "Team {0} ({1})", m_Tournament.EventController.GetTeamName( j ), part.Players.Count );
								else if ( m_Tournament.TournyType == TournyType.RandomTeam )
									txt = String.Format( "Team {0} ({1})", j + 1, part.Players.Count );
								else
									txt = String.Format( "Team {0} ({1})", j == 0 ? "Red" : "Blue", part.Players.Count );

								if ( color == -1 && match.Context != null && match.Winner == part )
									txt = Color( txt, 0x336633 );
								else if ( color == -1 && match.Context != null )
									txt = Color( txt, 0x663333 );

								sb.Append( txt );
							}
						}
						else if ( m_Tournament.TournyType == TournyType.FreeForAll )
						{
							sb.Append( "Free For All" );
						}

						string str = sb.ToString();

						if ( color >= 0 )
							str = Color( str, color );

						AddRightArrow( 25, y, ToButtonID( 5, index + i + 1 ), str );
					}

					break;
				}
				case TournyBracketGumpType.Match_Info:
				{
					TournyMatch match = obj as TournyMatch;

					if ( match == null )
						break;

					int ct = ( m_Tournament.TournyType == TournyType.FreeForAll ? 2 : match.Participants.Count );

					AddPage( 0 );
					AddBackground( 0, 0, 300, 60 + 18 + 20 + 20 + 20 + (ct*18) + 6, 9380 );

					AddLeftArrow( 25, 11, ToButtonID( 0, 5 ) );
					AddHtml( 25, 35, 250, 20, Center( "Rounds" ), false, false );

					AddHtml( 25, 53, 250, 20, String.Format( "Winner: {0}", match.Winner == null ? "N/A" : match.Winner.NameList ), false, false );
					AddHtml( 25, 73, 250, 20, String.Format( "State: {0}", match.InProgress ? "In progress" : match.Context != null ? "Complete" : "Waiting" ), false, false );
					AddHtml( 25, 93, 250, 20, String.Format( "Participants:" ), false, false );

					if ( m_Tournament.TournyType == TournyType.Standard )
					{
						for ( int i = 0; i < match.Participants.Count; ++i )
						{
							TournyParticipant part = (TournyParticipant)match.Participants[i];

							AddRightArrow( 25, 113 + (i * 18), ToButtonID( 6, i ), part.NameList );
						}
					}
					else if ( m_Tournament.EventController != null || m_Tournament.TournyType == TournyType.RandomTeam || m_Tournament.TournyType == TournyType.RedVsBlue )
					{
						for ( int i = 0; i < match.Participants.Count; ++i )
						{
							TournyParticipant part = (TournyParticipant)match.Participants[i];

							if ( m_Tournament.EventController != null )
								AddRightArrow( 25, 113 + (i * 18), ToButtonID( 6, i ), String.Format( "Team {0} ({1})", m_Tournament.EventController.GetTeamName( i ), part.Players.Count ) );
							else if ( m_Tournament.TournyType == TournyType.RandomTeam )
								AddRightArrow( 25, 113 + (i * 18), ToButtonID( 6, i ), String.Format( "Team {0} ({1})", i+1, part.Players.Count ) );
							else
								AddRightArrow( 25, 113 + (i * 18), ToButtonID( 6, i ), String.Format( "Team {0} ({1})", i==0?"Red":"Blue", part.Players.Count ) );
						}
					}
					else if ( m_Tournament.TournyType == TournyType.FreeForAll )
					{
						AddHtml( 25, 113, 250, 20, "Free For All", false, false );
					}

					break;
				}
			}
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			int type, index;

			if ( !FromButtonID( info.ButtonID, out type, out index ) )
				return;

			switch ( type )
			{
				case 0:
				{
					switch ( index )
					{
						case 0: m_From.SendGump( new TournamentBracketGump( m_From, m_Tournament, TournyBracketGumpType.Index, null, 0, null ) ); break;
						case 1: m_From.SendGump( new TournamentBracketGump( m_From, m_Tournament, TournyBracketGumpType.Participant_List, null, 0, null ) ); break;
						case 2: m_From.SendGump( new TournamentBracketGump( m_From, m_Tournament, TournyBracketGumpType.Round_List, null, 0, null ) ); break;
						case 4: m_From.SendGump( new TournamentBracketGump( m_From, m_Tournament, TournyBracketGumpType.Rules_Info, null, 0, null ) ); break;
						case 3:
						{
							Mobile mob = m_Object as Mobile;

							for ( int i = 0; i < m_Tournament.Participants.Count; ++i )
							{
								TournyParticipant part = (TournyParticipant)m_Tournament.Participants[i];

								if ( part.Players.Contains( mob ) )
								{
									m_From.SendGump( new TournamentBracketGump( m_From, m_Tournament, TournyBracketGumpType.Participant_Info, null, 0, part ) );
									break;
								}
							}

							break;
						}
						case 5:
						{
							TournyMatch match = m_Object as TournyMatch;

							if ( match == null )
								break;

							for ( int i = 0; i < m_Tournament.Pyramid.Levels.Count; ++i )
							{
								PyramidLevel level = (PyramidLevel)m_Tournament.Pyramid.Levels[i];

								if ( level.Matches.Contains( match ) )
									m_From.SendGump( new TournamentBracketGump( m_From, m_Tournament, TournyBracketGumpType.Round_Info, null, 0, level ) );
							}

							break;
						}
					}

					break;
				}
				case 1:
				{
					switch ( index )
					{
						case 0:
						{
							if ( m_List != null && m_Page > 0 )
								m_From.SendGump( new TournamentBracketGump( m_From, m_Tournament, m_Type, m_List, m_Page - 1, m_Object ) );

							break;
						}
						case 1:
						{
							if ( m_List != null && ((m_Page + 1) * m_PerPage) < m_List.Count )
								m_From.SendGump( new TournamentBracketGump( m_From, m_Tournament, m_Type, m_List, m_Page + 1, m_Object ) );

							break;
						}
					}

					break;
				}
				case 2:
				{
					if ( m_Type != TournyBracketGumpType.Participant_List )
						break;

					if ( index >= 0 && index < m_List.Count )
						m_From.SendGump( new TournamentBracketGump( m_From, m_Tournament, TournyBracketGumpType.Participant_Info, null, 0, m_List[index] ) );

					break;
				}
				case 3:
				{
					if ( m_Type != TournyBracketGumpType.Round_List )
						break;

					if ( index >= 0 && index < m_List.Count )
						m_From.SendGump( new TournamentBracketGump( m_From, m_Tournament, TournyBracketGumpType.Round_Info, null, 0, m_List[index] ) );

					break;
				}
				case 4:
				{
					if ( m_Type != TournyBracketGumpType.Participant_Info )
						break;

					TournyParticipant part = m_Object as TournyParticipant;

					if ( part != null && index >= 0 && index < part.Players.Count )
						m_From.SendGump( new TournamentBracketGump( m_From, m_Tournament, TournyBracketGumpType.Player_Info, null, 0, part.Players[index] ) );

					break;
				}
				case 5:
				{
					if ( m_Type != TournyBracketGumpType.Round_Info )
						break;

					PyramidLevel level = m_Object as PyramidLevel;

					if ( level == null )
						break;

					if ( index == 0 )
					{
						if ( level.FreeAdvance != null )
							m_From.SendGump( new TournamentBracketGump( m_From, m_Tournament, TournyBracketGumpType.Participant_Info, null, 0, level.FreeAdvance ) );
						else
							m_From.SendGump( new TournamentBracketGump( m_From, m_Tournament, m_Type, m_List, m_Page, m_Object ) );
					}
					else if ( index >= 1 && index <= level.Matches.Count )
					{
						m_From.SendGump( new TournamentBracketGump( m_From, m_Tournament, TournyBracketGumpType.Match_Info, null, 0, level.Matches[index-1] ) );
					}

					break;
				}
				case 6:
				{
					if ( m_Type != TournyBracketGumpType.Match_Info )
						break;

					TournyMatch match = m_Object as TournyMatch;

					if ( match != null && index >= 0 && index < match.Participants.Count )
						m_From.SendGump( new TournamentBracketGump( m_From, m_Tournament, TournyBracketGumpType.Participant_Info, null, 0, match.Participants[index] ) );

					break;
				}
			}
		}
	}

	public class TournamentBracketItem : Item
	{
		private TournamentController m_Tournament;

		[CommandProperty( AccessLevel.GameMaster )]
		public TournamentController Tournament{ get{ return m_Tournament; } set{ m_Tournament = value; } }

		public override string DefaultName
		{
			get { return "tournament bracket"; }
		}

		[Constructable]
		public TournamentBracketItem() : base( 3774 )
		{
			Movable = false;
		}

		public override void OnDoubleClick(Mobile from)
		{
			if ( !from.InRange( GetWorldLocation(), 2 ) )
			{
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that
			}
			else if ( m_Tournament != null )
			{
				Tournament tourny = m_Tournament.Tournament;

				if ( tourny != null )
				{
					from.CloseGump( typeof( TournamentBracketGump ) );
					from.SendGump( new TournamentBracketGump( from, tourny, TournyBracketGumpType.Index, null, 0, null ) );

					/*if ( tourny.Stage == TournamentStage.Fighting && tourny.Pyramid.Levels.Count > 0 )
						from.SendGump( new TournamentBracketGump( tourny, (PyramidLevel)tourny.Pyramid.Levels[tourny.Pyramid.Levels.Count - 1] ) );
					else
						from.SendGump( new TournamentBracketGump( tourny, 0 ) );*/
				}
			}
		}

		public TournamentBracketItem( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );

			writer.Write( (Item) m_Tournament );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_Tournament = reader.ReadItem() as TournamentController;
					break;
				}
			}
		}
	}
}