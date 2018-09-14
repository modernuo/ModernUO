using Server.Mobiles;
using System.Collections.Generic;

namespace Server.Engines.MLQuests.Rewards
{
	public class DummyReward : BaseReward
	{
		public DummyReward( TextDefinition name )
			: base( name )
		{
		}

		protected override int LabelHeight => 180;

		public override void AddRewardItems( PlayerMobile pm, List<Item> rewards )
		{
		}
	}
}
