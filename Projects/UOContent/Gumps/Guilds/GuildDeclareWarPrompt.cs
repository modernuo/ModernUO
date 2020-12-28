using Server.Guilds;
using Server.Prompts;

namespace Server.Gumps
{
    public class GuildDeclareWarPrompt : Prompt
    {
        private readonly Guild m_Guild;
        private readonly Mobile m_Mobile;

        public GuildDeclareWarPrompt(Mobile m, Guild g)
        {
            m_Mobile = m;
            m_Guild = g;
        }

        public override void OnCancel(Mobile from)
        {
            if (GuildGump.BadLeader(m_Mobile, m_Guild))
            {
                return;
            }

            GuildGump.EnsureClosed(m_Mobile);
            m_Mobile.SendGump(new GuildWarAdminGump(m_Mobile, m_Guild));
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (GuildGump.BadLeader(m_Mobile, m_Guild))
            {
                return;
            }

            text = text.Trim();

            if (text.Length >= 3)
            {
                var guilds = BaseGuild.Search(text).SafeConvertList<BaseGuild, Guild>();

                GuildGump.EnsureClosed(m_Mobile);

                if (guilds.Count > 0)
                {
                    m_Mobile.SendGump(new GuildDeclareWarGump(m_Mobile, m_Guild, guilds));
                }
                else
                {
                    m_Mobile.SendGump(new GuildWarAdminGump(m_Mobile, m_Guild));
                    m_Mobile.SendLocalizedMessage(1018003); // No guilds found matching - try another name in the search
                }
            }
            else
            {
                m_Mobile.SendMessage("Search string must be at least three letters in length.");
            }
        }
    }
}
