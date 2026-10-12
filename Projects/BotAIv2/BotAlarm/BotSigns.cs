using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The rules that decide when the shard should speak up: read once a minute, each one comparing a number
/// the shard already keeps against a threshold, over a window it names.
///
/// <para>
/// <b>Six rules, not sixty, and every one of them was checked against a live shard before its threshold was
/// chosen.</b> A watchdog is the easiest thing in this project to get wrong, because a rule that fires
/// wrongly costs more than no rule at all: a night of finding out that the instrument was the fault. The
/// debugger produced ten false alarms against two real defects in its first day, so the thresholds here sit
/// far from what the running shard actually does — errors were 0.3 a minute when this was written and the
/// alarm is at five; four out of five pieces of work were finishing and the alarm is at one in four.
/// </para>
///
/// <para>
/// <b>Everything is a difference over a window, never a total.</b> The counters in this assembly only ever
/// grow, and reading one as the state of this minute is the single most common way this shard has lied to
/// itself: "848 could not afford armour" was true, and was about the first five minutes of a session that
/// had been running for hours. So each rule keeps an anchor — a value and the moment it was taken — and asks
/// its question of the difference.
/// </para>
///
/// <para>
/// <b>A rule that cannot state its denominator is not written.</b> Errors are counted against the minute
/// they were read in, unfinished work against the work taken on, the stuck against the population, deaths
/// against the population, the silent market against the stalls and wants that were standing at the time.
/// </para>
///
/// <para>
/// <b>The thresholds are dials, so they move without a restart.</b> That is the point of tuning a watchdog
/// on a living shard: the first version of a rule is wrong, the evidence for how wrong it is arrives over
/// the following hour, and stopping the shard to change a number would throw that hour away.
/// </para>
/// </summary>
public static class BotSigns
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSigns));

    /// <summary>How often the rules are read.</summary>
    public static int TickMs { get; set; } = 60000;

    /// <summary>Errors in one look before the channel is told. The shard's own rate was 0.3 a minute.</summary>
    public static int ErrorsAt { get; set; } = 5;

    /// <summary>The window the work rules are judged over.</summary>
    public static int WorkMs { get; set; } = 300000;

    /// <summary>Fewest pieces of work taken on in that window before finishing them is judged at all.</summary>
    public static int WorkLeast { get; set; } = 20;

    /// <summary>The share of work taken that must finish. Four in five were finishing when this was written.</summary>
    /// <summary>
    /// Below this share of work finishing, over the work window, the shard says so.
    ///
    /// <para>
    /// <b>Raised from 0.25 to 0.55 on 08.09.2026, and the old figure was a measurement of a sick shard.</b>
    /// A quarter was chosen when the population was finishing between 9% and 49% of what it took on — which
    /// turned out to be the engine refusing every step of an exhausted mount, not the work being wrong. With
    /// that fixed the same population finishes 72% to 91% window after window, nine windows running, and a
    /// floor at a quarter could not fire again short of the shard stopping outright.
    /// </para>
    ///
    /// <para>
    /// Seventeen points below the worst window actually observed. Close enough that a real collapse trips it
    /// within one window; far enough that the ordinary swing between 72 and 91 does not. It is a threshold on
    /// a noisy ratio, so it wants revisiting whenever the population's shape changes — and the way to set it
    /// is the way this one was set: read nine consecutive windows off a healthy shard first.
    /// </para>
    /// </summary>
    public static double WorkFloor { get; set; } = 0.55;

    /// <summary>The share of the population standing still that is worth waking somebody for.</summary>
    public static double StandShare { get; set; } = 0.3;

    /// <summary>Fewest bots alive before the standing share means anything.</summary>
    public static int StandLeast { get; set; } = 10;

    /// <summary>How long a market with stalls and wants on it may move nothing at all.</summary>
    public static int MarketMs { get; set; } = 600000;

    /// <summary>Deaths in one work window that count as a plague rather than a bad afternoon.</summary>
    public static int DeathsAt { get; set; } = 10;

    /// <summary>How often the channel says that nothing is wrong.</summary>
    public static int AliveMs { get; set; } = 3600000;

    private static long _errorsAt;

    private static long _workAt;

    private static long _workTaken;

    private static long _workFinished;

    private static long _workDeaths;

    private static long _workFailed;

    private static long _workDropped;

    private static long _marketAt;

    private static long _marketSells;

    private static long _marketFills;

    private static long _aliveAt;

    private static long _aliveTaken;

    private static long _aliveFinished;

    private static long _aliveErrors;

    /// <summary>Times the rules have been read this session.</summary>
    public static long Looks { get; private set; }

    /// <summary>Sets every anchor to now, so the first window measures this session and not the last one.</summary>
    public static void Start()
    {
        var now = Core.TickCount;

        _errorsAt = now;
        _workAt = now;
        _marketAt = now;
        _aliveAt = now;

        _workTaken = BotWill.Taken;
        _workFinished = BotWill.Finished;
        _workDeaths = BotWill.Deaths;
        _workFailed = BotWill.Failed;
        _workDropped = BotWill.Dropped;

        _marketSells = Sold();
        _marketFills = BotMarket.Answered;

        _aliveTaken = BotWill.Taken;
        _aliveFinished = BotWill.Finished;
        _aliveErrors = BotTail.Errors;

        Looks = 0;
    }

    /// <summary>
    /// Whether a window is up, with half a tick of slack.
    ///
    /// <b>Without the slack a window equal to the tick fires every other tick.</b> Found the hour this was
    /// written: the heartbeat was dialled to a minute against a one-minute tick and came out two minutes
    /// apart, while the event it wrote still called its window "1m". A period and a sampling rate that are
    /// the same number do not divide cleanly, and the number that lies about it is the one in the report.
    /// </summary>
    private static bool Due(long now, long anchor, int every) => now - anchor >= every - TickMs / 2;

    /// <summary>
    /// The window as it actually was, not as it was meant to be. Every event says how long it looked over,
    /// and a rule that reports its intention instead of its measurement is a rule whose numbers cannot be
    /// compared with each other.
    /// </summary>
    private static string Window(long now, long anchor)
    {
        var seconds = (now - anchor) / 1000;

        return seconds < 90 ? $"{seconds}s" : $"{(seconds + 30) / 60}m";
    }

    /// <summary>
    /// Percentages, said the same way whatever the host's locale is. <c>P0</c> here renders with a
    /// non-breaking space on a Russian machine, and this shard has already lost time to locale-shaped
    /// differences between what a number looks like on the developer's box and everywhere else.
    /// </summary>
    private static int Percent(double share) => (int)Math.Round(share * 100);

    /// <summary>One look at everything. Called from the module's own clock.</summary>
    public static void Look()
    {
        Looks++;

        var now = Core.TickCount;

        Errors();
        Standing();

        if (Due(now, _workAt, WorkMs))
        {
            Work(now);
        }

        if (Due(now, _marketAt, MarketMs))
        {
            Market(now);
        }

        Overstated();

        if (Due(now, _aliveAt, AliveMs))
        {
            Heartbeat(now);
        }
    }

    /// <summary>How far above its payment a trade's claim must stand before it is an alarm: half again.</summary>
    public static double OverstatedBy { get; set; } = 1.5;

    /// <summary>
    /// And by how much, at least, in gold a minute. A ratio alone called a claim of three against a payment of nought an
    /// overstatement ("acquire is read by the auction at 3/min and pays 0/min", build 97, 22:40): true, and a number too
    /// small to steer anything.
    /// </summary>
    public static double OverstatedGap { get; set; } = 5.0;

    /// <summary>The trade the alarm is standing on, or null. Cleared in its own event when the gap closes.</summary>
    private static string _overstated;

    /// <summary>
    /// Claims that are ranks by design, named so the alarm passes them over: flight is reckoned at two thousand a minute
    /// so that it wins every auction, and unloading at a hundred and twenty so that a full pack is carried to the
    /// counter; neither is a promise about coin. A lesson ("drill-in") claims the worth of the skill it teaches and pays
    /// its fee in coin — the board measures coin and goods and prices skill at nothing, so every lesson reads as a loss
    /// (-37/min over 28 at 13:02 on 16.09.2026) and the claim is right. A claim that is a wager on purpose, or that is
    /// paid in a currency the board does not price, goes here with its reason and nowhere else — the alarm's whole value
    /// is that everything not on this list is meant to pay what it says.
    /// </summary>
    // "rescue" since 16.09.2026, evening: its number is a friend's need against a hunt, and the alarm read it at 103
    // claimed against 12 paid an hour after the claim was halved — the promise-and-wager class, not a stale number.
    // "harrow" since 17.09.2026: BotHarrow.Prior is written as a rank ("nothing else the Baron may take comes near it, which
    // is what makes him walk the town only when there is nowhere to go") and is paid in ground made safe, which the board
    // does not price. It was the only alarm standing all day on 17.09 — raised at every boot, 38 read against 25, 21, 17 and
    // 10 paid.
    // "band" since 25.09.2026: its takings are divided at the corpse and land in the members' packs through the squad's
    // share-out, so the band's own line reads empty by construction — 0/min over 2230 outcomes, read at 7 by the floor
    // under Patrick's claim of 30 (16.09.2026); raised at the boot of 25.09. The alarm names one trade at a time, so a claim
    // that can never pass hides every other behind it.
    public static readonly HashSet<string> Ranked = new(StringComparer.OrdinalIgnoreCase) { "flee", "unload", "drill-in", "rescue", "harrow", "band" };

    /// <summary>
    /// A trade whose claim, as the auction reads it, stands more than <see cref="OverstatedBy"/> times what it turned out
    /// to pay, on enough outcomes to be believed.
    ///
    /// <para>
    /// <b>As the auction reads it, from build 95.</b> The alarm compared the number typed in the source, and on enough
    /// outcomes to be believed the island's correction has already all but replaced that number: past
    /// <c>TradeConfidence</c> outcomes the claim keeps two parts in twenty-seven. So the alarm named one trade after
    /// another whose choices it no longer steered — mend at 74 against 20 was read by every auction as 24, sewing by a
    /// mind at 110 against 60 as 64 — and four claims were lowered on 16.09.2026 on its word. What it now names is a claim
    /// the correction cannot bring down, which is the floor (<c>BotCommons.LeastShare</c>) holding a quarter of a number
    /// that pays less than that: the claim still steering the population, which is the defect.
    /// </para>
    /// This is the class of defect the shard has paid for most often — a number in the source that stopped
    /// being true, with the whole population chasing it (opening numbers that seal themselves, two thresholds on one
    /// shelf) — and until 16.09.2026 it could be seen only on the Known tab, which nobody has open at four in the morning.
    /// Read off the same board the auction reads; the worst overstatement is named, and nothing is said until it
    /// changes.
    /// </summary>
    private static void Overstated()
    {
        var gaps = BotCommons.Gaps(0);
        string worst = null;
        var worstClaimed = 0.0;
        var worstMeasured = 0.0;
        var worstSettled = 0;

        for (var i = 0; i < gaps.Count; i++)
        {
            var (kind, claimed, measured, settled, _) = gaps[i];
            var reads = BotAppraisal.Reads(kind, claimed);

            // Unpaid work's claim is a rank, not a promise: flight is reckoned at two thousand a minute so that it wins
            // every auction, and it pays nothing on purpose. Only work that is meant to pay is measured against its claim.
            // <b>And only a claim an auction has read.</b> The first word of the alarm on build 95, 21:28, was "duel is read by
            // the auction at 50/min and pays -4/min": true, and about a number no auction ever reads, because a duel is
            // pressed on a bot by the tournament. So are the patrol's chase, a temptation and a sentence.
            if (settled < BotCommons.TradeConfidence || measured * OverstatedBy >= reads || reads - measured < OverstatedGap || BotAppraisal.IsUnpaid(kind) || Ranked.Contains(kind)
                || !BotWill.Auctioned(kind))
            {
                continue;
            }

            worst = kind;
            worstClaimed = reads;
            worstMeasured = measured;
            worstSettled = settled;

            break;
        }

        if (worst != null)
        {
            _overstated = worst;

            BotAlarm.Raise(
                "overstated",
                $"{worst} is read by the auction at {worstClaimed:F0}/min and pays {worstMeasured:F0}/min over {worstSettled} outcomes: a number in the source the correction cannot bring down",
                (long)worstClaimed,
                (long)worstMeasured,
                "-"
            );

            return;
        }

        if (_overstated != null)
        {
            BotAlarm.Clear("overstated", $"{_overstated}'s claim is back within {OverstatedBy:F1}x of what it pays; no trade overstates now", 0, 0, "-");
            _overstated = null;
        }
    }

    /// <summary>
    /// Errors printed since the last look. Cleared as soon as a quiet minute passes, because a burst that
    /// has stopped is a different thing from one that is still going and the difference is what a person
    /// woken at midnight most wants to know.
    /// </summary>
    private static void Errors()
    {
        var now = Core.TickCount;
        var found = BotTail.Since();
        var window = Window(now, _errorsAt);

        _errorsAt = now;

        if (found >= ErrorsAt)
        {
            BotAlarm.Raise(
                "errors",
                $"{found} errors printed in the last {window}; the newest reads: {BotTail.Worst}."
                + " The log is read through a redirect, so this count can lag the world by a buffer",
                found,
                ErrorsAt,
                window
            );

            return;
        }

        if (found == 0)
        {
            BotAlarm.Clear("errors", $"errors have stopped; none in the last {window}", 0, ErrorsAt, window);
        }
    }

    /// <summary>
    /// How much of the population is standing still, as a share of it. The number is the shard's own count
    /// of stalled bots, not a second opinion derived from positions.
    /// </summary>
    private static void Standing()
    {
        var alive = BotPopulation.Count;

        if (alive < StandLeast)
        {
            return;
        }

        var stuck = BotStall.Stuck;
        var share = (double)stuck / alive;

        if (share >= StandShare)
        {
            BotAlarm.Raise(
                "standing",
                $"{stuck} of {alive} bots are standing still, which is {Percent(share)}% of the population."
                + $" The shard's own worst case right now: {BotStall.Worst ?? "none named"}",
                stuck,
                alive,
                "now"
            );

            return;
        }

        if (share < StandShare / 2)
        {
            BotAlarm.Clear(
                "standing",
                $"the population is moving again: {stuck} of {alive} standing still",
                stuck,
                alive,
                "now"
            );
        }
    }

    /// <summary>
    /// Work taken on against work finished, over the work window, plus deaths in the same window. Judged
    /// only when enough work was taken on to divide by — otherwise a quiet five minutes reads as a broken
    /// shard, which is the shape of false alarm this project has produced most often.
    /// </summary>
    private static void Work(long now)
    {
        var finished = BotWill.Finished - _workFinished;
        var failed = BotWill.Failed - _workFailed;
        var dropped = BotWill.Dropped - _workDropped;
        var deaths = BotWill.Deaths - _workDeaths;
        var alive = BotPopulation.Count;
        var window = Window(now, _workAt);

        _workAt = now;
        _workTaken = BotWill.Taken;
        _workFinished = BotWill.Finished;
        _workDeaths = BotWill.Deaths;
        _workFailed = BotWill.Failed;
        _workDropped = BotWill.Dropped;

        // <b>Judged on the work that ended, which is what every build is measured on.</b> This read finished over taken, so
        // the alarm and the census were two numbers for one fact, and work taken in a window's last minute and still in hand
        // counted against it as though it had failed.
        var ended = finished + failed + dropped;

        // <b>And the loudest loop of the window, named.</b> On 14.09.2026 this alarm could not tell the shard from one bot in
        // a loop — Quill, Kelda and Hale each in turn — and every time the first hour went on finding out which (§3 C5, §5 S5).
        // Asked and emptied every window, so a loop from a quarter of an hour ago does not name this one.
        var loudest = BotBreaker.Loudest(out var who, out var kind, out var why, out var times, out var failures);

        if (ended >= WorkLeast)
        {
            var share = (double)finished / ended;
            var named = loudest
                ? $" The loudest: {who} failed at {kind} {times} of the window's {failures} failures ({Percent((double)times / System.Math.Max(1, failures))}%): {why}"
                : "";

            if (share < WorkFloor)
            {
                BotAlarm.Raise(
                    "work-not-finishing",
                    $"{finished} of {ended} pieces of work that ended in the last {window} finished, which is"
                    + $" {Percent(share)}%; {failed} failed and {dropped} were dropped.{named}",
                    finished,
                    ended,
                    window
                );
            }
            else
            {
                BotAlarm.Clear(
                    "work-not-finishing",
                    $"work is finishing again: {finished} of {ended} in the last {window}",
                    finished,
                    ended,
                    window
                );
            }
        }

        if (deaths >= DeathsAt)
        {
            BotAlarm.Raise(
                "dying",
                $"{deaths} bots died at their work in the last {window}, out of {alive} alive",
                deaths,
                alive,
                window
            );
        }
        else if (deaths == 0)
        {
            BotAlarm.Clear("dying", $"nobody has died in the last {window}", 0, alive, window);
        }
    }

    /// <summary>
    /// A market with shops open, wants said, and nothing moving either way. Both halves are required: an empty market is not a stuck
    /// one, and the difference is the whole of the alarm. Since 01.10.2026 "sold" is goods over a shop's till by the bank or handed
    /// over at a carrier's meeting, and "filled" is goods handed over against a spoken want by either road.
    /// </summary>
    /// <summary>Things sold hand to hand, both roads: over a shop's till by the bank, and at a carrier's meeting.</summary>
    private static long Sold() => BotShopkeep.Units + BotHandover.Units;

    private static void Market(long now)
    {
        var sells = Sold() - _marketSells;
        var fills = BotMarket.Answered - _marketFills;
        var stalls = BotShopkeep.OpenCount;
        var wants = BotMarket.Wants.Count;
        var window = Window(now, _marketAt);

        _marketAt = now;
        _marketSells = Sold();
        _marketFills = BotMarket.Answered;

        if (stalls == 0 || wants == 0)
        {
            return;
        }

        if (sells + fills == 0)
        {
            BotAlarm.Raise(
                "market-quiet",
                $"nothing has been bought or sold in {window}, with {stalls} shops open by the bank and"
                + $" {wants} wants said",
                0,
                stalls + wants,
                window
            );

            return;
        }

        BotAlarm.Clear(
            "market-quiet",
            $"the market is moving: {sells} sales and {fills} fills in {window}",
            sells + fills,
            stalls + wants,
            window
        );
    }

    /// <summary>
    /// The hourly "nothing is wrong", with the numbers it is asserting that about. Without it, a channel
    /// that has quietly stopped writing looks exactly like a shard with nothing to report — and this project
    /// has already lost an evening to a monitor whose filter matched nothing at all.
    /// </summary>
    private static void Heartbeat(long now)
    {
        var taken = BotWill.Taken - _aliveTaken;
        var finished = BotWill.Finished - _aliveFinished;
        var errors = BotTail.Errors - _aliveErrors;
        var window = Window(now, _aliveAt);

        _aliveAt = now;
        _aliveTaken = BotWill.Taken;
        _aliveFinished = BotWill.Finished;
        _aliveErrors = BotTail.Errors;

        BotAlarm.Alive(
            $"{BotPopulation.Count} bots alive, {finished} of {taken} pieces of work finished, {errors} errors,"
            + $" {BotStall.Stuck} standing still, {BotShopkeep.OpenCount} shops open and {BotMarket.Wants.Count} wants."
            + $" Alarms standing: {BotAlarm.Standing}",
            finished,
            taken,
            window
        );
    }

    /// <summary>One line for the boot log and the summaries.</summary>
    public static string Describe() =>
        $"{Looks} looks taken, thresholds: {ErrorsAt} errors a minute, work finishing below {Percent(WorkFloor)}% of"
        + $" {WorkLeast}+ taken in {WorkMs / 60000}m, {Percent(StandShare)}% of the population standing still,"
        + $" nothing traded in {MarketMs / 60000}m, {DeathsAt} deaths in {WorkMs / 60000}m";

    public static void Forget()
    {
        Looks = 0;
    }
}
