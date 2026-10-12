using System;
using System.Collections.Generic;
using System.Text.Json;
using Server.Items;
using Server.Logging;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// The Needs board counted by Patrick's measure: every line a bot asks for, whether it was met within its first hour, by whom,
/// and, when it was not, why (02.10.2026, build 391; step 1 of the Needs plan, <c>needs-design.md</c> §5 and §8).
///
/// <para>
/// <b>Nothing here decides anything.</b> The market calls in at the four places a line's life turns: a want said or topped up
/// (<see cref="Open"/>, <see cref="Topped"/>), units handed over (<see cref="Filled"/>, the one choke point
/// <see cref="BotMarket.Received(BotWant, int, int, Mobile, BotRoute)"/>), a want leaving the board (<see cref="Ended"/>) and the market's own beat
/// (<see cref="Aged"/>). Until this, "met", "taken back" and "orphaned" were bare counters with no clock, and the baseline of
/// 02.10 had to rebuild every line from the log by buyer and kind.
/// </para>
///
/// <para>
/// <b>How a line is settled, once.</b>
/// <list type="bullet">
/// <item><b>Met</b>: every unit handed to the buyer within <see cref="WindowMs"/> of the line opening, by one bot or jointly.</item>
/// <item><b>Failed</b>: not met at its hour; or, after its first <see cref="GraceMs"/>, stood down because the buyer could not pay
/// (Patrick, 02.10: "a buyer that can no longer pay after its 10th minute is a failure"), taken back by its buyer, or given up.
/// The cause is named from what the want recorded: the buyer dead or out of the world, part handed, taken by a carrier and not
/// brought, offered and nothing brought, passed over (the commonest reason), or nobody able looked.</item>
/// <item><b>Kept out, and printed</b>: the buyer gone; taken back or unpaid inside its first ten minutes (the shopper's pile wait
/// is three); the market switched off.</item>
/// </list>
/// A line met after its hour stays a failure and is counted again as met after the hour. A trader's want is procurement for
/// another bot's line and is counted apart, outside the share, as <see cref="BotGlut"/> keeps it out of demand.
/// </para>
/// </summary>
public static class BotLines
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotLines));

    /// <summary>The first hour: a line met inside it is met. Patrick's number.</summary>
    public static int WindowMs { get; set; } = 3600000;

    /// <summary>
    /// A line's first ten minutes: taken back or unpaid inside them it is kept out; after them it is a failure. The same ten
    /// minutes as <see cref="BotMarket.RaiseMs"/> by default, and a separate number on purpose.
    /// </summary>
    public static int GraceMs { get; set; } = 600000;

    /// <summary>A top-up later than this after the newest line opened is a new line; sooner, more units on the same one.</summary>
    public static int NewLineMs { get; set; } = 600000;

    /// <summary>A buyer dead this long of a failed line's hour, or dead at its end, names the failure "buyer dead".</summary>
    public static int DeadCauseMs { get; set; } = 900000;

    internal const int PassKinds = 12;

    private static readonly string[] _passNames =
    [
        "buyer past a carrier's reach", "buyer on another land", "no way through to the buyer", "buyer could not pay for one",
        "worth under the carrier's least", "maker short of material", "trader found nowhere to have it",
        "trader found no plan that paid", "trader judged the ground or road too dangerous", "buyer dead when looked at",
        "carrier's road to the buyer asks more than it brings", "buyer's pack has no room"
    ];

    public static string PassName(int kind) => kind >= 0 && kind < _passNames.Length ? _passNames[kind] : "?";

    // ---- Counted, since the shard started -----------------------------------------------------------------------------------

    public static long Opened { get; private set; }

    public static long Topups { get; private set; }

    public static long ProcOpened { get; private set; }

    public static long Restored { get; private set; }

    public static long Met { get; private set; }

    public static long MetOne { get; private set; }

    public static long MetJoint { get; private set; }

    /// <summary>
    /// Lines met by the buyer itself (06.10.2026, ribs17b): taken back as "had it another way" (<c>BotWithdraw.Have</c>) inside
    /// their hour with the line's open units in the buyer's own pack at that moment. Patrick, 06.10.2026: "пополнил запас сам —
    /// это успех" (it restocked itself: that is a success). Inside <see cref="Met"/>, apart from <see cref="MetOne"/> and
    /// <see cref="MetJoint"/>. A take-back without the units in hand keeps the ten-minute rule (kept out inside, failed after).
    /// </summary>
    public static long MetSelf { get; private set; }

    private const string SelfCause = "met by the buyer itself";

    /// <summary>Whether the buyer's pack holds what is still open of this line (its units less those handed).</summary>
    private static bool HoldsOpen(BotWant want, BotLine line)
    {
        var pack = want?.Buyer?.Self?.Backpack;

        return pack != null && want.Kind != null && pack.GetAmount(want.Kind) >= Math.Max(1, line.Units - line.Filled);
    }

    public static long Failed { get; private set; }

    public static long MetLate { get; private set; }

    public static long KeptOut { get; private set; }

    public static long ProcMet { get; private set; }

    public static long ProcFailed { get; private set; }

    public static long ProcKeptOut { get; private set; }

    /// <summary>
    /// Of <see cref="Met"/> and <see cref="Failed"/>, the lines for raw meat (<see cref="BotOven.Raw"/>): a cook's supper stock, whose
    /// wants the stores size to what the island brings (<c>BotStores.SupplyGated</c>, 03.10.2026). Printed beside the share for
    /// everything else, so that fewer rib lines said is never read as better service (review of build next, F2).
    /// </summary>
    public static long MetStock { get; private set; }

    public static long FailedStock { get; private set; }

    /// <summary>Whether a line is for raw meat. See <see cref="MetStock"/>.</summary>
    private static bool Stock(BotLine line)
    {
        var kind = line?.Want?.Kind;
        var raw = BotOven.Raw;

        for (var i = 0; kind != null && i < raw.Count; i++)
        {
            if (raw[i] == kind)
            {
                return true;
            }
        }

        return false;
    }

    private static string Percent(long met, long failed) => met + failed > 0 ? $"{met / (double)(met + failed):P1}" : "n/a";

    /// <summary>Met lines by route: a carrier's walk, a trader's book, the buyer at a bot's shop. A line met two ways counts in both.</summary>
    public static long ByCarrier { get; private set; }

    public static long ByTrader { get; private set; }

    public static long AtShop { get; private set; }

    private static readonly Dictionary<string, long> _failedBy = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, long> _keptOutBy = new(StringComparer.Ordinal);

    /// <summary>Minutes from opening to the last unit (met lines) and to the first unit (any line), the newest <see cref="Kept"/> of each.</summary>
    private const int Kept = 2048;

    private static readonly float[] _metMinutes = new float[Kept];

    private static readonly float[] _firstMinutes = new float[Kept];

    private static long _metCount;

    private static long _firstCount;

    // ---- Where a line's life turns ------------------------------------------------------------------------------------------

    /// <summary>A want said for the first time: its first line.</summary>
    internal static BotLine Open(BotWant want, int units)
    {
        var line = want.OpenLine(units, Core.TickCount, IsProcurement(want));

        Count(line);

        return line;
    }

    /// <summary>A want asked for more: more units on its newest line, or a new line when the newest is older than <see cref="NewLineMs"/> or settled.</summary>
    internal static BotLine Topped(BotWant want, int units)
    {
        if (want == null || units <= 0)
        {
            return null;
        }

        var now = Core.TickCount;
        var newest = want.Newest;

        if (newest is { Settled: false } && now - newest.OpenedTick < NewLineMs)
        {
            newest.More(units);

            return newest;
        }

        var line = want.OpenLine(units, now, IsProcurement(want));

        // "Of them" in the summary: Opened leaves a trader's procurement out, so this does too.
        if (!line.Procurement)
        {
            Topups++;
        }

        Count(line);

        logger.Information(
            "Line #{Line}: {Buyer} asked for {Units} more {Item} on its standing want, a new line",
            line.Name,
            want.Buyer?.Self?.Name,
            units,
            want.Kind?.Name
        );

        return line;
    }

    private static void Count(BotLine line)
    {
        if (line.Procurement)
        {
            ProcOpened++;
        }
        else
        {
            Opened++;
        }
    }

    /// <summary>Lines read back from the save. Their clocks go on; they are counted as carried over, not as opened.</summary>
    internal static void Carried(int lines) => Restored += Math.Max(0, lines);

    /// <summary>A trader's want is procurement for another bot's line. See the class remarks.</summary>
    // A quartermaster's want for its guild's store is procurement too, counted apart from the members' own lines (07.10.2026, officers20).
    private static bool IsProcurement(BotWant want) => BotTrading.Trader(want?.Buyer?.Self) || BotQuartermaster.Procures(want);

    /// <summary>Units handed to the buyer against the want: they fill its lines oldest first.</summary>
    internal static void Filled(BotWant want, int units, Mobile seller, BotRoute route)
    {
        if (want == null || units <= 0)
        {
            return;
        }

        var now = Core.TickCount;
        var lines = want.Lines;
        var left = units;

        for (var i = 0; i < lines.Count && left > 0; i++)
        {
            var line = lines[i];

            if (line.Complete)
            {
                continue;
            }

            var take = Math.Min(left, line.Units - line.Filled);

            if (take <= 0)
            {
                continue;
            }

            var first = !line.AnyFill;

            line.Fill(take, seller, route, now);
            left -= take;

            if (first && !line.Procurement)
            {
                Keep(_firstMinutes, ref _firstCount, (now - line.OpenedTick) / 60000.0);
            }

            if (line.Complete)
            {
                Completed(line, now);
            }
        }
    }

    private static void Completed(BotLine line, long now)
    {
        var age = now - line.OpenedTick;

        if (line.State == BotLineState.Open)
        {
            if (age <= WindowMs)
            {
                Settle(line, BotLineState.Met, default, now);

                return;
            }

            // Crossed its hour between two beats of the market: a failure like any other line past its hour.
            Settle(line, BotLineState.Failed, ("met after its hour", $"met {age / 60000.0:F1} minutes after it opened"), now);
        }

        if (line.State == BotLineState.Failed && !line.MetLate)
        {
            line.MetLate = true;
            MetLate++;

            logger.Information(
                "Line #{Line} ({Buyer}'s {Units} {Item}) was met after its hour, {Minutes:F1} minutes after it opened",
                line.Name,
                line.Want?.Buyer?.Self?.Name,
                line.Units,
                line.Want?.Kind?.Name,
                age / 60000.0
            );
        }
    }

    /// <summary>The market's beat: a line still open at its hour has failed it.</summary>
    internal static void Aged(BotWant want, long now)
    {
        var lines = want.Lines;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (line.State == BotLineState.Open && now - line.OpenedTick >= WindowMs)
            {
                AtHour(line, now);
            }
        }
    }

    /// <summary>
    /// A line at its hour, unmet. Failed, with its cause — unless its buyer is out of the world, resting (<c>BotRest</c> parks a
    /// bot on the internal map for hours): a line nobody could hand anything to is not the island's failure, and is kept out and
    /// printed.
    /// </summary>
    private static void AtHour(BotLine line, long now)
    {
        if (line.Want is { Away: true })
        {
            Settle(line, BotLineState.KeptOut, ("buyer out of the world, resting", "its buyer was out of the world at its hour"), now);

            return;
        }

        Settle(line, BotLineState.Failed, CauseAtHour(line, now), now);
    }

    /// <summary>The want has left the board: every line still open is settled by how it left.</summary>
    internal static void Ended(BotWant want, BotLineEnd end, string why)
    {
        if (want == null)
        {
            return;
        }

        var now = Core.TickCount;
        var lines = want.Lines;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (line.State != BotLineState.Open)
            {
                continue;
            }

            var age = now - line.OpenedTick;

            if (age >= WindowMs)
            {
                AtHour(line, now);

                continue;
            }

            switch (end)
            {
                case BotLineEnd.Met:
                    {
                        if (line.Complete)
                        {
                            Settle(line, BotLineState.Met, default, now);
                        }
                        else
                        {
                            Settle(line, BotLineState.Failed, ("ended unmet", $"the want ended with {line.Filled} of {line.Units} handed"), now);
                        }

                        break;
                    }
                case BotLineEnd.Orphaned:
                    {
                        Settle(line, BotLineState.KeptOut, ("buyer gone", "the buyer is gone"), now);

                        break;
                    }
                case BotLineEnd.Stopped:
                    {
                        Settle(line, BotLineState.KeptOut, ("market stopped", "the market stopped"), now);

                        break;
                    }
                case BotLineEnd.Withdrawn:
                    {
                        // Taken back for its own stock with the units in hand: met by the buyer itself (06.10.2026, ribs17b). See MetSelf.
                        if (!line.Procurement && why == "had it another way" && HoldsOpen(want, line))
                        {
                            Settle(line, BotLineState.Met, (SelfCause, "met by the buyer itself"), now);

                            break;
                        }

                        // "Nobody brought it, walked to a counter" (BotWithdraw.Walked), kept out: followed. See Watched.
                        if (age < GraceMs && !line.Procurement && why != null && why.StartsWith("nobody brought it", StringComparison.Ordinal))
                        {
                            Watch(want);
                        }

                        Settle(
                            line,
                            age < GraceMs ? BotLineState.KeptOut : BotLineState.Failed,
                            age < GraceMs ? ($"taken back in its first {GraceMs / 60000} min: {why}", $"taken back after {age / 60000.0:F1} min: {why}") : ($"taken back: {why}", $"taken back after {age / 60000.0:F1} min: {why}"),
                            now
                        );

                        break;
                    }
                case BotLineEnd.Unpaid:
                    {
                        Settle(
                            line,
                            age < GraceMs ? BotLineState.KeptOut : BotLineState.Failed,
                            age < GraceMs ? ($"could not pay in its first {GraceMs / 60000} min", $"{why} after {age / 60000.0:F1} min") : (why, $"{why} after {age / 60000.0:F1} min"),
                            now
                        );

                        break;
                    }
                default:
                    {
                        // Given up: at its price ceiling, or unable to reserve a louder price for longer than a want stands.
                        Settle(line, BotLineState.Failed, (why, $"{why} after {age / 60000.0:F1} min"), now);

                        break;
                    }
            }
        }
    }

    // ---- The kept-out lines followed for twenty minutes (06.10.2026, needs15) ------------------------------------------------

    /// <summary>
    /// How long a line taken back inside its first ten minutes is followed: did its buyer get the kind after all? The analyst's
    /// question of 06.10.2026 (an-needs14 §1.5): of 652 such lines on 04.10, 453 had a purchase within twenty minutes and 199 had
    /// none to be found in the log. Nought switches the watch off. Nothing here moves a line into or out of the share.
    /// </summary>
    public static int KeptWatchMs { get; set; } = 1200000;

    /// <summary>Kept-out lines whose buyer's pack held more of the kind within the watch, held no more by its end, or lost sight of (gone, dead or out of the world).</summary>
    public static long KeptGot { get; private set; }

    public static long KeptNotGot { get; private set; }

    public static long KeptLost { get; private set; }

    private static readonly List<(Mobile Buyer, Type Kind, int Base, long Tick)> _watched = [];

    private static void Watch(BotWant want)
    {
        if (KeptWatchMs <= 0 || want?.Kind == null || want.Buyer?.Self is not { Deleted: false } buyer || buyer.Backpack == null)
        {
            return;
        }

        // At most one watch a buyer and kind, and a bounded list: the instrument must not become a load of its own.
        for (var i = 0; i < _watched.Count; i++)
        {
            if (ReferenceEquals(_watched[i].Buyer, buyer) && _watched[i].Kind == want.Kind)
            {
                return;
            }
        }

        if (_watched.Count >= 2048)
        {
            return;
        }

        _watched.Add((buyer, want.Kind, buyer.Backpack.GetAmount(want.Kind), Core.TickCount));
    }

    /// <summary>
    /// The market's beat: each watched buyer's pack read for the kind. More than at the withdrawal is "got it" (a purchase, a make,
    /// a loot); at the watch's end, "not got". Read every beat (thirty seconds), so a thing bought and used up inside one beat is
    /// missed, which undercounts "got": a bound, as the analyst's was.
    /// </summary>
    internal static void Watched(long now)
    {
        for (var i = _watched.Count - 1; i >= 0; i--)
        {
            var (buyer, kind, held, tick) = _watched[i];

            if (buyer is not { Deleted: false, Alive: true } || buyer.Map == null || buyer.Map == Map.Internal || buyer.Backpack == null)
            {
                KeptLost++;
                _watched.RemoveAt(i);

                continue;
            }

            if (buyer.Backpack.GetAmount(kind) > held)
            {
                KeptGot++;
                _watched.RemoveAt(i);

                continue;
            }

            if (now - tick >= KeptWatchMs)
            {
                KeptNotGot++;
                _watched.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Why a line still standing at its hour failed it, from what its want recorded since it opened. First match wins, in the
    /// order a reader would look: the buyer could not be handed anything (dead, or out of the world); part came; a carrier took
    /// it and did not bring it; a walk, a haul or an order was offered and nothing came; able bots looked and passed it over;
    /// nobody able looked.
    /// </summary>
    private static (string Key, string Said) CauseAtHour(BotLine line, long now)
    {
        var want = line.Want;
        var buyer = want?.Buyer?.Self;
        var dead = want == null ? 0L : want.DeadMsAt(now) - line.DeadBase;

        if (buyer is not { Alive: true } || dead >= DeadCauseMs)
        {
            return ("buyer dead", $"its buyer lay dead {dead / 60000.0:F0} min of it{(buyer is { Alive: false } ? " and is dead now" : "")}");
        }

        var away = want == null ? 0L : want.AwayMsAt(now) - line.AwayBase;

        if (away >= DeadCauseMs)
        {
            return ("buyer out of the world", $"its buyer was out of the world, resting, {away / 60000.0:F0} min of it");
        }

        if (line.Filled > 0)
        {
            return ("partial", $"{line.Filled} of {line.Units} handed by {line.Hands} bot(s)");
        }

        var takes = line.TakesSince;

        if (takes > 0)
        {
            return ("taken by a carrier, not brought in time", $"{takes} carrier(s) took the walk and none arrived");
        }

        var offers = line.OffersSince;

        if (offers > 0)
        {
            return ("offered, nothing brought", $"offered {offers} times to bots' auctions as a walk, a haul or an order, and nothing reached the buyer");
        }

        var top = -1;
        var most = 0;
        var total = 0;

        for (var i = 0; i < PassKinds; i++)
        {
            var passed = line.PassedSince(i);

            total += passed;

            if (passed > most)
            {
                most = passed;
                top = i;
            }
        }

        if (top >= 0)
        {
            return ($"passed over: {PassName(top)}", $"passed over {total} times, {most} of them: {PassName(top)}");
        }

        // What the want recorded before a restart is not kept, only its lines: say so rather than "nobody looked".
        if (line.Restored)
        {
            return ("unmet across a restart", "carried over a restart with nothing recorded since");
        }

        return ("nobody able looked", "no bot holding it, able to make it or trading it looked at it");
    }

    private static void Settle(BotLine line, BotLineState state, (string Key, string Said) cause, long now)
    {
        if (line.State != BotLineState.Open)
        {
            return;
        }

        line.State = state;
        line.Cause = cause.Key;

        var want = line.Want;
        var buyer = want?.Buyer?.Self?.Name ?? "somebody";
        var item = want?.Kind?.Name ?? "goods";
        var minutes = (now - line.OpenedTick) / 60000.0;
        var tag = line.Procurement ? " (a trader's procurement)" : "";

        switch (state)
        {
            case BotLineState.Met:
                {
                    if (line.Procurement)
                    {
                        ProcMet++;
                    }
                    else
                    {
                        Met++;

                        if (Stock(line))
                        {
                            MetStock++;
                        }

                        if (cause.Key == SelfCause)
                        {
                            MetSelf++;
                        }
                        else if (line.Hands > 1)
                        {
                            MetJoint++;
                        }
                        else
                        {
                            MetOne++;
                        }

                        if ((line.Routes & BotRoute.Carrier) != 0)
                        {
                            ByCarrier++;
                        }

                        if ((line.Routes & BotRoute.Trader) != 0)
                        {
                            ByTrader++;
                        }

                        if ((line.Routes & BotRoute.Shop) != 0)
                        {
                            AtShop++;
                        }

                        Keep(_metMinutes, ref _metCount, minutes);
                    }

                    logger.Information(
                        "Line #{Line} ({Buyer}'s {Units} {Item}){Tag} met in {Minutes:F1} min by {Hands}",
                        line.Name,
                        buyer,
                        line.Units,
                        item,
                        tag,
                        minutes,
                        cause.Key == SelfCause ? "the buyer itself" : line.Hands > 1 ? $"{line.Hands} bots jointly" : "one bot"
                    );

                    break;
                }
            case BotLineState.Failed:
                {
                    if (line.Procurement)
                    {
                        ProcFailed++;
                    }
                    else
                    {
                        Failed++;
                        _failedBy[cause.Key] = _failedBy.GetValueOrDefault(cause.Key) + 1;

                        if (Stock(line))
                        {
                            FailedStock++;
                        }
                    }

                    logger.Information(
                        "Line #{Line} ({Buyer}'s {Units} {Item}){Tag} failed after {Minutes:F1} min: {Cause} — {Said}",
                        line.Name,
                        buyer,
                        line.Units,
                        item,
                        tag,
                        minutes,
                        cause.Key,
                        cause.Said
                    );

                    break;
                }
            default:
                {
                    if (line.Procurement)
                    {
                        ProcKeptOut++;
                    }
                    else
                    {
                        KeptOut++;
                        _keptOutBy[cause.Key] = _keptOutBy.GetValueOrDefault(cause.Key) + 1;
                    }

                    logger.Information(
                        "Line #{Line} ({Buyer}'s {Units} {Item}){Tag} kept out of the share after {Minutes:F1} min: {Cause}",
                        line.Name,
                        buyer,
                        line.Units,
                        item,
                        tag,
                        minutes,
                        cause.Key
                    );

                    break;
                }
        }
    }

    private static void Keep(float[] ring, ref long count, double minutes)
    {
        ring[(int)(count % ring.Length)] = (float)Math.Max(0.0, minutes);
        count++;
    }

    private static double Percentile(float[] ring, long count, double share)
    {
        var n = (int)Math.Min(count, ring.Length);

        if (n <= 0)
        {
            return 0.0;
        }

        var sorted = new float[n];

        Array.Copy(ring, sorted, n);
        Array.Sort(sorted);

        return sorted[Math.Clamp((int)Math.Ceiling(share * n) - 1, 0, n - 1)];
    }

    /// <summary>The share of lines met in their hour, of those settled met or failed; null before any.</summary>
    public static double? Share => Met + Failed > 0 ? Met / (double)(Met + Failed) : null;

    /// <summary>The lines standing now: open inside their hour, the part of those past half of it, and failed lines whose want still stands.</summary>
    private static (int Open, int Past30, int StandingFailed) Standing(long now)
    {
        var open = 0;
        var past30 = 0;
        var failed = 0;
        var wants = BotMarket.Wants;

        for (var i = 0; i < wants.Count; i++)
        {
            var lines = wants[i].Lines;

            for (var j = 0; j < lines.Count; j++)
            {
                var line = lines[j];

                if (line.Procurement)
                {
                    continue;
                }

                if (line.State == BotLineState.Open)
                {
                    open++;

                    if (now - line.OpenedTick >= WindowMs / 2)
                    {
                        past30++;
                    }
                }
                else if (line.State == BotLineState.Failed && !line.Complete)
                {
                    failed++;
                }
            }
        }

        return (open, past30, failed);
    }

    private static string Tally(Dictionary<string, long> counts, int most)
    {
        if (counts.Count == 0)
        {
            return "none";
        }

        List<(string Key, long Count)> ranked = [];

        foreach (var (key, count) in counts)
        {
            ranked.Add((key, count));
        }

        ranked.Sort(static (a, b) => b.Count.CompareTo(a.Count));

        using var say = ValueStringBuilder.Create(256);

        for (var i = 0; i < ranked.Count && i < most; i++)
        {
            if (i > 0)
            {
                say.Append(", ");
            }

            say.Append(ranked[i].Key);
            say.Append(' ');
            say.Append(ranked[i].Count);
        }

        if (ranked.Count > most)
        {
            say.Append(", and ");
            say.Append(ranked.Count - most);
            say.Append(" more causes");
        }

        return say.ToString();
    }

    /// <summary>
    /// The <c>Lines:</c> summary line, cumulative since the shard started. The share is met over met plus failed, of lines that are
    /// not a trader's procurement; kept-out lines are printed and stay out of it.
    /// </summary>
    public static string Describe()
    {
        var now = Core.TickCount;
        var (open, past30, standing) = Standing(now);
        var share = Share;

        return $"{Opened} opened ({Topups} of them as a top-up on a standing want), {Restored} carried over a restart; {open} open now ({past30} past half their hour), {standing} failed and still standing; "
               + $"in the first {WindowMs / 60000} min: {Met} met ({MetOne} by one bot, {MetJoint} jointly, {MetSelf} by the buyer itself (taken back holding what it asked for); by a carrier {ByCarrier}, a trader {ByTrader}, at a bot's shop {AtShop}; median {Percentile(_metMinutes, _metCount, 0.5):F1} min, 90th {Percentile(_metMinutes, _metCount, 0.9):F1}; first unit at median {Percentile(_firstMinutes, _firstCount, 0.5):F1} min), "
               + $"{Failed} failed ({Tally(_failedBy, 8)}), {MetLate} met after their hour; "
               + $"kept out {KeptOut} ({Tally(_keptOutBy, 5)}; of those taken back, followed {KeptWatchMs / 60000} min: {KeptGot} buyers held more of the kind within it, {KeptNotGot} did not, {KeptLost} lost sight of, {_watched.Count} still followed); "
               + $"share {(share is { } s ? $"{s:P1}" : "n/a")} ({Met} of {Met + Failed}, target 99 %): raw meat {Percent(MetStock, FailedStock)} ({MetStock} of {MetStock + FailedStock}; its asks sized to what the island brings, {BotStores.OutrunBots} bots held back from asking, {BotStores.HeldNow()} now; ribs brought {BotMarket.Pace(typeof(RawRibs))} an hour at the last quarter-hour's pace, {BotMarket.Brought(typeof(RawRibs))} in the hour, room {BotStores.StockRoom(typeof(RawRibs))} against {BotMarket.Wanting(typeof(RawRibs))} asked; {BotStores.SizedToRoom + BotStores.SizedThin} lines said smaller than {BotStores.MeatBatch}, {BotStores.Sated} taken back for the buyer's own ribs), everything else {Percent(Met - MetStock, Failed - FailedStock)} ({Met - MetStock} of {Met - MetStock + Failed - FailedStock}); "
               + $"traders' procurement, outside the share: {ProcOpened} opened, {ProcMet} met, {ProcFailed} failed, {ProcKeptOut} kept out";
    }

    /// <summary>The <c>needs</c> block of <c>/api/state</c>: the same counters as <see cref="Describe"/>, and the oldest lines standing.</summary>
    public static void Web(Utf8JsonWriter w)
    {
        var now = Core.TickCount;
        var (open, past30, standing) = Standing(now);

        w.WritePropertyName("needs");
        w.WriteStartObject();
        w.WriteNumber("windowMin", WindowMs / 60000);
        w.WriteNumber("opened", Opened);
        w.WriteNumber("restored", Restored);
        w.WriteNumber("met", Met);
        w.WriteNumber("metJoint", MetJoint);
        w.WriteNumber("metSelf", MetSelf);
        w.WriteNumber("failed", Failed);
        w.WriteNumber("metLate", MetLate);
        w.WriteNumber("keptOut", KeptOut);
        w.WriteNumber("keptGot", KeptGot);
        w.WriteNumber("keptNotGot", KeptNotGot);
        w.WriteNumber("keptLost", KeptLost);

        if (Share is { } share)
        {
            w.WriteNumber("share", Math.Round(share, 4));
        }
        else
        {
            w.WriteNull("share");
        }

        w.WriteNumber("target", 0.99);
        w.WriteNumber("metStock", MetStock);
        w.WriteNumber("failedStock", FailedStock);
        Ratio(w, "shareStock", MetStock, FailedStock);
        Ratio(w, "shareOther", Met - MetStock, Failed - FailedStock);
        w.WriteNumber("stockHeldBack", BotStores.OutrunBots);
        w.WriteNumber("stockHeldNow", BotStores.HeldNow());
        w.WriteNumber("stockPace", BotMarket.Pace(typeof(RawRibs)));
        w.WriteNumber("stockHour", BotMarket.Brought(typeof(RawRibs)));
        w.WriteNumber("stockRoom", BotStores.StockRoom(typeof(RawRibs)));
        w.WriteNumber("stockAsked", BotMarket.Wanting(typeof(RawRibs)));
        w.WriteNumber("stockSizedToRoom", BotStores.SizedToRoom);
        w.WriteNumber("stockSizedThin", BotStores.SizedThin);
        w.WriteNumber("stockSated", BotStores.Sated);
        w.WriteNumber("stockSatedEarly", BotStores.SatedEarly);
        w.WriteNumber("medianMin", Math.Round(Percentile(_metMinutes, _metCount, 0.5), 1));
        w.WriteNumber("p90Min", Math.Round(Percentile(_metMinutes, _metCount, 0.9), 1));
        w.WriteNumber("open", open);
        w.WriteNumber("openPastHalf", past30);
        w.WriteNumber("failedStanding", standing);

        Counts(w, "failedBy", _failedBy);
        Counts(w, "keptOutBy", _keptOutBy);

        w.WritePropertyName("procurement");
        w.WriteStartObject();
        w.WriteNumber("opened", ProcOpened);
        w.WriteNumber("met", ProcMet);
        w.WriteNumber("failed", ProcFailed);
        w.WriteNumber("keptOut", ProcKeptOut);
        w.WriteEndObject();

        BotMarket.WebReserve(w);

        // The oldest lines still standing, open or failed, at most forty: what the page shows as the board's worst.
        List<BotLine> standingLines = [];
        var wants = BotMarket.Wants;

        for (var i = 0; i < wants.Count; i++)
        {
            var lines = wants[i].Lines;

            for (var j = 0; j < lines.Count; j++)
            {
                if (!lines[j].Complete && lines[j].State is BotLineState.Open or BotLineState.Failed)
                {
                    standingLines.Add(lines[j]);
                }
            }
        }

        standingLines.Sort(static (a, b) => a.OpenedTick.CompareTo(b.OpenedTick));

        w.WritePropertyName("lines");
        w.WriteStartArray();

        for (var i = 0; i < standingLines.Count && i < 40; i++)
        {
            var line = standingLines[i];

            w.WriteStartObject();
            w.WriteString("line", line.Name);
            w.WriteString("buyer", line.Want?.Buyer?.Self?.Name ?? "?");
            w.WriteString("kind", line.Want?.Kind?.Name ?? "?");
            w.WriteNumber("units", line.Units);
            w.WriteNumber("filled", line.Filled);
            w.WriteNumber("offer", line.Want?.Offer ?? 0);
            w.WriteNumber("ageMin", Math.Round((now - line.OpenedTick) / 60000.0, 1));
            w.WriteString("state", line.State.ToString());
            w.WriteNumber("hands", line.Hands);
            w.WriteBoolean("procurement", line.Procurement);

            if (line.Cause != null)
            {
                w.WriteString("cause", line.Cause);
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();
        w.WriteEndObject();
    }

    private static void Ratio(Utf8JsonWriter w, string name, long met, long failed)
    {
        if (met + failed > 0)
        {
            w.WriteNumber(name, Math.Round(met / (double)(met + failed), 4));
        }
        else
        {
            w.WriteNull(name);
        }
    }

    private static void Counts(Utf8JsonWriter w, string name, Dictionary<string, long> counts)
    {
        w.WritePropertyName(name);
        w.WriteStartObject();

        foreach (var (key, count) in counts)
        {
            w.WriteNumber(key, count);
        }

        w.WriteEndObject();
    }

    /// <summary>A new world: nothing counted in the old one is about this one.</summary>
    public static void Forget()
    {
        Opened = 0;
        Topups = 0;
        ProcOpened = 0;
        Restored = 0;
        Met = 0;
        MetOne = 0;
        MetJoint = 0;
        MetSelf = 0;
        Failed = 0;
        MetLate = 0;
        KeptOut = 0;
        ProcMet = 0;
        ProcFailed = 0;
        ProcKeptOut = 0;
        MetStock = 0;
        FailedStock = 0;
        ByCarrier = 0;
        ByTrader = 0;
        AtShop = 0;
        _failedBy.Clear();
        _keptOutBy.Clear();
        _watched.Clear();
        KeptGot = 0;
        KeptNotGot = 0;
        KeptLost = 0;
        _metCount = 0;
        _firstCount = 0;
    }
}

/// <summary>How a want left the board, for its lines. See <see cref="BotLines.Ended"/>.</summary>
public enum BotLineEnd
{
    Met,
    Unpaid,
    Withdrawn,
    Abandoned,
    Orphaned,
    Stopped
}
