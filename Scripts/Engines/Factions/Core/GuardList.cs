using System;
using System.Collections.Generic;

namespace Server.Factions
{
	public class GuardList
	{
		public GuardDefinition Definition { get; }

		public List<BaseFactionGuard> Guards { get; }

		public BaseFactionGuard Construct()
		{
			try{ return Activator.CreateInstance( Definition.Type ) as BaseFactionGuard; }
			catch{ return null; }
		}

		public GuardList( GuardDefinition definition )
		{
			Definition = definition;
			Guards = new List<BaseFactionGuard>();
		}
	}
}