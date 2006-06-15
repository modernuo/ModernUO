using System;
using Server;

namespace Server.Engines.PartySystem
{
	public class PartyMemberInfo
	{
		private Mobile m_Mobile;
		private bool m_CanLoot;

		public Mobile Mobile{ get{ return m_Mobile; } }
		public bool CanLoot{ get{ return m_CanLoot; } set{ m_CanLoot = value; } }

		public PartyMemberInfo( Mobile m )
		{
			m_Mobile = m;
			m_CanLoot = !Core.ML;
		}
	}
}