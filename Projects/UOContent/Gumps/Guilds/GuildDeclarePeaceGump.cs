using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildDeclarePeaceGump : GuildListGump
    {
        private GuildDeclarePeaceGump(Mobile from, Guild guild) : base(from, guild, true, guild.Enemies)
        {
        }

        public static void DisplayTo(Mobile from, Guild guild)
        {
            if (from?.NetState == null || guild == null)
            {
                return;
            }

            GuildGump.EnsureClosed(from);
            from.SendGump(new GuildDeclarePeaceGump(from, guild));
        }

        protected override void BuildHeader(ref DynamicGumpBuilder builder)
        {
            builder.AddHtmlLocalized(20, 10, 400, 35, 1011137); // Select the guild you wish to declare peace with.

            builder.AddButton(20, 400, 4005, 4007, 1);
            builder.AddHtmlLocalized(55, 400, 245, 30, 1011138); // Send the olive branch.

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
                        var g = _list[index];

                        if (g != null)
                        {
                            _guild.RemoveEnemy(g);
                            // Guild Message: You are now at peace with this guild:
                            _guild.GuildMessage(1018018, true, $"{g.Name} ({g.Abbreviation})");

                            if (_guild.Enemies.Count > 0)
                            {
                                DisplayTo(_mobile, _guild);
                            }
                            else
                            {
                                GuildmasterGump.DisplayTo(_mobile, _guild);
                            }
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
