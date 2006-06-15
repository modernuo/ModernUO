using System;
using Server;
using Server.Items;

namespace Server.Engines.Craft
{
	public abstract class CustomCraft
	{
		private Mobile m_From;
		private CraftItem m_CraftItem;
		private CraftSystem m_CraftSystem;
		private Type m_TypeRes;
		private BaseTool m_Tool;
		private int m_Quality;

		public Mobile From{ get{ return m_From; } }
		public CraftItem CraftItem{ get{ return m_CraftItem; } }
		public CraftSystem CraftSystem{ get{ return m_CraftSystem; } }
		public Type TypeRes{ get{ return m_TypeRes; } }
		public BaseTool Tool{ get{ return m_Tool; } }
		public int Quality{ get{ return m_Quality; } }

		public CustomCraft( Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality )
		{
			m_From = from;
			m_CraftItem = craftItem;
			m_CraftSystem = craftSystem;
			m_TypeRes = typeRes;
			m_Tool = tool;
			m_Quality = quality;
		}

		public abstract void EndCraftAction();
		public abstract Item CompleteCraft( out int message );
	}
}