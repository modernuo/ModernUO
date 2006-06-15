using System;
using System.Xml;
using Server;
using Server.Regions;
using Server.Mobiles;

namespace Server.Engines.Quests
{
	public class QuestNoEntryRegion : BaseRegion
	{
		private Type m_Quest;
		private Type m_MinObjective;
		private Type m_MaxObjective;
		private int m_Message;

		public Type Quest{ get{ return m_Quest; } }
		public Type MinObjective{ get{ return m_MinObjective; } }
		public Type MaxObjective{ get{ return m_MaxObjective; } }
		public int Message{ get{ return m_Message; } }

		public QuestNoEntryRegion( XmlElement xml, Map map, Region parent ) : base( xml, map, parent )
		{
			XmlElement questEl = xml["quest"];

			ReadType( questEl, "type", ref m_Quest );
			ReadType( questEl, "min", ref m_MinObjective, false );
			ReadType( questEl, "max", ref m_MaxObjective, false );
			ReadInt32( questEl, "message", ref m_Message, false );
		}

		public override bool OnMoveInto( Mobile m, Direction d, Point3D newLocation, Point3D oldLocation )
		{
			if ( !base.OnMoveInto ( m, d, newLocation, oldLocation ) )
				return false;

			if ( m.AccessLevel > AccessLevel.Player )
				return true;

			if( m is BaseCreature )
			{
				BaseCreature bc = m as BaseCreature;

				if( !bc.Controlled && !bc.Summoned )
					return true;
			}

			if ( m_Quest == null )
				return true;

			PlayerMobile player = m as PlayerMobile;

			if ( player != null && player.Quest != null && player.Quest.GetType() == m_Quest
				&& ( m_MinObjective == null || player.Quest.FindObjective( m_MinObjective ) != null )
				&& ( m_MaxObjective == null || player.Quest.FindObjective( m_MaxObjective ) == null ) )
			{
				return true;
			}
			else
			{
				if ( m_Message != 0 )
					m.SendLocalizedMessage( m_Message );

				return false;
			}
		}
	}
}