using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildRejectWarGump : GuildListGump
    {
        private GuildRejectWarGump(Guild guild) : base(guild, true, guild.WarInvitations)
        {
        }

        public static void DisplayTo(Mobile from, Guild guild)
        {
            if (from?.NetState == null || guild == null)
            {
                return;
            }

            GuildGump.EnsureClosed(from);
            from.SendGump(new GuildRejectWarGump(guild));
        }

        protected override void BuildHeader(ref DynamicGumpBuilder builder)
        {
            builder.AddHtmlLocalized(20, 10, 400, 35, 1011148); // Select the guild to reject their invitations:

            builder.AddButton(20, 400, 4005, 4007, 1);
            builder.AddHtmlLocalized(55, 400, 245, 30, 1011101); // Reject war invitations.

            builder.AddButton(300, 400, 4005, 4007, 2);
            builder.AddHtmlLocalized(335, 400, 100, 35, 1011012); // CANCEL
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            var from = state.Mobile;
            if (GuildGump.BadLeader(from, _guild))
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
                        var g = _list[index];

                        if (g != null)
                        {
                            _guild.WarInvitations.Remove(g);
                            g.WarDeclarations.Remove(_guild);

                            if (_guild.WarInvitations.Count > 0)
                            {
                                DisplayTo(from, _guild);
                            }
                            else
                            {
                                GuildmasterGump.DisplayTo(from, _guild);
                            }
                        }
                    }
                }
            }
            else if (info.ButtonID == 2)
            {
                GuildmasterGump.DisplayTo(from, _guild);
            }
        }
    }
}
