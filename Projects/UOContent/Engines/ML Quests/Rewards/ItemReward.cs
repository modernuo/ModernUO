using System;
using System.Collections.Generic;
using Server.Engines.MLQuests.Items;
using Server.Mobiles;
using Server.Utilities;

namespace Server.Engines.MLQuests.Rewards
{
    public class ItemReward : BaseReward
    {
        public static readonly ItemReward
            SmallBagOfTrinkets = new(1072268, typeof(SmallBagOfTrinkets)); // A small bag of trinkets.

        public static readonly ItemReward
            BagOfTrinkets = new(1072341, typeof(BagOfTrinkets)); // A bag of trinkets.

        public static readonly ItemReward
            BagOfTreasure = new(1072583, typeof(BagOfTreasure)); // A bag of treasure.

        public static readonly ItemReward
            LargeBagOfTreasure = new(1072706, typeof(LargeBagOfTreasure)); // A large bag of treasure.

        public static readonly ItemReward Strongbox = new(1072584, typeof(RewardStrongbox)); // A strongbox.

        public static readonly ItemReward
            TailorSatchel = new(1074282, typeof(TailorSatchel)); // Craftsman's Satchel

        public static readonly ItemReward
            BlacksmithSatchel = new(1074282, typeof(BlacksmithSatchel)); // Craftsman's Satchel

        public static readonly ItemReward
            FletchingSatchel = new(1074282, typeof(FletchingSatchel)); // Craftsman's Satchel

        public static readonly ItemReward
            CarpentrySatchel = new(1074282, typeof(CarpentrySatchel)); // Craftsman's Satchel

        public static readonly ItemReward
            TinkerSatchel = new(1074282, typeof(TinkerSatchel)); // Craftsman's Satchel

        private readonly int m_Amount;

        private readonly Type m_Type;

        public ItemReward(TextDefinition name = null, Type type = null, int amount = 1) : base(name)
        {
            m_Type = type;
            m_Amount = amount;
        }

        public virtual Item CreateItem()
        {
            Item spawnedItem = null;

            try
            {
                spawnedItem = m_Type.CreateInstance<Item>();
            }
            catch (Exception e)
            {
                if (MLQuestSystem.Debug)
                {
                    Console.WriteLine("WARNING: ItemReward.CreateItem failed for {0}: {1}", m_Type, e);
                }
            }

            return spawnedItem;
        }

        public override void AddRewardItems(PlayerMobile pm, List<Item> rewards)
        {
            var reward = CreateItem();

            if (reward == null)
            {
                return;
            }

            if (reward.Stackable)
            {
                if (m_Amount > 1)
                {
                    reward.Amount = m_Amount;
                }

                rewards.Add(reward);
            }
            else
            {
                for (var i = 0; i < m_Amount; ++i)
                {
                    rewards.Add(reward);

                    if (i < m_Amount - 1)
                    {
                        reward = CreateItem();

                        if (reward == null)
                        {
                            return;
                        }
                    }
                }
            }
        }
    }
}
