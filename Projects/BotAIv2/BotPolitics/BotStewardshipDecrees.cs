using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Mobiles;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// The steward's decrees: what is lawful in a town now and what each costs (<see cref="Options"/>), the rule that picks one, the
/// model's question, what each does (<see cref="Apply"/>), and Argus's two commands (<c>do steward</c>, <c>do decree</c>).
///
/// <para>
/// <b>The rule, in its order:</b> the militia when an invasion of the town has stood five minutes with no company come; a tax
/// cut when the regard has sunk three rounds running, by a point and a half, under a tax; a bounty raised on a head wanted in the town and at large for a
/// quarter of an hour; a captain hired when the town has none, or one at war with or loathed by the steward's guild, or its
/// wage budget is spent while heads are at large or an invasion stands; an outcast guild barred for a day (the council's outcast
/// of the town, with none of its members living in it, not seated by it, and a captain to keep the line); a tax raised when the coffer is under twice the floor, the allowance has stopped or a decree was wanted and could not
/// be paid; a caravan when the town's counters are short of a kind the bots found missing; a holiday when the regard is under
/// 45 or the coffer rich; a tax cut when the coffer is rich. The model is shown every lawful decree, including tax steps the rule
/// would not take, and may answer "none".
/// </para>
/// </summary>
public static partial class BotStewardship
{
    private sealed class Option
    {
        public string Id;

        public string Kind;

        public string Arg;

        public int Percent;

        public int Cost;

        /// <summary>Whether the rule takes it; the model is shown it either way.</summary>
        public bool Rule;

        public string Why;

        public string Means;
    }

    /// <summary>Set when a lawful decree was wanted and only the coffer stood in its way: a reason to raise the tax.</summary>
    private static bool _wantedPoor;

    private static string Poor(Office o, int cost)
    {
        _wantedPoor = true;

        return $"the coffer holds {o.Coffer}gp and keeps {Floor}gp: {cost}gp is more than it can spend";
    }

    // ---- what is lawful -----------------------------------------------------------------------------------------------------

    private static string CanTax(Office o, int percent, long now)
    {
        if (percent == 0 || Math.Abs(percent) > Math.Max(1, TaxMostStep))
        {
            return $"a tax decree moves it by 1 to {TaxMostStep} points";
        }

        if (o.TaxedOnce && now - o.TaxedAt < TaxEveryMs)
        {
            return $"the tax was set {(now - o.TaxedAt) / 60000} minutes ago, and once in {TaxEveryMs / 60000} is the law";
        }

        var next = o.Tax + percent / 100.0;

        if (next < TaxFloor - 0.00001 || next > TaxCeiling + 0.00001)
        {
            return $"the tax would be {Pct(next)}, outside {Pct(TaxFloor)} to {Pct(TaxCeiling)}";
        }

        if (next < -0.00001 && o.Coffer < 2 * Floor)
        {
            return $"a tax under nought is paid by the coffer, and it holds {o.Coffer}gp";
        }

        return null;
    }

    private static string CanCaptain(Office o, BotTowns.Town town, string guild)
    {
        if (!BotCouncil.Running || !BotCouncil.Captaincy)
        {
            return "the towns' councils or their captaincy are off";
        }

        if (string.IsNullOrEmpty(guild) || BotGuilds.Named(guild) is not { Disbanded: false } g)
        {
            return $"there is no guild called \"{guild}\"";
        }

        if (!BotCouncil.Eligible(g, town))
        {
            return $"{g.Name} has done none of {town.Name}'s tasks, or is put out of it";
        }

        if (o.Bars.ContainsKey(g.Name))
        {
            return $"{g.Name} is barred from {town.Name}";
        }

        if (o.Guild != g.Name && BotRegard.AtWar(BotGuilds.Named(o.Guild), g))
        {
            return $"{g.Name} is at war with the steward's {o.Guild}";
        }

        if (BotCouncil.CaptainIn(town.Name) == g.Name && BotCouncil.BudgetLeft(town.Name) >= 2 * BotCouncil.Wage)
        {
            return $"{g.Name} captains the guard already with {BotCouncil.BudgetLeft(town.Name)}gp of wages left";
        }

        return o.Coffer - HireGold < Floor ? Poor(o, HireGold) : null;
    }

    private static string CanBounty(Office o, BotTowns.Town town, BotMobile head, long now, out BotCrime.Sheet sheet)
    {
        sheet = null;

        if (!BotCrime.Running)
        {
            return "the town's books on crime are off";
        }

        if (head == null || BotCrime.Of(head.Name) is not { Posted: true } s)
        {
            return $"{head?.Name ?? "nobody"} is not on the wanted list: a steward prices only a head the town's law has posted";
        }

        sheet = s;

        if (BotOutlaw.Jailed(head))
        {
            return $"{head.Name} is in a cell already";
        }

        if (!string.Equals(s.Town, town.Name, StringComparison.OrdinalIgnoreCase))
        {
            return $"{head.Name} is wanted in {s.Town}, not in {town.Name}";
        }

        if (now - s.PostedTick < BountyWaitMs)
        {
            return $"{head.Name} was posted {(now - s.PostedTick) / 60000} minutes ago; the hunters have {BountyWaitMs / 60000} first";
        }

        if (o.Raised.TryGetValue(head.Name, out var at) && now - at < BountyAgainMs)
        {
            return $"the bounty on {head.Name} was raised {(now - at) / 60000} minutes ago";
        }

        var most = BotCrime.BountyMost + BountyAbove;

        if (s.Bounty >= most)
        {
            return $"{head.Name}'s bounty is {s.Bounty}gp, the most a town pays";
        }

        var cost = Math.Min(BountyRaise, most - s.Bounty);

        return o.Coffer - cost < Floor ? Poor(o, cost) : null;
    }

    private static string CanMilitia(Office o, BotTowns.Town town, long now, out BotInvasion.Raid raid)
    {
        raid = null;

        if (!BotInvasion.Running || !BotSeasons.Running)
        {
            return "invasions are off";
        }

        if (BotInvasion.Now is not { Over: false } r || r.Town != town)
        {
            return $"no invasion stands on {town.Name}'s roads: the militia is called against creatures on the road";
        }

        raid = r;

        if (r.Squads.Count > 0)
        {
            return "a company is in the field already";
        }

        if (now - r.Began < MilitiaWaitMs)
        {
            return $"the invasion was called {(now - r.Began) / 60000} minutes ago; the town's call has {MilitiaWaitMs / 60000} first";
        }

        if (o.MilitiaRaid == r.Began && o.MilitiaCalled)
        {
            return "the steward called the militia against this invasion already";
        }

        return o.Coffer - MilitiaGold < Floor ? Poor(o, MilitiaGold) : null;
    }

    private static string CanBar(Office o, BotTowns.Town town, string name)
    {
        if (string.IsNullOrEmpty(name) || BotGuilds.Named(name) is not { Disbanded: false } g)
        {
            return $"there is no guild called \"{name}\"";
        }

        if (g.Name == o.Guild)
        {
            return "a steward does not bar its own guild";
        }

        if (o.Bars.ContainsKey(g.Name))
        {
            return $"{g.Name} is barred already";
        }

        if (o.Bars.Count >= MostBars)
        {
            return $"{town.Name} bars {MostBars} guilds already";
        }

        if (!BotCouncil.IsOutcast(g.Name, town.Name) && BotRegard.Of(o.Guild, g.Name) > BarRegard)
        {
            return $"{g.Name} is no outcast of {town.Name}: its post-holders and its steward bear it no grudge";
        }

        var living = 0;

        for (var i = 0; g.Members != null && i < g.Members.Count; i++)
        {
            if (g.Members[i] is BotMobile { Deleted: false } m && ReferenceEquals(BotResidence.Of(m), town))
            {
                living++;
            }
        }

        if (living > 0)
        {
            return $"{living} of {g.Name}'s members live in {town.Name}: a bar would keep them from their homes";
        }

        var seat = BotSeat.Of(g.Name);

        if (seat != Point3D.Zero && ReferenceEquals(BotTowns.Nearest(seat), town))
        {
            return $"{g.Name} is seated by {town.Name}";
        }

        var captain = BotCouncil.CaptainIn(town.Name);

        if (captain == null || captain == g.Name)
        {
            return $"{town.Name} has no captain of the guard to keep the line";
        }

        return o.Coffer - BarCost < Floor ? Poor(o, BarCost) : null;
    }

    private static string CanHoliday(Office o, BotTowns.Town town)
    {
        if (!BotFair.Running || !BotSeasons.Running)
        {
            return "fairs are off";
        }

        if (BotFair.State != BotFair.Phase.None)
        {
            return $"a fair is {(BotFair.State == BotFair.Phase.Open ? "open" : "announced")} in {BotFair.Town?.Name} already";
        }

        if (o.Pending > 0)
        {
            return "a holiday is being announced already";
        }

        var since = (DateTime.UtcNow - BotFair.LastUtc).TotalHours;

        if (BotFair.LastUtc != DateTime.MinValue && since < HolidayGapHours)
        {
            return $"the last fair opened {since:0.#} hours ago; {HolidayGapHours:0.#} between fairs";
        }

        var mine = (DateTime.UtcNow - o.HolidayUtc).TotalHours;

        if (o.HolidayUtc != DateTime.MinValue && mine < HolidayEveryHours)
        {
            return $"{town.Name} kept a holiday {mine:0.#} hours ago";
        }

        if (Shops(town) < BotFair.LeastShops)
        {
            return $"{town.Name} has fewer than {BotFair.LeastShops} shopkeepers for a fair";
        }

        return o.Coffer - HolidayCost < Floor ? Poor(o, HolidayCost) : null;
    }

    private static string CanCaravan(Office o, BotTowns.Town to, long now, out string kind)
    {
        kind = null;

        if (!BotCaravans.Running || !BotSeasons.Running)
        {
            return "caravans are off";
        }

        if (to is not { FromHome: true })
        {
            return $"{to?.Name ?? "that town"} is not reached from home";
        }

        if (BotCaravans.Now != null || o.CaravanPending)
        {
            return "a caravan is on the road already";
        }

        if (o.CaravanOnce && now - o.CaravanAt < CaravanEveryMs)
        {
            return $"the steward ordered a caravan {(now - o.CaravanAt) / 60000} minutes ago";
        }

        kind = Short(to);

        if (kind == null)
        {
            return $"nothing the bots found missing is short at {to.Name}'s counters";
        }

        return o.Coffer - CaravanGold < Floor ? Poor(o, CaravanGold) : null;
    }

    private static int Shops(BotTowns.Town town)
    {
        var shops = BotShops.Shops;
        var count = 0;

        for (var i = 0; i < shops.Count; i++)
        {
            if (shops[i] is { Deleted: false } v && town.Bounds.Contains(v.Location))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>A kind the bots found a shelf empty of that this town's shopkeepers hold under the caravans' low share, or null.</summary>
    private static string Short(BotTowns.Town town)
    {
        var dry = BotShops.Dry();
        var shops = BotShops.Shops;

        for (var d = 0; d < dry.Count && d < 12; d++)
        {
            var kind = dry[d].Kind;

            if (!BotTrading.Goods(kind))
            {
                continue;
            }

            for (var i = 0; i < shops.Count; i++)
            {
                if (shops[i] is not { Deleted: false } v || !town.Bounds.Contains(v.Location) || !v.IsActiveSeller)
                {
                    continue;
                }

                var offered = v.GetBuyInfo();

                for (var e = 0; e < offered.Length; e++)
                {
                    if (offered[e] is GenericBuyInfo info && info.Type == kind && info.MaxAmount > 0 && info.Amount < info.MaxAmount * BotCaravans.LowShare)
                    {
                        return kind.Name;
                    }
                }
            }
        }

        return null;
    }

    // ---- the rule's choice --------------------------------------------------------------------------------------------------

    /// <summary>Whether this guild should captain this town's guard now, and who the steward would hire.</summary>
    private static string Captain(Office o, BotTowns.Town town, out string why)
    {
        why = null;

        var now = BotCouncil.CaptainIn(town.Name);
        var mine = BotGuilds.Named(o.Guild);
        var guard = 0;

        for (var i = 0; i < _heads.Count; i++)
        {
            if (string.Equals(_heads[i].Sheet.Town, town.Name, StringComparison.OrdinalIgnoreCase))
            {
                guard++;
            }
        }

        var raided = BotInvasion.Now is { Over: false } raid && raid.Town == town;

        if (now == null)
        {
            why = $"{town.Name} has no captain of the guard";
        }
        else if (BotRegard.AtWar(mine, BotGuilds.Named(now)))
        {
            why = $"its captain {now} is at war with the steward's {o.Guild}";
        }
        else if (now != o.Guild && BotRegard.Of(o.Guild, now) <= -30.0)
        {
            why = $"the steward's {o.Guild} thinks {BotRegard.Of(o.Guild, now):F0} of its captain {now}";
        }
        else if ((guard > 0 || raided) && BotCouncil.BudgetLeft(town.Name) < BotCouncil.Wage)
        {
            why = $"the guard's wages are spent with {(guard > 0 ? $"{guard} wanted at large" : "an invasion on the road")}";

            return now;
        }
        else
        {
            return null;
        }

        string best = null;
        var bestRegard = double.MinValue;
        var collector = BotCouncil.CollectorIn(town.Name);

        foreach (var st in BotBurgh.All)
        {
            if (st.Town != town.Name || st.Guild == collector || st.Guild == now || BotGuilds.Named(st.Guild) is not { Disbanded: false } g
                || !BotCouncil.Eligible(g, town) || o.Bars.ContainsKey(g.Name) || g.Name != o.Guild && BotRegard.AtWar(mine, g))
            {
                continue;
            }

            var regard = g.Name == o.Guild ? 100.0 : BotRegard.Of(o.Guild, g.Name);

            if (regard > bestRegard)
            {
                bestRegard = regard;
                best = g.Name;
            }
        }

        return best;
    }

    /// <summary>Every decree lawful in the town now, in the rule's order. See the class note.</summary>
    private static void Options(Office o, BotTowns.Town town, long now, List<Option> into)
    {
        _wantedPoor = false;

        var regard = o.Samples.Count > 0 ? o.Samples[^1] : Regard(o, town);

        if (CanMilitia(o, town, now, out var raid) == null)
        {
            into.Add(
                new Option
                {
                    Id = "militia", Kind = "militia", Cost = MilitiaGold, Rule = true,
                    Why = $"the invasion out of {raid.From} has stood on the road {raid.Bearing} for {(now - raid.Began) / 60000} minutes with no company come",
                    Means = $"call the militia: {MilitiaGold}gp more head money for the creatures on the road {raid.Bearing}, and the call made again"
                }
            );
        }

        // Sunk three rounds running, and by a point and a half: a tax the town will not bear much longer.
        var sinking = o.Samples.Count >= 4 && o.Samples[^1] < o.Samples[^2] - 0.0001 && o.Samples[^2] < o.Samples[^3] - 0.0001
                      && o.Samples[^3] < o.Samples[^4] - 0.0001 && o.Samples[^4] - o.Samples[^1] >= 1.5;

        for (var p = 1; p <= Math.Max(1, TaxMostStep); p++)
        {
            if (CanTax(o, -p, now) == null)
            {
                var rule = sinking && o.Tax > 0.0 && p == Math.Min(2, Math.Max(1, (int)Math.Ceiling(o.Tax * 100 - 0.0001)));

                into.Add(
                    new Option
                    {
                        Id = $"tax-{p}", Kind = "tax", Percent = -p, Rule = rule,
                        Why = rule ? $"the town's regard has sunk three rounds running under a tax of {Pct(o.Tax)}" : "a cut",
                        Means = $"cut the shops' tax by {p} point{(p > 1 ? "s" : "")} to {Pct(o.Tax - p / 100.0)}: bots pay less at the town's counters, and below nought the coffer pays the difference"
                    }
                );
            }
        }

        for (var i = 0; i < _heads.Count; i++)
        {
            var (head, _) = _heads[i];

            if (CanBounty(o, town, head, now, out var sheet) == null)
            {
                var cost = Math.Min(BountyRaise, BotCrime.BountyMost + BountyAbove - sheet.Bounty);

                into.Add(
                    new Option
                    {
                        Id = $"bounty:{head.Name}", Kind = "bounty", Arg = head.Name, Cost = cost, Rule = true,
                        Why = $"{head.Name} has been wanted in {town.Name} for {sheet.Why} for {(now - sheet.PostedTick) / 60000} minutes at {sheet.Bounty}gp",
                        Means = $"raise the bounty on {head.Name} (wanted for {sheet.Why}) from {sheet.Bounty}gp to {sheet.Bounty + cost}gp, paid to whoever takes it"
                    }
                );
            }
        }

        var hire = Captain(o, town, out var hireWhy);

        if (hire != null && CanCaptain(o, town, hire) == null)
        {
            into.Add(
                new Option
                {
                    Id = $"captain:{hire}", Kind = "captain", Arg = hire, Cost = HireGold, Rule = true, Why = hireWhy,
                    Means = $"hire {hire} as captain of the guard for {HireMs / 3600000.0:0.#} hours, {HireGold}gp more for its watches ({BotCouncil.Wage}gp each, paid as they are stood)"
                }
            );
        }

        foreach (var g in BotGuilds.Standing)
        {
            if (g is not { Disbanded: false } || g.Name == o.Guild)
            {
                continue;
            }

            if (!BotCouncil.IsOutcast(g.Name, town.Name) || CanBar(o, town, g.Name) != null)
            {
                continue;
            }

            into.Add(
                new Option
                {
                    Id = $"bar:{g.Name}", Kind = "bar", Arg = g.Name, Cost = BarCost, Rule = true,
                    Why = $"{g.Name} is {town.Name}'s outcast",
                    Means = $"bar {g.Name} from {town.Name} for {BarHours:0} hours: its members are turned back at the guard line and refused work in the town; {BarCost}gp to the guard's watches"
                }
            );
        }

        for (var p = 1; p <= Math.Max(1, TaxMostStep); p++)
        {
            if (CanTax(o, p, now) == null)
            {
                var need = (o.Coffer < 2 * Floor && (!o.Flowing || _wantedPoor)) && regard >= 40.0 && now - o.Since >= PoorMs / 2;
                var rule = need && p == (o.Coffer < Floor ? 2 : 1);

                into.Add(
                    new Option
                    {
                        Id = $"tax+{p}", Kind = "tax", Percent = p, Rule = rule,
                        Why = rule ? $"the coffer holds {o.Coffer}gp{(o.Flowing ? " and a decree it wanted could not be paid" : " and the treasury gives no allowance")}" : "a raise",
                        Means = $"raise the shops' tax by {p} point{(p > 1 ? "s" : "")} to {Pct(o.Tax + p / 100.0)}: the town's counters charge every bot more and the difference fills the coffer; the town likes it less, and traders buy elsewhere"
                    }
                );
            }
        }

        if (CanCaravan(o, town, now, out var kind) == null)
        {
            into.Add(
                new Option
                {
                    Id = $"caravan:{town.Name}", Kind = "caravan", Arg = town.Name, Cost = CaravanGold, Rule = true,
                    Why = $"{town.Name}'s counters are short of {kind}",
                    Means = $"order a caravan to {town.Name} with what its counters are short of ({kind}): {CaravanGold}gp towards the trader's and guards' pay"
                }
            );
        }

        if (CanHoliday(o, town) == null)
        {
            var rule = regard < HolidayRegard || o.Coffer >= Rich;

            into.Add(
                new Option
                {
                    Id = "holiday", Kind = "holiday", Cost = HolidayCost, Rule = rule,
                    Why = regard < HolidayRegard ? $"the town's regard is {regard:F0}" : $"the coffer is rich, {o.Coffer}gp",
                    Means = $"declare a holiday: a fair in {town.Name} in an hour (counters cheaper for bots, a lottery, the championship), {HolidayCost}gp on the feast"
                }
            );
        }

        if (o.Coffer >= Rich)
        {
            for (var i = 0; i < into.Count; i++)
            {
                if (into[i].Kind == "tax" && into[i].Percent == (o.Coffer >= 2 * Rich ? -2 : -1))
                {
                    var cut = into[i];

                    cut.Rule = true;
                    cut.Why = $"the coffer is rich, {o.Coffer}gp";

                    // A rich coffer's cut comes last in the rule's order, after everything the town needs.
                    into.RemoveAt(i);
                    into.Add(cut);

                    break;
                }
            }
        }

        // The rule takes the first it would; the model sees them all. Those the rule would take go first.
        var ruled = 0;

        for (var i = 0; i < into.Count; i++)
        {
            if (into[i].Rule)
            {
                var opt = into[i];

                into.RemoveAt(i);
                into.Insert(ruled++, opt);
            }
        }

        if (ruled == 0 && (Ask == null || !ModelChooses))
        {
            into.Clear();
        }
    }

    private static BotStewardAsk Question(Office o, BotTowns.Town town, List<Option> options, double regard)
    {
        var now = Core.TickCount;

        using var brief = ValueStringBuilder.Create(1024);

        brief.Append($"Town: {town.Name}. You: {o.Head ?? "the head"} of the guild {o.Guild}, its steward for {(now - o.Since) / 60000} minutes.\n");
        brief.Append(
            $"Coffer: {o.Coffer}gp. It keeps {Floor}gp; under that for {PoorMs / 60000} minutes and you are deposed. The treasury's allowance of {Allowance}gp an hour is {(o.Flowing ? "coming in" : "stopped")}.\n"
        );
        brief.Append($"Shops' tax: {Pct(o.Tax)} (lawful {Pct(TaxFloor)} to {Pct(TaxCeiling)}). The town's counters took {o.LastTakings}gp from bots last round.\n");
        brief.Append(
            $"The town's regard of you: {regard:F0} of 100, {Trend(o):+0.0;-0.0;0} over the last {Math.Max(0, o.Samples.Count - 1)} rounds. If it falls {FallLeast:0.#} or more over {FallMs / 60000} minutes without rising once, you are deposed.\n"
        );

        var heads = 0;

        for (var i = 0; i < _heads.Count; i++)
        {
            var (bot, sheet) = _heads[i];

            if (string.Equals(sheet.Town, town.Name, StringComparison.OrdinalIgnoreCase))
            {
                brief.Append(heads == 0 ? "Wanted at large in the town: " : ", ");
                brief.Append($"{bot.Name} for {sheet.Why} ({sheet.Bounty}gp)");
                heads++;
            }
        }

        brief.Append(heads == 0 ? "Nobody wanted in the town is at large.\n" : ".\n");

        if (BotInvasion.Now is { Over: false } raid && raid.Town == town)
        {
            brief.Append($"Invasion: creatures out of {raid.From} on the road {raid.Bearing}, {raid.Squads.Count} companies in the field, {raid.Killed} of {raid.Heads} killed.\n");
        }

        brief.Append($"Captain of the guard: {BotCouncil.CaptainIn(town.Name) ?? "none"} ({BotCouncil.BudgetLeft(town.Name)}gp of wages left); tax collector: {BotCouncil.CollectorIn(town.Name) ?? "none"}.\n");

        if (o.Bars.Count > 0)
        {
            brief.Append("Barred now: ");

            var first = true;

            foreach (var (guild, until) in o.Bars)
            {
                brief.Append(first ? "" : ", ");
                first = false;
                brief.Append($"{guild} for {(until - DateTime.UtcNow).TotalHours:0.#}h more");
            }

            brief.Append(".\n");
        }

        brief.Append("Decrees you may give now:\n- none: nothing this round.\n");

        var ids = new string[options.Count + 1];
        var means = new string[options.Count + 1];

        ids[0] = "none";
        means[0] = "nothing this round";

        for (var i = 0; i < options.Count; i++)
        {
            ids[i + 1] = options[i].Id;
            means[i + 1] = options[i].Means;
            brief.Append($"- {options[i].Id}: {options[i].Means}{(options[i].Cost > 0 ? $" (costs the coffer {options[i].Cost}gp)" : "")}.\n");
        }

        return new BotStewardAsk { Town = town.Name, Guild = o.Guild, Head = o.Head, Brief = brief.ToString(), Ids = ids, Means = means };
    }

    // ---- what a decree does -------------------------------------------------------------------------------------------------

    private static void Record(Office o, string kind, string text, int cost, string by)
    {
        o.Decrees.Add(new Decree { Utc = DateTime.UtcNow, Kind = kind, Text = text, Cost = cost, By = by });

        while (o.Decrees.Count > Math.Max(1, Remembered))
        {
            o.Decrees.RemoveAt(0);
        }

        Decrees++;

        if (by == "the model")
        {
            ByModel++;
        }
        else if (by == "Argus")
        {
            ByArgus++;
        }
        else
        {
            ByRule++;
        }
    }

    /// <summary>Gives the decree, checked again first: the town may have changed since it was offered. Says what came of it.</summary>
    private static string Apply(Office o, BotTowns.Town town, Option opt, string by, string speech, string why)
    {
        var now = Core.TickCount;
        string refused;
        string text = null;
        string said = null;
        var loud = false;
        var cost = 0;

        switch (opt.Kind)
        {
            case "tax":
                {
                    refused = CanTax(o, opt.Percent, now);

                    if (refused != null)
                    {
                        break;
                    }

                    var was = o.Tax;

                    o.Tax = Math.Clamp(was + opt.Percent / 100.0, TaxFloor, TaxCeiling);
                    o.TaxedOnce = true;
                    o.TaxedAt = now;
                    RecountTax();
                    Mood(o, -2.0 * opt.Percent);

                    if (opt.Percent > 0)
                    {
                        TaxUp++;
                    }
                    else
                    {
                        TaxDown++;
                    }

                    text = $"the shops' tax is {Pct(o.Tax)} (it was {Pct(was)})";
                    said = opt.Percent > 0
                        ? $"every counter in {town.Name} charges {Pct(o.Tax)} from this hour, for the town's coffer."
                        : $"every counter in {town.Name} charges {Pct(o.Tax)} from this hour. The town gives back what it can.";

                    break;
                }
            case "captain":
                {
                    refused = CanCaptain(o, town, opt.Arg);

                    if (refused != null)
                    {
                        break;
                    }

                    if (!BotCouncil.Appoint(town, opt.Arg, HireGold, now + Math.Max(60000, HireMs)))
                    {
                        refused = "the council would not seat the captain";

                        break;
                    }

                    o.Coffer -= HireGold;
                    BotCity.Refund(HireGold);
                    WageGold += HireGold;
                    cost = HireGold;
                    Captains++;
                    Mood(o, 2.0);
                    text = $"{opt.Arg} captains the guard for {HireMs / 3600000.0:0.#}h, {HireGold}gp more for its watches";
                    said = $"{opt.Arg} keeps the peace of {town.Name}. The town pays its watches.";

                    break;
                }
            case "bounty":
                {
                    var head = BotCrime.Named(opt.Arg);

                    refused = CanBounty(o, town, head, now, out var sheet);

                    if (refused != null)
                    {
                        break;
                    }

                    var added = BotCrime.Raise(head.Name, Math.Min(BountyRaise, o.Coffer - Floor), BotCrime.BountyMost + BountyAbove);

                    if (added <= 0)
                    {
                        refused = "the town's books would not take a higher bounty";

                        break;
                    }

                    o.Coffer -= added;
                    BotCity.Refund(added);
                    BountyGold += added;
                    cost = added;
                    o.Raised[head.Name] = now;
                    Bounties++;
                    Mood(o, 2.0);
                    loud = true;
                    text = $"the bounty on {head.Name} is {sheet.Bounty}gp (+{added})";
                    said = $"{head.Name} is worth {sheet.Bounty} gold to {town.Name} now. Bring it in.";

                    break;
                }
            case "militia":
                {
                    refused = CanMilitia(o, town, now, out var raid);

                    if (refused != null)
                    {
                        break;
                    }

                    // Through the events' own hold, so their line balances: out of the coffer into the purse, held again for the raid.
                    o.Coffer -= MilitiaGold;
                    BotCity.Refund(MilitiaGold);

                    var held = BotSeasons.Hold(MilitiaGold, MilitiaGold);

                    if (held <= 0)
                    {
                        if (BotCity.Escrow(MilitiaGold))
                        {
                            o.Coffer += MilitiaGold;
                        }

                        refused = "the treasury could not hold the head money for the raid";

                        break;
                    }

                    raid.Pool += held;

                    if (raid.HeadPay > 0)
                    {
                        raid.HeadPay += Math.Max(1, held / Math.Max(1, raid.Heads - raid.Killed));
                    }

                    if (raid.Calls >= BotInvasion.MostCalls)
                    {
                        raid.Calls = Math.Max(0, BotInvasion.MostCalls - 1);
                    }

                    raid.LastCall = 0;
                    o.MilitiaRaid = raid.Began;
                    o.MilitiaCalled = true;
                    MilitiaPaid += held;
                    cost = held;
                    Militias++;
                    Mood(o, 5.0);
                    loud = true;
                    text = $"the militia called against the invasion out of {raid.From}, {held}gp more head money ({raid.Pool}gp in all)";
                    said = $"{town.Name} calls its militia! Creatures out of {raid.From} stand on the road {raid.Bearing}: {raid.Pool} gold for their heads.";

                    break;
                }
            case "bar":
                {
                    refused = CanBar(o, town, opt.Arg);

                    if (refused != null)
                    {
                        break;
                    }

                    var until = DateTime.UtcNow.AddHours(Math.Max(0.1, BarHours));

                    o.Bars[opt.Arg] = until;
                    RebuildBars();
                    o.Coffer -= BarCost;
                    BotCity.Refund(BarCost);
                    BotCouncil.Fund(town.Name, BarCost);
                    WageGold += BarCost;
                    cost = BarCost;
                    BarsSet++;
                    Mood(o, 2.0);
                    loud = true;
                    text = $"{opt.Arg} barred from the town until {until:dd.MM HH:mm} UTC";
                    said = $"{opt.Arg} is barred from {town.Name} for a day. The guard will turn its people back.";

                    if (BotGuilds.Head(BotGuilds.Named(opt.Arg)) is { Alive: true } theirs)
                    {
                        BotVoice.Say(theirs, "guild", $"{town.Name}'s steward has barred us from the town for a day. We buy elsewhere.", true);
                    }

                    break;
                }
            case "holiday":
                {
                    refused = CanHoliday(o, town);

                    if (refused != null)
                    {
                        break;
                    }

                    // Held until the fair is announced in the town; spent then, given back if it is not.
                    o.Coffer -= HolidayCost;
                    o.Pending = HolidayCost;
                    o.PendingAt = now;
                    o.HolidayUtc = DateTime.UtcNow;
                    BotFair.Asked = town.Name;
                    BotFair.CallNow = true;
                    cost = HolidayCost;
                    Holidays++;
                    loud = true;
                    text = $"a holiday: a fair in the town in {BotFair.AheadMs / 60000} minutes, {HolidayCost}gp on the feast";
                    said = $"{town.Name} keeps a holiday! A fair opens in {BotFair.AheadMs / 60000} minutes, and the town feasts.";

                    break;
                }
            case "caravan":
                {
                    var to = BotTowns.Find(opt.Arg) ?? town;

                    refused = CanCaravan(o, to, now, out var kind);

                    if (refused != null)
                    {
                        break;
                    }

                    o.Coffer -= CaravanGold;
                    BotCity.Refund(CaravanGold);
                    o.CaravanPending = true;
                    o.CaravanOnce = true;
                    o.CaravanAt = now;
                    o.CaravanMark = BotCaravans.Posted;
                    o.CaravanTo = to.Name;
                    o.CaravanPaid = CaravanGold;
                    BotCaravans.For = to.Name;
                    BotCaravans.CallNow = true;
                    CaravanPaid += CaravanGold;
                    cost = CaravanGold;
                    Caravans++;
                    loud = true;
                    text = $"a caravan to {to.Name} for its want of {kind}, {CaravanGold}gp towards it";
                    said = $"{to.Name} wants {kind}: the steward pays towards a caravan. Traders, guards, to the road!";

                    break;
                }
            default:
                {
                    refused = $"no decree \"{opt.Kind}\"";

                    break;
                }
        }

        if (refused != null)
        {
            Refused++;

            logger.Information("{Head}, {Town}'s steward, could not decree {Id} ({By}): {Refused}", o.Head, town.Name, opt.Id, by, refused);

            return $"refused: {refused}";
        }

        Record(o, opt.Kind, text, cost, by);

        logger.Information(
            "{Head} of {Guild}, {Town}'s steward, decreed {Text} by {By}: {Why}; the coffer holds {Coffer}gp, the town's regard is {Regard:F0}",
            o.Head,
            o.Guild,
            town.Name,
            text,
            by,
            why ?? opt.Why ?? "no reason given",
            o.Coffer,
            Regard(o, town)
        );

        var line = !string.IsNullOrWhiteSpace(speech) && speech.Length <= 240 ? speech.Trim() : said;

        Speak(o, town, line, loud, opt.Kind);

        return $"{town.Name}: {text}; the coffer holds {o.Coffer}gp";
    }

    /// <summary>A holiday's fair and a caravan's posting, settled the minute after the decree.</summary>
    private static void Resolve(long now)
    {
        foreach (var o in _offices.Values)
        {
            if (o.Pending > 0)
            {
                if (BotFair.State != BotFair.Phase.None && string.Equals(BotFair.Town?.Name, o.Town, StringComparison.OrdinalIgnoreCase))
                {
                    BotCity.Disbursed(o.Pending);
                    FeastGold += o.Pending;
                    o.Pending = 0;
                    Mood(o, 6.0);
                }
                else if (now - o.PendingAt >= 120000)
                {
                    logger.Information("{Town}'s holiday was not held: the fair went to {Fair}; {Gold}gp back in the coffer", o.Town, BotFair.Town?.Name ?? "no town", o.Pending);

                    o.Coffer += o.Pending;
                    o.Pending = 0;
                    HolidaysFailed++;

                    if (string.Equals(BotFair.Asked, o.Town, StringComparison.OrdinalIgnoreCase))
                    {
                        BotFair.Asked = null;
                    }
                }
            }

            if (!o.CaravanPending)
            {
                continue;
            }

            if (BotCaravans.Posted > o.CaravanMark)
            {
                o.CaravanPending = false;
                BotCaravans.For = null;
                Mood(o, 3.0);

                logger.Information("{Town}'s caravan is posted by its steward's decree: {To}", o.Town, BotCaravans.Now?.To?.Name ?? o.CaravanTo);
            }
            else if (now - o.CaravanAt >= 60000)
            {
                o.CaravanPending = false;
                BotCaravans.For = null;
                CaravansFailed++;

                var back = BotCity.Escrow(o.CaravanPaid) ? o.CaravanPaid : 0;

                o.Coffer += back;
                CaravanBack += back;

                logger.Information("{Town}'s caravan to {To} was not posted (nothing it was short of could be carried there); {Back}gp back in the coffer", o.Town, o.CaravanTo, back);
            }
        }
    }

    // ---- Argus --------------------------------------------------------------------------------------------------------------

    /// <summary>The town named at the head of <paramref name="tail"/>, the longest name first, and the rest.</summary>
    private static BotTowns.Town TownIn(string tail, out string rest)
    {
        rest = "";
        tail = (tail ?? "").Trim();

        BotTowns.Town best = null;

        var towns = BotTowns.All;

        for (var i = 0; i < towns.Count; i++)
        {
            var name = towns[i].Name;

            if (name != null && tail.StartsWith(name, StringComparison.OrdinalIgnoreCase) && (tail.Length == name.Length || tail[name.Length] == ' ')
                && (best == null || name.Length > best.Name.Length))
            {
                best = towns[i];
            }
        }

        if (best != null)
        {
            rest = tail[best.Name.Length..].Trim();
        }

        return best;
    }

    /// <summary><c>do steward [&lt;town&gt;]</c>: who governs, the coffer, the tax, the regard, the bars, the last decrees and what is lawful now.</summary>
    public static string Tell(string tail)
    {
        if (string.IsNullOrWhiteSpace(tail))
        {
            return Describe();
        }

        var town = TownIn(tail, out _);

        if (town == null || !_offices.TryGetValue(town.Name, out var o))
        {
            return $"there is no town called \"{tail.Trim()}\" with a stewardship; the towns: {Names()}.";
        }

        var now = Core.TickCount;

        using var say = ValueStringBuilder.Create(1024);

        if (o.Guild == null)
        {
            say.Append($"{town.Name} has no steward: no guild has done {LeastDone} of its tasks and may hold it. ");
        }
        else
        {
            say.Append(
                $"{town.Name}'s steward is {o.Head ?? "?"} of {o.Guild}, for {(now - o.Since) / 60000} minutes; coffer {o.Coffer}gp{(o.Pending > 0 ? $" (+{o.Pending} held for a holiday)" : "")}, allowance {(o.Flowing ? "coming in" : "stopped")}; tax {Pct(o.Tax)}; regard {Regard(o, town):F0} ({Trend(o):+0.0;-0.0;0} over {Math.Max(0, o.Samples.Count - 1)} rounds); next round in {Math.Max(0, (o.NextRound - now) / 60000)} minutes. "
            );
        }

        if (o.Bars.Count > 0)
        {
            say.Append("Barred: ");

            var first = true;

            foreach (var (guild, until) in o.Bars)
            {
                say.Append(first ? "" : ", ");
                first = false;
                say.Append($"{guild} until {until:dd.MM HH:mm} UTC");
            }

            say.Append(". ");
        }

        say.Append(o.Decrees.Count == 0 ? "No decrees yet. " : "Last decrees: ");

        for (var i = o.Decrees.Count - 1; i >= 0; i--)
        {
            var d = o.Decrees[i];

            say.Append($"{(DateTime.UtcNow - d.Utc).TotalMinutes:0}m ago {d.Text} (by {d.By}){(i > 0 ? "; " : ". ")}");
        }

        if (o.Guild != null)
        {
            List<Option> options = [];

            Options(o, town, now, options);

            say.Append(options.Count == 0 ? "Nothing is lawful to decree now." : "Lawful now: ");

            for (var i = 0; i < options.Count; i++)
            {
                say.Append($"{options[i].Id}{(options[i].Rule ? "*" : "")}{(i < options.Count - 1 ? ", " : " (* the rule would)")}");
            }
        }

        return say.ToString();
    }

    private static string Names()
    {
        using var say = ValueStringBuilder.Create(256);

        foreach (var o in _offices.Values)
        {
            say.Append(say.Length > 0 ? ", " : "");
            say.Append(o.Town);
        }

        return say.Length == 0 ? "none yet" : say.ToString();
    }

    /// <summary>
    /// <c>do decree &lt;town&gt; &lt;tax +2 | captain [guild] | bounty &lt;bot&gt; | militia | bar &lt;guild&gt; | holiday | caravan [&lt;town&gt;]&gt;</c>:
    /// the steward gives it by Argus's word, if it is lawful and the coffer can pay. Logged and spoken like any decree.
    /// </summary>
    public static string ByHand(string tail)
    {
        if (!Running)
        {
            return "the stewards are off (dial BotStewardship.Running).";
        }

        var town = TownIn(tail, out var rest);

        if (town == null || !_offices.TryGetValue(town.Name, out var o))
        {
            return $"decree wants a town first; the towns: {Names()}.";
        }

        if (o.Guild == null)
        {
            return $"{town.Name} has no steward to give a decree.";
        }

        var space = rest.IndexOf(' ');
        var verb = (space < 0 ? rest : rest[..space]).ToLowerInvariant();
        var arg = space < 0 ? "" : rest[(space + 1)..].Trim();
        var now = Core.TickCount;
        Option opt;

        switch (verb)
        {
            case "tax":
                {
                    if (!int.TryParse(arg.Replace("%", ""), out var percent) || percent == 0)
                    {
                        return "tax wants a signed step in points: decree <town> tax +2, or tax -1.";
                    }

                    opt = new Option { Id = $"tax{(percent > 0 ? "+" : "")}{percent}", Kind = "tax", Percent = percent };

                    break;
                }
            case "captain":
                {
                    var guild = arg.Length > 0 ? BotGuilds.Named(arg)?.Name ?? arg : Captain(o, town, out _) ?? "";

                    if (guild.Length == 0)
                    {
                        return $"captain wants a guild: {town.Name}'s steward would hire nobody now.";
                    }

                    opt = new Option { Id = $"captain:{guild}", Kind = "captain", Arg = guild };

                    break;
                }
            case "bounty":
                {
                    if (arg.Length == 0)
                    {
                        return "bounty wants a bot on the wanted list: decree <town> bounty <bot>.";
                    }

                    opt = new Option { Id = $"bounty:{arg}", Kind = "bounty", Arg = arg };

                    break;
                }
            case "militia":
                {
                    opt = new Option { Id = "militia", Kind = "militia" };

                    break;
                }
            case "bar":
                {
                    if (arg.Length == 0)
                    {
                        return "bar wants a guild: decree <town> bar <guild>.";
                    }

                    opt = new Option { Id = $"bar:{arg}", Kind = "bar", Arg = BotGuilds.Named(arg)?.Name ?? arg };

                    break;
                }
            case "holiday":
                {
                    opt = new Option { Id = "holiday", Kind = "holiday" };

                    break;
                }
            case "caravan":
                {
                    var to = arg.Length > 0 ? TownIn(arg, out _) : town;

                    if (to == null)
                    {
                        return $"there is no town called \"{arg}\".";
                    }

                    opt = new Option { Id = $"caravan:{to.Name}", Kind = "caravan", Arg = to.Name };

                    break;
                }
            default:
                {
                    return "decree <town> tax +2 | captain [<guild>] | bounty <bot> | militia | bar <guild> | holiday | caravan [<town>].";
                }
        }

        return Apply(o, town, opt, "Argus", null, "asked by hand through the door");
    }
}
