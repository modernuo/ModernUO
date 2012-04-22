using System;
using System.Text;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.ConPVP
{
	public class Participant
	{
		private DuelContext m_Context;
		private DuelPlayer[] m_Players;
		private TournyParticipant m_TournyPart;

		public int Count{ get{ return m_Players.Length; } }
		public DuelPlayer[] Players{ get{ return m_Players; } }
		public DuelContext Context{ get{ return m_Context; } }
		public TournyParticipant TournyPart{ get{ return m_TournyPart; } set{ m_TournyPart = value; } }

		public DuelPlayer Find( Mobile mob )
		{
			if ( mob is PlayerMobile )
			{
				PlayerMobile pm = (PlayerMobile)mob;

				if ( pm.DuelContext == m_Context && pm.DuelPlayer.Participant == this )
					return pm.DuelPlayer;

				return null;
			}

			for ( int i = 0; i < m_Players.Length; ++i )
			{
				if ( m_Players[i] != null && m_Players[i].Mobile == mob )
					return m_Players[i];
			}

			return null;
		}

		public bool Contains( Mobile mob )
		{
			return ( Find( mob ) != null );
		}

		public void Broadcast( int hue, string message, string nonLocalOverhead, string localOverhead )
		{
			for ( int i = 0; i < m_Players.Length; ++i )
			{
				if ( m_Players[i] != null )
				{
					if ( message != null )
						m_Players[i].Mobile.SendMessage( hue, message );

					if ( nonLocalOverhead != null )
						m_Players[i].Mobile.NonlocalOverheadMessage( Network.MessageType.Regular, hue, false, String.Format( nonLocalOverhead, m_Players[i].Mobile.Name, m_Players[i].Mobile.Female ? "her" : "his" ) );

					if ( localOverhead != null )
						m_Players[i].Mobile.LocalOverheadMessage( Network.MessageType.Regular, hue, false, localOverhead );
				}
			}
		}

		public int FilledSlots
		{
			get
			{
				int count = 0;

				for ( int i = 0; i < m_Players.Length; ++i )
				{
					if ( m_Players[i] != null )
						++count;
				}

				return count;
			}
		}

		public bool HasOpenSlot
		{
			get
			{
				for ( int i = 0; i < m_Players.Length; ++i )
				{
					if ( m_Players[i] == null )
						return true;
				}

				return false;
			}
		}

		public bool Eliminated
		{
			get
			{
				for ( int i = 0; i < m_Players.Length; ++i )
				{
					if ( m_Players[i] != null && !m_Players[i].Eliminated )
						return false;
				}

				return true;
			}
		}

		public string NameList
		{
			get
			{
				StringBuilder sb = new StringBuilder();

				for ( int i = 0; i < m_Players.Length; ++i )
				{
					if ( m_Players[i] == null )
						continue;

					Mobile mob = m_Players[i].Mobile;

					if ( sb.Length > 0 )
						sb.Append( ", " );

					sb.Append( mob.Name );
				}

				if ( sb.Length == 0 )
					return "Empty";

				return sb.ToString();
			}
		}

		public void Nullify( DuelPlayer player )
		{
			if ( player == null )
				return;

			int index = Array.IndexOf( m_Players, player );

			if ( index == -1 )
				return;

			m_Players[index] = null;
		}

		public void Remove( DuelPlayer player )
		{
			if ( player == null )
				return;

			int index = Array.IndexOf( m_Players, player );

			if ( index == -1 )
				return;

			DuelPlayer[] old = m_Players;
			m_Players = new DuelPlayer[old.Length - 1];

			for ( int i = 0; i < index; ++i )
				m_Players[i] = old[i];

			for ( int i = index + 1; i < old.Length; ++i )
				m_Players[i - 1] = old[i];
		}

		public void Remove( Mobile player )
		{
			Remove( Find( player ) );
		}

		public void Add( Mobile player )
		{
			if ( Contains( player ) )
				return;

			for ( int i = 0; i < m_Players.Length; ++i )
			{
				if ( m_Players[i] == null )
				{
					m_Players[i] = new DuelPlayer( player, this );
					return;
				}
			}

			Resize( m_Players.Length + 1 );
			m_Players[m_Players.Length - 1] = new DuelPlayer( player, this );
		}

		public void Resize( int count )
		{
			DuelPlayer[] old = m_Players;
			m_Players = new DuelPlayer[count];

			if ( old != null )
			{
				int ct = 0;

				for ( int i = 0; i < old.Length; ++i )
				{
					if ( old[i] != null && ct < count )
						m_Players[ct++] = old[i];
				}
			}
		}

		public Participant( DuelContext context, int count )
		{
			m_Context = context;
			//m_Stakes = new StakesContainer( context, this );
			Resize( count );
		}
	}

	public class DuelPlayer
	{
		private Mobile m_Mobile;
		private bool m_Eliminated;
		private bool m_Ready;
		private Participant m_Participant;

		public Mobile Mobile{ get{ return m_Mobile; } }
		public bool Ready{ get{ return m_Ready; } set{ m_Ready = value; } }
		public bool Eliminated{ get{ return m_Eliminated; } set{ m_Eliminated = value; if ( m_Participant.Context.m_Tournament != null && m_Eliminated ){ m_Participant.Context.m_Tournament.OnEliminated( this ); m_Mobile.SendEverything(); } } }
		public Participant Participant{ get{ return m_Participant; } set{ m_Participant = value; } }

		public DuelPlayer( Mobile mob, Participant p )
		{
			m_Mobile = mob;
			m_Participant = p;

			if ( mob is PlayerMobile )
				((PlayerMobile)mob).DuelPlayer = this;
		}
	}
}