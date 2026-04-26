using Server.Gumps;

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
        private readonly QuestItemInfo[] _info;

        private QuestItemInfoGump(QuestItemInfo[] info) : base(485, 75) => _info = info;

        public static void DisplayTo(Mobile from, QuestItemInfo[] info)
        {
            if (from?.NetState == null || info == null || info.Length == 0)
            {
                return;
            }

            from.SendGump(new QuestItemInfoGump(info));
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            var height = 100 + _info.Length * 75;

            builder.AddPage();

            builder.AddBackground(5, 10, 145, height, 5054);

            builder.AddImageTiled(13, 20, 125, 10, 2624);
            builder.AddAlphaRegion(13, 20, 125, 10);

            builder.AddImageTiled(13, height - 10, 128, 10, 2624);
            builder.AddAlphaRegion(13, height - 10, 128, 10);

            builder.AddImageTiled(13, 20, 10, height - 30, 2624);
            builder.AddAlphaRegion(13, 20, 10, height - 30);

            builder.AddImageTiled(131, 20, 10, height - 30, 2624);
            builder.AddAlphaRegion(131, 20, 10, height - 30);

            builder.AddHtmlLocalized(67, 35, 120, 20, 1011233, White); // INFO

            builder.AddImage(62, 52, 9157);
            builder.AddImage(72, 52, 9157);
            builder.AddImage(82, 52, 9157);

            builder.AddButton(25, 31, 1209, 1210, 777);

            builder.AddPage(1);

            for (var i = 0; i < _info.Length; ++i)
            {
                var cur = _info[i];

                AddHtmlObject(ref builder, 25, 65 + i * 75, 110, 20, cur.Name, 1153, false, false);
                builder.AddItem(45, 85 + i * 75, cur.ItemID);
            }
        }
    }
}
