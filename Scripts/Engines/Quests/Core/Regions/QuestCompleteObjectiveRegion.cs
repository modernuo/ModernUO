using System;
using System.Xml;
using Server;
using Server.Regions;
using Server.Mobiles;

namespace Server.Engines.Quests
{
	public class QuestCompleteObjectiveRegion : BaseRegion
	{
		private Type m_Quest;
		private Type m_Objective;

		public Type Quest{ get{ return m_Quest ; } }
		public Type Objective{ get{ return m_Objective; } }

		public QuestCompleteObjectiveRegion( XmlElement xml, Map map, Region parent ) : base( xml, map, parent )
		{
			XmlElement questEl = xml["quest"];

			ReadType( questEl, "type", ref m_Quest );
			ReadType( questEl, "complete", ref m_Objective );
		}

		public override void OnEnter( Mobile m )
		{
			base.OnEnter( m );

			if ( m_Quest != null && m_Objective != null )
			{
				PlayerMobile player = m as PlayerMobile;

				if ( player != null && player.Quest != null && player.Quest.GetType() == m_Quest )
				{
					QuestObjective obj = player.Quest.FindObjective( m_Objective );

					if ( obj != null && !obj.Completed )
						obj.Complete();
				}
			}
		}
	}
}