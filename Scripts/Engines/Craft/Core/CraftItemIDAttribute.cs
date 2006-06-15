using System;
using Server;

namespace Server.Engines.Craft
{
	[AttributeUsage( AttributeTargets.Class )]
	public class CraftItemIDAttribute : Attribute
	{
		private int m_ItemID;

		public int ItemID{ get{ return m_ItemID; } }

		public CraftItemIDAttribute( int itemID )
		{
			m_ItemID = itemID;
		}
	}
}