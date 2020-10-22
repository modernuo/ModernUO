using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildGump : Gump
    {
        private readonly Guild m_Guild;
        private readonly Mobile m_Mobile;

        public GuildGump(Mobile beholder, Guild guild) : base(20, 30)
        {
            m_Mobile = beholder;
            m_Guild = guild;

            Draggable = false;

            AddPage(0);
            AddBackground(0, 0, 550, 400, 5054);
            AddBackground(10, 10, 530, 380, 3000);

            AddHtml(20, 15, 200, 35, guild.Name);

            var leader = guild.Leader;

            if (leader != null)
            {
                var leadTitle = leader.GuildTitle?.Trim();
                var leadName = (leader.Name?.Trim()).DefaultIfNullOrEmpty("(empty)");
                var text = leadTitle?.Length > 0 ? $"{leadTitle}: {leadName}" : leadName;

                AddHtml(220, 15, 250, 35, text);
            }

            AddButton(20, 50, 4005, 4007, 1);
            AddHtmlLocalized(55, 50, 100, 20, 1013022); // Loyal to

            var fealty = beholder.GuildFealty;

            if (fealty == null || !guild.IsMember(fealty))
            {
                fealty = leader;
            }

            fealty ??= beholder;

            var fealtyName = (fealty.Name?.Trim()).DefaultIfNullOrEmpty("(empty)");

            if (beholder == fealty)
            {
                AddHtmlLocalized(55, 70, 470, 20, 1018002); // yourself
            }
            else
            {
                AddHtml(55, 70, 470, 20, fealtyName);
            }

            AddButton(215, 50, 4005, 4007, 2);
            AddHtmlLocalized(250, 50, 170, 20, 1013023);                                       // Display guild abbreviation
            AddHtmlLocalized(250, 70, 50, 20, beholder.DisplayGuildTitle ? 1011262 : 1011263); // on/off

            AddButton(20, 100, 4005, 4007, 3);
            AddHtmlLocalized(55, 100, 470, 30, 1011086); // View the current roster.

            AddButton(20, 130, 4005, 4007, 4);
            AddHtmlLocalized(55, 130, 470, 30, 1011085); // Recruit someone into the guild.

            if (guild.Candidates.Count > 0)
            {
                AddButton(20, 160, 4005, 4007, 5);
                AddHtmlLocalized(55, 160, 470, 30, 1011093); // View list of candidates who have been sponsored to the guild.
            }
            else
            {
                AddImage(20, 160, 4020);
                AddHtmlLocalized(55, 160, 470, 30, 1013031); // There are currently no candidates for membership.
            }

            AddButton(20, 220, 4005, 4007, 6);
            AddHtmlLocalized(55, 220, 470, 30, 1011087); // View the guild's charter.

            AddButton(20, 250, 4005, 4007, 7);
            AddHtmlLocalized(55, 250, 470, 30, 1011092); // Resign from the guild.

            AddButton(20, 280, 4005, 4007, 8);
            AddHtmlLocalized(55, 280, 470, 30, 1011095); // View list of guilds you are at war with.

            if (beholder.AccessLevel >= AccessLevel.GameMaster || beholder == leader)
            {
                AddButton(20, 310, 4005, 4007, 9);
                AddHtmlLocalized(55, 310, 470, 30, 1011094); // Access guildmaster functions.
            }
            else
            {
                AddImage(20, 310, 4020);
                AddHtmlLocalized(55, 310, 470, 30, 1018013); // Reserved for guildmaster
            }

            AddButton(20, 360, 4005, 4007, 0);
            AddHtmlLocalized(55, 360, 470, 30, 1011441); // EXIT
        }

        public static void EnsureClosed(Mobile m)
        {
            m.CloseGump<DeclareFealtyGump>();
            m.CloseGump<GrantGuildTitleGump>();
            m.CloseGump<GuildAdminCandidatesGump>();
            m.CloseGump<GuildCandidatesGump>();
            m.CloseGump<GuildChangeTypeGump>();
            m.CloseGump<GuildCharterGump>();
            m.CloseGump<GuildDismissGump>();
            m.CloseGump<GuildGump>();
            m.CloseGump<GuildmasterGump>();
            m.CloseGump<GuildRosterGump>();
            m.CloseGump<GuildWarGump>();
        }

        public static bool BadLeader(Mobile m, Guild g)
        {
            if (m.Deleted || g.Disbanded || m.AccessLevel < AccessLevel.GameMaster && g.Leader != m)
            {
                return true;
            }

            var stone = g.Guildstone;

            return stone?.Deleted != false || !m.InRange(stone.GetWorldLocation(), 2);
        }

        public static bool BadMember(Mobile m, Guild g)
        {
            if (m.Deleted || g.Disbanded || m.AccessLevel < AccessLevel.GameMaster && !g.IsMember(m))
            {
                return true;
            }

            var stone = g.Guildstone;

            return stone?.Deleted != false || !m.InRange(stone.GetWorldLocation(), 2);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (BadMember(m_Mobile, m_Guild))
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 1: // Loyalty
                    {
                        EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new DeclareFealtyGump(m_Mobile, m_Guild));

                        break;
                    }
                case 2: // Toggle display abbreviation
                    {
                        m_Mobile.DisplayGuildTitle = !m_Mobile.DisplayGuildTitle;

                        EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildGump(m_Mobile, m_Guild));

                        break;
                    }
                case 3: // View the current roster
                    {
                        EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildRosterGump(m_Mobile, m_Guild));

                        break;
                    }
                case 4: // Recruit
                    {
                        m_Mobile.Target = new GuildRecruitTarget(m_Mobile, m_Guild);

                        break;
                    }
                case 5: // Membership candidates
                    {
                        EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildCandidatesGump(m_Mobile, m_Guild));

                        break;
                    }
                case 6: // View charter
                    {
                        EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildCharterGump(m_Mobile, m_Guild));

                        break;
                    }
                case 7: // Resign
                    {
                        m_Guild.RemoveMember(m_Mobile);

                        break;
                    }
                case 8: // View wars
                    {
                        EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildWarGump(m_Mobile, m_Guild));

                        break;
                    }
                case 9: // Guildmaster functions
                    {
                        if (m_Mobile.AccessLevel >= AccessLevel.GameMaster || m_Guild.Leader == m_Mobile)
                        {
                            EnsureClosed(m_Mobile);
                            m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
                        }

                        break;
                    }
            }
        }
    }
}
