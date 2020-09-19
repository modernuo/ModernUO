using System;
using Server.Utilities;

namespace Server.Engines.VeteranRewards
{
    public class RewardEntry
    {
        public RewardEntry(RewardCategory category, int name, Type itemType, params object[] args)
        {
            Category = category;
            ItemType = itemType;
            RequiredExpansion = Expansion.None;
            Name = name;
            Args = args;
            category.Entries.Add(this);
        }

        public RewardEntry(RewardCategory category, string name, Type itemType, params object[] args)
        {
            Category = category;
            ItemType = itemType;
            RequiredExpansion = Expansion.None;
            NameString = name;
            Args = args;
            category.Entries.Add(this);
        }

        public RewardEntry(
            RewardCategory category, int name, Type itemType, Expansion requiredExpansion,
            params object[] args
        )
        {
            Category = category;
            ItemType = itemType;
            RequiredExpansion = requiredExpansion;
            Name = name;
            Args = args;
            category.Entries.Add(this);
        }

        public RewardEntry(
            RewardCategory category, string name, Type itemType, Expansion requiredExpansion,
            params object[] args
        )
        {
            Category = category;
            ItemType = itemType;
            RequiredExpansion = requiredExpansion;
            NameString = name;
            Args = args;
            category.Entries.Add(this);
        }

        public RewardList List { get; set; }

        public RewardCategory Category { get; }

        public Type ItemType { get; }

        public Expansion RequiredExpansion { get; }

        public int Name { get; }

        public string NameString { get; }

        public object[] Args { get; }

        public Item Construct()
        {
            try
            {
                var item = ItemType.CreateInstance<Item>(Args);

                if (item is IRewardItem rewardItem)
                {
                    rewardItem.IsRewardItem = true;
                }

                return item;
            }
            catch
            {
                // ignored
            }

            return null;
        }
    }
}
