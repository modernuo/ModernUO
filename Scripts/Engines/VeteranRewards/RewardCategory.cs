using System;
using System.Collections;

namespace Server.Engines.VeteranRewards
{
	public class RewardCategory
	{
		private int m_Name;
		private string m_NameString;
		private ArrayList m_Entries;

		public int Name{ get{ return m_Name; } }
		public string NameString{ get{ return m_NameString; } }
		public ArrayList Entries{ get{ return m_Entries; } }

		public RewardCategory( int name )
		{
			m_Name = name;
			m_Entries = new ArrayList();
		}

		public RewardCategory( string name )
		{
			m_NameString = name;
			m_Entries = new ArrayList();
		}
	}
}