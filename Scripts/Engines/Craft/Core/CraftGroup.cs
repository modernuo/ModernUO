using System;

namespace Server.Engines.Craft
{
	public class CraftGroup
	{
		private CraftItemCol m_arCraftItem;

		private string m_NameString;
		private int m_NameNumber;

		public CraftGroup( TextDefinition groupName )
		{
			m_NameNumber = groupName;
			m_NameString = groupName;
			m_arCraftItem = new CraftItemCol();
		}

		public void AddCraftItem( CraftItem craftItem )
		{
			m_arCraftItem.Add( craftItem );
		}

		public CraftItemCol CraftItems
		{
			get { return m_arCraftItem; }
		}

		public string NameString
		{
			get { return m_NameString; }
		}

		public int NameNumber
		{
			get { return m_NameNumber; }
		}
	}
}