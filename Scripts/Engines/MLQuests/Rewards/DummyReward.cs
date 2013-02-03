using System;
using Server;
using Server.Mobiles;
using Server.Gumps;
using System.Collections.Generic;

namespace Server.Engines.MLQuests.Rewards
{
	public class DummyReward : BaseReward
	{
		public DummyReward( TextDefinition name )
			: base( name )
		{
		}

		protected override int LabelHeight { get { return 180; } }

		public override void AddRewardItems( PlayerMobile pm, List<Item> rewards )
		{
		}
	}
}
