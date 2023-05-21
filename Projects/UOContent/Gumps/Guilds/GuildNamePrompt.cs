using Server.Guilds;
using Server.Prompts;

namespace Server.Gumps
{
    public class GuildNamePrompt : Prompt
    {
        private readonly Guild m_Guild;
        private readonly Mobile m_Mobile;

        public GuildNamePrompt(Mobile m, Guild g)
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

            if (text.Length > 40)
            {
                text = text[..40];
            }

            if (text.Length > 0)
            {
                if (BaseGuild.FindByName(text) != null)
                {
                    m_Mobile.SendMessage($"{text} conflicts with the name of an existing guild.");
                }
                else
                {
                    m_Guild.Name = text;
                    m_Guild.GuildMessage(1018024, true, text); // The name of your guild has changed:
                }
            }

            GuildGump.EnsureClosed(m_Mobile);
            m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
        }
    }
}
