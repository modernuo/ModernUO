using Server.Gumps;
using Server.Network;

namespace Server.Engines.AntiBot
{
    public class AntiBotGump : Gump
    {
        public readonly Mobile _mobile;
        public readonly int _code;

        public AntiBotGump(Mobile mobile, int code) : base(150, 150)
        {
            _mobile = mobile;
            _code = code;

            Closable = false;
            Disposable = false;
            Draggable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 350, 220, 9270);

            AddHtml(20, 20, 310, 25, "<CENTER><B><BASEFONT COLOR=#FFFFFF>Anti-Bot Verification</BASEFONT></B></CENTER>", false, false);
            AddHtml(20, 50, 310, 40, $"<CENTER><BASEFONT COLOR=#FFFFFF>Please enter the following number:</B></BASEFONT></CENTER>", false, false);
            AddHtml(20, 55, 310, 40, $"<CENTER><BASEFONT COLOR=#FFFFFF><BR><B style=\"color: #FF0000;\">{code}</B></BASEFONT></CENTER>", false, false);
            AddHtml(20, 100, 310, 50, $"<BASEFONT COLOR=#FFFFFF>You have 2 minutes to input the correct number.<BR>Incorrect numbers, cancellations, or timeouts will disconnect you from the server.</BASEFONT>", false, false);
            
            AddBackground(20, 160, 200, 25, 3000);
            AddTextEntry(25, 165, 190, 20, 0, 0, "");
            
            AddButton(230, 160, 4005, 4007, 1, GumpButtonType.Reply, 0); // Submit
            AddButton(290, 160, 4017, 4019, 0, GumpButtonType.Reply, 0); // Cancel
            
            AddHtml(230, 190, 40, 20, "<CENTER><BASEFONT COLOR=#FFFFFF>Submit</BASEFONT></CENTER>", false, false);
            AddHtml(290, 190, 40, 20, "<CENTER><BASEFONT COLOR=#FFFFFF>Cancel</BASEFONT></CENTER>", false, false);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var from = sender?.Mobile;
            if (from == null)
            {
                return;
            }

            bool cancelled = info.ButtonID == 0;
            int enteredCode = 0;

            if (!cancelled)
            {
                var textEntry = info.GetTextEntry(0);
                if (textEntry != null && !int.TryParse(textEntry.Trim(), out enteredCode))
                {
                    enteredCode = -1; // Invalid input - will cause disconnect
                }
            }

            AntiBotSystem.ProcessResponse(from, enteredCode, cancelled);
        }
    }
}
