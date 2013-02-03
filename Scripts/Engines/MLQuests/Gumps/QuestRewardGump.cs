using System;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.MLQuests.Gumps
{
	public class QuestRewardGump : BaseQuestGump
	{
		private MLQuestInstance m_Instance;

		public QuestRewardGump( MLQuestInstance instance )
			: base( 1072201 ) // Reward
		{
			m_Instance = instance;

			MLQuest quest = instance.Quest;
			PlayerMobile pm = instance.Player;

			CloseOtherGumps( pm );

			SetTitle( quest.Title );
			RegisterButton( ButtonPosition.Left, ButtonGraphic.Accept, 1 );

			SetPageCount( 1 );

			BuildPage();
			AddRewards( quest );
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			if ( info.ButtonID == 1 )
				m_Instance.ClaimRewards();
		}
	}
}
