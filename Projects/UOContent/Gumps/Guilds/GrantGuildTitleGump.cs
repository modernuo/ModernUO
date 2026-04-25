using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GrantGuildTitleGump : GuildMobileListGump
    {
        private GrantGuildTitleGump(Mobile from, Guild guild) : base(from, guild, true, guild.Members)
        {
        }

        public static void DisplayTo(Mobile from, Guild guild)
        {
            if (from?.NetState == null || guild == null)
            {
                return;
            }

            GuildGump.EnsureClosed(from);
            from.SendGump(new GrantGuildTitleGump(from, guild));
        }

        protected override void BuildHeader(ref DynamicGumpBuilder builder)
        {
            builder.AddHtmlLocalized(20, 10, 400, 35, 1011118); // Grant a title to another member.

            builder.AddButton(20, 400, 4005, 4007, 1);
            builder.AddHtmlLocalized(55, 400, 245, 30, 1011127); // I dub thee...

            builder.AddButton(300, 400, 4005, 4007, 2);
            builder.AddHtmlLocalized(335, 400, 100, 35, 1011012); // CANCEL
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (GuildGump.BadLeader(_mobile, _guild))
            {
                return;
            }

            if (info.ButtonID == 1)
            {
                var switches = info.Switches;

                if (switches.Length > 0)
                {
                    var index = switches[0];

                    if (index >= 0 && index < _list.Count)
                    {
                        var m = _list[index];

                        if (m?.Deleted == false)
                        {
                            _mobile.SendLocalizedMessage(1013074); // New title (20 characters max):
                            _mobile.Prompt = new GuildTitlePrompt(_mobile, m, _guild);
                        }
                    }
                }
            }
            else if (info.ButtonID == 2)
            {
                GuildmasterGump.DisplayTo(_mobile, _guild);
            }
        }
    }
}
