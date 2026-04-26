using System;
using Server.Guilds;
using Server.Prompts;

namespace Server.Gumps
{
    public class GuildCharterPrompt : Prompt
    {
        private readonly Guild _guild;

        public GuildCharterPrompt(Guild g) => _guild = g;

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

            if (textSpan.Length > 50)
            {
                textSpan = textSpan[..50];
            }

            if (textSpan.Length > 0)
            {
                _guild.Charter = textSpan.ToString();
            }

            from.SendLocalizedMessage(1013072); // Enter the new website for the guild (50 characters max):
            from.Prompt = new GuildWebsitePrompt(from, _guild);

            GuildmasterGump.DisplayTo(from, _guild);
        }
    }
}
