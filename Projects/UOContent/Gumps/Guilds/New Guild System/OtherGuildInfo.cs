using System;
using Server.Factions;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Guilds
{
    public class OtherGuildInfo : BaseGuildGump
    {
        private readonly Guild _other;

        public OtherGuildInfo(PlayerMobile pm, Guild g, Guild otherGuild) : base(pm, g, 10, 40)
        {
            _other = otherGuild;

            g.CheckExpiredWars();
        }

        protected override bool ShowTabStrip => false;

        private static void AddButtonAndBackground(ref DynamicGumpBuilder builder, int x, int y, int buttonID, int locNum)
        {
            builder.AddBackground(x, y, 225, 26, 0x2486);
            builder.AddButton(x + 5, y + 5, 0x845, 0x846, buttonID);
            builder.AddHtmlLocalized(x + 30, y + 3, 185, 26, locNum, 0x0);
        }

        protected override void BuildContent(ref DynamicGumpBuilder builder)
        {
            builder.AddBackground(0, 0, 520, 335, 0x242C);

            var g = Guild.GetAllianceLeader(guild);
            var other = Guild.GetAllianceLeader(_other);

            var war = g.FindPendingWar(other);
            var activeWar = g.FindActiveWar(other);

            var alliance = guild.Alliance;
            var otherAlliance = _other.Alliance;
            // NOTE TO SELF: Only only alliance leader can see pending guild alliance statuses

            var pendingWar = war != null;
            var activeWarFlag = activeWar != null;

            builder.AddHtmlLocalized(20, 15, 480, 26, 1062975, 0x0); // <div align=center><i>Guild Relationship</i></div>
            builder.AddImageTiled(20, 40, 480, 2, 0x2711);
            builder.AddHtmlLocalized(20, 50, 120, 26, 1062954, 0x0, true); // <i>Guild Name</i>
            builder.AddHtml(150, 53, 360, 26, _other.Name);

            builder.AddHtmlLocalized(20, 80, 120, 26, 1063025, 0x0, true); // <i>Alliance</i>

            if (otherAlliance?.IsMember(_other) == true)
            {
                builder.AddHtml(150, 83, 360, 26, otherAlliance.Name);
            }

            builder.AddHtmlLocalized(20, 110, 120, 26, 1063139, 0x0, true); // <i>Abbreviation</i>
            builder.AddHtml(150, 113, 120, 26, _other.Abbreviation);

            var kills = "0/0";
            var time = "00:00";
            var otherKills = "0/0";

            WarDeclaration otherWar;

            if (activeWarFlag)
            {
                kills = $"{activeWar.Kills}/{activeWar.MaxKills}";

                var timeRemaining = TimeSpan.Zero;

                if (activeWar.WarLength != TimeSpan.Zero && activeWar.WarBeginning + activeWar.WarLength > Core.Now)
                {
                    timeRemaining = activeWar.WarBeginning + activeWar.WarLength - Core.Now;
                }

                time = $"{timeRemaining.Hours:D2}:{DateTime.MinValue + timeRemaining:mm}";

                otherWar = _other.FindActiveWar(guild);
                if (otherWar != null)
                {
                    otherKills = $"{otherWar.Kills}/{otherWar.MaxKills}";
                }
            }
            else if (pendingWar)
            {
                kills = Html.Color($"{war.Kills}/{war.MaxKills}", 0x990000);
                time = Html.Color($"{war.WarLength.Hours:D2}:{DateTime.MinValue + war.WarLength:mm}", 0x990000);

                otherWar = _other.FindPendingWar(guild);
                if (otherWar != null)
                {
                    otherKills = Html.Color($"{otherWar.Kills}/{otherWar.MaxKills}", 0x990000);
                }
            }

            builder.AddHtmlLocalized(280, 110, 120, 26, 1062966, 0x0, true); // <i>Your Kills</i>
            builder.AddHtml(410, 113, 120, 26, kills);

            builder.AddHtmlLocalized(20, 140, 120, 26, 1062968, 0x0, true); // <i>Time Remaining</i>
            builder.AddHtml(150, 143, 120, 26, time);

            builder.AddHtmlLocalized(280, 140, 120, 26, 1062967, 0x0, true); // <i>Their Kills</i>
            builder.AddHtml(410, 143, 120, 26, otherKills);

            builder.AddImageTiled(20, 172, 480, 2, 0x2711);

            var number = 1062973; // <div align=center>You are at peace with this guild.</div>

            if (pendingWar)
            {
                if (war.WarRequester)
                {
                    number = 1063027; // <div align=center>You have challenged this guild to war!</div>
                }
                else
                {
                    number = 1062969; // <div align=center>This guild has challenged you to war!</div>

                    AddButtonAndBackground(ref builder, 20, 260, 5, 1062981);  // Accept Challenge
                    AddButtonAndBackground(ref builder, 275, 260, 6, 1062983); // Modify Terms
                }

                AddButtonAndBackground(ref builder, 20, 290, 7, 1062982); // Dismiss Challenge
            }
            else if (activeWarFlag)
            {
                number = 1062965; // <div align=center>You are at war with this guild!</div>
                AddButtonAndBackground(ref builder, 20, 290, 8, 1062980); // Surrender
            }
            else if (alliance != null && alliance == otherAlliance) // alliance, Same Alliance
            {
                if (alliance.IsMember(guild) && alliance.IsMember(_other)) // Both in Same alliance, full members
                {
                    number = 1062970; // <div align=center>You are allied with this guild.</div>

                    if (alliance.Leader == guild)
                    {
                        AddButtonAndBackground(ref builder, 20, 260, 12, 1062984); // Remove Guild from Alliance

                        // Note: No 'confirmation' like the other leader guild promotion things
                        // Promote to Alliance Leader
                        AddButtonAndBackground(ref builder, 275, 260, 13, 1063433);
                        // Remove guild from alliance	//Promote to Alliance Leader
                    }

                    // Show roster, Centered, up
                    AddButtonAndBackground(ref builder, 148, 215, 10, 1063164); // Show Alliance Roster
                    // Leave Alliance
                    AddButtonAndBackground(ref builder, 20, 290, 11, 1062985); // Leave Alliance
                }
                else if (alliance.Leader == guild && alliance.IsPendingMember(_other))
                {
                    number = 1062971; // <div align=center>You have requested an alliance with this guild.</div>

                    // Show Alliance Roster, Centered, down.
                    AddButtonAndBackground(ref builder, 148, 245, 10, 1063164); // Show Alliance Roster
                    // Withdraw Request
                    AddButtonAndBackground(ref builder, 20, 290, 14, 1062986); // Withdraw Request

                    builder.AddHtml(150, 83, 360, 26, alliance.Name.Color(0x99));
                }
                else if (alliance.Leader == _other && alliance.IsPendingMember(guild))
                {
                    number = 1062972; // <div align=center>This guild has requested an alliance.</div>

                    // Show alliance Roster, top
                    AddButtonAndBackground(ref builder, 148, 215, 10, 1063164); // Show Alliance Roster
                    // Deny Request
                    // Accept Request
                    AddButtonAndBackground(ref builder, 20, 260, 15, 1062988); // Deny Request
                    AddButtonAndBackground(ref builder, 20, 290, 16, 1062987); // Accept Request

                    builder.AddHtml(150, 83, 360, 26, alliance.Name.Color(0x99));
                }
            }
            else
            {
                AddButtonAndBackground(ref builder, 20, 260, 2, 1062990); // Request Alliance
                AddButtonAndBackground(ref builder, 20, 290, 1, 1062989); // Declare War!
            }

            AddButtonAndBackground(ref builder, 275, 290, 0, 3000091); // Cancel

            builder.AddHtmlLocalized(20, 180, 480, 30, number, 0x0, true);
            builder.AddImageTiled(20, 245, 480, 2, 0x2711);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (!(sender.Mobile is PlayerMobile pm && IsMember(pm, guild)))
            {
                return;
            }

            var playerRank = pm.GuildRank;

            var guildLeader = Guild.GetAllianceLeader(guild);
            var otherGuild = Guild.GetAllianceLeader(_other);

            var war = guildLeader.FindPendingWar(otherGuild);
            var activeWar = guildLeader.FindActiveWar(otherGuild);
            var otherWar = otherGuild.FindPendingWar(guildLeader);

            var alliance = guild.Alliance;
            var otherAlliance = otherGuild.Alliance;

            switch (info.ButtonID)
            {
                case 5: // Accept the war
                    {
                        if (war?.WarRequester == false && activeWar == null)
                        {
                            if (!playerRank.GetFlag(RankFlags.ControlWarStatus))
                            {
                                pm.SendLocalizedMessage(1063440); // You don't have permission to negotiate wars.
                            }
                            else if (alliance != null && alliance.Leader != guild)
                            {
                                // ~1_val~ is not the leader of the ~2_val~ alliance.
                                pm.SendLocalizedMessage(1063239, $"{guild.Name}\t{alliance.Name}");

                                // You need to negotiate via ~1_val~ instead.
                                pm.SendLocalizedMessage(1070707, alliance.Leader.Name);
                            }
                            else
                            {
                                // Accept the war
                                guild.PendingWars.Remove(war);
                                war.WarBeginning = Core.Now;
                                guild.AcceptedWars.Add(war);

                                if (alliance?.IsMember(guild) == true)
                                {
                                    // Guild Message: Your guild is now at war with ~1_GUILDNAME~
                                    alliance.AllianceMessage(1070769, otherAlliance?.Name ?? otherGuild.Name);
                                    alliance.InvalidateMemberProperties();
                                }
                                else
                                {
                                    // Guild Message: Your guild is now at war with ~1_GUILDNAME~
                                    guild.GuildMessage(1070769, otherAlliance?.Name ?? otherGuild.Name);
                                    guild.InvalidateMemberProperties();
                                }
                                // Technically  SHOULD say Your guild is now at war w/out any info, intentional diff.

                                otherGuild.PendingWars.Remove(otherWar);
                                otherWar.WarBeginning = Core.Now;
                                otherGuild.AcceptedWars.Add(otherWar);

                                if (otherAlliance != null && _other.Alliance.IsMember(_other))
                                {
                                    // Guild Message: Your guild is now at war with ~1_GUILDNAME~
                                    otherAlliance.AllianceMessage(1070769, alliance?.Name ?? guild.Name);
                                    otherAlliance.InvalidateMemberProperties();
                                }
                                else
                                {
                                    // Guild Message: Your guild is now at war with ~1_GUILDNAME~
                                    otherGuild.GuildMessage(1070769, alliance?.Name ?? guild.Name);
                                    otherGuild.InvalidateMemberProperties();
                                }
                            }
                        }

                        break;
                    }
                case 6: // Modify war terms
                    {
                        if (war?.WarRequester == false && activeWar == null)
                        {
                            if (!playerRank.GetFlag(RankFlags.ControlWarStatus))
                            {
                                pm.SendLocalizedMessage(1063440); // You don't have permission to negotiate wars.
                            }
                            else if (alliance != null && alliance.Leader != guild)
                            {
                                // ~1_val~ is not the leader of the ~2_val~ alliance.
                                pm.SendLocalizedMessage(1063239, $"{guild.Name}\t{alliance.Name}");

                                // You need to negotiate via ~1_val~ instead.
                                pm.SendLocalizedMessage(1070707, alliance.Leader.Name);
                            }
                            else
                            {
                                pm.SendGump(new WarDeclarationGump(pm, guild, otherGuild));
                            }
                        }

                        break;
                    }
                case 7: // Dismiss war
                    {
                        if (war != null)
                        {
                            if (!playerRank.GetFlag(RankFlags.ControlWarStatus))
                            {
                                pm.SendLocalizedMessage(1063440); // You don't have permission to negotiate wars.
                            }
                            else if (alliance != null && alliance.Leader != guild)
                            {
                                // ~1_val~ is not the leader of the ~2_val~ alliance.
                                pm.SendLocalizedMessage(1063239, $"{guild.Name}\t{alliance.Name}");

                                // You need to negotiate via ~1_val~ instead.
                                pm.SendLocalizedMessage(1070707, alliance.Leader.Name);
                            }
                            else
                            {
                                // Dismiss the war
                                guild.PendingWars.Remove(war);
                                otherGuild.PendingWars.Remove(otherWar);
                                pm.SendLocalizedMessage(1070752); // The proposal has been updated.
                                // Messages to opposing guild? (Testing on OSI says no)
                            }
                        }

                        break;
                    }
                case 8: // Surrender
                    {
                        if (!playerRank.GetFlag(RankFlags.ControlWarStatus))
                        {
                            pm.SendLocalizedMessage(1063440); // You don't have permission to negotiate wars.
                        }
                        else if (alliance != null && alliance.Leader != guild)
                        {
                            // ~1_val~ is not the leader of the ~2_val~ alliance.
                            pm.SendLocalizedMessage(1063239, $"{guild.Name}\t{alliance.Name}");

                            // You need to negotiate via ~1_val~ instead.
                            pm.SendLocalizedMessage(1070707, alliance.Leader.Name);
                        }
                        else
                        {
                            if (activeWar != null)
                            {
                                if (alliance?.IsMember(guild) == true)
                                {
                                    // You have lost the war with ~1_val~.
                                    alliance.AllianceMessage(1070740, otherAlliance?.Name ?? otherGuild.Name);
                                    alliance.InvalidateMemberProperties();
                                }
                                else
                                {
                                    // You have lost the war with ~1_val~.
                                    guild.GuildMessage(1070740, otherAlliance?.Name ?? otherGuild.Name);
                                    guild.InvalidateMemberProperties();
                                }

                                guild.AcceptedWars.Remove(activeWar);

                                if (otherAlliance?.IsMember(otherGuild) == true)
                                {
                                    // You have won the war against ~1_val~!
                                    otherAlliance.AllianceMessage(1070739, guild.Alliance?.Name ?? guild.Name);
                                    otherAlliance.InvalidateMemberProperties();
                                }
                                else
                                {
                                    // You have won the war against ~1_val~!
                                    otherGuild.GuildMessage(1070739, guild.Alliance?.Name ?? guild.Name);
                                    otherGuild.InvalidateMemberProperties();
                                }

                                otherGuild.AcceptedWars.Remove(otherGuild.FindActiveWar(guild));
                            }
                        }

                        break;
                    }
                case 1: // Declare War
                    {
                        if (war == null && activeWar == null)
                        {
                            if (!playerRank.GetFlag(RankFlags.ControlWarStatus))
                            {
                                pm.SendLocalizedMessage(1063440); // You don't have permission to negotiate wars.
                            }
                            else if (alliance != null && alliance.Leader != guild)
                            {
                                // ~1_val~ is not the leader of the ~2_val~ alliance.
                                pm.SendLocalizedMessage(1063239, $"{guild.Name}\t{alliance.Name}");

                                // You need to negotiate via ~1_val~ instead.
                                pm.SendLocalizedMessage(1070707, alliance.Leader.Name);
                            }
                            else if (otherAlliance != null && otherAlliance.Leader != _other)
                            {
                                // ~1_val~ is not the leader of the ~2_val~ alliance.
                                pm.SendLocalizedMessage(1063239, $"{_other.Name}\t{otherAlliance.Name}");

                                // You need to negotiate via ~1_val~ instead.
                                pm.SendLocalizedMessage(1070707, otherAlliance.Leader.Name);
                            }
                            else
                            {
                                pm.SendGump(new WarDeclarationGump(pm, guild, _other));
                            }
                        }

                        break;
                    }

                case 2: // Request Alliance
                    {
                        if (alliance == null)
                        {
                            if (!playerRank.GetFlag(RankFlags.AllianceControl))
                            {
                                pm.SendLocalizedMessage(1070747); // You don't have permission to create an alliance.
                            }
                            else if (Faction.Find(guild.Leader) != Faction.Find(_other.Leader))
                            {
                                // You cannot propose an alliance to a guild with a different faction allegiance.
                                pm.SendLocalizedMessage(1070758);
                            }
                            else if (otherAlliance != null)
                            {
                                if (otherAlliance.IsPendingMember(_other))
                                {
                                    // ~1_val~ is currently considering another alliance proposal.
                                    pm.SendLocalizedMessage(1063416, _other.Name);
                                }
                                else
                                {
                                    // ~1_val~ already belongs to an alliance.
                                    pm.SendLocalizedMessage(1063426, _other.Name);
                                }
                            }
                            else if (_other.AcceptedWars.Count > 0 || _other.PendingWars.Count > 0)
                            {
                                // ~1_val~ is currently involved in a guild war.
                                pm.SendLocalizedMessage(1063427, _other.Name);
                            }
                            else if (guild.AcceptedWars.Count > 0 || guild.PendingWars.Count > 0)
                            {
                                // ~1_val~ is currently involved in a guild war.
                                pm.SendLocalizedMessage(1063427, guild.Name);
                            }
                            else
                            {
                                pm.SendLocalizedMessage(1063439); // Enter a name for the new alliance:
                                pm.BeginPrompt(CreateAlliance_Callback);
                            }
                        }
                        else
                        {
                            if (!playerRank.GetFlag(RankFlags.AllianceControl))
                            {
                                pm.SendLocalizedMessage(1063436); // You don't have permission to negotiate an alliance.
                            }
                            else if (alliance.Leader != guild)
                            {
                                // ~1_val~ is not the leader of the ~2_val~ alliance.
                                pm.SendLocalizedMessage(1063239, $"{guild.Name}\t{alliance.Name}");
                            }
                            else if (otherAlliance != null)
                            {
                                if (otherAlliance.IsPendingMember(_other))
                                {
                                    // ~1_val~ is currently considering another alliance proposal.
                                    pm.SendLocalizedMessage(1063416, _other.Name);
                                }
                                else
                                {
                                    // ~1_val~ already belongs to an alliance.
                                    pm.SendLocalizedMessage(1063426, _other.Name);
                                }
                            }
                            else if (alliance.IsPendingMember(guild))
                            {
                                // ~1_val~ is currently considering another alliance proposal.
                                pm.SendLocalizedMessage(1063416, guild.Name);
                            }
                            else if (_other.AcceptedWars.Count > 0 || _other.PendingWars.Count > 0)
                            {
                                // ~1_val~ is currently involved in a guild war.
                                pm.SendLocalizedMessage(1063427, _other.Name);
                            }
                            else if (guild.AcceptedWars.Count > 0 || guild.PendingWars.Count > 0)
                            {
                                // ~1_val~ is currently involved in a guild war.
                                pm.SendLocalizedMessage(1063427, guild.Name);
                            }
                            else if (Faction.Find(guild.Leader) != Faction.Find(_other.Leader))
                            {
                                // You cannot propose an alliance to a guild with a different faction allegiance.
                                pm.SendLocalizedMessage(1070758);
                            }
                            else
                            {
                                // An invitation to join your alliance has been sent to ~1_val~.
                                pm.SendLocalizedMessage(1070750, _other.Name);

                                _other.GuildMessage(1070780, guild.Name); // ~1_val~ has proposed an alliance.

                                _other.Alliance = alliance; // Calls addPendingGuild
                            }
                        }

                        break;
                    }
                case 10: // Show Alliance Roster
                    {
                        if (alliance != null && alliance == otherAlliance)
                        {
                            pm.SendGump(new AllianceInfo.AllianceRosterGump(pm, guild, alliance));
                        }

                        break;
                    }
                case 11: // Leave Alliance
                    {
                        if (!playerRank.GetFlag(RankFlags.AllianceControl))
                        {
                            pm.SendLocalizedMessage(1063436); // You don't have permission to negotiate an alliance.
                        }
                        else if (alliance?.IsMember(guild) == true)
                        {
                            guild.Alliance = null; // Calls alliance.RemoveGuild

                            _other.InvalidateWarNotoriety();

                            guild.InvalidateMemberNotoriety();
                        }

                        break;
                    }
                case 12: // Remove Guild from alliance
                    {
                        if (!playerRank.GetFlag(RankFlags.AllianceControl))
                        {
                            pm.SendLocalizedMessage(1063436); // You don't have permission to negotiate an alliance.
                        }
                        else if (alliance != null && alliance.Leader != guild)
                        {
                            // ~1_val~ is not the leader of the ~2_val~ alliance.
                            pm.SendLocalizedMessage(1063239, $"{guild.Name}\t{alliance.Name}");
                        }
                        else if (alliance?.IsMember(guild) == true && alliance.IsMember(_other))
                        {
                            _other.Alliance = null;

                            _other.InvalidateMemberNotoriety();

                            guild.InvalidateWarNotoriety();
                        }

                        break;
                    }
                case 13: // Promote to Alliance leader
                    {
                        if (!playerRank.GetFlag(RankFlags.AllianceControl))
                        {
                            pm.SendLocalizedMessage(1063436); // You don't have permission to negotiate an alliance.
                        }
                        else if (alliance != null && alliance.Leader != guild)
                        {
                            // ~1_val~ is not the leader of the ~2_val~ alliance.
                            pm.SendLocalizedMessage(1063239, $"{guild.Name}\t{alliance.Name}");
                        }
                        else if (alliance?.IsMember(guild) == true && alliance.IsMember(_other))
                        {
                            // ~1_val~ is now the leader of ~2_val~.
                            pm.SendLocalizedMessage(1063434, $"{_other.Name}\t{alliance.Name}");

                            alliance.Leader = _other;
                        }

                        break;
                    }
                case 14: // Withdraw Request
                    {
                        if (!playerRank.GetFlag(RankFlags.AllianceControl))
                        {
                            pm.SendLocalizedMessage(1063436); // You don't have permission to negotiate an alliance.
                        }
                        else if (alliance != null && alliance.Leader == guild && alliance.IsPendingMember(_other))
                        {
                            _other.Alliance = null;
                            pm.SendLocalizedMessage(1070752); // The proposal has been updated.
                        }

                        break;
                    }
                case 15: // Deny Alliance Request
                    {
                        if (!playerRank.GetFlag(RankFlags.AllianceControl))
                        {
                            pm.SendLocalizedMessage(1063436); // You don't have permission to negotiate an alliance.
                        }
                        else if (alliance != null && otherAlliance != null && alliance.Leader == _other &&
                                 otherAlliance.IsPendingMember(guild))
                        {
                            // The proposal has been updated.
                            // _other.GuildMessage( 1070782 );
                            // // ~1_val~ has responded to your proposal.
                            // // Per OSI commented out.
                            pm.SendLocalizedMessage(1070752);
                            guild.Alliance = null;
                        }

                        break;
                    }
                case 16: // Accept Alliance Request
                    {
                        if (!playerRank.GetFlag(RankFlags.AllianceControl))
                        {
                            pm.SendLocalizedMessage(1063436); // You don't have permission to negotiate an alliance.
                        }
                        else if (otherAlliance != null && otherAlliance.Leader == _other &&
                                 otherAlliance.IsPendingMember(guild))
                        {
                            pm.SendLocalizedMessage(1070752); // The proposal has been updated.

                            // No need to verify it's in the guild or already a member, the function does this
                            otherAlliance.TurnToMember(_other);

                            otherAlliance.TurnToMember(guild);
                        }

                        break;
                    }
            }
        }

        public void CreateAlliance_Callback(Mobile from, string text)
        {
            if (from is not PlayerMobile pm)
            {
                return;
            }

            var alliance = guild.Alliance;
            var otherAlliance = _other.Alliance;

            if (!IsMember(from, guild) || alliance != null)
            {
                return;
            }

            var playerRank = pm.GuildRank;

            if (!playerRank.GetFlag(RankFlags.AllianceControl))
            {
                pm.SendLocalizedMessage(1070747); // You don't have permission to create an alliance.
            }
            else if (Faction.Find(guild.Leader) != Faction.Find(_other.Leader))
            {
                // Notes about this: OSI only cares/checks when proposing, you can change your faction all you want later.
                // You cannot propose an alliance to a guild with a different faction allegiance.
                pm.SendLocalizedMessage(1070758);
            }
            else if (otherAlliance != null)
            {
                if (otherAlliance.IsPendingMember(_other))
                {
                    // ~1_val~ is currently considering another alliance proposal.
                    pm.SendLocalizedMessage(1063416, _other.Name);
                }
                else
                {
                    pm.SendLocalizedMessage(1063426, _other.Name); // ~1_val~ already belongs to an alliance.
                }
            }
            else if (_other.AcceptedWars.Count > 0 || _other.PendingWars.Count > 0)
            {
                pm.SendLocalizedMessage(1063427, _other.Name); // ~1_val~ is currently involved in a guild war.
            }
            else if (guild.AcceptedWars.Count > 0 || guild.PendingWars.Count > 0)
            {
                pm.SendLocalizedMessage(1063427, guild.Name); // ~1_val~ is currently involved in a guild war.
            }
            else
            {
                var name = text.AsSpan().Trim().FixHtml();

                if (!CheckProfanity(name))
                {
                    pm.SendLocalizedMessage(1070886); // That alliance name is not allowed.
                }
                else if (name.Length > Guild.NameLimit)
                {
                    // An alliance name cannot exceed ~1_val~ characters in length.
                    pm.SendLocalizedMessage(1070887, Guild.NameLimit.ToString());
                }
                else if (AllianceInfo.Alliances.ContainsKey(name.ToLower()))
                {
                    pm.SendLocalizedMessage(1063428); // That alliance name is not available.
                }
                else
                {
                    // An invitation to join your alliance has been sent to ~1_val~.
                    pm.SendLocalizedMessage(1070750, _other.Name);

                    _other.GuildMessage(1070780, guild.Name); // ~1_val~ has proposed an alliance.

                    new AllianceInfo(guild, name, _other);
                }
            }
        }
    }
}
