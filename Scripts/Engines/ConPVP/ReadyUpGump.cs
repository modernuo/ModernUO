using System;
using System.Collections;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;

namespace Server.Engines.ConPVP
{
	public class ReadyUpGump : Gump
	{
		private Mobile m_From;
		private DuelContext m_Context;

		public string Center( string text )
		{
			return String.Format( "<CENTER>{0}</CENTER>", text );
		}

		public void AddGoldenButton( int x, int y, int bid )
		{
			AddButton( x  , y  , 0xD2, 0xD2, bid, GumpButtonType.Reply, 0 );
			AddButton( x+3, y+3, 0xD8, 0xD8, bid, GumpButtonType.Reply, 0 );
		}

		public ReadyUpGump( Mobile from, DuelContext context ) : base( 50, 50 )
		{
			m_From = from;
			m_Context = context;

			Closable = false;
			AddPage( 0 );

			if ( context.Rematch )
			{
				int height = 25 + 20 + 10 + 22 + 25;

				AddBackground( 0, 0, 210, height, 9250 );
				AddBackground( 10, 10, 190, height - 20, 0xDAC );

				AddHtml( 35, 25, 140, 20, Center( "Rematch?" ), false, false );

				AddButton( 35, 55, 247, 248, 1, GumpButtonType.Reply, 0 );
				AddButton( 115, 55, 242, 241, 2, GumpButtonType.Reply, 0 );
			}
			else
			{
				#region Participants
				AddPage( 1 );

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

				height += 10 + 22 + 25;

				AddBackground( 0, 0, 260, height, 9250 );
				AddBackground( 10, 10, 240, height - 20, 0xDAC );

				AddHtml( 35, 25, 190, 20, Center( "Participants" ), false, false );

				int y = 20 + 25;

				for ( int i = 0; i < parts.Count; ++i )
				{
					Participant p = (Participant)parts[i];

					y += 4;

					int offset = 0;

					if ( p.Players.Length > 1 )
					{
						AddHtml( 35, y, 176, 20, String.Format( "Team #{0}", i + 1 ), false, false );
						y += 22;
						offset = 10;
					}

					for ( int j = 0; j < p.Players.Length; ++j )
					{
						DuelPlayer pl = p.Players[j];

						string name = ( pl == null ? "(Empty)" : pl.Mobile.Name );

						AddHtml( 35 + offset, y, 166, 20, name, false, false );

						y += 22;
					}
				}

				y += 8;

				AddHtml( 35, y, 176, 20, "Continue?", false, false );

				y -= 2;

				AddButton( 102, y, 247, 248, 0, GumpButtonType.Page, 2 );
				AddButton( 169, y, 242, 241, 2, GumpButtonType.Reply, 0 );
				#endregion

				#region Rules
				AddPage( 2 );

				Ruleset ruleset = context.Ruleset;
				Ruleset basedef = ruleset.Base;

				height = 25 + 20 + 5 + 20 + 20 + 4;

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

				height += 10 + 22 + 25;

				AddBackground( 0, 0, 260, height, 9250 );
				AddBackground( 10, 10, 240, height - 20, 0xDAC );

				AddHtml( 35, 25, 190, 20, Center( "Rules" ), false, false );

				AddHtml( 35, 50, 190, 20, String.Format( "Set: {0}", basedef.Title ), false, false );

				y = 70;

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

				y += 8;

				AddHtml( 35, y, 176, 20, "Continue?", false, false );

				y -= 2;

				AddButton( 102, y, 247, 248, 1, GumpButtonType.Reply, 0 );
				AddButton( 169, y, 242, 241, 3, GumpButtonType.Reply, 0 );
				#endregion
			}
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			if ( !m_Context.Registered || !m_Context.ReadyWait )
				return;

			switch ( info.ButtonID )
			{
				case 1: // okay
				{
					PlayerMobile pm = m_From as PlayerMobile;

					if ( pm == null )
						break;

					pm.DuelPlayer.Ready = true;
					m_Context.SendReadyGump();

					break;
				}
				case 2: // reject participants
				{
					m_Context.RejectReady( m_From, "participants" );
					break;
				}
				case 3: // reject rules
				{
					m_Context.RejectReady( m_From, "rules" );
					break;
				}
			}
		}
	}
}