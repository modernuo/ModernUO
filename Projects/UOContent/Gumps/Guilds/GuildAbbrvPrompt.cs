using Server.Guilds;
using Server.Prompts;

namespace Server.Gumps
{
    public class GuildAbbrvPrompt : Prompt
    {
        private readonly Guild m_Guild;
        private readonly Mobile m_Mobile;

        public GuildAbbrvPrompt(Mobile m, Guild g)
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
            m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (GuildGump.BadLeader(m_Mobile, m_Guild))
            {
                return;
            }

            text = text.Trim();

            if (text.Length > 3)
            {
                text = text[..3];
            }

            if (text.Length > 0)
            {
                if (BaseGuild.FindByAbbrev(text) != null)
                {
                    m_Mobile.SendMessage($"{text} conflicts with the abbreviation of an existing guild.");
                }
                else
                {
                    m_Guild.Abbreviation = text;
                    m_Guild.GuildMessage(1018025, true, text); // Your guild abbreviation has changed:
                }
            }

            GuildGump.EnsureClosed(m_Mobile);
            m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
        }
    }
}
