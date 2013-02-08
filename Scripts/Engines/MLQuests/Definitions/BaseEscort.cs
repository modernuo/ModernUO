using System;
using Server;

namespace Server.Engines.MLQuests.Definitions
{
	// Base class for escorts providing the AwardHumanInNeed option
	public class BaseEscort : MLQuest
	{
		public virtual bool AwardHumanInNeed { get { return true; } }

		public BaseEscort()
		{
			CompletionNotice = CompletionNoticeShort;
		}

		public override void GetRewards( MLQuestInstance instance )
		{
			if ( AwardHumanInNeed )
				HumanInNeed.AwardTo( instance.Player );

			base.GetRewards( instance );
		}
	}
}
