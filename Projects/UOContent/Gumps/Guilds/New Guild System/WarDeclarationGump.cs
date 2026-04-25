using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Guilds
{
    public class WarDeclarationGump : BaseGuildGump
    {
        private readonly Guild _other;

        public WarDeclarationGump(PlayerMobile pm, Guild g, Guild otherGuild) : base(pm, g)
        {
            _other = otherGuild;
        }

        protected override bool ShowTabStrip => false;

        protected override void BuildContent(ref DynamicGumpBuilder builder)
        {
            var war = guild.FindPendingWar(_other);

            builder.AddBackground(0, 0, 500, 340, 0x24AE);
            builder.AddBackground(65, 50, 370, 30, 0x2486);
            // <div align=center><i>Declaration of War</i></div>
            builder.AddHtmlLocalized(75, 55, 370, 26, 1062979, 0x3C00);
            builder.AddImage(410, 45, 0x232C);
            builder.AddHtmlLocalized(65, 95, 200, 20, 1063009, 0x14AF); // <i>Duration of War</i>
            builder.AddHtmlLocalized(65, 120, 400, 20, 1063010, 0x0);   // Enter the number of hours the war will last.
            builder.AddBackground(65, 150, 40, 30, 0x2486);
            builder.AddTextEntry(70, 154, 50, 30, 0x481, 10, war?.WarLength.Hours.ToString() ?? "0");
            builder.AddHtmlLocalized(65, 195, 200, 20, 1063011, 0x14AF); // <i>Victory Condition</i>
            builder.AddHtmlLocalized(65, 220, 400, 20, 1063012, 0x0);    // Enter the winning number of kills.
            builder.AddBackground(65, 250, 40, 30, 0x2486);
            builder.AddTextEntry(70, 254, 50, 30, 0x481, 11, war?.MaxKills.ToString() ?? "0");
            builder.AddBackground(190, 270, 130, 26, 0x2486);
            builder.AddButton(195, 275, 0x845, 0x846, 0);
            builder.AddHtmlLocalized(220, 273, 90, 26, 1006045, 0x0); // Cancel
            builder.AddBackground(330, 270, 130, 26, 0x2486);
            builder.AddButton(335, 275, 0x845, 0x846, 1);
            builder.AddHtmlLocalized(360, 273, 90, 26, 1062989, 0x5000); // Declare War!
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var pm = sender.Mobile as PlayerMobile;

            if (!IsMember(pm, guild))
            {
                return;
            }

            var playerRank = pm!.GuildRank;

            switch (info.ButtonID)
            {
                case 1:
                    {
                        var alliance = guild.Alliance;
                        var otherAlliance = _other.Alliance;

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
                            var activeWar = guild.FindActiveWar(_other);

                            if (activeWar == null)
                            {
                                var war = guild.FindPendingWar(_other);
                                var otherWar = _other.FindPendingWar(guild);

                                // Note: OSI differs from what it says on website. unlimited war = 0 kills/0 hrs.
                                // Not > 999. (sidenote: they both cap at 65535, 7.5 years, but, still.)
                                var tKills = info.GetTextEntry(11);
                                var tWarLength = info.GetTextEntry(10);

                                var maxKills = tKills == null
                                    ? 0
                                    : Math.Clamp(Utility.ToInt32(tKills), 0, 0xFFFF);
                                var warLength = TimeSpan.FromHours(
                                    tWarLength == null
                                        ? 0
                                        : Math.Clamp(Utility.ToInt32(tWarLength), 0, 0xFFFF)
                                );

                                if (war != null)
                                {
                                    war.MaxKills = maxKills;
                                    war.WarLength = warLength;
                                    war.WarRequester = true;
                                }
                                else
                                {
                                    guild.PendingWars.Add(new WarDeclaration(guild, _other, maxKills, warLength, true));
                                }

                                if (otherWar != null)
                                {
                                    otherWar.MaxKills = maxKills;
                                    otherWar.WarLength = warLength;
                                    otherWar.WarRequester = false;
                                }
                                else
                                {
                                    _other.PendingWars.Add(new WarDeclaration(_other, guild, maxKills, warLength, false));
                                }

                                if (war != null)
                                {
                                    pm.SendLocalizedMessage(1070752); // The proposal has been updated.
                                }
                                else
                                {
                                    // ~1_val~ has proposed a war.
                                    _other.GuildMessage(
                                        1070781,
                                        guild.Alliance != null
                                            ? guild.Alliance.Name
                                            : guild.Name
                                    );
                                }

                                // War proposal has been sent to ~1_val~.
                                pm.SendLocalizedMessage(
                                    1070751,
                                    _other.Alliance != null
                                        ? _other.Alliance.Name
                                        : _other.Name
                                );
                            }
                        }

                        break;
                    }
                default:
                    {
                        pm.SendGump(new OtherGuildInfo(pm, guild, _other));
                        break;
                    }
            }
        }
    }
}
