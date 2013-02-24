using System;
using Server;
using Server.Engines.MLQuests;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.MLQuests.Gumps
{
	public class QuestOfferGump : BaseQuestGump
	{
		private MLQuest m_Quest;
		private IQuestGiver m_Quester;

		public QuestOfferGump( MLQuest quest, IQuestGiver quester, PlayerMobile pm )
			: base( 1049010 ) // Quest Offer
		{
			m_Quest = quest;
			m_Quester = quester;

			CloseOtherGumps( pm );
			pm.CloseGump( typeof( QuestOfferGump ) );

			SetTitle( quest.Title );
			RegisterButton( ButtonPosition.Left, ButtonGraphic.Accept, 1 );
			RegisterButton( ButtonPosition.Right, ButtonGraphic.Refuse, 2 );

			SetPageCount( 3 );

			BuildPage();
			AddDescription( quest );

			BuildPage();
			AddObjectives( quest );

			BuildPage();
			AddRewardsPage( quest );
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			PlayerMobile pm = sender.Mobile as PlayerMobile;

			if ( pm == null )
				return;

			switch ( info.ButtonID )
			{
				case 1: // Accept
				{
					m_Quest.OnAccept( m_Quester, pm );
					break;
				}
				case 2: // Refuse
				{
					m_Quest.OnRefuse( m_Quester, pm );
					break;
				}
			}
		}
	}
}
