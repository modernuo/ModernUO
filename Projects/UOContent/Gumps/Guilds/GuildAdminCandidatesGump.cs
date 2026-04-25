using Server.Factions;
using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildAdminCandidatesGump : GuildMobileListGump
    {
        private GuildAdminCandidatesGump(Mobile from, Guild guild) : base(from, guild, true, guild.Candidates)
        {
        }

        public static void DisplayTo(Mobile from, Guild guild)
        {
            if (from?.NetState == null || guild == null)
            {
                return;
            }

            GuildGump.EnsureClosed(from);
            from.SendGump(new GuildAdminCandidatesGump(from, guild));
        }

        protected override void BuildHeader(ref DynamicGumpBuilder builder)
        {
            builder.AddHtmlLocalized(20, 10, 400, 35, 1013075); // Accept or Refuse candidates for membership

            builder.AddButton(20, 400, 4005, 4007, 1);
            builder.AddHtmlLocalized(55, 400, 245, 30, 1013076); // Accept

            builder.AddButton(300, 400, 4005, 4007, 2);
            builder.AddHtmlLocalized(335, 400, 100, 35, 1013077); // Refuse
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (GuildGump.BadLeader(_mobile, _guild))
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 0:
                    {
                        GuildmasterGump.DisplayTo(_mobile, _guild);
                        break;
                    }
                case 1: // Accept
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
                                    var guildState = PlayerState.Find(_guild.Leader);
                                    var targetState = PlayerState.Find(m);

                                    var guildFaction = guildState?.Faction;
                                    var targetFaction = targetState?.Faction;

                                    if (guildFaction != targetFaction)
                                    {
                                        if (guildFaction == null)
                                        {
                                            // That player cannot join a non-faction guild.
                                            _mobile.SendLocalizedMessage(1013027);
                                        }
                                        else if (targetFaction == null)
                                        {
                                            // That player must be in a faction before joining this guild.
                                            _mobile.SendLocalizedMessage(1013026);
                                        }
                                        else
                                        {
                                            // That person has a different faction affiliation.
                                            _mobile.SendLocalizedMessage(1013028);
                                        }

                                        break;
                                    }

                                    if (targetState?.IsLeaving == true)
                                    {
                                        // OSI does this quite strangely, so we'll just do it this way
                                        _mobile.SendMessage(
                                            "That person is quitting their faction and so you may not recruit them."
                                        );
                                        break;
                                    }

                                    _guild.Candidates.Remove(m);
                                    _guild.Accepted.Add(m);

                                    if (_guild.Candidates.Count > 0)
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

                        break;
                    }
                case 2: // Refuse
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
                                    _guild.Candidates.Remove(m);

                                    if (_guild.Candidates.Count > 0)
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

                        break;
                    }
            }
        }
    }
}
