using Server;
using Server.Gumps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Systems.Achievements.Gumps
{
    class AchievementObtainedGump : Gump
    {
        private BaseAchievement ach;

        public AchievementObtainedGump(BaseAchievement ach):base(470,389)
        {
            this.ach = ach;

            this.Closable = true;
            this.Disposable = true;
            this.Draggable = true;
            this.Resizable = false;
            this.AddPage(0);
            this.AddBackground(39, 38, 350, 100, 9270);
            this.AddAlphaRegion(48, 45, 332, 86);

            Rectangle2D bounds = ItemBounds.Table[ach.ItemIcon];
            int y = 60;
            if (ach.ItemIcon > 0)
                this.AddItem(80 - bounds.Width / 2 - bounds.X, (30 - bounds.Height / 2 - bounds.Y) + y, ach.ItemIcon );
            this.AddLabel(121, 55, 49, ach.Title);
            this.AddHtml(120, 80, 167, 42, ach.Desc, (bool)true, (bool)true);
            this.AddLabel(275, 51, 61, @"COMPLETE");
            this.AddBackground(320, 72, 44, 47, 9200);
            this.AddLabel(337, 87, 0, ach.RewardPoints.ToString());
        }
    }
}
