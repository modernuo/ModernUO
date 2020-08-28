using Server.Gumps;
using Server.Network;

namespace Server
{
    public class VirtueStatusGump : Gump
    {
        private readonly Mobile m_Beholder;

        public VirtueStatusGump(Mobile beholder) : base(0, 0)
        {
            m_Beholder = beholder;

            AddPage(0);

            AddImage(30, 40, 2080);
            AddImage(47, 77, 2081);
            AddImage(47, 147, 2081);
            AddImage(47, 217, 2081);
            AddImage(47, 267, 2083);
            AddImage(70, 213, 2091);

            AddPage(1);

            AddHtml(140, 73, 200, 20, "The Virtues");

            AddHtmlLocalized(80, 100, 100, 40, 1051000);  // Humility
            AddHtmlLocalized(80, 129, 100, 40, 1051001);  // Sacrifice
            AddHtmlLocalized(80, 159, 100, 40, 1051002);  // Compassion
            AddHtmlLocalized(80, 189, 100, 40, 1051003);  // Spirituality
            AddHtmlLocalized(200, 100, 200, 40, 1051004); // Valor
            AddHtmlLocalized(200, 129, 200, 40, 1051005); // Honor
            AddHtmlLocalized(200, 159, 200, 40, 1051006); // Justice
            AddHtmlLocalized(200, 189, 200, 40, 1051007); // Honesty

            AddHtmlLocalized(75, 224, 220, 60, 1052062); // Click on a blue gem to view your status in that virtue.

            AddButton(60, 100, 1210, 1210, 1);
            AddButton(60, 129, 1210, 1210, 2);
            AddButton(60, 159, 1210, 1210, 3);
            AddButton(60, 189, 1210, 1210, 4);
            AddButton(180, 100, 1210, 1210, 5);
            AddButton(180, 129, 1210, 1210, 6);
            AddButton(180, 159, 1210, 1210, 7);
            AddButton(180, 189, 1210, 1210, 8);

            AddButton(280, 43, 4014, 4014, 9);
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            switch (info.ButtonID)
            {
                case 1:
                    {
                        m_Beholder.SendGump(new VirtueInfoGump(m_Beholder, VirtueName.Humility, 1052051));
                        break;
                    }
                case 2:
                    {
                        m_Beholder.SendGump(
                            new VirtueInfoGump(
                                m_Beholder,
                                VirtueName.Sacrifice,
                                1052053,
                                @"http://update.uo.com/design_389.html"
                            )
                        );
                        break;
                    }
                case 3:
                    {
                        m_Beholder.SendGump(
                            new VirtueInfoGump(
                                m_Beholder,
                                VirtueName.Compassion,
                                1053000,
                                @"http://update.uo.com/design_412.html"
                            )
                        );
                        break;
                    }
                case 4:
                    {
                        m_Beholder.SendGump(new VirtueInfoGump(m_Beholder, VirtueName.Spirituality, 1052056));
                        break;
                    }
                case 5:
                    {
                        m_Beholder.SendGump(
                            new VirtueInfoGump(
                                m_Beholder,
                                VirtueName.Valor,
                                1054033,
                                @"http://update.uo.com/design_427.html"
                            )
                        );
                        break;
                    }
                case 6:
                    {
                        m_Beholder.SendGump(
                            new VirtueInfoGump(
                                m_Beholder,
                                VirtueName.Honor,
                                1052058,
                                @"http://guide.uo.com/virtues_2.html"
                            )
                        );
                        break;
                    }
                case 7:
                    {
                        m_Beholder.SendGump(
                            new VirtueInfoGump(
                                m_Beholder,
                                VirtueName.Justice,
                                1052059,
                                @"http://update.uo.com/design_413.html"
                            )
                        );
                        break;
                    }
                case 8:
                    {
                        m_Beholder.SendGump(new VirtueInfoGump(m_Beholder, VirtueName.Honesty, 1052060));
                        break;
                    }
                case 9:
                    {
                        m_Beholder.SendGump(new VirtueGump(m_Beholder, m_Beholder));
                        break;
                    }
            }
        }
    }
}
