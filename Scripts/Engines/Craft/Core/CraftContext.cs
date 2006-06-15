using System;
using System.Collections;

namespace Server.Engines.Craft
{
	public enum CraftMarkOption
	{
		MarkItem,
		DoNotMark,
		PromptForMark
	}

	public class CraftContext
	{
		private ArrayList m_Items;
		private int m_LastResourceIndex;
		private int m_LastResourceIndex2;
		private int m_LastGroupIndex;
		private bool m_DoNotColor;
		private CraftMarkOption m_MarkOption;

		public ArrayList Items{ get{ return m_Items; } }
		public int LastResourceIndex{ get{ return m_LastResourceIndex; } set{ m_LastResourceIndex = value; } }
		public int LastResourceIndex2{ get{ return m_LastResourceIndex2; } set{ m_LastResourceIndex2 = value; } }
		public int LastGroupIndex{ get{ return m_LastGroupIndex; } set{ m_LastGroupIndex = value; } }
		public bool DoNotColor{ get{ return m_DoNotColor; } set{ m_DoNotColor = value; } }
		public CraftMarkOption MarkOption{ get{ return m_MarkOption; } set{ m_MarkOption = value; } }

		public CraftContext()
		{
			m_Items = new ArrayList();
			m_LastResourceIndex = -1;
			m_LastResourceIndex2 = -1;
			m_LastGroupIndex = -1;
		}

		public CraftItem LastMade
		{
			get
			{
				if ( m_Items.Count > 0 )
					return (CraftItem)m_Items[0];

				return null;
			}
		}

		public void OnMade( CraftItem item )
		{
			m_Items.Remove( item );

			if ( m_Items.Count == 10 )
				m_Items.RemoveAt( 9 );

			m_Items.Insert( 0, item );
		}
	}
}