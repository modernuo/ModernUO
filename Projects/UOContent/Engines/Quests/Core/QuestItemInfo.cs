namespace Server.Engines.Quests
{
    public class QuestItemInfo
    {
        public QuestItemInfo(object name, int itemID)
        {
            Name = name;
            ItemID = itemID;
        }

        public object Name { get; set; }

        public int ItemID { get; set; }
    }

    public class QuestItemInfoGump : BaseQuestGump
    {
        public QuestItemInfoGump(QuestItemInfo[] info) : base(485, 75)
        {
            var height = 100 + info.Length * 75;

            AddPage(0);

            AddBackground(5, 10, 145, height, 5054);

            AddImageTiled(13, 20, 125, 10, 2624);
            AddAlphaRegion(13, 20, 125, 10);

            AddImageTiled(13, height - 10, 128, 10, 2624);
            AddAlphaRegion(13, height - 10, 128, 10);

            AddImageTiled(13, 20, 10, height - 30, 2624);
            AddAlphaRegion(13, 20, 10, height - 30);

            AddImageTiled(131, 20, 10, height - 30, 2624);
            AddAlphaRegion(131, 20, 10, height - 30);

            AddHtmlLocalized(67, 35, 120, 20, 1011233, White); // INFO

            AddImage(62, 52, 9157);
            AddImage(72, 52, 9157);
            AddImage(82, 52, 9157);

            AddButton(25, 31, 1209, 1210, 777);

            AddPage(1);

            for (var i = 0; i < info.Length; ++i)
            {
                var cur = info[i];

                AddHtmlObject(25, 65 + i * 75, 110, 20, cur.Name, 1153, false, false);
                AddItem(45, 85 + i * 75, cur.ItemID);
            }
        }
    }
}
