using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildWarGump : Gump
    {
        private readonly Guild m_Guild;
        private readonly Mobile m_Mobile;

        public GuildWarGump(Mobile from, Guild guild) : base(20, 30)
        {
            m_Mobile = from;
            m_Guild = guild;

            Draggable = false;

            AddPage(0);
            AddBackground(0, 0, 550, 440, 5054);
            AddBackground(10, 10, 530, 420, 3000);

            AddHtmlLocalized(20, 10, 500, 35, 1011133); // <center>WARFARE STATUS</center>

            AddButton(20, 400, 4005, 4007, 1);
            AddHtmlLocalized(55, 400, 300, 35, 1011120); // Return to the main menu.

            AddPage(1);

            AddButton(375, 375, 5224, 5224, 0, GumpButtonType.Page, 2);
            AddHtmlLocalized(410, 373, 100, 25, 1011066); // Next page

            AddHtmlLocalized(20, 45, 400, 20, 1011134); // We are at war with:

            var enemies = guild.Enemies;

            if (enemies.Count == 0)
            {
                AddHtmlLocalized(20, 65, 400, 20, 1013033); // No current wars
            }
            else
            {
                for (var i = 0; i < enemies.Count; ++i)
                {
                    var g = enemies[i];

                    AddHtml(20, 65 + i * 20, 300, 20, g.Name);
                }
            }

            AddPage(2);

            AddButton(375, 375, 5224, 5224, 0, GumpButtonType.Page, 3);
            AddHtmlLocalized(410, 373, 100, 25, 1011066); // Next page

            AddButton(30, 375, 5223, 5223, 0, GumpButtonType.Page, 1);
            AddHtmlLocalized(65, 373, 150, 25, 1011067); // Previous page

            AddHtmlLocalized(20, 45, 400, 20, 1011136); // Guilds that we have declared war on:

            var declared = guild.WarDeclarations;

            if (declared.Count == 0)
            {
                AddHtmlLocalized(20, 65, 400, 20, 1018012); // No current invitations received for war.
            }
            else
            {
                for (var i = 0; i < declared.Count; ++i)
                {
                    var g = declared[i];

                    AddHtml(20, 65 + i * 20, 300, 20, g.Name);
                }
            }

            AddPage(3);

            AddButton(30, 375, 5223, 5223, 0, GumpButtonType.Page, 2);
            AddHtmlLocalized(65, 373, 150, 25, 1011067); // Previous page

            AddHtmlLocalized(20, 45, 400, 20, 1011135); // Guilds that have declared war on us:

            var invites = guild.WarInvitations;

            if (invites.Count == 0)
            {
                AddHtmlLocalized(20, 65, 400, 20, 1013055); // No current war declarations
            }
            else
            {
                for (var i = 0; i < invites.Count; ++i)
                {
                    var g = invites[i];

                    AddHtml(20, 65 + i * 20, 300, 20, g.Name);
                }
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (GuildGump.BadMember(m_Mobile, m_Guild))
            {
                return;
            }

            if (info.ButtonID == 1)
            {
                GuildGump.EnsureClosed(m_Mobile);
                m_Mobile.SendGump(new GuildGump(m_Mobile, m_Guild));
            }
        }
    }
}
