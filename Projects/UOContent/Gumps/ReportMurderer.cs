using System;
using System.Collections.Generic;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.SkillHandlers;

namespace Server.Gumps
{
    public class ReportMurdererGump : Gump
    {
        private readonly List<Mobile> m_Killers;
        private int m_Idx;

        private ReportMurdererGump(List<Mobile> killers, int idx = 0) : base(0, 0)
        {
            m_Killers = killers;
            m_Idx = idx;
            BuildGump();
        }

        public static void Initialize()
        {
            EventSink.PlayerDeath += EventSink_PlayerDeath;
        }

        public static void EventSink_PlayerDeath(Mobile m)
        {
            var killers = new List<Mobile>();
            var toGive = new List<Mobile>();

            foreach (var ai in m.Aggressors)
            {
                if (ai.Attacker.Player && ai.CanReportMurder && !ai.Reported)
                {
                    if (!Core.SE || !((PlayerMobile)m).RecentlyReported.Contains(ai.Attacker))
                    {
                        killers.Add(ai.Attacker);
                        ai.Reported = true;
                        ai.CanReportMurder = false;
                    }
                }

                if (ai.Attacker.Player && Core.Now - ai.LastCombatTime < TimeSpan.FromSeconds(30.0) &&
                    !toGive.Contains(ai.Attacker))
                {
                    toGive.Add(ai.Attacker);
                }
            }

            foreach (var ai in m.Aggressed)
            {
                if (ai.Defender.Player && Core.Now - ai.LastCombatTime < TimeSpan.FromSeconds(30.0) &&
                    !toGive.Contains(ai.Defender))
                {
                    toGive.Add(ai.Defender);
                }
            }

            foreach (var g in toGive)
            {
                var n = Notoriety.Compute(g, m);

                var ourKarma = g.Karma;
                var innocent = n == Notoriety.Innocent;
                var criminal = n == Notoriety.Criminal || n == Notoriety.Murderer;

                var fameAward = m.Fame / 200;
                var karmaAward = 0;

                if (innocent)
                {
                    karmaAward = ourKarma > -2500 ? -850 : -110 - m.Karma / 100;
                }
                else if (criminal)
                {
                    karmaAward = 50;
                }

                Titles.AwardFame(g, fameAward, false);
                Titles.AwardKarma(g, karmaAward, true);
            }

            if (m is PlayerMobile mobile && mobile.NpcGuild == NpcGuild.ThievesGuild)
            {
                return;
            }

            if (killers.Count > 0)
            {
                new GumpTimer(m, killers).Start();
            }
        }

        private void BuildGump()
        {
            AddBackground(265, 205, 320, 290, 5054);
            Closable = false;
            Resizable = false;

            AddPage(0);

            AddImageTiled(225, 175, 50, 45, 0xCE);  // Top left corner
            AddImageTiled(267, 175, 315, 44, 0xC9); // Top bar
            AddImageTiled(582, 175, 43, 45, 0xCF);  // Top right corner
            AddImageTiled(225, 219, 44, 270, 0xCA); // Left side
            AddImageTiled(582, 219, 44, 270, 0xCB); // Right side
            AddImageTiled(225, 489, 44, 43, 0xCC);  // Lower left corner
            AddImageTiled(267, 489, 315, 43, 0xE9); // Lower Bar
            AddImageTiled(582, 489, 43, 43, 0xCD);  // Lower right corner

            AddPage(1);

            AddHtml(260, 234, 300, 140, m_Killers[m_Idx].Name); // Player's Name
            AddHtmlLocalized(260, 254, 300, 140, 1049066);      // Would you like to report...

            AddButton(260, 300, 0xFA5, 0xFA7, 1);
            AddHtmlLocalized(300, 300, 300, 50, 1046362); // Yes

            AddButton(360, 300, 0xFA5, 0xFA7, 2);
            AddHtmlLocalized(400, 300, 300, 50, 1046363); // No
        }

        public static void ReportedListExpiry_Callback(PlayerMobile from, Mobile killer)
        {
            if (from.RecentlyReported.Contains(killer))
            {
                from.RecentlyReported.Remove(killer);
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            var from = (PlayerMobile)state.Mobile;

            switch (info.ButtonID)
            {
                case 1:
                    {
                        var killer = m_Killers[m_Idx];
                        if (killer?.Deleted == false)
                        {
                            killer.Kills++;
                            killer.ShortTermMurders++;

                            if (Core.SE)
                            {
                                from.RecentlyReported.Add(killer);
                                Timer.StartTimer(TimeSpan.FromMinutes(10), () => ReportedListExpiry_Callback(from, killer));
                            }

                            if (killer is PlayerMobile pk)
                            {
                                pk.ResetKillTime();
                                pk.SendLocalizedMessage(1049067); // You have been reported for murder!

                                if (pk.Kills == 5)
                                {
                                    pk.SendLocalizedMessage(502134); // You are now known as a murderer!
                                }
                                else if (Stealing.SuspendOnMurder && pk.Kills == 1 && pk.NpcGuild == NpcGuild.ThievesGuild)
                                {
                                    pk.SendLocalizedMessage(501562); // You have been suspended by the Thieves Guild.
                                }
                            }
                        }

                        break;
                    }
                case 2:
                    {
                        break;
                    }
            }

            m_Idx++;
            if (m_Idx < m_Killers.Count)
            {
                from.SendGump(new ReportMurdererGump( m_Killers, m_Idx));
            }
        }

        private class GumpTimer : Timer
        {
            private readonly List<Mobile> m_Killers;
            private readonly Mobile m_Victim;

            public GumpTimer(Mobile victim, List<Mobile> killers) : base(TimeSpan.FromSeconds(4.0))
            {
                m_Victim = victim;
                m_Killers = killers;
            }

            protected override void OnTick()
            {
                m_Victim.SendGump(new ReportMurdererGump(m_Killers));
            }
        }
    }
}
