using System.Collections.Generic;
using Server.Factions;
using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildDeclareWarGump : GuildListGump
    {
        private GuildDeclareWarGump(Guild guild, List<Guild> list) : base(guild, true, list)
        {
        }

        public static void DisplayTo(Mobile from, Guild guild, List<Guild> list)
        {
            if (from?.NetState == null || guild == null || list == null || list.Count == 0)
            {
                return;
            }

            GuildGump.EnsureClosed(from);
            from.SendGump(new GuildDeclareWarGump(guild, list));
        }

        protected override void BuildHeader(ref DynamicGumpBuilder builder)
        {
            builder.AddHtmlLocalized(20, 10, 400, 35, 1011065); // Select the guild you wish to declare war on.

            builder.AddButton(20, 400, 4005, 4007, 1);
            builder.AddHtmlLocalized(55, 400, 245, 30, 1011068); // Send the challenge!

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
                            if (g == _guild)
                            {
                                from.SendLocalizedMessage(501184); // You cannot declare war against yourself!
                            }
                            else if (g.WarInvitations.Contains(_guild) && _guild.WarDeclarations.Contains(g) ||
                                     _guild.IsWar(g))
                            {
                                from.SendLocalizedMessage(501183); // You are already at war with that guild.
                            }
                            else if (Faction.Find(_guild.Leader) != null)
                            {
                                from.SendLocalizedMessage(1005288); // You cannot declare war while you are in a faction
                            }
                            else
                            {
                                if (!_guild.WarDeclarations.Contains(g))
                                {
                                    _guild.WarDeclarations.Add(g);
                                    // Guild Message: Your guild has sent an invitation for war:
                                    _guild.GuildMessage(1018019, true, $"{g.Name} ({g.Abbreviation})");
                                }

                                if (!g.WarInvitations.Contains(_guild))
                                {
                                    g.WarInvitations.Add(_guild);
                                    // Guild Message: Your guild has received an invitation to war:
                                    g.GuildMessage(1018021, true, $"{_guild.Name} ({_guild.Abbreviation})");
                                }
                            }

                            GuildWarAdminGump.DisplayTo(from, _guild);
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
