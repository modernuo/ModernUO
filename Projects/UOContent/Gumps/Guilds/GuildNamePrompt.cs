using System;
using Server.Guilds;
using Server.Prompts;

namespace Server.Gumps
{
    public class GuildNamePrompt : Prompt
    {
        private readonly Guild _guild;

        public GuildNamePrompt(Mobile m, Guild g) => _guild = g;

        public override void OnCancel(Mobile from)
        {
            if (GuildGump.BadLeader(from, _guild))
            {
                return;
            }

            GuildmasterGump.DisplayTo(from, _guild);
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (GuildGump.BadLeader(from, _guild))
            {
                return;
            }

            var textSpan = text.AsSpan().Trim();

            if (textSpan.Length > 40)
            {
                textSpan = textSpan[..40];
            }

            if (textSpan.Length > 0)
            {
                if (BaseGuild.FindByName(textSpan) != null)
                {
                    from.SendMessage($"{textSpan} conflicts with the name of an existing guild.");
                }
                else
                {
                    text = textSpan.ToString();
                    _guild.Name = text;
                    _guild.GuildMessage(1018024, true, text); // The name of your guild has changed:
                }
            }

            GuildmasterGump.DisplayTo(from, _guild);
        }
    }
}
