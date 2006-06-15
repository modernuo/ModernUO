using System;
using Server;

namespace Server.Factions
{
	public class GuardList
	{
		private GuardDefinition m_Definition;
		private FactionGuardCollection m_Guards;

		public GuardDefinition Definition{ get{ return m_Definition; } }
		public FactionGuardCollection Guards{ get{ return m_Guards; } }

		public BaseFactionGuard Construct()
		{
			try{ return Activator.CreateInstance( m_Definition.Type ) as BaseFactionGuard; }
			catch{ return null; }
		}

		public GuardList( GuardDefinition definition )
		{
			m_Definition = definition;
			m_Guards = new FactionGuardCollection();
		}
	}
}