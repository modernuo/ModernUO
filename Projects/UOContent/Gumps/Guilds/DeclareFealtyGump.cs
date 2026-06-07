using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class DeclareFealtyGump : GuildMobileListGump
    {
        private DeclareFealtyGump(Guild guild) : base(guild, true, guild.Members)
        {
        }

        public static void DisplayTo(Mobile from, Guild guild)
        {
            if (from?.NetState == null || guild == null)
            {
                return;
            }

            GuildGump.EnsureClosed(from);
            from.SendGump(new DeclareFealtyGump(guild));
        }

        protected override void BuildHeader(ref DynamicGumpBuilder builder)
        {
            builder.AddHtmlLocalized(20, 10, 400, 35, 1011097); // Declare your fealty

            builder.AddButton(20, 400, 4005, 4007, 1);
            builder.AddHtmlLocalized(55, 400, 250, 35, 1011098); // I have selected my new lord.

            builder.AddButton(300, 400, 4005, 4007, 0);
            builder.AddHtmlLocalized(335, 400, 100, 35, 1011012); // CANCEL
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            var from = state.Mobile;
            if (info.ButtonID == 0 || GuildGump.BadMember(from, _guild))
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
                            state.Mobile.GuildFealty = m;
                        }
                    }
                }
            }

            GuildGump.DisplayTo(from, _guild);
        }
    }
}
