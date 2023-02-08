using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Rewards
{
    public abstract class BaseReward
    {
        public BaseReward(TextDefinition name) => Name = name;

        public TextDefinition Name { get; set; }

        protected virtual int LabelHeight => 16;

        public void WriteToGump(Gump g, int x, ref int y)
        {
            Name.AddHtmlText(g, x, y, 280, LabelHeight, false, false, 0x15F90, 0xBDE784);
        }

        public abstract void AddRewardItems(PlayerMobile pm, List<Item> rewards);
    }
}
