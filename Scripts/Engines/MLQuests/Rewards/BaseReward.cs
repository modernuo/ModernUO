using Server.Mobiles;
using Server.Gumps;
using System.Collections.Generic;

namespace Server.Engines.MLQuests.Rewards
{
	public abstract class BaseReward
	{
		public TextDefinition Name { get; set; }

		public BaseReward( TextDefinition name )
		{
			Name = name;
		}

		protected virtual int LabelHeight => 16;

		public void WriteToGump( Gump g, int x, ref int y )
		{
			TextDefinition.AddHtmlText( g, x, y, 280, LabelHeight, Name, false, false, 0x15F90, 0xBDE784 );
		}

		public abstract void AddRewardItems( PlayerMobile pm, List<Item> rewards );
	}
}
