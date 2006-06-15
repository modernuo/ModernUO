using System;
using System.Collections;
using Server;

namespace Server.Engines.PartySystem
{
	public class DeclineTimer : Timer
	{
		private Mobile m_Mobile, m_Leader;

		private static Hashtable m_Table = new Hashtable();

		public static void Start( Mobile m, Mobile leader )
		{
			DeclineTimer t = (DeclineTimer)m_Table[m];

			if ( t != null )
				t.Stop();

			m_Table[m] = t = new DeclineTimer( m, leader );
			t.Start();
		}

		private DeclineTimer( Mobile m, Mobile leader ) : base( TimeSpan.FromSeconds( 10.0 ) )
		{
			m_Mobile = m;
			m_Leader = leader;
		}

		protected override void OnTick()
		{
			m_Table.Remove( m_Mobile );

			if ( m_Mobile.Party == m_Leader && PartyCommands.Handler != null )
				PartyCommands.Handler.OnDecline( m_Mobile, m_Leader );
		}
	}
}