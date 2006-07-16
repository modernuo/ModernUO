using System;
using Server;
using System.Collections.Generic;

namespace Server.Factions
{
	public class GuardList
	{
		private GuardDefinition m_Definition;
		private List<BaseFactionGuard> m_Guards;

		public GuardDefinition Definition{ get{ return m_Definition; } }
		public List<BaseFactionGuard> Guards{ get{ return m_Guards; } }

		public BaseFactionGuard Construct()
		{
			try{ return Activator.CreateInstance( m_Definition.Type ) as BaseFactionGuard; }
			catch{ return null; }
		}

		public GuardList( GuardDefinition definition )
		{
			m_Definition = definition;
			m_Guards = new List<BaseFactionGuard>();
		}
	}
}