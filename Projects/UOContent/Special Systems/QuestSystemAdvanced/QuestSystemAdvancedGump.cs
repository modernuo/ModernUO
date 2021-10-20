using Server.Gumps;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.QuestSystemAdvanced
{
    public class QuestSystemAdvancedGump: Gump
    {
        private static QuestDefinition Quest;
        public static void Configure()
        {
            CommandSystem.Register("CustomGump", AccessLevel.Administrator, e => DisplayTo(e));
        }

        public static QuestSystemAdvancedGump DisplayTo(CommandEventArgs args)
        {
            Mobile user = args.Mobile;

            if (user == null || user.Deleted || !user.Player || user.NetState == null)
                return null;

            if (args.Arguments[0] != null)
            {
                Quest = QuestIO.GetQuestById(int.Parse(args.Arguments[0]));

                if (Quest != null)
                {
                    user.CloseGump<QuestSystemAdvancedGump>();

                    var gump = new QuestSystemAdvancedGump(user);

                    user.SendGump(gump);

                    return gump;
                }
            }

            return null;
        }

        public Mobile User { get; }

        private QuestSystemAdvancedGump(Mobile user)
            : base(0, 0)
        {
            User = user;

            Draggable = true;
            Closable = true;
            Resizable = false;
            Disposable = false;

            AddPage(0);
            AddBackground(18, 71, 412, 640, 83);
            AddBackground(44, 101, 361, 547, 9300);
            AddBackground(28, 25, 401, 47, 302);
            AddImage(1, 10, 1417);
            AddImage(21, 23, 11013);
            AddLabel(163, 38, 1160, "Defiance Quest Log");
            AddHtml(58, 163, 336, 312, $"<BASEFONT COLOR=#000000>{Quest.Description}</BASEFONT>", false, true);
            AddLabel(61, 126, 1380, Quest.Title);
            AddLabel(62, 488, 1380, "Reward/s");
            AddBackground(57, 532, 337, 104, 302);
            AddHtml(68, 544, 312, 76, "", false, true);
            AddItem(73, 549, 8532);
            AddLabel(121, 545, 1160, "a spider minion");
            AddLabel(121, 560, 1152, "collected from a spider queen");
            AddLabel(63, 510, 1152, "Choose or collect your reward/s after completion");
            AddItem(72, 587, 3823);
            AddLabel(120, 583, 1160, "6000 gold");
            AddLabel(120, 598, 1152, "common loot");
            AddButton(336, 660, 247, 248, (int)Buttons.Button1, GumpButtonType.Reply, 0);
            AddButton(46, 663, 2119, 248, (int)Buttons.Button2, GumpButtonType.Reply, 0);
            AddLabel(176, 662, 1152, $"Accept for {Quest.XP} (XP)");
        }

        public enum Buttons
        {
            Button1 = 1,
            Button2 = 2,
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
        }

        public override void OnServerClose(NetState owner)
        {
        }
    }
}
