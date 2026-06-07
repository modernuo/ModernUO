using System;
using Server.Guilds;
using Server.Prompts;

namespace Server.Gumps
{
    public class GuildTitlePrompt : Prompt
    {
        private readonly Guild m_Guild;
        private readonly Mobile m_Target;

        public GuildTitlePrompt(Mobile target, Guild g)
        {
            m_Target = target;
            m_Guild = g;
        }

        public override void OnCancel(Mobile from)
        {
            if (GuildGump.BadLeader(from, m_Guild))
            {
                return;
            }

            if (m_Target.Deleted || !m_Guild.IsMember(m_Target))
            {
                return;
            }

            GuildmasterGump.DisplayTo(from, m_Guild);
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (GuildGump.BadLeader(from, m_Guild))
            {
                return;
            }

            if (m_Target.Deleted || !m_Guild.IsMember(m_Target))
            {
                return;
            }

            var textSpan = text.AsSpan().Trim();

            if (textSpan.Length > 20)
            {
                textSpan = textSpan[..20];
            }

            if (textSpan.Length > 0)
            {
                m_Target.GuildTitle = textSpan.ToString();
            }

            GuildmasterGump.DisplayTo(from, m_Guild);
        }
    }
}
