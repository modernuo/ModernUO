using System;

namespace Server.Engines.Craft
{
	[AttributeUsage( AttributeTargets.Class )]
	public class CraftItemIDAttribute : Attribute
	{
		public int ItemID { get; }

		public CraftItemIDAttribute( int itemID )
		{
			ItemID = itemID;
		}
	}
}