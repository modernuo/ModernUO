using System;

namespace Server.Factions
{
	public class GuardDefinition
	{
		private Type m_Type;

		private int m_Price;
		private int m_Upkeep;
		private int m_Maximum;

		private int m_ItemID;

		private TextDefinition m_Header;
		private TextDefinition m_Label;

		public Type Type => m_Type;

		public int Price => m_Price;
		public int Upkeep => m_Upkeep;
		public int Maximum => m_Maximum;
		public int ItemID => m_ItemID;

		public TextDefinition Header => m_Header;
		public TextDefinition Label => m_Label;

		public GuardDefinition( Type type, int itemID, int price, int upkeep, int maximum, TextDefinition header, TextDefinition label )
		{
			m_Type = type;

			m_Price = price;
			m_Upkeep = upkeep;
			m_Maximum = maximum;
			m_ItemID = itemID;

			m_Header = header;
			m_Label = label;
		}
	}
}