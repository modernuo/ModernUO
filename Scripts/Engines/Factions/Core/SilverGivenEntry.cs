using System;

namespace Server.Factions
{
	public class SilverGivenEntry
	{
		public static readonly TimeSpan ExpirePeriod = TimeSpan.FromHours( 3.0 );

		private Mobile m_GivenTo;
		private DateTime m_TimeOfGift;

		public Mobile GivenTo => m_GivenTo;
		public DateTime TimeOfGift => m_TimeOfGift;

		public bool IsExpired => ( m_TimeOfGift + ExpirePeriod ) < DateTime.UtcNow;

		public SilverGivenEntry( Mobile givenTo )
		{
			m_GivenTo = givenTo;
			m_TimeOfGift = DateTime.UtcNow;
		}
	}
}