using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Zento
{
	public class FirstKillObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Kill 10 Deathwatch Beetle Hatchlings and return to Ansella Gryen.
				return 1063316;
			}
		}

		public override void RenderProgress( BaseQuestGump gump )
		{
			if ( !Completed )
			{
				// Deathwatch Beetle Hatchlings killed:
				gump.AddHtmlLocalized( 70, 260, 270, 100, 1063318, 0x12DC6BF, false, false );

				gump.AddLabel( 70, 280, 0x64, "0" );
				gump.AddLabel( 100, 280, 0x64, "/" );
				gump.AddLabel( 130, 280, 0x64, "10" );
			}
			else
			{
				base.RenderProgress( gump );
			}
		}

		public FirstKillObjective()
		{
		}

		public override void OnKill( BaseCreature creature, Container corpse )
		{
			if ( creature is DeathwatchBeetleHatchling )
				Complete();
		}

		public override void OnComplete()
		{
			System.AddObjective( new SecondKillObjective() );
		}
	}

	public class SecondKillObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* Great job! One less terrible hatchling in the Waste!<BR><BR>
				 * 
				 * Once you've killed 10 of the Deathwatch Beetle Hatchlings,
				 * return to Ansella for your reward!
				 */
				return 1063320;
			}
		}

		public override void RenderProgress( BaseQuestGump gump )
		{
			if ( !Completed )
			{
				// Deathwatch Beetle Hatchlings killed:
				gump.AddHtmlLocalized( 70, 260, 270, 100, 1063318, 0x12DC6BF, false, false );

				gump.AddLabel( 70, 280, 0x64, "1" );
				gump.AddLabel( 100, 280, 0x64, "/" );
				gump.AddLabel( 130, 280, 0x64, "10" );
			}
			else
			{
				base.RenderProgress( gump );
			}
		}

		public SecondKillObjective()
		{
		}

		public override void OnKill( BaseCreature creature, Container corpse )
		{
			if ( creature is DeathwatchBeetleHatchling )
			{
				Complete();
				System.AddObjective( new ThirdKillObjective( 2 ) );
			}
		}

		public override void OnRead()
		{
			if ( !Completed )
			{
				Complete();
				System.AddObjective( new ThirdKillObjective( 1 ) );
			}
		}
	}

	public class ThirdKillObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Continue killing Deathwatch Beetle Hatchlings.
				return 1063319;
			}
		}

		public override void RenderProgress( BaseQuestGump gump )
		{
			if ( !Completed )
			{
				// Deathwatch Beetle Hatchlings killed:
				gump.AddHtmlLocalized( 70, 260, 270, 100, 1063318, 0x12DC6BF, false, false );

				gump.AddLabel( 70, 280, 0x64, CurProgress.ToString() );
				gump.AddLabel( 100, 280, 0x64, "/" );
				gump.AddLabel( 130, 280, 0x64, "10" );
			}
			else
			{
				base.RenderProgress( gump );
			}
		}

		public override int MaxProgress{ get{ return 10; } }

		public ThirdKillObjective( int startingProgress )
		{
			CurProgress = startingProgress;
		}

		public ThirdKillObjective()
		{
		}

		public override void OnKill( BaseCreature creature, Container corpse )
		{
			if ( creature is DeathwatchBeetleHatchling )
				CurProgress++;
		}

		public override void OnComplete()
		{
			System.AddObjective( new ReturnObjective() );
		}
	}

	public class ReturnObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Return to Ansella Gryen for your reward.
				return 1063313;
			}
		}

		public ReturnObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new EndConversation() );
		}
	}
}