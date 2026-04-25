using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildDismissGump : GuildMobileListGump
    {
        private GuildDismissGump(Mobile from, Guild guild) : base(from, guild, true, guild.Members)
        {
        }

        public static void DisplayTo(Mobile from, Guild guild)
        {
            if (from?.NetState == null || guild == null)
            {
                return;
            }

            GuildGump.EnsureClosed(from);
            from.SendGump(new GuildDismissGump(from, guild));
        }

        protected override void BuildHeader(ref DynamicGumpBuilder builder)
        {
            builder.AddHtmlLocalized(20, 10, 400, 35, 1011124); // Whom do you wish to dismiss?

            builder.AddButton(20, 400, 4005, 4007, 1);
            builder.AddHtmlLocalized(55, 400, 245, 30, 1011125); // Kick them out!

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
                            _guild.RemoveMember(m);

                            if (_mobile.AccessLevel >= AccessLevel.GameMaster || _mobile == _guild.Leader)
                            {
                                GuildmasterGump.DisplayTo(_mobile, _guild);
                            }
                        }
                    }
                }
            }
            else if (info.ButtonID == 2 && (_mobile.AccessLevel >= AccessLevel.GameMaster || _mobile == _guild.Leader))
            {
                GuildmasterGump.DisplayTo(_mobile, _guild);
            }
        }
    }
}
