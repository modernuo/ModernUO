using System;

namespace Server.Factions
{
	public class SilverGivenEntry
	{
		public static readonly TimeSpan ExpirePeriod = TimeSpan.FromHours( 3.0 );

		private Mobile m_GivenTo;
		private DateTime m_TimeOfGift;

		public Mobile GivenTo{ get{ return m_GivenTo; } }
		public DateTime TimeOfGift{ get{ return m_TimeOfGift; } }

		public bool IsExpired{ get{ return ( m_TimeOfGift + ExpirePeriod ) < DateTime.Now; } }

		public SilverGivenEntry( Mobile givenTo )
		{
			m_GivenTo = givenTo;
			m_TimeOfGift = DateTime.Now;
		}
	}
}