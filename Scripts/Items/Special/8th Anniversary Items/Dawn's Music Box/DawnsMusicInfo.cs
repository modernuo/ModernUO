using System;

namespace Server.Items
{
	public enum DawnsMusicRarity
	{
		Common,
		Uncommon,
		Rare,
	}

	public class DawnsMusicInfo
	{
		private int m_Name;

		public int Name
		{
			get { return m_Name; }
		}

		private DawnsMusicRarity m_Rarity;

		public DawnsMusicRarity Rarity
		{
			get { return m_Rarity; }
		}

		public DawnsMusicInfo( int name, DawnsMusicRarity rarity )
		{
			m_Name = name;
			m_Rarity = rarity;
		}
	}
}
