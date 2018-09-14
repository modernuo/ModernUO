using System.Collections.Generic;

namespace Server.Engines.VeteranRewards
{
	public class RewardCategory
	{
		public int Name { get; }

		public string NameString { get; }

		public List<RewardEntry> Entries { get; }

		public RewardCategory( int name )
		{
			Name = name;
			Entries = new List<RewardEntry>();
		}

		public RewardCategory( string name )
		{
			NameString = name;
			Entries = new List<RewardEntry>();
		}
	}
}
