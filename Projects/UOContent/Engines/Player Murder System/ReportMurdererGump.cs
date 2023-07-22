using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.SkillHandlers;

namespace Server.Engines.PlayerMurderSystem;

public class ReportMurdererGump : Gump
{
    // Recently reported
    private static TimeSpan _recentlyReportedDelay;
    private static readonly HashSet<(Mobile, Mobile)> _recentlyReported = new();

    private readonly List<Mobile> _killers;
    private int _idx;

    private ReportMurdererGump(List<Mobile> killers, int idx = 0) : base(0, 0)
    {
        _killers = killers;
        _idx = idx;
        BuildGump();
    }

    public static void Initialize()
    {
        _recentlyReportedDelay = ServerConfiguration.GetOrUpdateSetting("murderSystem.recentlyReportedDelay", TimeSpan.FromMinutes(10));
        EventSink.PlayerDeath += OnPlayerDeath;
    }

    public static void OnPlayerDeath(Mobile m)
    {
        List<Mobile> killers = null;
        HashSet<Mobile> toGive = null;

        // Guards won't take reports of the death of a thief!
        bool notInThievesGuild = m is not PlayerMobile { NpcGuild: NpcGuild.ThievesGuild };

        foreach (var ai in m.Aggressors)
        {
            if (ai.Attacker.Player && ai.CanReportMurder && !ai.Reported)
            {
                if (!Core.SE || !_recentlyReported.Contains((m, ai.Attacker)))
                {
                    if (notInThievesGuild)
                    {
                        killers ??= new List<Mobile>();
                        killers.Add(ai.Attacker);
                    }

                    ai.Reported = true;
                    ai.CanReportMurder = false;
                }
            }

            if (ai.Attacker.Player && Core.Now - ai.LastCombatTime < TimeSpan.FromSeconds(30.0))
            {
                toGive ??= new HashSet<Mobile>();
                toGive.Add(ai.Attacker);
            }
        }

        foreach (var ai in m.Aggressed)
        {
            if (ai.Defender.Player && Core.Now - ai.LastCombatTime < TimeSpan.FromSeconds(30.0))
            {
                toGive ??= new HashSet<Mobile>();
                toGive.Add(ai.Defender);
            }
        }

        if (toGive?.Count > 0)
        {
            foreach (var g in toGive)
            {
                var n = Notoriety.Compute(g, m);

                var ourKarma = g.Karma;
                var innocent = n == Notoriety.Innocent;
                var criminal = n is Notoriety.Criminal or Notoriety.Murderer;

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
        }

        if (notInThievesGuild && killers?.Count > 0)
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

        AddHtml(260, 234, 300, 140, _killers[_idx].Name); // Player's Name
        AddHtmlLocalized(260, 254, 300, 140, 1049066);    // Would you like to report...

        AddButton(260, 300, 0xFA5, 0xFA7, 1);
        AddHtmlLocalized(300, 300, 300, 50, 1046362); // Yes

        AddButton(360, 300, 0xFA5, 0xFA7, 2);
        AddHtmlLocalized(400, 300, 300, 50, 1046363); // No
    }

    public override void OnResponse(NetState state, RelayInfo info)
    {
        var from = (PlayerMobile)state.Mobile;

        switch (info.ButtonID)
        {
            case 1:
                {
                    var killer = _killers[_idx];
                    if (killer?.Deleted == false)
                    {
                        if (Core.SE)
                        {
                            if (_recentlyReported.Add((from, killer)))
                            {
                                Timer.DelayCall(
                                    _recentlyReportedDelay,
                                    static (f, k) => _recentlyReported.Remove((f, k)),
                                    from,
                                    killer
                                );
                            }
                        }

                        if (killer is PlayerMobile pk)
                        {
                            // Increment their short term murders, their kills, and reset the murder decay time
                            PlayerMurderSystem.OnPlayerMurder(pk);

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

        _idx++;
        if (_idx < _killers.Count)
        {
            from.SendGump(new ReportMurdererGump(_killers, _idx));
        }
    }

    private class GumpTimer : Timer
    {
        private readonly List<Mobile> _killers;
        private readonly Mobile _victim;

        public GumpTimer(Mobile victim, List<Mobile> killers) : base(TimeSpan.FromSeconds(4.0))
        {
            _victim = victim;
            _killers = killers;
        }

        protected override void OnTick()
        {
            _victim.SendGump(new ReportMurdererGump(_killers));
        }
    }
}
