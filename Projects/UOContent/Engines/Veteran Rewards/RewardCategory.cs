using System.Collections.Generic;

namespace Server.Engines.VeteranRewards
{
    public class RewardCategory
    {
        public RewardCategory(int name)
        {
            Name = name;
            Entries = [];
        }

        public RewardCategory(string name)
        {
            NameString = name;
            Entries = [];
        }

        public int Name { get; }

        public string NameString { get; }

        public List<RewardEntry> Entries { get; }
    }
}
