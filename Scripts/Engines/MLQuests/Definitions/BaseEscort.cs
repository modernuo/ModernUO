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
		}

		public override void OnComplete( MLQuestInstance instance )
		{
			instance.Player.SendLocalizedMessage( 1046258, "", 0x23 ); // Your quest is complete.
		}

		public override void GetRewards( MLQuestInstance instance )
		{
			if ( AwardHumanInNeed )
				HumanInNeed.AwardTo( instance.Player );

			base.GetRewards( instance );
		}
	}
}
