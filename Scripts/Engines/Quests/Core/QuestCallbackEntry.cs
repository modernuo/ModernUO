using System;
using Server;
using Server.ContextMenus;

namespace Server.Engines.Quests
{
	public class QuestCallbackEntry : ContextMenuEntry
	{
		private QuestCallback m_Callback;

		public QuestCallbackEntry( int number, QuestCallback callback ) : this( number, -1, callback )
		{
		}

		public QuestCallbackEntry( int number, int range, QuestCallback callback ) : base( number, range )
		{
			m_Callback = callback;
		}

		public override void OnClick()
		{
			if ( m_Callback != null )
				m_Callback();
		}
	}
}