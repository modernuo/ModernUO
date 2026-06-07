using System;
using Server.Guilds;
using Server.Prompts;

namespace Server.Gumps
{
    public class GuildAbbrvPrompt : Prompt
    {
        private readonly Guild _guild;

        public GuildAbbrvPrompt(Guild g) => _guild = g;

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

            if (textSpan.Length > 3)
            {
                textSpan = textSpan[..3];
            }

            if (textSpan.Length > 0)
            {
                if (BaseGuild.FindByAbbrev(textSpan) != null)
                {
                    from.SendMessage($"{textSpan} conflicts with the abbreviation of an existing guild.");
                }
                else
                {
                    text = textSpan.ToString();
                    _guild.Abbreviation = text;
                    _guild.GuildMessage(1018025, true, text); // Your guild abbreviation has changed:
                }
            }

            GuildmasterGump.DisplayTo(from, _guild);
        }
    }
}
