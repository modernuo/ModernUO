using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Guilds
{
    public class GuildMemberInfoGump : BaseGuildGump
    {
        private readonly PlayerMobile _member;
        private readonly bool _toKick;
        private readonly bool _toLeader;

        public GuildMemberInfoGump(
            PlayerMobile pm, Guild g, PlayerMobile member, bool toKick, bool toPromoteToLeader
        ) : base(pm, g, 10, 40)
        {
            _toLeader = toPromoteToLeader;
            _toKick = toKick;
            _member = member;
        }

        protected override bool ShowTabStrip => false;

        protected override void BuildContent(ref DynamicGumpBuilder builder)
        {
            builder.AddBackground(0, 0, 350, 255, 0x242C);
            builder.AddHtmlLocalized(20, 15, 310, 26, 1063018, 0x0); // <div align=center><i>Guild Member Information</i></div>
            builder.AddImageTiled(20, 40, 310, 2, 0x2711);

            builder.AddHtmlLocalized(20, 50, 150, 26, 1062955, 0x0, true); // <i>Name</i>
            builder.AddHtml(180, 53, 150, 26, _member.Name);

            builder.AddHtmlLocalized(20, 80, 150, 26, 1062956, 0x0, true); // <i>Rank</i>
            builder.AddHtmlLocalized(180, 83, 150, 26, _member.GuildRank.Name, 0x0);

            builder.AddHtmlLocalized(20, 110, 150, 26, 1062953, 0x0, true); // <i>Guild Title</i>
            builder.AddHtml(180, 113, 150, 26, _member.GuildTitle);
            builder.AddImageTiled(20, 142, 310, 2, 0x2711);

            builder.AddBackground(20, 150, 310, 26, 0x2486);
            builder.AddButton(25, 155, 0x845, 0x846, 4);
            builder.AddHtmlLocalized(
                50,
                153,
                270,
                26,
                _member == player.GuildFealty && guild.Leader != _member ? 1063082 : 1062996,
                0x0
            ); // Clear/Cast Vote For This Member

            builder.AddBackground(20, 180, 150, 26, 0x2486);
            builder.AddButton(25, 185, 0x845, 0x846, 1);
            builder.AddHtmlLocalized(50, 183, 110, 26, 1062993, _toLeader ? 0x990000 : 0); // Promote

            builder.AddBackground(180, 180, 150, 26, 0x2486);
            builder.AddButton(185, 185, 0x845, 0x846, 3);
            builder.AddHtmlLocalized(210, 183, 110, 26, 1062995, 0x0); // Set Guild Title

            builder.AddBackground(20, 210, 150, 26, 0x2486);
            builder.AddButton(25, 215, 0x845, 0x846, 2);
            builder.AddHtmlLocalized(50, 213, 110, 26, 1062994, 0x0); // Demote

            builder.AddBackground(180, 210, 150, 26, 0x2486);
            builder.AddButton(185, 215, 0x845, 0x846, 5);
            builder.AddHtmlLocalized(210, 213, 110, 26, 1062997, _toKick ? 0x5000 : 0); // Kick
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (sender.Mobile is not PlayerMobile pm || !IsMember(pm, guild) || !IsMember(_member, guild))
            {
                return;
            }

            var playerRank = pm.GuildRank;
            var targetRank = _member.GuildRank;

            switch (info.ButtonID)
            {
                case 1: // Promote
                    {
                        if (playerRank.GetFlag(RankFlags.CanPromoteDemote) &&
                            (playerRank.Rank - 1 > targetRank.Rank ||
                             playerRank == RankDefinition.Leader && playerRank.Rank > targetRank.Rank))
                        {
                            targetRank = RankDefinition.Ranks[targetRank.Rank + 1];

                            if (targetRank == RankDefinition.Leader)
                            {
                                if (_toLeader)
                                {
                                    _member.GuildRank = targetRank;
                                    // The guild information for ~1_val~ has been updated.
                                    pm.SendLocalizedMessage(1063156, _member.Name);
                                    pm.SendLocalizedMessage(1063156, pm.Name);
                                    guild.Leader = _member;
                                }
                                else
                                {
                                    // Are you sure you wish to make this member the new guild leader?
                                    pm.SendLocalizedMessage(1063144);
                                    pm.SendGump(new GuildMemberInfoGump(player, guild, _member, false, true));
                                }
                            }
                            else
                            {
                                _member.GuildRank = targetRank;
                                // The guild information for ~1_val~ has been updated.
                                pm.SendLocalizedMessage(1063156, _member.Name);
                            }
                        }
                        else
                        {
                            pm.SendLocalizedMessage(1063143); // You don't have permission to promote this member.
                        }

                        break;
                    }
                case 2: // Demote
                    {
                        if (playerRank.GetFlag(RankFlags.CanPromoteDemote) && playerRank.Rank > targetRank.Rank)
                        {
                            if (targetRank == RankDefinition.Lowest)
                            {
                                if (RankDefinition.Lowest.Name.Number == 1062963)
                                {
                                    pm.SendLocalizedMessage(1063333); // You can't demote a ronin.
                                }
                                else
                                {
                                    pm.SendMessage($"You can't demote a {RankDefinition.Lowest.Name}.");
                                }
                            }
                            else
                            {
                                _member.GuildRank = RankDefinition.Ranks[targetRank.Rank - 1];
                                // The guild information for ~1_val~ has been updated.
                                pm.SendLocalizedMessage(1063156, _member.Name);
                            }
                        }
                        else
                        {
                            pm.SendLocalizedMessage(1063146); // You don't have permission to demote this member.
                        }

                        break;
                    }
                case 3: // Set Guild title
                    {
                        if (playerRank.GetFlag(RankFlags.CanSetGuildTitle) &&
                            (playerRank.Rank > targetRank.Rank || _member == player))
                        {
                            // Enter the new title for this guild member or 'none' to remove a title:
                            pm.SendLocalizedMessage(1011128);

                            pm.BeginPrompt(SetTitle_Callback);
                        }
                        else if (_member.GuildTitle == null || _member.GuildTitle.Length <= 0)
                        {
                            // You don't have the permission to set that member's guild title.
                            pm.SendLocalizedMessage(1070746);
                        }
                        else
                        {
                            // You don't have permission to change this member's guild title.
                            pm.SendLocalizedMessage(1063148);
                        }

                        break;
                    }
                case 4: // Vote
                    {
                        if (_member == pm.GuildFealty && guild.Leader != _member)
                        {
                            pm.SendLocalizedMessage(1063158); // You have cleared your vote for guild leader.
                        }
                        else if (guild.CanVote(_member))
                        {
                            if (_member == guild.Leader)
                            {
                                pm.SendLocalizedMessage(1063424); // You can't vote for the current guild leader.
                            }
                            else if (!guild.CanBeVotedFor(_member))
                            {
                                pm.SendLocalizedMessage(1063425); // You can't vote for an inactive guild member.
                            }
                            else
                            {
                                pm.GuildFealty = _member;
                                // You cast your vote for ~1_val~ for guild leader.
                                pm.SendLocalizedMessage(1063159, _member.Name);
                            }
                        }
                        else
                        {
                            pm.SendLocalizedMessage(1063149); // You don't have permission to vote.
                        }

                        break;
                    }
                case 5: // Kick
                    {
                        if (playerRank.GetFlag(RankFlags.RemovePlayers) && playerRank.Rank > targetRank.Rank ||
                            playerRank.GetFlag(RankFlags.RemoveLowestRank) && targetRank == RankDefinition.Lowest)
                        {
                            if (_toKick)
                            {
                                guild.RemoveMember(_member);
                                pm.SendLocalizedMessage(1063157); // The member has been removed from your guild.
                            }
                            else
                            {
                                // Are you sure you wish to kick this member from the guild?
                                pm.SendLocalizedMessage(1063152);
                                pm.SendGump(new GuildMemberInfoGump(player, guild, _member, true, false));
                            }
                        }
                        else
                        {
                            pm.SendLocalizedMessage(1063151); // You don't have permission to remove this member.
                        }

                        break;
                    }
            }
        }

        public void SetTitle_Callback(Mobile from, string text)
        {
            if (from is not PlayerMobile pm || _member == null)
            {
                return;
            }

            if (_member.Guild is not Guild g || !IsMember(pm, g) ||
                !(pm.GuildRank.GetFlag(RankFlags.CanSetGuildTitle) &&
                  (pm.GuildRank.Rank > _member.GuildRank.Rank || pm == _member)))
            {
                if (_member.GuildTitle == null || _member.GuildTitle.Length <= 0)
                {
                    // You don't have the permission to set that member's guild title.
                    pm.SendLocalizedMessage(1070746);
                }
                else
                {
                    // You don't have permission to change this member's guild title.
                    pm.SendLocalizedMessage(1063148);
                }

                return;
            }

            var title = text.AsSpan().Trim().FixHtml();

            if (title.Length > 20)
            {
                from.SendLocalizedMessage(501178); // That title is too long.
            }
            else if (!CheckProfanity(title))
            {
                from.SendLocalizedMessage(501179); // That title is disallowed.
            }
            else
            {
                if (title.InsensitiveEquals("none"))
                {
                    _member.GuildTitle = null;
                }
                else
                {
                    _member.GuildTitle = title;
                }

                // The guild information for ~1_val~ has been updated.
                pm.SendLocalizedMessage(1063156, _member.Name);
            }
        }
    }
}
