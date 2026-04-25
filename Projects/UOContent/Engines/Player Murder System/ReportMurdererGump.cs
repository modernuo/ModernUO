using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Gumps;
using Server.Misc;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.PlayerMurderSystem;

public class ReportMurdererGump : StaticGump<ReportMurdererGump>
{
    private readonly List<Mobile> _killers;
    private int _idx;

    private ReportMurdererGump(List<Mobile> killers, int idx = 0) : base(0, 0)
    {
        _killers = killers;
        _idx = idx;
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    public static void OnPlayerDeathEvent(PlayerMobile m)
    {
        List<Mobile> killers = null;
        HashSet<Mobile> toGive = null;

        // Guards won't take reports of the death of a thief!
        var notInThievesGuild = m.NpcGuild != NpcGuild.ThievesGuild;

        foreach (var ai in m.Aggressors)
        {
            if (ai.Attacker.Player && ai.CanReportMurder && !ai.Reported && !PlayerMurderSystem.IsRecentlyReported(m, ai.Attacker))
            {
                if (notInThievesGuild)
                {
                    killers ??= new List<Mobile>();
                    killers.Add(ai.Attacker);
                }

                ai.Reported = true;
                ai.CanReportMurder = false;
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
            var (fameAward, _) = Titles.ComputeKillAwards(m, m.Map);

            foreach (var g in toGive)
            {
                Titles.AwardFame(g, fameAward, false);
            }
        }

        if (notInThievesGuild && killers?.Count > 0)
        {
            new GumpTimer(m, killers).Start();
        }
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.SetNoClose();
        builder.SetNoResize();

        builder.AddBackground(265, 205, 320, 290, 5054);

        builder.AddPage();

        builder.AddImageTiled(225, 175, 50, 45, 0xCE);  // Top left corner
        builder.AddImageTiled(267, 175, 315, 44, 0xC9); // Top bar
        builder.AddImageTiled(582, 175, 43, 45, 0xCF);  // Top right corner
        builder.AddImageTiled(225, 219, 44, 270, 0xCA); // Left side
        builder.AddImageTiled(582, 219, 44, 270, 0xCB); // Right side
        builder.AddImageTiled(225, 489, 44, 43, 0xCC);  // Lower left corner
        builder.AddImageTiled(267, 489, 315, 43, 0xE9); // Lower Bar
        builder.AddImageTiled(582, 489, 43, 43, 0xCD);  // Lower right corner

        builder.AddPage(1);

        builder.AddHtmlPlaceholder(260, 234, 300, 140, "killerName"); // Player's Name
        builder.AddHtmlLocalized(260, 254, 300, 140, 1049066);    // Would you like to report...

        builder.AddButton(260, 300, 0xFA5, 0xFA7, 1);
        builder.AddHtmlLocalized(300, 300, 300, 50, 1046362); // Yes

        builder.AddButton(360, 300, 0xFA5, 0xFA7, 2);
        builder.AddHtmlLocalized(400, 300, 300, 50, 1046363); // No
    }

    protected override void BuildStrings(ref GumpStringsBuilder builder)
    {
        builder.SetStringSlot("killerName", _killers[_idx].Name);
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = (PlayerMobile)state.Mobile;

        switch (info.ButtonID)
        {
            case 1:
                {
                    PlayerMurderSystem.ReportMurder(from, _killers[_idx]);
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
            if (PlayerMurderSystem.BountiesEnabled && _victim is PlayerMobile pm)
            {
                pm.SendGump(new BountyReportMurdererGump(pm, _killers));
            }
            else
            {
                _victim.SendGump(new ReportMurdererGump(_killers));
            }
        }
    }
}
