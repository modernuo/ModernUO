using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildGump : DynamicGump
    {
        private readonly Guild _guild;
        private readonly Mobile _mobile;

        public override bool Singleton => true;

        private GuildGump(Mobile beholder, Guild guild) : base(20, 30)
        {
            _mobile = beholder;
            _guild = guild;
        }

        public static void DisplayTo(Mobile from, Guild guild)
        {
            if (from?.NetState == null || guild == null)
            {
                return;
            }

            EnsureClosed(from);
            from.SendGump(new GuildGump(from, guild));
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.SetNoMove();

            builder.AddPage();
            builder.AddBackground(0, 0, 550, 400, 5054);
            builder.AddBackground(10, 10, 530, 380, 3000);

            builder.AddHtml(20, 15, 200, 35, _guild.Name);

            var leader = _guild.Leader;

            if (leader != null)
            {
                var leadTitle = leader.GuildTitle?.Trim();
                var leadName = (leader.Name?.Trim()).DefaultIfNullOrEmpty("(empty)");
                var text = leadTitle?.Length > 0 ? $"{leadTitle}: {leadName}" : leadName;

                builder.AddHtml(220, 15, 250, 35, text);
            }

            builder.AddButton(20, 50, 4005, 4007, 1);
            builder.AddHtmlLocalized(55, 50, 100, 20, 1013022); // Loyal to

            var fealty = _mobile.GuildFealty;

            if (fealty == null || !_guild.IsMember(fealty))
            {
                fealty = leader;
            }

            fealty ??= _mobile;

            var fealtyName = (fealty.Name?.Trim()).DefaultIfNullOrEmpty("(empty)");

            if (_mobile == fealty)
            {
                builder.AddHtmlLocalized(55, 70, 470, 20, 1018002); // yourself
            }
            else
            {
                builder.AddHtml(55, 70, 470, 20, fealtyName);
            }

            builder.AddButton(215, 50, 4005, 4007, 2);
            builder.AddHtmlLocalized(250, 50, 170, 20, 1013023);                                       // Display guild abbreviation
            builder.AddHtmlLocalized(250, 70, 50, 20, _mobile.DisplayGuildTitle ? 1011262 : 1011263); // on/off

            builder.AddButton(20, 100, 4005, 4007, 3);
            builder.AddHtmlLocalized(55, 100, 470, 30, 1011086); // View the current roster.

            builder.AddButton(20, 130, 4005, 4007, 4);
            builder.AddHtmlLocalized(55, 130, 470, 30, 1011085); // Recruit someone into the guild.

            if (_guild.Candidates.Count > 0)
            {
                builder.AddButton(20, 160, 4005, 4007, 5);
                builder.AddHtmlLocalized(55, 160, 470, 30, 1011093); // View list of candidates who have been sponsored to the guild.
            }
            else
            {
                builder.AddImage(20, 160, 4020);
                builder.AddHtmlLocalized(55, 160, 470, 30, 1013031); // There are currently no candidates for membership.
            }

            builder.AddButton(20, 220, 4005, 4007, 6);
            builder.AddHtmlLocalized(55, 220, 470, 30, 1011087); // View the guild's charter.

            builder.AddButton(20, 250, 4005, 4007, 7);
            builder.AddHtmlLocalized(55, 250, 470, 30, 1011092); // Resign from the guild.

            builder.AddButton(20, 280, 4005, 4007, 8);
            builder.AddHtmlLocalized(55, 280, 470, 30, 1011095); // View list of guilds you are at war with.

            if (_mobile.AccessLevel >= AccessLevel.GameMaster || _mobile == leader)
            {
                builder.AddButton(20, 310, 4005, 4007, 9);
                builder.AddHtmlLocalized(55, 310, 470, 30, 1011094); // Access guildmaster functions.
            }
            else
            {
                builder.AddImage(20, 310, 4020);
                builder.AddHtmlLocalized(55, 310, 470, 30, 1018013); // Reserved for guildmaster
            }

            builder.AddButton(20, 360, 4005, 4007, 0);
            builder.AddHtmlLocalized(55, 360, 470, 30, 1011441); // EXIT
        }

        public static void EnsureClosed(Mobile m)
        {
            var gumps = m.GetGumps();

            gumps.Close<DeclareFealtyGump>();
            gumps.Close<GrantGuildTitleGump>();
            gumps.Close<GuildAdminCandidatesGump>();
            gumps.Close<GuildCandidatesGump>();
            gumps.Close<GuildChangeTypeGump>();
            gumps.Close<GuildCharterGump>();
            gumps.Close<GuildDismissGump>();
            gumps.Close<GuildGump>();
            gumps.Close<GuildmasterGump>();
            gumps.Close<GuildRosterGump>();
            gumps.Close<GuildWarGump>();
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

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (BadMember(_mobile, _guild))
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 1: // Loyalty
                    {
                        DeclareFealtyGump.DisplayTo(_mobile, _guild);
                        break;
                    }
                case 2: // Toggle display abbreviation
                    {
                        _mobile.DisplayGuildTitle = !_mobile.DisplayGuildTitle;
                        DisplayTo(_mobile, _guild);
                        break;
                    }
                case 3: // View the current roster
                    {
                        GuildRosterGump.DisplayTo(_mobile, _guild);
                        break;
                    }
                case 4: // Recruit
                    {
                        _mobile.Target = new GuildRecruitTarget(_mobile, _guild);
                        break;
                    }
                case 5: // Membership candidates
                    {
                        GuildCandidatesGump.DisplayTo(_mobile, _guild);
                        break;
                    }
                case 6: // View charter
                    {
                        GuildCharterGump.DisplayTo(_mobile, _guild);
                        break;
                    }
                case 7: // Resign
                    {
                        _guild.RemoveMember(_mobile);
                        break;
                    }
                case 8: // View wars
                    {
                        GuildWarGump.DisplayTo(_mobile, _guild);
                        break;
                    }
                case 9: // Guildmaster functions
                    {
                        if (_mobile.AccessLevel >= AccessLevel.GameMaster || _guild.Leader == _mobile)
                        {
                            GuildmasterGump.DisplayTo(_mobile, _guild);
                        }

                        break;
                    }
            }
        }
    }
}
