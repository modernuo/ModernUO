using Server.Gumps;
using System.Collections.Generic;
using Server.Network;
using System.Linq;
using System;

namespace Scripts.Systems.Achievements.Gumps
{
    class AchievementGump : Gump
    {
        private int m_curTotal;
        private Dictionary<int, AchieveData> m_curAchieves;

        public AchievementGump(Dictionary<int, AchieveData> achieves, int total,int category = 1) : base(25, 25)
        {

            m_curAchieves = achieves;
            m_curTotal = total;
            this.Closable = true;
            this.Disposable = true;
            this.Draggable = true;
            this.Resizable = false;
            this.AddPage(0);
            this.AddBackground(11, 15, 758, 575, 2600);
            this.AddBackground(57, 92, 666, 478, 9250);
            this.AddBackground(321, 104, 386, 453, 9270);
            this.AddBackground(72, 104, 245, 453, 9270);
            this.AddBackground(72, 34, 635, 53, 9270);
            this.AddBackground(327, 0, 133, 41, 9200);
            this.AddLabel(292, 52, 68, @"Defiance Beta Achievement System");
            this.AddLabel(360, 11, 82, total + @" Points");
            this.AddBackground(341, 522, 353, 26, 9200);

            int cnt = 0;
            var reqCat = AchievementSystem.Categories.FirstOrDefault(c => c.ID == category);
            if (reqCat == null)
            {
                Console.WriteLine("Couldnt find Achievement Cat: " + category);
                reqCat = AchievementSystem.Categories.First();
            }

            for (int i = 0;i < AchievementSystem.Categories.Count; i++)
            {
                int x = 90;
                int bgID = 9200;
                var cat = AchievementSystem.Categories[i];

                if (cat.Parent != 0 && cat.ID != reqCat.ID && cat.Parent != reqCat.ID && cat.Parent != reqCat.Parent)
                    continue;
                if(cat.Parent != 0)
                    x += 20;
                if(cat.ID == category)
                    bgID = 5120;

                this.AddBackground(x, 123 + (cnt * 31), 18810 / x, 25, bgID);
                if (cat.ID == category) // selected
                    this.AddImage(x + 12, 129 + (cnt * 31), 1210);
                else
                    this.AddButton(x + 12, 129 + (cnt * 31), 1209, 1210, 5000 + cat.ID, GumpButtonType.Reply, 0);
                this.AddLabel(x + 32, 125 + (cnt * 31), 0, cat.Name);
                cnt++;
            }
            cnt = 0;
            foreach( var ac in AchievementSystem.Achievements)
            {

                if (ac.CategoryID == category)
                {
                    if(ac.PreReq != null)
                    {
                        if (!achieves.ContainsKey(ac.PreReq.ID))
                            continue;
                        if(achieves[ac.PreReq.ID].CompletedOn != DateTime.MinValue)
                            continue;

                    }
                    if (achieves.ContainsKey(ac.ID))
                    {
                        AddAchieve(ac, cnt, achieves[ac.ID]);
                    }
                    else
                    {
                        if (ac.HiddenTillComplete)
                            continue;
                        AddAchieve(ac, cnt,null);
                    }
                    cnt++;
                }
            }
        }

        private void AddAchieve(BaseAchievement ac, int i, AchieveData acheiveData)
        {
            int index = i % 4;
            if(index == 0)
            {
                this.AddButton(658, 524, 4005, 4006, 0, GumpButtonType.Page, (i / 4) + 1);
                AddPage((i / 4) + 1);
                this.AddLabel(484, 526, 32, "Page " + ((i / 4) + 1));
                this.AddButton(345, 524, 4014, 4015, 0, GumpButtonType.Page, i/4);
            }
            int bg = 9350;
            if (acheiveData != null && acheiveData.CompletedOn != DateTime.MinValue)
                bg = 9300;
            this.AddBackground(340, 122 + (index * 100), 347, 97, bg);
            this.AddLabel(414, 131 + (index * 100), 49, ac.Title);
            if(ac.ItemIcon > 0)
                this.AddItem(357, 147 + (index * 100), ac.ItemIcon);
            this.AddImageTiled(416, 203 + (index * 100), 95, 9, 9750);

            var step = 95.0 / ac.CompletionTotal;
            var progress = 0;
            // Todo
            //if (acheiveData != null && acheiveData.CompletedOn != DateTime.MinValue)
             //   progress = acheiveData.Progress;
            if(acheiveData != null)
                progress = acheiveData.Progress;
            this.AddImageTiled(416, 203 + (index * 100), (int)(progress * step), 9, 9752);
            this.AddHtml(413, 152 + (index * 100), 194, 47,ac.Desc, (bool)true, (bool)true);
            if (acheiveData != null && acheiveData.CompletedOn != DateTime.MinValue)
                this.AddLabel(530, 127 + (index * 100), 32, acheiveData.CompletedOn.ToString());

            if(ac.CompletionTotal > 1)
                this.AddLabel(522, 196 + (index * 100), 0, progress + @" / " + ac.CompletionTotal);

            this.AddBackground(628, 149 + (index * 100), 48, 48, 9200);
            this.AddLabel(648, 163 + (index * 100), 32, ac.RewardPoints.ToString());

        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 0)
                return;
            var btn = info.ButtonID - 5000;
            sender.Mobile.SendGump(new AchievementGump(m_curAchieves, m_curTotal, btn));
        }


    }
}

