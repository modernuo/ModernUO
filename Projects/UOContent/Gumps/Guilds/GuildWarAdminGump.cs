using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildWarAdminGump : Gump
    {
        private readonly Guild m_Guild;
        private readonly Mobile m_Mobile;

        public GuildWarAdminGump(Mobile from, Guild guild) : base(20, 30)
        {
            m_Mobile = from;
            m_Guild = guild;

            Draggable = false;

            AddPage(0);
            AddBackground(0, 0, 550, 440, 5054);
            AddBackground(10, 10, 530, 420, 3000);

            AddHtmlLocalized(20, 10, 510, 35, 1011105); // <center>WAR FUNCTIONS</center>

            AddButton(20, 40, 4005, 4007, 1);
            AddHtmlLocalized(55, 40, 400, 30, 1011099); // Declare war through guild name search.

            var count = 0;

            if (guild.Enemies.Count > 0)
            {
                AddButton(20, 160 + count * 30, 4005, 4007, 2);
                AddHtmlLocalized(55, 160 + count++ * 30, 400, 30, 1011103); // Declare peace.
            }
            else
            {
                AddHtmlLocalized(20, 160 + count++ * 30, 400, 30, 1013033); // No current wars
            }

            if (guild.WarInvitations.Count > 0)
            {
                AddButton(20, 160 + count * 30, 4005, 4007, 3);
                AddHtmlLocalized(55, 160 + count++ * 30, 400, 30, 1011100); // Accept war invitations.

                AddButton(20, 160 + count * 30, 4005, 4007, 4);
                AddHtmlLocalized(55, 160 + count++ * 30, 400, 30, 1011101); // Reject war invitations.
            }
            else
            {
                AddHtmlLocalized(20, 160 + count++ * 30, 400, 30, 1018012); // No current invitations received for war.
            }

            if (guild.WarDeclarations.Count > 0)
            {
                AddButton(20, 160 + count * 30, 4005, 4007, 5);
                AddHtmlLocalized(55, 160 + count++ * 30, 400, 30, 1011102); // Rescind your war declarations.
            }
            else
            {
                AddHtmlLocalized(20, 160 + count++ * 30, 400, 30, 1013055); // No current war declarations
            }

            AddButton(20, 400, 4005, 4007, 6);
            AddHtmlLocalized(55, 400, 400, 35, 1011104); // Return to the previous menu.
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (GuildGump.BadLeader(m_Mobile, m_Guild))
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 1: // Declare war
                    {
                        m_Mobile.SendLocalizedMessage(1018001); // Declare war through search - Enter Guild Name:
                        m_Mobile.Prompt = new GuildDeclareWarPrompt(m_Mobile, m_Guild);

                        break;
                    }
                case 2: // Declare peace
                    {
                        GuildGump.EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildDeclarePeaceGump(m_Mobile, m_Guild));

                        break;
                    }
                case 3: // Accept war
                    {
                        GuildGump.EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildAcceptWarGump(m_Mobile, m_Guild));

                        break;
                    }
                case 4: // Reject war
                    {
                        GuildGump.EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildRejectWarGump(m_Mobile, m_Guild));

                        break;
                    }
                case 5: // Rescind declarations
                    {
                        GuildGump.EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildRescindDeclarationGump(m_Mobile, m_Guild));

                        break;
                    }
                case 6: // Return
                    {
                        GuildGump.EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));

                        break;
                    }
            }
        }
    }
}
