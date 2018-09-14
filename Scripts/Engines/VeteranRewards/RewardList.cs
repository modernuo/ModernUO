using System;

namespace Server.Engines.VeteranRewards
{
	public class RewardList
	{
		public TimeSpan Age { get; }

		public RewardEntry[] Entries { get; }

		public RewardList( TimeSpan interval, int index, RewardEntry[] entries )
		{
			Age = TimeSpan.FromDays( interval.TotalDays * index );
			Entries = entries;

			for ( int i = 0; i < entries.Length; ++i )
				entries[i].List = this;
		}
	}
}