using System;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.MLQuests.Gumps
{
	public class QuestReportBackGump : BaseQuestGump
	{
		private MLQuestInstance m_Instance;

		public QuestReportBackGump( MLQuestInstance instance )
			: base( 3006156 ) // Quest Conversation
		{
			m_Instance = instance;

			MLQuest quest = instance.Quest;
			PlayerMobile pm = instance.Player;

			// TODO: Check close sequence
			CloseOtherGumps( pm );

			SetTitle( quest.Title );
			RegisterButton( ButtonPosition.Left, ButtonGraphic.Continue, 4 );
			RegisterButton( ButtonPosition.Right, ButtonGraphic.Close, 3 );

			SetPageCount( 1 );

			BuildPage();
			AddConversation( quest.CompletionMessage );
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			if ( info.ButtonID == 4 )
				m_Instance.ContinueReportBack( true );
		}
	}
}
