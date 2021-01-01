using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Guilds
{
    public class WarDeclarationGump : BaseGuildGump
    {
        private readonly Guild m_Other;

        public WarDeclarationGump(PlayerMobile pm, Guild g, Guild otherGuild) : base(pm, g)
        {
            m_Other = otherGuild;
            var war = g.FindPendingWar(otherGuild);

            AddPage(0);

            AddBackground(0, 0, 500, 340, 0x24AE);
            AddBackground(65, 50, 370, 30, 0x2486);
            AddHtmlLocalized(75, 55, 370, 26, 1062979, 0x3C00); // <div align=center><i>Declaration of War</i></div>
            AddImage(410, 45, 0x232C);
            AddHtmlLocalized(65, 95, 200, 20, 1063009, 0x14AF); // <i>Duration of War</i>
            AddHtmlLocalized(65, 120, 400, 20, 1063010, 0x0);   // Enter the number of hours the war will last.
            AddBackground(65, 150, 40, 30, 0x2486);
            AddTextEntry(70, 154, 50, 30, 0x481, 10, war?.WarLength.Hours.ToString() ?? "0");
            AddHtmlLocalized(65, 195, 200, 20, 1063011, 0x14AF); // <i>Victory Condition</i>
            AddHtmlLocalized(65, 220, 400, 20, 1063012, 0x0);    // Enter the winning number of kills.
            AddBackground(65, 250, 40, 30, 0x2486);
            AddTextEntry(70, 254, 50, 30, 0x481, 11, war?.MaxKills.ToString() ?? "0");
            AddBackground(190, 270, 130, 26, 0x2486);
            AddButton(195, 275, 0x845, 0x846, 0);
            AddHtmlLocalized(220, 273, 90, 26, 1006045, 0x0); // Cancel
            AddBackground(330, 270, 130, 26, 0x2486);
            AddButton(335, 275, 0x845, 0x846, 1);
            AddHtmlLocalized(360, 273, 90, 26, 1062989, 0x5000); // Declare War!
        }

        public override void OnResponse(NetState sender, RelayInfo info)
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
                        var otherAlliance = m_Other.Alliance;

                        if (!playerRank.GetFlag(RankFlags.ControlWarStatus))
                        {
                            pm.SendLocalizedMessage(1063440); // You don't have permission to negotiate wars.
                        }
                        else if (alliance != null && alliance.Leader != guild)
                        {
                            pm.SendLocalizedMessage(
                                1063239,
                                $"{guild.Name}\t{alliance.Name}"
                            ); // ~1_val~ is not the leader of the ~2_val~ alliance.
                            pm.SendLocalizedMessage(
                                1070707,
                                alliance.Leader.Name
                            ); // You need to negotiate via ~1_val~ instead.
                        }
                        else if (otherAlliance != null && otherAlliance.Leader != m_Other)
                        {
                            pm.SendLocalizedMessage(
                                1063239,
                                $"{m_Other.Name}\t{otherAlliance.Name}"
                            ); // ~1_val~ is not the leader of the ~2_val~ alliance.
                            pm.SendLocalizedMessage(
                                1070707,
                                otherAlliance.Leader.Name
                            ); // You need to negotiate via ~1_val~ instead.
                        }
                        else
                        {
                            var activeWar = guild.FindActiveWar(m_Other);

                            if (activeWar == null)
                            {
                                var war = guild.FindPendingWar(m_Other);
                                var otherWar = m_Other.FindPendingWar(guild);

                                // Note: OSI differs from what it says on website.  unlimited war = 0 kills/ 0 hrs.  Not > 999.  (sidenote: they both cap at 65535, 7.5 years, but, still.)
                                var tKills = info.GetTextEntry(11);
                                var tWarLength = info.GetTextEntry(10);

                                var maxKills = tKills == null
                                    ? 0
                                    : Math.Clamp(Utility.ToInt32(info.GetTextEntry(11).Text), 0, 0xFFFF);
                                var warLength = TimeSpan.FromHours(
                                    tWarLength == null
                                        ? 0
                                        : Math.Clamp(Utility.ToInt32(info.GetTextEntry(10).Text), 0, 0xFFFF)
                                );

                                if (war != null)
                                {
                                    war.MaxKills = maxKills;
                                    war.WarLength = warLength;
                                    war.WarRequester = true;
                                }
                                else
                                {
                                    guild.PendingWars.Add(new WarDeclaration(guild, m_Other, maxKills, warLength, true));
                                }

                                if (otherWar != null)
                                {
                                    otherWar.MaxKills = maxKills;
                                    otherWar.WarLength = warLength;
                                    otherWar.WarRequester = false;
                                }
                                else
                                {
                                    m_Other.PendingWars.Add(new WarDeclaration(m_Other, guild, maxKills, warLength, false));
                                }

                                if (war != null)
                                {
                                    pm.SendLocalizedMessage(1070752); // The proposal has been updated.
                                }
                                else
                                {
                                    m_Other.GuildMessage(
                                        1070781,
                                        guild.Alliance != null
                                            ? guild.Alliance.Name
                                            : guild.Name
                                    ); // ~1_val~ has proposed a war.
                                }

                                pm.SendLocalizedMessage(
                                    1070751,
                                    m_Other.Alliance != null
                                        ? m_Other.Alliance.Name
                                        : m_Other.Name
                                ); // War proposal has been sent to ~1_val~.
                            }
                        }

                        break;
                    }
                default:
                    {
                        pm.SendGump(new OtherGuildInfo(pm, guild, m_Other));
                        break;
                    }
            }
        }
    }
}
