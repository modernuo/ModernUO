using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildRosterGump : GuildMobileListGump
    {
        private GuildRosterGump(Guild guild) : base(guild, false, guild.Members)
        {
        }

        public static void DisplayTo(Mobile from, Guild guild)
        {
            if (from?.NetState == null || guild == null)
            {
                return;
            }

            GuildGump.EnsureClosed(from);
            from.SendGump(new GuildRosterGump(guild));
        }

        protected override void BuildHeader(ref DynamicGumpBuilder builder)
        {
            builder.AddHtml(20, 10, 500, 35, $"<center>{_guild.Name}</center>");

            builder.AddButton(20, 400, 4005, 4007, 1);
            builder.AddHtmlLocalized(55, 400, 300, 35, 1011120); // Return to the main menu.
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            var from = state.Mobile;
            if (GuildGump.BadMember(from, _guild))
            {
                return;
            }

            if (info.ButtonID == 1)
            {
                GuildGump.DisplayTo(from, _guild);
            }
        }
    }
}
