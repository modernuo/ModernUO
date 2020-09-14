using Server.Factions;
using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildAdminCandidatesGump : GuildMobileListGump
    {
        public GuildAdminCandidatesGump(Mobile from, Guild guild) : base(from, guild, true, guild.Candidates)
        {
        }

        protected override void Design()
        {
            AddHtmlLocalized(20, 10, 400, 35, 1013075); // Accept or Refuse candidates for membership

            AddButton(20, 400, 4005, 4007, 1);
            AddHtmlLocalized(55, 400, 245, 30, 1013076); // Accept

            AddButton(300, 400, 4005, 4007, 2);
            AddHtmlLocalized(335, 400, 100, 35, 1013077); // Refuse
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (GuildGump.BadLeader(m_Mobile, m_Guild))
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 0:
                    {
                        GuildGump.EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));

                        break;
                    }
                case 1: // Accept
                    {
                        var switches = info.Switches;

                        if (switches.Length > 0)
                        {
                            var index = switches[0];

                            if (index >= 0 && index < m_List.Count)
                            {
                                var m = m_List[index];

                                if (m?.Deleted == false)
                                {
                                    var guildState = PlayerState.Find(m_Guild.Leader);
                                    var targetState = PlayerState.Find(m);

                                    var guildFaction = guildState?.Faction;
                                    var targetFaction = targetState?.Faction;

                                    if (guildFaction != targetFaction)
                                    {
                                        if (guildFaction == null)
                                        {
                                            m_Mobile.SendLocalizedMessage(
                                                1013027
                                            ); // That player cannot join a non-faction guild.
                                        }
                                        else if (targetFaction == null)
                                        {
                                            m_Mobile.SendLocalizedMessage(
                                                1013026
                                            ); // That player must be in a faction before joining this guild.
                                        }
                                        else
                                        {
                                            m_Mobile.SendLocalizedMessage(
                                                1013028
                                            ); // That person has a different faction affiliation.
                                        }

                                        break;
                                    }

                                    if (targetState?.IsLeaving == true)
                                    {
                                        // OSI does this quite strangely, so we'll just do it this way
                                        m_Mobile.SendMessage(
                                            "That person is quitting their faction and so you may not recruit them."
                                        );
                                        break;
                                    }

                                    m_Guild.Candidates.Remove(m);
                                    m_Guild.Accepted.Add(m);

                                    GuildGump.EnsureClosed(m_Mobile);

                                    if (m_Guild.Candidates.Count > 0)
                                    {
                                        m_Mobile.SendGump(new GuildAdminCandidatesGump(m_Mobile, m_Guild));
                                    }
                                    else
                                    {
                                        m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
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

                            if (index >= 0 && index < m_List.Count)
                            {
                                var m = m_List[index];

                                if (m?.Deleted == false)
                                {
                                    m_Guild.Candidates.Remove(m);

                                    GuildGump.EnsureClosed(m_Mobile);

                                    if (m_Guild.Candidates.Count > 0)
                                    {
                                        m_Mobile.SendGump(new GuildAdminCandidatesGump(m_Mobile, m_Guild));
                                    }
                                    else
                                    {
                                        m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
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
