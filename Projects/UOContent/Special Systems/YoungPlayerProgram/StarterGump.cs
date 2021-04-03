using Server.Gumps;
using Server.Network;
using Server.Special_Systems.YoungPlayerProgram;

namespace Server.Gumps
{
    public class StarterGump : Gump
    {
        public static void Configure()
        {
            CommandSystem.Register("StarterGump", AccessLevel.Administrator, e => DisplayTo(e.Mobile));
        }

        public static StarterGump DisplayTo(Mobile user)
        {
            if (user == null || user.Deleted || !user.Player || user.NetState == null)
                return null;

            user.CloseGump<StarterGump>();

            var gump = new StarterGump(user);

            user.SendGump(gump);

            return gump;
        }

        public Mobile User { get; }

        public StarterGump(Mobile user)
            : base(0, 0)
        {
            User = user;

            Draggable = true;
            Closable = false;
            Resizable = false;
            Disposable = false;

            AddPage(0);
            AddBackground(221, 76, 508, 500, 30536);
            AddBackground(252, 107, 446, 438, 9270);
            AddImage(183, 43, 10400);
            AddImage(183, 264, 10402);
            AddImage(381, 78, 10452);
            AddLabel(430, 89, 1160, "Welcome");
            AddBackground(274, 138, 406, 243, 9300);
            AddHtml(388, 156, 273, 207, "You suddenly find yourself in a strange place and ask yourself if you are dreaming. It all feels so tangible. You are interested in exploring everything and to learn more about this feeling.  But now it's time to find your way around here. You find a piece of paper with a label in your pocket. It says \"Young\". It looks like you apparently have a protected status.", false, false);
            AddButton(608, 498, 247, 248, (int)Buttons.Button1, GumpButtonType.Reply, 0);
            AddButton(274, 497, 241, 243, (int)Buttons.Button2, GumpButtonType.Reply, 0);
            AddImage(242, 106, 991);
            AddHtml(276, 394, 399, 95, "<basefont color=#FFFFFF size=7>Your feeling tells you that you are safe on this island. If you feel safe enough to survive you want to move out into the wide world. Do you want to stay on the island until you feel safe enough?<basefont/>", false, false);
        }

        public enum Buttons
        {
            Button1 = 1,
            Button2 = 2,
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var index = info.ButtonID;

            switch (index)
            {
                case 0: // EXIT
                    {
                        User.CloseGump<StarterGump>();
                        break;
                    }
                case 1: // Set Filter
                    {
                        User.CloseGump<StarterGump>();
                        foreach (var item in User.Backpack.Items)
                        {
                            if (item.GetType() == typeof(YoungPlayerDeed))
                            {
                                var youngPlayerDeed = (YoungPlayerDeed)item;
                                youngPlayerDeed.TutorialRowStarted = true;
                                youngPlayerDeed.TutorialIsActive = true;

                            }
                        } 
                        //m_From.SendGump(new BOBFilterGump(m_From, m_Book));

                        break;
                    }
            }
        }

        public override void OnServerClose(NetState owner)
        {
        }
    }
}
