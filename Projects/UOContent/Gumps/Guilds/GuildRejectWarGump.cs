using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildRejectWarGump : GuildListGump
    {
        public GuildRejectWarGump(Mobile from, Guild guild) : base(from, guild, true, guild.WarInvitations)
        {
        }

        protected override void Design()
        {
            AddHtmlLocalized(20, 10, 400, 35, 1011148); // Select the guild to reject their invitations:

            AddButton(20, 400, 4005, 4007, 1);
            AddHtmlLocalized(55, 400, 245, 30, 1011101); // Reject war invitations.

            AddButton(300, 400, 4005, 4007, 2);
            AddHtmlLocalized(335, 400, 100, 35, 1011012); // CANCEL
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (GuildGump.BadLeader(m_Mobile, m_Guild))
            {
                return;
            }

            if (info.ButtonID == 1)
            {
                var switches = info.Switches;

                if (switches.Length > 0)
                {
                    var index = switches[0];

                    if (index >= 0 && index < m_List.Count)
                    {
                        var g = m_List[index];

                        if (g != null)
                        {
                            m_Guild.WarInvitations.Remove(g);
                            g.WarDeclarations.Remove(m_Guild);

                            GuildGump.EnsureClosed(m_Mobile);

                            if (m_Guild.WarInvitations.Count > 0)
                            {
                                m_Mobile.SendGump(new GuildRejectWarGump(m_Mobile, m_Guild));
                            }
                            else
                            {
                                m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
                            }
                        }
                    }
                }
            }
            else if (info.ButtonID == 2)
            {
                GuildGump.EnsureClosed(m_Mobile);
                m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
            }
        }
    }
}
