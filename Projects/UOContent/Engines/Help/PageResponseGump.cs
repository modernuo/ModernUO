using Server.Gumps;
using Server.Network;

namespace Server.Engines.Help
{
    public class PageResponseGump : Gump
    {
        private readonly Mobile m_From;
        private readonly string m_Name;
        private readonly string m_Text;

        public PageResponseGump(Mobile from, string name, string text) : base(0, 0)
        {
            m_From = from;
            m_Name = name;
            m_Text = text;

            AddBackground(50, 25, 540, 430, 2600);

            AddPage(0);

            // <CENTER><U>Ultima Online Help Response</U></CENTER>
            AddHtmlLocalized(150, 40, 360, 40, 1062610);

            AddHtml(80, 90, 480, 290, $"{name} tells {from.Name}: {text}", true, true);

            // Clicking the OKAY button will remove the reponse you have received.
            AddHtmlLocalized(80, 390, 480, 40, 1062611);
            AddButton(400, 417, 2074, 2075, 1); // OKAY

            AddButton(475, 417, 2073, 2072, 0); // CANCEL
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID != 1)
            {
                m_From.SendGump(new MessageSentGump(m_From, m_Name, m_Text));
            }
        }
    }
}
