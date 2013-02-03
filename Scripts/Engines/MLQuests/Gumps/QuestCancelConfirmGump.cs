using System;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.MLQuests.Gumps
{
	public class QuestCancelConfirmGump : Gump
	{
		private MLQuestInstance m_Instance;
		private bool m_CloseGumps;

		public QuestCancelConfirmGump( MLQuestInstance instance )
			: this( instance, true )
		{
		}

		public QuestCancelConfirmGump( MLQuestInstance instance, bool closeGumps )
			: base( 120, 50 )
		{
			m_Instance = instance;
			m_CloseGumps = closeGumps;

			if ( closeGumps )
				BaseQuestGump.CloseOtherGumps( instance.Player );

			AddPage( 0 );

			Closable = false;

			AddImageTiled( 0, 0, 348, 262, 0xA8E );
			AddAlphaRegion( 0, 0, 348, 262 );

			AddImage( 0, 15, 0x27A8 );
			AddImageTiled( 0, 30, 17, 200, 0x27A7 );
			AddImage( 0, 230, 0x27AA );

			AddImage( 15, 0, 0x280C );
			AddImageTiled( 30, 0, 300, 17, 0x280A );
			AddImage( 315, 0, 0x280E );

			AddImage( 15, 244, 0x280C );
			AddImageTiled( 30, 244, 300, 17, 0x280A );
			AddImage( 315, 244, 0x280E );

			AddImage( 330, 15, 0x27A8 );
			AddImageTiled( 330, 30, 17, 200, 0x27A7 );
			AddImage( 330, 230, 0x27AA );

			AddImage( 333, 2, 0x2716 );
			AddImage( 333, 248, 0x2716 );
			AddImage( 2, 248, 0x2716 );
			AddImage( 2, 2, 0x2716 );

			AddHtmlLocalized( 25, 22, 200, 20, 1049000, 0x7D00, false, false ); // Confirm Quest Cancellation
			AddImage( 25, 40, 0xBBF );

			/*
			 * This quest will give you valuable information, skills
			 * and equipment that will help you advance in the
			 * game at a quicker pace.<BR>
			 * <BR>
			 * Are you certain you wish to cancel at this time?
			 */
			AddHtmlLocalized( 25, 55, 300, 120, 1060836, 0xFFFFFF, false, false );

			MLQuest quest = instance.Quest;

			if ( quest.IsChainTriggered || quest.NextQuest != null )
			{
				AddRadio( 25, 145, 0x25F8, 0x25FB, false, 2 );
				AddHtmlLocalized( 60, 150, 280, 20, 1075023, 0xFFFFFF, false, false ); // Yes, I want to quit this entire chain!
			}

			AddRadio( 25, 180, 0x25F8, 0x25FB, true, 1 );
			AddHtmlLocalized( 60, 185, 280, 20, 1049005, 0xFFFFFF, false, false ); // Yes, I really want to quit this quest!

			AddRadio( 25, 215, 0x25F8, 0x25FB, false, 0 );
			AddHtmlLocalized( 60, 220, 280, 20, 1049006, 0xFFFFFF, false, false ); // No, I don't want to quit.

			AddButton( 265, 220, 0xF7, 0xF8, 7, GumpButtonType.Reply, 0 );
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			if ( m_Instance.Removed )
				return;

			switch ( info.ButtonID )
			{
				case 7: // Okay
				{
					if ( info.IsSwitched( 2 ) )
						m_Instance.Cancel( true );
					else if ( info.IsSwitched( 1 ) )
						m_Instance.Cancel( false );

					sender.Mobile.SendGump( new QuestLogGump( m_Instance.Player, m_CloseGumps ) );
					break;
				}
			}
		}
	}
}
