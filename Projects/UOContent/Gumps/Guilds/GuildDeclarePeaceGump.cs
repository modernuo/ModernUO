using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildDeclarePeaceGump : GuildListGump
    {
        public GuildDeclarePeaceGump(Mobile from, Guild guild) : base(from, guild, true, guild.Enemies)
        {
        }

        protected override void Design()
        {
            AddHtmlLocalized(20, 10, 400, 35, 1011137); // Select the guild you wish to declare peace with.

            AddButton(20, 400, 4005, 4007, 1);
            AddHtmlLocalized(55, 400, 245, 30, 1011138); // Send the olive branch.

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
                            m_Guild.RemoveEnemy(g);
                            // Guild Message: You are now at peace with this guild:
                            m_Guild.GuildMessage(1018018, true, $"{g.Name} ({g.Abbreviation})");

                            GuildGump.EnsureClosed(m_Mobile);

                            if (m_Guild.Enemies.Count > 0)
                            {
                                m_Mobile.SendGump(new GuildDeclarePeaceGump(m_Mobile, m_Guild));
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
