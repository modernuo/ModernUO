using Server.Guilds;
using Server.Prompts;

namespace Server.Gumps
{
    public class GuildDeclareWarPrompt : Prompt
    {
        private readonly Guild _guild;

        public GuildDeclareWarPrompt(Guild g) => _guild = g;

        public override void OnCancel(Mobile from)
        {
            if (GuildGump.BadLeader(from, _guild))
            {
                return;
            }

            GuildWarAdminGump.DisplayTo(from, _guild);
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (GuildGump.BadLeader(from, _guild))
            {
                return;
            }

            var textSpan = text.Trim();

            if (textSpan.Length >= 3)
            {
                var guilds = BaseGuild.Search(textSpan).SafeConvertList<BaseGuild, Guild>();

                if (guilds.Count > 0)
                {
                    GuildDeclareWarGump.DisplayTo(from, _guild, guilds);
                }
                else
                {
                    GuildWarAdminGump.DisplayTo(from, _guild);
                    from.SendLocalizedMessage(1018003); // No guilds found matching - try another name in the search
                }
            }
            else
            {
                from.SendMessage("Search string must be at least three letters in length.");
            }
        }
    }
}
