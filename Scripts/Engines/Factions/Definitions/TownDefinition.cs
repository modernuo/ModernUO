using System;

namespace Server.Factions
{
	public class TownDefinition
	{
		private int m_Sort;
		private int m_SigilID;

		private string m_Region;

		private string m_FriendlyName;

		private TextDefinition m_TownName;
		private TextDefinition m_TownStoneHeader;
		private TextDefinition m_StrongholdMonolithName;
		private TextDefinition m_TownMonolithName;
		private TextDefinition m_TownStoneName;
		private TextDefinition m_SigilName;
		private TextDefinition m_CorruptedSigilName;

		private Point3D m_Monolith;
		private Point3D m_TownStone;

		public int Sort{ get{ return m_Sort; } }
		public int SigilID{ get{ return m_SigilID; } }

		public string Region{ get{ return m_Region; } }
		public string FriendlyName{ get{ return m_FriendlyName; } }

		public TextDefinition TownName{ get{ return m_TownName; } }
		public TextDefinition TownStoneHeader{ get{ return m_TownStoneHeader; } }
		public TextDefinition StrongholdMonolithName{ get{ return m_StrongholdMonolithName; } }
		public TextDefinition TownMonolithName{ get{ return m_TownMonolithName; } }
		public TextDefinition TownStoneName{ get{ return m_TownStoneName; } }
		public TextDefinition SigilName{ get{ return m_SigilName; } }
		public TextDefinition CorruptedSigilName{ get{ return m_CorruptedSigilName; } }

		public Point3D Monolith{ get{ return m_Monolith; } }
		public Point3D TownStone{ get{ return m_TownStone; } }

		public TownDefinition( int sort, int sigilID, string region, string friendlyName, TextDefinition townName, TextDefinition townStoneHeader, TextDefinition strongholdMonolithName, TextDefinition townMonolithName, TextDefinition townStoneName, TextDefinition sigilName, TextDefinition corruptedSigilName, Point3D monolith, Point3D townStone )
		{
			m_Sort = sort;
			m_SigilID = sigilID;
			m_Region = region;
			m_FriendlyName = friendlyName;
			m_TownName = townName;
			m_TownStoneHeader = townStoneHeader;
			m_StrongholdMonolithName = strongholdMonolithName;
			m_TownMonolithName = townMonolithName;
			m_TownStoneName = townStoneName;
			m_SigilName = sigilName;
			m_CorruptedSigilName = corruptedSigilName;
			m_Monolith = monolith;
			m_TownStone = townStone;
		}
	}
}