using System.Collections.Generic;
using Server.Factions;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Guilds
{
    public class GuildRosterGump : BaseGuildListGump<PlayerMobile>
    {
        private static readonly InfoField<PlayerMobile>[] m_Fields =
        {
            new(1062955, 130, NameComparer.Instance),  // Name
            new(1062956, 80, RankComparer.Instance),   // Rank
            new(1062952, 80, LastOnComparer.Instance), // Last On
            new(1062953, 150, TitleComparer.Instance)  // Guild Title
        };

        public GuildRosterGump(PlayerMobile pm, Guild g) : this(pm, g, LastOnComparer.Instance)
        {
        }

        public GuildRosterGump(
            PlayerMobile pm, Guild g, IComparer<PlayerMobile> currentComparer, bool ascending = false,
            string filter = "", int startNumber = 0
        )
            : base(
                pm,
                g,
                g.Members.SafeConvertList<Mobile, PlayerMobile>(),
                currentComparer,
                ascending,
                filter,
                startNumber,
                m_Fields
            )
        {
            PopulateGump();
        }

        public override void PopulateGump()
        {
            base.PopulateGump();

            AddHtmlLocalized(266, 43, 110, 26, 1062974, 0xF); // Guild Roster
        }

        public override void DrawEndingEntry(int itemNumber)
        {
            AddBackground(225, 148 + itemNumber * 28, 150, 26, 0x2486);
            AddButton(230, 153 + itemNumber * 28, 0x845, 0x846, 8);
            AddHtmlLocalized(255, 151 + itemNumber * 28, 110, 26, 1062992, 0x0); // Invite Player
        }

        protected override TextDefinition[] GetValuesFor(PlayerMobile pm, int aryLength)
        {
            var defs = new TextDefinition[aryLength];

            var name = $"{pm.Name}{(player.GuildFealty == pm && player.GuildFealty != guild.Leader ? " *" : "")}";

            if (pm == player)
            {
                name = Color(name, 0x006600);
            }
            else if (pm.NetState != null)
            {
                name = Color(name, 0x000066);
            }

            defs[0] = name;
            defs[1] = pm.GuildRank.Name;
            defs[2] = pm.NetState != null ? 1063015 : pm.LastOnline.ToString("yyyy-MM-dd");
            defs[3] = pm.GuildTitle ?? "";

            return defs;
        }

        protected override bool IsFiltered(PlayerMobile pm, string filter)
        {
            if (pm == null)
            {
                return true;
            }

            return !pm.Name.InsensitiveContains(filter);
        }

        public override Gump GetResentGump(
            PlayerMobile pm, Guild g, IComparer<PlayerMobile> comparer, bool ascending,
            string filter, int startNumber
        ) =>
            new GuildRosterGump(pm, g, comparer, ascending, filter, startNumber);

        public override Gump GetObjectInfoGump(PlayerMobile pm, Guild g, PlayerMobile o) =>
            new GuildMemberInfoGump(pm, g, o, false, false);

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            base.OnResponse(sender, info);

            if (sender.Mobile is not PlayerMobile pm || !IsMember(pm, guild))
            {
                return;
            }

            if (info.ButtonID == 8)
            {
                if (pm.GuildRank.GetFlag(RankFlags.CanInvitePlayer))
                {
                    pm.SendLocalizedMessage(1063048); // Whom do you wish to invite into your guild?
                    pm.BeginTarget(-1, false, TargetFlags.None, InvitePlayer_Callback, guild);
                }
                else
                {
                    pm.SendLocalizedMessage(503301); // You don't have permission to do that.
                }
            }
        }

        public void InvitePlayer_Callback(Mobile from, object targeted, Guild g)
        {
            var pm = from as PlayerMobile;
            var targ = targeted as PlayerMobile;

            var guildState = PlayerState.Find(g.Leader);
            var targetState = PlayerState.Find(targ);

            var guildFaction = guildState?.Faction;
            var targetFaction = targetState?.Faction;

            if (pm == null || !IsMember(pm, guild) || !pm.GuildRank.GetFlag(RankFlags.CanInvitePlayer))
            {
                from.SendLocalizedMessage(503301); // You don't have permission to do that.
            }
            else if (targ == null)
            {
                pm.SendLocalizedMessage(1063334); // That isn't a valid player.
            }
            else if (!targ.AcceptGuildInvites)
            {
                pm.SendLocalizedMessage(1063049, targ.Name); // ~1_val~ is not accepting guild invitations.
            }
            else if (g.IsMember(targ))
            {
                pm.SendLocalizedMessage(1063050, targ.Name); // ~1_val~ is already a member of your guild!
            }
            else if (targ.Guild != null)
            {
                pm.SendLocalizedMessage(1063051, targ.Name); // ~1_val~ is already a member of a guild.
            }
            // TODO: Check message if CreateGuildGump Open
            else if (targ.HasGump<BaseGuildGump>() || targ.HasGump<CreateGuildGump>())
            {
                pm.SendLocalizedMessage(1063052, targ.Name); // ~1_val~ is currently considering another guild invitation.
            }
            else if (targ.Young && guildFaction != null)
            {
                pm.SendLocalizedMessage(1070766); // You cannot invite a young player to your faction-aligned guild.
            }
            else if (guildFaction != targetFaction)
            {
                if (guildFaction == null)
                {
                    pm.SendLocalizedMessage(1013027); // That player cannot join a non-faction guild.
                }
                else if (targetFaction == null)
                {
                    pm.SendLocalizedMessage(1013026); // That player must be in a faction before joining this guild.
                }
                else
                {
                    pm.SendLocalizedMessage(1013028); // That person has a different faction affiliation.
                }
            }
            else if (targetState?.IsLeaving == true)
            {
                // OSI does this quite strangely, so we'll just do it this way
                pm.SendMessage("That person is quitting their faction and so you may not recruit them.");
            }
            else
            {
                pm.SendLocalizedMessage(1063053, targ.Name); // You invite ~1_val~ to join your guild.
                targ.SendGump(new GuildInvitationRequest(targ, guild, pm));
            }
        }

        private class NameComparer : IComparer<PlayerMobile>
        {
            public static readonly IComparer<PlayerMobile> Instance = new NameComparer();

            public int Compare(PlayerMobile x, PlayerMobile y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                return x.Name.InsensitiveCompare(y.Name);
            }
        }

        private class LastOnComparer : IComparer<PlayerMobile>
        {
            public static readonly IComparer<PlayerMobile> Instance = new LastOnComparer();

            public int Compare(PlayerMobile x, PlayerMobile y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                var aState = x.NetState;
                var bState = y.NetState;

                if (aState == null && bState == null)
                {
                    return x.LastOnline.CompareTo(y.LastOnline);
                }

                if (aState == null)
                {
                    return -1;
                }

                if (bState == null)
                {
                    return 1;
                }

                return 0;
            }
        }

        private class TitleComparer : IComparer<PlayerMobile>
        {
            public static readonly IComparer<PlayerMobile> Instance = new TitleComparer();

            public int Compare(PlayerMobile x, PlayerMobile y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                return x.GuildTitle.InsensitiveCompare(y.GuildTitle);
            }
        }

        private class RankComparer : IComparer<PlayerMobile>
        {
            public static readonly IComparer<PlayerMobile> Instance = new RankComparer();

            public int Compare(PlayerMobile x, PlayerMobile y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                return x.GuildRank.Rank.CompareTo(y.GuildRank.Rank);
            }
        }
    }
}
