using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Engines.MLQuests
{
	public class QuestArea
	{
		private TextDefinition m_Name; // So we can add custom names, different from the Region name
		private string m_RegionName;
		private Map m_ForceMap;

		public TextDefinition Name
		{
			get { return m_Name; }
			set { m_Name = value; }
		}

		public string RegionName
		{
			get { return m_RegionName; }
			set { m_RegionName = value; }
		}

		public Map ForceMap
		{
			get { return m_ForceMap; }
			set { m_ForceMap = value; }
		}

		public QuestArea( TextDefinition name, string region )
			: this( name, region, null )
		{
		}

		public QuestArea( TextDefinition name, string region, Map forceMap )
		{
			m_Name = name;
			m_RegionName = region;
			m_ForceMap = forceMap;

			if ( MLQuestSystem.Debug )
				ValidationQueue<QuestArea>.Add( this );
		}

		public bool Contains( Mobile mob )
		{
			return Contains( mob.Region );
		}

		public bool Contains( Region reg )
		{
			if ( reg == null || ( m_ForceMap != null && reg.Map != m_ForceMap ) )
				return false;

			return reg.IsPartOf( m_RegionName );
		}

		// Debug method
		public void Validate()
		{
			bool found = false;

			foreach ( Region r in Region.Regions )
			{
				if ( r.Name == m_RegionName && ( m_ForceMap == null || r.Map == m_ForceMap ) )
				{
					found = true;
					break;
				}
			}

			if ( !found )
				Console.WriteLine( "Warning: QuestArea region '{0}' does not exist (ForceMap = {1})", m_RegionName, ( m_ForceMap == null ) ? "-null-" : m_ForceMap.ToString() );
		}
	}
}
