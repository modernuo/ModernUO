using System.Collections.Generic;

namespace Server.Engines.VeteranRewards
{
	public class RewardCategory
	{
		private int m_Name;
		private string m_NameString;
		private List<RewardEntry> m_Entries;

		public int Name => m_Name;
		public string NameString => m_NameString;
		public List<RewardEntry> Entries  => m_Entries;

		public RewardCategory( int name )
		{
			m_Name = name;
			m_Entries = new List<RewardEntry>();
		}

		public RewardCategory( string name )
		{
			m_NameString = name;
			m_Entries = new List<RewardEntry>();
		}
	}
}
