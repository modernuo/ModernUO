using System;
using System.Text;
using System.Collections.Generic;
using Server;
using Server.Prompts;
using Server.Gumps;
using Server.Network;
using Server.Items;
using Server.Misc;
using Server.Commands;

namespace Server.Mobiles
{
	public interface ITownCrierEntryList
	{
		List<TownCrierEntry> Entries{ get; }
		TownCrierEntry GetRandomEntry();
		TownCrierEntry AddEntry( string[] lines, TimeSpan duration );
		void RemoveEntry( TownCrierEntry entry );
	}

	public class GlobalTownCrierEntryList : ITownCrierEntryList
	{
		private static GlobalTownCrierEntryList m_Instance;

		public static void Initialize()
		{
			CommandSystem.Register( "TownCriers", AccessLevel.GameMaster, new CommandEventHandler( TownCriers_OnCommand ) );
		}

		[Usage( "TownCriers" )]
		[Description( "Manages the global town crier list." )]
		public static void TownCriers_OnCommand( CommandEventArgs e )
		{
			e.Mobile.SendGump( new TownCrierGump( e.Mobile, Instance ) );
		}

		public static GlobalTownCrierEntryList Instance
		{
			get
			{
				if ( m_Instance == null )
					m_Instance = new GlobalTownCrierEntryList();

				return m_Instance;
			}
		}

		public bool IsEmpty{ get{ return ( m_Entries == null || m_Entries.Count == 0 ); } }

		public GlobalTownCrierEntryList()
		{
		}

		private List<TownCrierEntry> m_Entries;

		public List<TownCrierEntry> Entries
		{
			get{ return m_Entries; }
		}

		public TownCrierEntry GetRandomEntry()
		{
			if ( m_Entries == null || m_Entries.Count == 0 )
				return null;

			for ( int i = m_Entries.Count - 1; m_Entries != null && i >= 0; --i )
			{
				if ( i >= m_Entries.Count )
					continue;

				TownCrierEntry tce = m_Entries[i];

				if ( tce.Expired )
					RemoveEntry( tce );
			}

			if ( m_Entries == null || m_Entries.Count == 0 )
				return null;

			return m_Entries[Utility.Random( m_Entries.Count )];
		}

		public TownCrierEntry AddEntry( string[] lines, TimeSpan duration )
		{
			if ( m_Entries == null )
				m_Entries = new List<TownCrierEntry>();

			TownCrierEntry tce = new TownCrierEntry( lines, duration );

			m_Entries.Add( tce );

			List<TownCrier> instances = TownCrier.Instances;

			for ( int i = 0; i < instances.Count; ++i )
				instances[i].ForceBeginAutoShout();

			return tce;
		}

		public void RemoveEntry( TownCrierEntry tce )
		{
			if ( m_Entries == null )
				return;

			m_Entries.Remove( tce );

			if ( m_Entries.Count == 0 )
				m_Entries = null;
		}
	}

	public class TownCrierEntry
	{
		private string[] m_Lines;
		private DateTime m_ExpireTime;

		public string[] Lines{ get{ return m_Lines; } }
		public DateTime ExpireTime{ get{ return m_ExpireTime; } }
		public bool Expired{ get{ return ( DateTime.Now >= m_ExpireTime ); } }

		public TownCrierEntry( string[] lines, TimeSpan duration )
		{
			m_Lines = lines;

			if ( duration < TimeSpan.Zero )
				duration = TimeSpan.Zero;
			else if ( duration > TimeSpan.FromDays( 365.0 ) )
				duration = TimeSpan.FromDays( 365.0 );

			m_ExpireTime = DateTime.Now + duration;
		}
	}

	public class TownCrierDurationPrompt : Prompt
	{
		private ITownCrierEntryList m_Owner;

		public TownCrierDurationPrompt( ITownCrierEntryList owner )
		{
			m_Owner = owner;
		}

		public override void OnResponse( Mobile from, string text )
		{
			TimeSpan ts;

			if( !TimeSpan.TryParse( text, out ts ) )
			{
				from.SendMessage( "Value was not properly formatted. Use: <hours:minutes:seconds>" );
				from.SendGump( new TownCrierGump( from, m_Owner ) );
				return;
			}

			if ( ts < TimeSpan.Zero )
				ts = TimeSpan.Zero;

			from.SendMessage( "Duration set to: {0}", ts );
			from.SendMessage( "Enter the first line to shout:" );

			from.Prompt = new TownCrierLinesPrompt( m_Owner, null, new List<String>(), ts );
		}

		public override void OnCancel( Mobile from )
		{
			from.SendLocalizedMessage( 502980 ); // Message entry cancelled.
			from.SendGump( new TownCrierGump( from, m_Owner ) );
		}
	}

	public class TownCrierLinesPrompt : Prompt
	{
		private ITownCrierEntryList m_Owner;
		private TownCrierEntry m_Entry;
		private List<String> m_Lines;
		private TimeSpan m_Duration;

		public TownCrierLinesPrompt( ITownCrierEntryList owner, TownCrierEntry entry, List<String> lines, TimeSpan duration )
		{
			m_Owner = owner;
			m_Entry = entry;
			m_Lines = lines;
			m_Duration = duration;
		}

		public override void OnResponse( Mobile from, string text )
		{
			m_Lines.Add( text );

			from.SendMessage( "Enter the next line to shout, or press <ESC> if the message is finished." );
			from.Prompt = new TownCrierLinesPrompt( m_Owner, m_Entry, m_Lines, m_Duration );
		}

		public override void OnCancel( Mobile from )
		{
			if ( m_Entry != null )
				m_Owner.RemoveEntry( m_Entry );

			if ( m_Lines.Count > 0 )
			{
				m_Owner.AddEntry( m_Lines.ToArray(), m_Duration );
				from.SendMessage( "Message has been set." );
			}
			else
			{
				if ( m_Entry != null )
					from.SendMessage( "Message deleted." );
				else
					from.SendLocalizedMessage( 502980 ); // Message entry cancelled.
			}

			from.SendGump( new TownCrierGump( from, m_Owner ) );
		}
	}

	public class TownCrierGump : Gump
	{
		private Mobile m_From;
		private ITownCrierEntryList m_Owner;

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			if ( info.ButtonID == 1 )
			{
				m_From.SendMessage( "Enter the duration for the new message. Format: <hours:minutes:seconds>" );
				m_From.Prompt = new TownCrierDurationPrompt( m_Owner );
			}
			else if ( info.ButtonID > 1 )
			{
				List<TownCrierEntry> entries = m_Owner.Entries;
				int index = info.ButtonID - 2;

				if ( entries != null && index < entries.Count )
				{
					TownCrierEntry tce = entries[index];
					TimeSpan ts = tce.ExpireTime - DateTime.Now;

					if ( ts < TimeSpan.Zero )
						ts = TimeSpan.Zero;

					m_From.SendMessage( "Editing entry #{0}.", index + 1 );
					m_From.SendMessage( "Enter the first line to shout:" );
					m_From.Prompt = new TownCrierLinesPrompt( m_Owner, tce, new List<String>(), ts );
				}
			}
		}

		public TownCrierGump( Mobile from, ITownCrierEntryList owner ) : base( 50, 50 )
		{
			m_From = from;
			m_Owner = owner;

			from.CloseGump( typeof( TownCrierGump ) );

			AddPage( 0 );

			List<TownCrierEntry> entries = owner.Entries;

			owner.GetRandomEntry(); // force expiration checks

			int count = 0;

			if ( entries != null )
				count = entries.Count;

			AddImageTiled( 0, 0, 300, 38 + (count == 0 ? 20 : (count * 85)), 0xA40 );
			AddAlphaRegion( 1, 1, 298, 36 + (count == 0 ? 20 : (count * 85)) );

			AddHtml( 8, 8, 300 - 8 - 30, 20, "<basefont color=#FFFFFF><center>TOWN CRIER MESSAGES</center></basefont>", false, false );

			AddButton( 300 - 8 - 30, 8, 0xFAB, 0xFAD, 1, GumpButtonType.Reply, 0 );

			if ( count == 0 )
			{
				AddHtml( 8, 30, 284, 20, "<basefont color=#FFFFFF>The crier has no news.</basefont>", false, false );
			}
			else
			{
				for ( int i = 0; i < entries.Count; ++i )
				{
					TownCrierEntry tce = (TownCrierEntry)entries[i];

					TimeSpan toExpire = tce.ExpireTime - DateTime.Now;

					if ( toExpire < TimeSpan.Zero )
						toExpire = TimeSpan.Zero;

					StringBuilder sb = new StringBuilder();

					sb.Append( "[Expires: " );

					if ( toExpire.TotalHours >= 1 )
					{
						sb.Append( (int)toExpire.TotalHours );
						sb.Append( ':' );
						sb.Append( toExpire.Minutes.ToString( "D2" ) );
					}
					else
					{
						sb.Append( toExpire.Minutes );
					}

					sb.Append( ':' );
					sb.Append( toExpire.Seconds.ToString( "D2" ) );

					sb.Append( "] " );

					for ( int j = 0; j < tce.Lines.Length; ++j )
					{
						if ( j > 0 )
							sb.Append( "<br>" );

						sb.Append( tce.Lines[j] );
					}

					AddHtml( 8, 35 + (i * 85), 254, 80, sb.ToString(), true, true );

					AddButton( 300 - 8 - 26, 35 + (i * 85), 0x15E1, 0x15E5, 2 + i, GumpButtonType.Reply, 0 );
				}
			}
		}
	}

	public class TownCrier : Mobile, ITownCrierEntryList
	{
		private List<TownCrierEntry> m_Entries;
		private Timer m_NewsTimer;
		private Timer m_AutoShoutTimer;

		public List<TownCrierEntry> Entries
		{
			get{ return m_Entries; }
		}

		public TownCrierEntry GetRandomEntry()
		{
			if ( m_Entries == null || m_Entries.Count == 0 )
				return GlobalTownCrierEntryList.Instance.GetRandomEntry();

			for ( int i = m_Entries.Count - 1; m_Entries != null && i >= 0; --i )
			{
				if ( i >= m_Entries.Count )
					continue;

				TownCrierEntry tce = m_Entries[i];

				if ( tce.Expired )
					RemoveEntry( tce );
			}

			if ( m_Entries == null || m_Entries.Count == 0 )
				return GlobalTownCrierEntryList.Instance.GetRandomEntry();

			TownCrierEntry entry = GlobalTownCrierEntryList.Instance.GetRandomEntry();

			if ( entry == null || Utility.RandomBool() )
				entry = m_Entries[Utility.Random( m_Entries.Count )];

			return entry;
		}

		public void ForceBeginAutoShout()
		{
			if ( m_AutoShoutTimer == null )
				m_AutoShoutTimer = Timer.DelayCall( TimeSpan.FromSeconds( 5.0 ), TimeSpan.FromMinutes( 1.0 ), new TimerCallback( AutoShout_Callback ) );
		}

		public TownCrierEntry AddEntry( string[] lines, TimeSpan duration )
		{
			if ( m_Entries == null )
				m_Entries = new List<TownCrierEntry>();

			TownCrierEntry tce = new TownCrierEntry( lines, duration );

			m_Entries.Add( tce );

			if ( m_AutoShoutTimer == null )
				m_AutoShoutTimer = Timer.DelayCall( TimeSpan.FromSeconds( 5.0 ), TimeSpan.FromMinutes( 1.0 ), new TimerCallback( AutoShout_Callback ) );

			return tce;
		}

		public void RemoveEntry( TownCrierEntry tce )
		{
			if ( m_Entries == null )
				return;

			m_Entries.Remove( tce );

			if ( m_Entries.Count == 0 )
				m_Entries = null;

			if ( m_Entries == null && GlobalTownCrierEntryList.Instance.IsEmpty )
			{
				if ( m_AutoShoutTimer != null )
					m_AutoShoutTimer.Stop();

				m_AutoShoutTimer = null;
			}
		}

		private void AutoShout_Callback()
		{
			TownCrierEntry tce = GetRandomEntry();

			if ( tce == null )
			{
				if ( m_AutoShoutTimer != null )
					m_AutoShoutTimer.Stop();

				m_AutoShoutTimer = null;
			}
			else if ( m_NewsTimer == null )
			{
				m_NewsTimer = Timer.DelayCall( TimeSpan.FromSeconds( 1.0 ), TimeSpan.FromSeconds( 3.0 ), new TimerStateCallback( ShoutNews_Callback ), new object[]{ tce, 0 } );

				PublicOverheadMessage( MessageType.Regular, 0x3B2, 502976 ); // Hear ye! Hear ye!
			}
		}

		private void ShoutNews_Callback( object state )
		{
			object[] states = (object[])state;
			TownCrierEntry tce = (TownCrierEntry)states[0];
			int index = (int)states[1];

			if ( index < 0 || index >= tce.Lines.Length )
			{
				if ( m_NewsTimer != null )
					m_NewsTimer.Stop();

				m_NewsTimer = null;
			}
			else
			{
				PublicOverheadMessage( MessageType.Regular, 0x3B2, false, tce.Lines[index] );
				states[1] = index + 1;
			}
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.AccessLevel >= AccessLevel.GameMaster )
				from.SendGump( new TownCrierGump( from, this ) );
			else
				base.OnDoubleClick( from );
		}

		public override bool HandlesOnSpeech( Mobile from )
		{
			return ( m_NewsTimer == null && from.Alive && InRange( from, 12 ) );
		}

		public override void OnSpeech( SpeechEventArgs e )
		{
			if ( m_NewsTimer == null && e.HasKeyword( 0x30 ) && e.Mobile.Alive && InRange( e.Mobile, 12 ) ) // *news*
			{
				Direction = GetDirectionTo( e.Mobile );

				TownCrierEntry tce = GetRandomEntry();

				if ( tce == null )
				{
					PublicOverheadMessage( MessageType.Regular, 0x3B2, 1005643 ); // I have no news at this time.
				}
				else
				{
					m_NewsTimer = Timer.DelayCall( TimeSpan.FromSeconds( 1.0 ), TimeSpan.FromSeconds( 3.0 ), new TimerStateCallback( ShoutNews_Callback ), new object[]{ tce, 0 } );

					PublicOverheadMessage( MessageType.Regular, 0x3B2, 502978 ); // Some of the latest news!
				}
			}
		}

		private static List<TownCrier> m_Instances = new List<TownCrier>();

		public static List<TownCrier> Instances
		{
			get{ return m_Instances; }
		}

		[Constructable]
		public TownCrier()
		{
			m_Instances.Add( this );

			InitStats( 100, 100, 25 );

			Title = "the town crier";
			Hue = Utility.RandomSkinHue();

			if ( !Core.AOS )
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

			AddItem( new FancyShirt( Utility.RandomBlueHue() ) );

			Item skirt;

			switch ( Utility.Random( 2 ) )
			{
				case 0: skirt = new Skirt(); break;
				default: case 1: skirt = new Kilt(); break;
			}

			skirt.Hue = Utility.RandomGreenHue();

			AddItem( skirt );

			AddItem( new FeatheredHat( Utility.RandomGreenHue() ) );

			Item boots;

			switch ( Utility.Random( 2 ) )
			{
				case 0: boots = new Boots(); break;
				default: case 1: boots = new ThighBoots(); break;
			}

			AddItem( boots );

			Utility.AssignRandomHair( this );
		}

		public override bool CanBeDamaged()
		{
			return false;
		}

		public override void OnDelete()
		{
			m_Instances.Remove( this );
			base.OnDelete();
		}

		public TownCrier( Serial serial ) : base( serial )
		{
			m_Instances.Add( this );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( Core.AOS && NameHue == 0x35 )
				NameHue = -1;
		}
	}
}