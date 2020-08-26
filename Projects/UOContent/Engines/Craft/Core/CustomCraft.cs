using System;
using Server.Items;

namespace Server.Engines.Craft
{
    public abstract class CustomCraft
    {
        public CustomCraft(
            Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool,
            int quality
        )
        {
            From = from;
            CraftItem = craftItem;
            CraftSystem = craftSystem;
            TypeRes = typeRes;
            Tool = tool;
            Quality = quality;
        }

        public Mobile From { get; }

        public CraftItem CraftItem { get; }

        public CraftSystem CraftSystem { get; }

        public Type TypeRes { get; }

        public BaseTool Tool { get; }

        public int Quality { get; }

        public abstract void EndCraftAction();
        public abstract Item CompleteCraft(out int message);
    }
}
