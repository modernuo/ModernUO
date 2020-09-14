using Server.Gumps;
using Server.Network;

namespace Server.Multis
{
    public class ConfirmDryDockGump : Gump
    {
        private readonly BaseBoat m_Boat;
        private readonly Mobile m_From;

        public ConfirmDryDockGump(Mobile from, BaseBoat boat) : base(150, 200)
        {
            m_From = from;
            m_Boat = boat;

            m_From.CloseGump<ConfirmDryDockGump>();

            AddPage(0);

            AddBackground(0, 0, 220, 170, 5054);
            AddBackground(10, 10, 200, 150, 3000);

            AddHtmlLocalized(20, 20, 180, 80, 1018319, true); // Do you wish to dry dock this boat?

            AddHtmlLocalized(55, 100, 140, 25, 1011011); // CONTINUE
            AddButton(20, 100, 4005, 4007, 2);

            AddHtmlLocalized(55, 125, 140, 25, 1011012); // CANCEL
            AddButton(20, 125, 4005, 4007, 1);
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID == 2)
            {
                m_Boat.EndDryDock(m_From);
            }
        }
    }
}
