using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>What a score was made of. Built for the winner and the runner-up only, and only for the log.</summary>
public readonly struct BotWeigh
{
    public BotWeigh(
        double estimate,
        double nearness,
        double novelty,
        double room,
        double caution,
        double purse,
        double score,
        double stopped = 1.0,
        double revel = 1.0,
        double charter = 1.0,
        double ground = 1.0,
        double calling = 1.0,
        double toll = 1.0,
        double nature = 1.0,
        BotTrait natureBit = BotTrait.None,
        double day = 1.0,
        string dayWhy = null
    )
    {
        Day = day;
        DayWhy = dayWhy;
        Estimate = estimate;
        Nearness = nearness;
        Novelty = novelty;
        Room = room;
        Caution = caution;
        Purse = purse;
        Score = score;
        Stopped = stopped;
        Revel = revel;
        Charter = charter;
        Ground = ground;
        Calling = calling;
        Toll = toll;
        Nature = nature;
        NatureBit = natureBit;
    }

    /// <summary>Gold-equivalent per minute expected, prior and experience together.</summary>
    public double Estimate { get; }

    public double Nearness { get; }

    public double Novelty { get; }

    public double Room { get; }

    public double Caution { get; }

    public double Purse { get; }

    /// <summary>The carrying-ceiling factor. One unless the bot is over its load and the work needs a step.</summary>
    public double Stopped { get; }

    /// <summary>Whatever the watcher declared worth doing this quarter of an hour. One nearly always.</summary>
    public double Revel { get; }

    /// <summary>What this bot's guild asked for, as a multiplier. See <c>BotCharter</c>.</summary>
    public double Charter { get; }

    /// <summary>Whose land the work stands on. One on ground no guild has claimed, which is most of it.</summary>
    public double Ground { get; }

    /// <summary>
    /// Whether this is the bot's own trade, another class's, or anybody's work, as a multiplier on the whole score.
    /// See <c>BotCalling</c>, and for why it is not inside the root.
    /// </summary>
    public double Calling { get; }

    /// <summary>What is left of hunting pay after the toll of the guild whose land it is. See BotToll.Factor.</summary>
    public double Toll { get; }

    /// <summary>The bot's nature on this work, outside the root like the calling (07.10.2026, traits17). See <c>BotTraits.Nature</c>.</summary>
    public double Nature { get; }

    /// <summary>Which of its traits bit, for the sentence.</summary>
    public BotTrait NatureBit { get; }

    /// <summary>The hour of the day on this work, outside the root like the nature (PLAN-life A1). See <c>BotDay.Factor</c>.</summary>
    public double Day { get; }

    /// <summary>Why the hour bit, for the sentence: "the evening (tavern)".</summary>
    public string DayWhy { get; }

    public double Score { get; }

    /// <summary>
    /// What the score was made of, written so it adds up.
    ///
    /// <para>
    /// <b>It read as a product and is not one, and that cost an hour of chasing a defect that was not
    /// there.</b> "8/min = 14 × 0.97 × 0.49 × 0.93 × 0.15 × 1.00" multiplies out to 0.9, not to 8, so the
    /// line looked like a decision whose stated reasons had nothing to do with its stated answer — which is
    /// the exact shape of the worst defects on this shard. The arithmetic was right all along: the factors
    /// are combined as a geometric mean, not a product (see <see cref="BotAppraisal"/> on why), and only the
    /// sentence was wrong. A log that cannot be checked against itself is worse than a shorter one.
    /// </para>
    /// </summary>
    public string Describe()
    {
        // The calling multiplies the score after the root rather than joining the product under it, so the root
        // is recovered by taking it back out; the line then checks against itself: score = estimate x root x calling.
        var bend = Estimate > 0.0 && Calling > 0.0 && Toll > 0.0 && Nature > 0.0 && Day > 0.0 ? Score / (Estimate * Calling * Toll * Nature * Day) : 0.0;

        // <b>And then three more factors were added over a fortnight and none of them was ever printed.</b>
        // The note above is about a line that could not be checked against itself; the same thing happened
        // again quietly, because the carrying ceiling, the watcher's revels and now the guild lands all
        // multiply into the product while the sentence went on naming five things. A bot refusing work at a
        // fiftieth for a full pack read as a bot refusing work for no stated reason at all.
        //
        // Printed only when they bite, which is the compromise: these lines are written thousands of times
        // an hour and all three are one nearly always, so the common sentence is exactly the one Patrick
        // already reads — and the moment any of them is doing something, it says so.
        var extra = "";

        if (Stopped < 1.0 || Revel != 1.0 || Ground != 1.0 || Charter != 1.0)
        {
            extra = $" × load {Stopped:F2} × revel {Revel:F2} × ground {Ground:F2} × charter {Charter:F2}";
        }

        var called = Calling == 1.0 ? "" : $"; × {Calling:F2} for {BotCalling.Word(Calling)}";

        if (Toll != 1.0)
        {
            called += $"; × {Toll:F2} after the toll on this land";
        }

        if (Nature != 1.0)
        {
            called += $"; × {Nature:F2} for its nature ({BotTraits.Words(NatureBit)})";
        }

        if (Day != 1.0)
        {
            called += $"; × {Day:F2} for {DayWhy ?? "the hour"}";
        }

        // The root named as it stands, since BotAppraisal.Root became a dial: the words must not say "fifth" over a seventh.
        return $"{Score:F0}/min = {Estimate:F0} × {bend:F2}, that being the {Nth(BotAppraisal.Root)} root of "
               + $"near {Nearness:F2} × new {Novelty:F2} × room {Room:F2} × safe {Caution:F2} × purse {Purse:F2}{extra}{called}";
    }

    /// <summary>A root's name in words, as the take line has always said it: "fifth" for five.</summary>
    private static string Nth(int root) =>
        root switch
        {
            2 => "square",
            3 => "cube",
            4 => "fourth",
            5 => "fifth",
            6 => "sixth",
            7 => "seventh",
            8 => "eighth",
            9 => "ninth",
            _ => $"{root}th"
        };

    public override string ToString() => Describe();
}

/// <summary>
/// What a piece of work is worth to this bot, right now. One number, in gold-equivalent per minute, so that
/// every want on the shard competes in the same unit.
///
/// <para>
/// <b>The estimate does the work and the considerations only bend it.</b> That is the opposite way round
/// from the first version, where a goal's attraction was a sum of hand-tuned weights and the actual takings
/// were never measured at all — so nobody could say whether mining was better than hunting, including the
/// bots, and the answer was whatever the weights said it was.
/// </para>
///
/// <para>
/// <b>Multiplied, then rooted.</b> Multiplying normalised factors drives every score towards zero as
/// factors are added, so a sixth consideration would quietly make the whole population less decisive; the
/// geometric mean is the standard compensation for it, and it means adding a consideration changes the
/// ordering without changing the scale. Any factor of zero is a veto and stops the sum early.
/// </para>
/// </summary>
public static class BotAppraisal
{
    /// <summary>Times an unpaid deed was let past the earnings veto.</summary>
    public static long Unpaid { get; private set; }

    /// <summary>Every kind of work that has said it is paid nothing on purpose. For the watchers' trade table.</summary>
    private static readonly HashSet<string> _unpaidKinds = [];

    /// <summary>Whether this kind of work is paid nothing on purpose, as far as any appraisal has seen.</summary>
    public static bool IsUnpaid(string kind) => kind != null && _unpaidKinds.Contains(kind);

    /// <summary>Every kind of work whose claim is a posted price. See <see cref="BotDeed.Posted"/>.</summary>
    private static readonly HashSet<string> _postedKinds = [];

    /// <summary>Whether this kind of work claims a posted price, as far as any appraisal has seen.</summary>
    public static bool IsPosted(string kind) => kind != null && _postedKinds.Contains(kind);

    /// <summary>
    /// What the auction reads a trade's claim as: the typed number for unpaid work, the posted price by its share earned,
    /// and anything else corrected towards what the trade has paid. The one reading the overstated alarm is asked about.
    /// </summary>
    public static double Reads(string kind, double claim) =>
        IsUnpaid(kind) ? claim
        : IsPosted(kind) ? BotCommons.Realised(kind, claim)
        : BotCommons.Corrected(kind, claim);

    /// <summary>
    /// Offers marked down because the bot is past its carrying ceiling and the work needs a step.
    ///
    /// Its own bucket: this reading high is not a fault in the work, it is a population that is loading
    /// itself to a standstill, and the number to look at beside it is how often BotUnload had to sell from
    /// where the bot stood.
    ///
    /// <b>It counts a mark-down and never counted a decision, and reading it as one wasted a night.</b> The
    /// number climbed exactly as designed all through 09.09.2026 while the bots it counted went on taking
    /// the very work it was marking, because a fiftieth under a fifth root is 0.46, and 0.46 of a wage a bot
    /// cannot earn still beats all of a wage it can. What the ceiling decides now is an order, and the
    /// order is counted where orders are made: <c>BotWill.Grounded</c> and <c>BotWill.Dislodged</c> are the
    /// pair that say whether anything came of this one.
    /// </summary>
    public static long Stopped { get; private set; }

    /// <summary>
    /// Appraisals refused because the work changes money over a counter and the pack has no room for a coin.
    ///
    /// Counts appraisals, not decisions: a bot in that state is refused once per offer per review until its pack is
    /// emptied, and the number to read beside it is <c>BotUnload.Jammed</c>, the offers of the trip that makes the room. See
    /// <see cref="BotDeed.AtCounter"/>.
    /// </summary>
    public static long Pocketless { get; private set; }

    /// <summary>
    /// How long a bot whose walk gave up without closing a tile is not offered another long walk from the same footing.
    ///
    /// <para>
    /// <b>The walk's failure was filed against the place it was going, and the trouble was the place it began.</b> On
    /// 21.09.2026 (session 21:58, build 176) Fenna of The Crown stood at (1181, 1122), north-west of the river, and was
    /// sent to her guild's claims on the near bank: 75 stakes failed in three hours, 65 of them "having set out 278 off
    /// and got no nearer than 278", one every seventy-two seconds, a tenth of every failure on the shard — 67 of the 77
    /// walks that gave up that session had closed nothing, and 65 were hers. Fifteen different squares, so every note
    /// about a square (<c>BotRefused</c>, and build 177's <c>BotHolder.Missed</c>) was a note about the wrong thing, and
    /// the breaker saw fifteen different reasons at four to the five minutes. The road existed: at 00:15 a supply run
    /// took her by way of a shopkeeper and she was on her guild's counter in under three minutes. The planner, asked
    /// for the square from that one bank, comes back Partial for ever (C1; see BotRoads for why).
    /// </para>
    ///
    /// <para>
    /// So the note is about the footing: where the bot stood, and how long a walk it was. While it still stands within
    /// <see cref="BecalmedStay"/> of that, work at least half as far off is refused; nearer work is untouched, and nearer
    /// work is what moves it — after which the note no longer describes it. A bot with nothing near falls to the stall
    /// watch and is carried home, which is that watch's job and takes minutes rather than a night. Ten minutes, as
    /// <c>BotHolder.MissedMs</c> is: the cost of being wrong is a bot doing something close by for that long.
    /// </para>
    /// </summary>
    public static int BecalmedMs { get; set; } = 600000;

    /// <summary>How far a bot may wander and still be standing where its walk gave up.</summary>
    public static int BecalmedStay { get; set; } = 24;

    /// <summary>Tiles a walk must have closed by the time it gives up not to count as having closed nothing.</summary>
    public static int BecalmedGain { get; set; } = 8;

    /// <summary>
    /// How long a walk has to have been for its giving up to say anything about the footing. Sixty-four: in the first
    /// half hour of build 179 Ulla's 34-tile walk after herbs closed nothing — a fence, not a river — and the note kept
    /// her off a seam 210 tiles away. A short walk that fails is the stall watch's and the pocket record's business.
    /// </summary>
    public static int BecalmedFar { get; set; } = 64;

    /// <summary>
    /// Minutes a bot must have been out of work before a prowl its ledger would refuse is let in at the unpaid floor.
    /// Two: four times the homeward threshold, and half of the stall watch's patience, so a bot walks before it is reported.
    /// </summary>
    public static double RestlessAfter { get; set; } = 2.0;

    /// <summary>Prowls let in at the unpaid floor for a bot that had been out of work for <see cref="RestlessAfter"/>. Counts offers.</summary>
    public static long Restless { get; private set; }

    /// <summary>Offers refused because the bot and the work were on different sides of a dungeon's edge. Counts offers.</summary>
    public static long Undergroundish { get; private set; }

    /// <summary>Appraisals refused for it. Counts offers, as <see cref="Pocketless"/> does; <see cref="Calms"/> counts bots' walks.</summary>
    public static long Becalmed { get; private set; }

    /// <summary>Walks that gave up having closed nothing, which is each time the note was written.</summary>
    public static long Calms { get; private set; }

    /// <summary>
    /// Told by whatever gives a walk up: where the bot stood, how far off it set out, and the nearest it got. Writes the
    /// note only when the walk closed less than <see cref="BecalmedGain"/>. One place, because two watchdogs give walks up —
    /// the one above every undertaking and the miner's own, which is shorter and fires first.
    /// </summary>
    internal static void Becalm(BotResolve resolve, Point3D at, int setOut, int nearest)
    {
        // <b>Closing a little is not arriving.</b> Faron Ashdown, north-west of the river on 24.09.2026, gave up three
        // herb walks in six minutes having closed 32, 49 and 96 tiles of 229, 340 and 259 — each stopped at the water,
        // each still 160 to 290 tiles short, and none wrote the note, because each had closed more than BecalmedGain. A
        // walk that gives up still a long walk short of where it was going is a fact about the footing too.
        if (resolve == null || setOut < BecalmedFar || setOut - nearest >= BecalmedGain && nearest < BecalmedFar)
        {
            return;
        }

        resolve.BecalmedAt = at;
        resolve.BecalmedOff = setOut;
        resolve.BecalmedTick = Core.TickCount;
        resolve.Becalmed = true;
        Calms++;
    }

    /// <summary>
    /// What work that needs a step is worth to a bot that cannot take one. A fiftieth.
    ///
    /// <para>
    /// <b>It does not do what this used to claim, and no number here could.</b> The claim was "enough that
    /// anything doable on the spot wins, never so little that it becomes a refusal", and a factor cannot
    /// meet both demands at once, because the score takes the <see cref="Root"/>th root of the product, the fifth as it stands:
    /// a fiftieth reaches the score as 0.46, which loses to nothing, and the value that would win outright
    /// is the value that vetoes. Lysa the Woodsman took a chop at 229/min over her own unloading on
    /// 09.09.2026 with this factor doing exactly what it was set to do.
    /// </para>
    ///
    /// <para>
    /// The winning is done by rank in <c>BotWill.Auction</c> now. What is left here is the half a factor is
    /// good at and the half that is true: work a bot cannot start is worth less to it, so it is priced
    /// lower and the log line says by how much. It still ranks two walking errands against each other
    /// honestly, and it still holds an unloading in hand against a passing better price.
    /// </para>
    /// </summary>
    public static double StoppedShare { get; set; } = 0.02;

    /// <summary>
    /// A multiplier on one kind of work, set by something outside this assembly, or null.
    ///
    /// <para>
    /// <b>The one seam through which the watcher may move the population without commanding it.</b> Argus
    /// lives in the minds assembly, which depends on this one and must never be depended on in return — so
    /// it cannot be called from here. It fills this in instead, and what arrives is a number in exactly the
    /// same shape as crowding and caution: a factor, competing honestly with them.
    /// </para>
    ///
    /// <para>
    /// Deliberately not a command. A revel that triples the worth of mining does not order anybody to mine;
    /// it makes mining beat what else is on offer for those it beats, and a bot with something better in
    /// front of it goes on doing that. A revel nobody enters is a fact about the price, and the watcher is
    /// told so when it pays nobody.
    /// </para>
    /// </summary>
    public static Func<string, double> Revelry { get; set; }

    /// <summary>
    /// The root taken of the factors' product: a sharpness, not a count.
    ///
    /// <para>
    /// <b>This said "how many factors bend the estimate", and it was five while the product had nine.</b> Nearness,
    /// novelty, room, caution, purse, standing, revel, ground and charter all multiply in, and four or five of them read
    /// one nearly always, so five is close to the number that actually moves — but it was never a count, and at the ninth
    /// root a nearness of 0.5 would reach the score as 0.93 instead of 0.87 and crowding would bite less. A dial since
    /// 15.09.2026, so a sharper or a softer root can be tried off, on and off again on a running shard; the take line names
    /// the root in use.
    /// </para>
    /// </summary>
    public static int Root { get; set; } = 5;

    /// <summary>
    /// How hard a crowd puts a bot off. At four fifths, work that four bots in five are already doing is
    /// worth about a fifth of what it would be worth alone.
    ///
    /// <para>
    /// This is the whole answer to what the first version's population became: 116 traders to 14 fighters.
    /// Nothing was wrong with any individual decision — trade paid, so everybody traded. A want whose value
    /// does not fall as it gets crowded is a want that everybody ends up having, and utility scoring on its
    /// own starves whole roles. It is arithmetic on a fact every bot can see, in the same spirit as the
    /// squads: nobody has to be told anything.
    /// </para>
    /// </summary>
    public static double CrowdBite { get; set; } = 0.8;

    /// <summary>The least a crowded piece of work may be discounted to. Never zero: somebody has to be third.</summary>
    public static double LeastRoom { get; set; } = 0.1;

    /// <summary>
    /// The least an empty purse may discount work that does not pay in coin.
    ///
    /// Never zero, for exactly the reason the crowding floor is not: a bot with no money still has to be
    /// able to walk somewhere, look for a fight, and pick things up off the ground. See the note where this
    /// is applied for the ten minutes it cost to find out.
    /// </summary>
    public static double LeastPurse { get; set; } = 0.1;

    /// <summary>How hard doing the same thing in the same place lately puts a bot off.</summary>
    public static double RepetitionBite { get; set; } = 0.35;

    /// <summary>What is left of a piece of work in a place where it lately went badly.</summary>
    public static double Suspicion { get; set; } = 0.15;

    /// <summary>
    /// What the work in hand is worth beyond its own score.
    ///
    /// <para>
    /// The single most important number for behaviour that looks deliberate, and the direct answer to the
    /// first version's worst habit: it re-chose from scratch every tick, so a bot two steps into a journey to
    /// town would notice a skeleton and go back to hunting, then notice the town again. <b>Any intention
    /// longer than a second was impossible in principle</b> — not unlikely, impossible. A bonus to whatever
    /// is already being done is the standard fix in utility-based systems, and it is cheap: at a quarter, a
    /// new want has to be clearly better rather than marginally better.
    /// </para>
    /// </summary>
    public static double Inertia { get; set; } = 1.25;

    /// <summary>
    /// The score, and what it was made of.
    ///
    /// <para>
    /// Zero means "not this": another map, cannot be afforded, nothing expected, or no money in it for a bot
    /// that needs money. A zero is never a small number here — it is a refusal, and it stops the arithmetic.
    /// </para>
    /// </summary>
    public static double Weigh(IBotWilful bot, BotDeed deed, double share, out BotWeigh weigh) =>
        Weigh(bot, deed, share, out weigh, out _);

    /// <summary>
    /// The same score, and which of the five refusals it was when the answer is nought.
    ///
    /// <para>
    /// <b>Five different facts left this method as the same zero.</b> Another map, cannot afford it, nothing
    /// expected there, and a product flattened to nought are separate faults with separate repairs, and on
    /// 02.09.2026 five bots stood at the Britain shops with 350–540gp apiece while the record said only "1
    /// offered work, none of it scored above nothing". Naming the refusal is the difference between knowing
    /// that and knowing what to fix.
    /// </para>
    /// </summary>
    public static double Weigh(IBotWilful bot, BotDeed deed, double share, out BotWeigh weigh, out string veto) =>
        Weigh(bot, deed, share, out weigh, out veto, false);

    /// <summary>
    /// The same score, for work that may be the work in hand.
    ///
    /// <para>
    /// <b>The price of starting is not asked of work that has started.</b> The Will weighs the work a bot holds again at
    /// every review, through this same method, and the first gate below is "cannot pay to start" — while an inscription's
    /// price is its batch of blanks and a brew's or a sewing's is the lot it buys, fixed for the life of the errand, so
    /// every one of them that has bought its materials is below its own start price. On build 49 at 05:22:38 on
    /// 15.09.2026 Ulwin put down an inscription taken at 328 a minute, fourteen scrolls written, 51gp spent on it and 58 a
    /// minute so far, for an acquire at 7 — "inscribe costs 100gp and it has 66gp". Work put down at "worth 0/min" came
    /// to six to thirteen a session all night before that line could say why, brews and inscriptions among it in most.
    /// Every other refusal still stands for the work in hand.
    /// </para>
    /// </summary>
    public static double Weigh(IBotWilful bot, BotDeed deed, double share, out BotWeigh weigh, out string veto, bool inHand)
    {
        weigh = default;
        veto = null;

        var body = bot?.Self;
        var resolve = bot?.Resolve;

        if (body == null || resolve == null || deed == null)
        {
            veto = "nothing to weigh";

            return 0.0;
        }

        var map = body.Map;

        // Another map is another problem. Nothing here can plan across one, and pretending otherwise is how
        // the first version produced errands to places that were never reached.
        if (map == null || map == Map.Internal || deed.Map != map)
        {
            veto = $"{deed.Kind} is on another map";

            return 0.0;
        }

        // <b>The dungeons are another map that happens to share a map.</b> The block they are cut into has no road
        // to the island (see BotPopulation.Carry): a party is put down by MoveToWorld and put back by Carry, and a
        // delve says its Where is the muster on the island for exactly this reason. Nothing else does. In the session
        // of 21:58 on 21.09.2026 the auction offered members standing in the Orc Caves between two fights the shops
        // of Britain — Joss "taking 1 Spear to Clarinda", eleven of them refused at once, 28 peddle and unload
        // refusals that night, 17 in the first half hour of build 179 (Emrys seven in three seconds) — and offered
        // bots on the island rescues in the caves. The reach ledger knew; the appraisal never asked it, and each
        // refusal is a failure on a ledger that learns a shopkeeper is "no good" from a bot that could not have got
        // to any shopkeeper. Standing work is not asked: it needs no step.
        // <b>Since 29.09.2026 the question is the gates', not the box's.</b> A dungeon with a mouth the bot can walk to is
        // joined to the island, and work there is priced by its distance like any other; a place no gate joins is vetoed
        // as before. Until the gates are read the old answer stands. See BotGates.Joined.
        if (!deed.Standing && !BotGates.Joined(deed.Map ?? body.Map, body.Location, deed.Where))
        {
            Undergroundish++;

            veto = BotDungeon.Under(body.Location)
                ? $"{deed.Kind} is on ground no gate joins to where this bot is, underground"
                : $"{deed.Kind} is on ground no gate joins to this bot's";

            return 0.0;
        }

        // Cannot pay to start. Checked before anything expensive, which is also why it is first. Not asked of the work
        // in hand, which has started - see above.
        // What it has free: money its wants on the board hold is promised (02.10.2026, build 391). See BotYield.Spendable.
        if (!inHand && deed.Outlay > 0 && BotYield.Spendable(body) < deed.Outlay)
        {
            veto = $"{deed.Kind} costs {deed.Outlay}gp and it has {BotYield.Spendable(body)}gp free of {BotYield.Wealth(body)}gp";

            return 0.0;
        }

        // Cannot take the coin. A veto and not a factor, because this is "it cannot happen" rather than "it is a poor
        // idea": the counter bounces the coin on the first beat however well the work would pay. See BotDeed.AtCounter.
        if (deed.AtCounter && !BotYield.Pocket(body))
        {
            Pocketless++;

            veto = $"{deed.Kind} goes through a counter and the pack has no room for a coin, at {body.Backpack?.TotalItems ?? 0} of {body.Backpack?.MaxItems ?? 0} things";

            return 0.0;
        }

        // Rested by the breaker: this bot has failed this kind of work for the same reason too often, too lately. A veto,
        // because it is "not now" said by the bot's own record rather than "a poor idea", and it ends on its own clock.
        // See BotBreaker.
        // Never a defence: a bot set upon always hits back, whatever its rescues of others have come to (build 370, Patrick's
        // «it must hit back in a fight»). See BotWill.DefenceFinished.
        if (deed is not BotRescue { Own: true } && BotBreaker.Resting(body, deed.Kind, out var restedAfter, out var restLeftMs))
        {
            veto = $"{deed.Kind} failed {Math.Max(2, BotBreaker.Failures)} times running for the same reason and rests {restLeftMs / 1000}s more, after \"{restedAfter}\"";

            return 0.0;
        }

        // <b>And the same deed against the same target, failing the moment it was taken (07.10.2026, loops18).</b> Defences too, against
        // that one foe only: the hurried decision asks this same question, so it cannot turn the loop at the clock's pace. Never the
        // work in hand. See BotTreadmill.
        if (!inHand && BotTreadmill.Resting(body, deed, out var losingRest, out var treadLeftMs))
        {
            veto = losingRest
                ? $"{deed.Kind} against {deed.Foe?.Name ?? "it"} was judged lost a moment ago and rests {treadLeftMs / 1000}s more"
                : $"{deed.Kind} failed at once twice running against the same target and rests {treadLeftMs / 1000}s more";

            return 0.0;
        }

        // <b>The ground this bot keeps dying on, and the road to the work (01.10.2026, build 388).</b> On 386 the ten bots that
        // died most carried 44 % of the deaths, one every six to eight minutes each, because the breaker rested a kind of work
        // and the bot took the next kind across the same ground; and 124 of 139 prowl deaths fell on the road, more than a
        // hundred tiles from the prowl's target, because only the goal's square was ever weighed. Two vetoes on the bot's own
        // clocks, not asked of the work in hand, of standing work, of a flight, or of the work that keeps a bot with its
        // company: ground the bot died on twice lately is refused as a goal and as a crossing (BotBreaker.GroundResting) to any
        // work that brings the bot alone; and a road whose record of deaths, or whose patrols, ask more than the work brings
        // (its company's strength, when it raises or is in one: BotDeed.Brings) is refused before anybody walks it. The walk
        // home is never refused: a bot kept from its home never rests, and home is where it rises.
        if (!inHand && !deed.Standing && deed.Where != Point3D.Zero && !BotBreaker.Spares(deed.Kind) && deed.Kind != BotHomeward.Trade)
        {
            var brings = deed.Brings(body);

            if (brings <= BotThreat.Power(body) * BotBreaker.GroundCompany
                && BotBreaker.GroundResting(body, map, body.Location, deed.Where, out var groundWhy, out var groundLeftMs))
            {
                veto = $"{deed.Kind} lies on or across ground this bot has lately died on twice ({groundWhy}), rested {groundLeftMs / 1000}s more";

                return 0.0;
            }

            // <b>The line first, then the road the planner would draw (06.10.2026, road15).</b> The straight line refused 88 % of
            // all far roads on build 400 for squares the walk would have gone round; a road it refuses is now judged by the
            // planner's own road, and the sentence says which of the two refused. See BotDanger.Judge.
            if (BotDanger.Road(body, map, body.Location, deed.Where, brings, out var asked, out var at, out var what, deed.Kind, resolve.Deed != null))
            {
                veto = $"{deed.Kind} is over a road that asks {asked:F0} of strength at ({at.X}, {at.Y}) by {what}, against the {brings:F0} it brings ({BotDanger.RoadHow ?? "on the straight line"})";

                return 0.0;
            }
        }

        // Becalmed: a walk of this bot's lately gave up from about here without closing a tile, and this work is another
        // long walk from the same footing. A veto on its own clock, like the breaker's, and lifted by standing somewhere
        // else. Not asked of the work in hand, which the walk's own watchdog is already judging. See BecalmedMs.
        // <b>Standing work needs no step, and the note is about steps — except when the standing work walks.</b> An unload
        // answers Standing because a stall takes goods wherever the seller is, and it was never asked here; but an unload
        // with a counter in it walks to that counter. Merrick Ashdown, north-west of the river on 24.09.2026, failed 17
        // unloads in 40 minutes (05:45–06:13), one every 72 seconds, "the walk stopped closing 340 tiles short of
        // (1427, 1690) … got no nearer than 340": the note was written every time and never read, because the deed that
        // walked the 340 tiles said it did not walk. An unload listing on the spot has no place (Where is nought) and
        // stays exempt, and so does any standing work for a bot past its ceiling, which lists where it stands instead
        // of walking (BotUnload.Advance) — refusing it that would lock the one cure behind the condition it cures (C8).
        if (!inHand && resolve.Becalmed
            && (!deed.Standing
                || deed.Where != Point3D.Zero && Tiles(body.Location, deed.Where) > BecalmedStay
                && BotLadder.Load(body) <= BotLadder.Ceiling(body)))
        {
            if (Core.TickCount - resolve.BecalmedTick >= BecalmedMs || !Utility.InRange(body.Location, resolve.BecalmedAt, BecalmedStay))
            {
                resolve.Becalmed = false;
            }
            else
            {
                var off = Tiles(body.Location, deed.Where);

                if (off * 2 >= resolve.BecalmedOff && off > BecalmedStay)
                {
                    Becalmed++;

                    veto = $"{deed.Kind} is {off} tiles off, and a walk of {resolve.BecalmedOff} from about here closed nothing {(Core.TickCount - resolve.BecalmedTick) / 1000}s ago";

                    return 0.0;
                }
            }
        }

        // <b>Ground where bots have been dying lately is not walked into for ordinary work.</b> The danger map has always
        // known where that is and was kept for captains; the only caution this appraisal read was the bot's own ledger,
        // which is keyed by trade and records disappointment rather than harm. From 20:40 on 14.09.2026 twenty-eight bots
        // died in twenty minutes in one field north-east of Britain, most of them having put down unloading, selling,
        // cooking, mining or chopping to run, and every offer that took them there read "safe 1.00". Work that goes where
        // the fighting is on purpose and calls from outside are not kept out: they are how such ground is dealt with, and
        // keeping them out would lock the remedy behind the condition it cures. See BotPeril.Lethal.
        // <b>And past a count of deaths, closed to the work that deals with it as well.</b> Companies are the remedy for
        // ground where bots die, until the ground is a plague beast making spawn faster than a company kills it: nine
        // deaths in thirteen minutes on build 47 with lone prowls already kept out. Running away is never kept out. See
        // BotPeril.CloseDeaths.
        // <b>From 26.09.2026 the fight there closes the ground, and the dead no longer do.</b> Patrick's answer once the world
        // was awake round the bots: they "go round only extremely dangerous places, like the swamp, where the hostility of the
        // forces is several times theirs". Both counts of the dead above closed the ground round the guilds' halls to the
        // guilds' own companies and to the restock the entry threshold sends a bot on (BotProvision): 150 restocks, 84
        // harrows and 72 reclaims were the first veto printed in the first forty minutes awake. Running away is never kept
        // out. See BotPeril.Overwhelm.
        if (deed is not BotBolt && BotPeril.Overwhelms(map, deed.Where, body.Location, deed.Brings(body), out var hostile))
        {
            var brought = Math.Max(1.0, deed.Brings(body));

            veto = $"{deed.Kind} lies where the hostile strength is {hostile:F0}, {hostile / brought:F1} times the {brought:F0} going there, against {BotPeril.Overwhelm:F1} that close it to everything but running";

            return 0.0;
        }

        // <b>A red keeps out of guarded towns.</b> The engine's guards kill a murderer on sight there, and a red sent
        // to a counter in Britain is a red sent to die. Running and a sentence are never kept out. See BotOutlaw.
        if (deed is not BotBolt && deed is not BotSentence && BotOutlaw.Keeps(body, map, deed.Where))
        {
            veto = $"{deed.Kind} lies in a guarded town and {body.Name} is red";

            return 0.0;
        }

        // <b>A barred guild keeps out of the town that barred it</b> (07.10.2026, steward20): its steward's decree for a day.
        // Running and a sentence are never kept out. See BotStewardship.Refuses.
        if (deed is not BotBolt && deed is not BotSentence && BotStewardship.Refuses(body, map, deed.Where, out var barred))
        {
            veto = $"{deed.Kind} {barred}";

            return 0.0;
        }

        // <b>The claim is corrected before the place is even considered.</b> deed.Expects is a constant
        // somebody typed — forty-five a minute for a sweep, eight for a prowl — and until now nothing on the
        // shard had ever checked one against what the work actually paid. BotCommons.Corrected is the shard
        // measuring its own assertions; the ledger then does what it always did, which is to say what this
        // bot knows about this ground. Two different corrections, in the right order: what the trade is
        // worth, then what the place is worth.
        // <b>And not corrected at all when the work pays nothing on purpose.</b> The correction measures a
        // claim against what the trade has paid, and an Unpaid deed pays nought by construction — so the
        // measurement is always nought, the correction always drags the claim to its floor, and every
        // rally, quarrel, stake, eviction and hall on the shard was offered at a quarter of what somebody
        // typed. Measured on the night of 13.09.2026: rally claimed 400 and was offered at 175, then 100;
        // stake claimed 200 and was offered at 50, 318 times out of 370. The floor is the same defect as
        // the ledger's below, one step earlier: a number that is a statement of priority is being read as
        // a wager, and the wager is settled in a coin the work never handles. The typed number stands.
        // <b>And a posted price is corrected by how much of such prices is earned, not towards the trade's average.</b> See
        // BotDeed.Posted.
        var claim = deed.Unpaid ? deed.Expects
            : deed.Posted ? BotCommons.Realised(deed.Kind, deed.Expects)
            : BotCommons.Corrected(deed.Kind, deed.Expects);

        if (deed.Unpaid)
        {
            _unpaidKinds.Add(deed.Kind);
        }
        else if (deed.Posted)
        {
            _postedKinds.Add(deed.Kind);
        }

        // <b>And the ledger is not asked about work that is not about money either.</b> The ledger's answer
        // is what this bot has earned on this ground, which is exactly the wrong question to put to an errand
        // whose whole purpose is ground nobody has stood on: the frontier has paid nobody anything by
        // definition, so the answer is nought, every time, for ever.
        //
        // Measured on 08.09.2026, and it is why the Baron would not scout. He took it once — "35/min = 40 ×
        // 0.88" — and from the next review on it read "over scout ... at 0/min" while he walked round the
        // town at six. Both of his offices are unpaid by construction, so the one with the higher claim has
        // to be allowed to say so; judging them on takings ranks them both at nothing and then picks between
        // two nothings on tie-breaks.
        var estimate = deed.Unpaid ? claim : resolve.Ledger.Expect(deed.Kind, map, deed.Where, claim);

        if (estimate <= 0.0)
        {
            // Work that says it is not about money is not judged on money. It still has to be the best thing
            // on offer to be taken - it comes in at the smallest number that is not a refusal, so anything
            // that pays at all beats it - but it can no longer be thrown away for being what it is.
            if (deed.Unpaid)
            {
                Unpaid++;

                estimate = 0.01;
            }
            else if (deed.Kind == BotProwl.Trade && resolve.Urges.BarrenMinutes(Core.TickCount) >= RestlessAfter)
            {
                // <b>The walk to somewhere else is the answer to having nothing to do, and the ledger was
                // refusing it to exactly the bots that had nothing to do.</b> Prowl is the shard's designated
                // answer to an empty review — "scored at almost nothing and the right answer anyway" — and its
                // ledger at a place is what prowls begun there paid, which near a hall whose members have died
                // walking out of it is deeply negative: −38/min at The Needle's hall on the morning of
                // 22.09.2026, against claims of 2 to 20, so six archers stood at the door reading "nothing" and
                // were reported by the stall watch one after another (7 of the session's 13 idle reports, at one
                // hall). Once a bot has been out of work for RestlessAfter, prowl is let in at the unpaid floor:
                // still below anything that pays, still under the danger map's caution and every other factor,
                // and the ledger's opinion of the place is kept for the bot that has a choice.
                Restless++;

                estimate = 0.01;
            }
            else
            {
                veto = $"{deed.Kind} is expected to pay {estimate:F1}/min here, against a claim of {claim:F1}";

                return 0.0;
            }
        }

        // How much of the time this would cost is spent working rather than walking. The reason distance is
        // not a penalty of its own: half an hour of digging is worth a five-minute walk and five minutes of
        // digging is not, and only the ratio says so.
        var work = Math.Max(0.1, deed.Minutes);

        // <b>The road, not the crow.</b> From 26.09.2026 a bot walks round a river by the chart (BotChart) instead of
        // failing at the bank, so work on the far side is work it will now actually go to — and ninety tiles straight
        // from the east bank to The Lantern's hall is some 1,170 steps by road. Priced straight, such work would be
        // taken as near; the road map knows how much further round the far side lies (BotRoads.Farther).
        var road = Tiles(body.Location, deed.Where)
            + (deed.Where == Point3D.Zero ? 0 : BotRoads.Farther(map, body.Location, deed.Where));
        var travel = road * (double)BotWalk.StepDelayMs(false) / 60000.0;
        var nearness = work / (work + travel);

        // Boredom does not compete with work here — it makes repetition wear out faster. A bot with nothing
        // else on will still go back to the same field; a bored one needs it to be worth more.
        var spins = resolve.Ledger.Spins(deed.Kind, map, deed.Where);
        var novelty = 1.0 / (1.0 + spins * RepetitionBite * (1.0 + resolve.Urges.Boredom));

        var room = Math.Clamp(1.0 - share * CrowdBite, LeastRoom, 1.0);

        var caution = resolve.Ledger.Cautious(deed.Kind, map, deed.Where) ? Suspicion : 1.0;

        // A purse of skill does not buy a pickaxe. This is the only place being short of money changes what
        // a bot picks, and when it is not short the factor is exactly one for everything.
        //
        // <b>Floored, and leaving it unfloored stopped a bot doing anything at all.</b> Need is
        // <c>1 - wealth/outlay</c> and reaches exactly one the moment a purse is empty, so every piece of
        // work whose <c>Coin</c> is nought came out at exactly nought — and a nought here is a refusal, as
        // the note at the top of this method says in as many words. Prowling has <c>Coin</c> of nought.
        // Prowling is also this shard's designated answer to having nothing to do: "scored at almost nothing
        // and the right answer anyway — it is a walk to somewhere else". So the bot that most needed to go
        // and find work was the only one forbidden from looking for it. Cedric spent his last coins on
        // scrolls at 22:30 on 25.08.2026 and stood in a field for ten minutes with an empty holding and a
        // contentment of nought, while the summary said seven times that nothing was worth doing.
        //
        // A tenth, exactly as <see cref="LeastRoom"/> is, and for the same reason: being short of money
        // should make paying work far more attractive, not make everything else impossible. The two floors
        // are now the same shape, which is how the crowding factor has always avoided this.
        var purse = Math.Clamp(
            1.0 - resolve.Urges.Need * (1.0 - Math.Clamp(deed.Coin, 0.0, 1.0)),
            LeastPurse,
            1.0
        );

        // <b>A factor, not a veto, and the veto version cost three crafters twenty-seven minutes apiece.</b>
        // Past its carrying ceiling the engine refuses a bot every move, so work that begins with a walk is
        // work it cannot start — but refusing such work outright leaves a bot with nothing at all whenever
        // the one errand it *can* do is unavailable for reasons of its own. Measured within the hour of
        // writing it: Ulric, Wulfric and Roderic stood on the Free rung for 1677, 1640 and 1632 seconds,
        // every offer refused with "mine needs a step, and this bot is carrying 242 of 222", and not one of
        // them ever reached the unloading that would have freed it.
        //
        // <b>And a factor with a floor is not a preference either, which is the correction of 09.09.2026.</b>
        // What stood here promised that "at a fiftieth, anything that can be done standing still wins
        // outright". It never did: the score takes the fifth root of the product, so a fiftieth arrives as
        // 0.46, and half of a wage a bot cannot earn goes on beating all of a wage it can. The middle
        // position was imaginary. Between the veto that made monuments and the multiplier that changed
        // nothing there is no third number to find, because the two failures are one root apart.
        //
        // What this still is, and it is worth keeping: an honest price. Work a bot cannot begin is worth
        // less to it than work it can, the log line says by how much, and two walking errands are still
        // ranked against each other properly. What decides is one rung up, in BotWill.Auction, where a full
        // pack orders the offers instead of pricing them; the floor the note above is about lives on there
        // unchanged, because a rank with nothing above it changes no answer at all. See BotDeed.Standing.
        var stopped = !deed.Standing && BotLadder.Load(body) > BotLadder.Ceiling(body) ? StoppedShare : 1.0;

        if (stopped < 1.0)
        {
            Stopped++;
        }

        // Whatever the watcher has declared worth doing this quarter of an hour. One when it has declared
        // nothing, which is nearly always. See Revelry.
        var revel = Revelry?.Invoke(deed.Kind) ?? 1.0;

        // Whose ground the work stands on. One everywhere no guild has built, which on this island is most
        // of it — so this costs nothing until a hall goes up, and it can never be nought. See BotLand: a
        // claim that refused would be four guilds between them forbidding the population to work.
        var ground = BotLand.Worth(body, map, deed.Where);

        // What this bot's own guild has asked its band to be doing. One for a bot in no guild and one for a
        // guild whose maker has charged it with nothing, which is nearly always. See BotCharter: it is a
        // price and never an order, in exactly the shape the revel and the ground already are.
        var charter = BotCharter.Worth(body, deed);

        var product = nearness * novelty * room * caution * purse * stopped * revel * ground * charter;

        if (product <= 0.0)
        {
            veto = $"{deed.Kind} weighed out at nothing: near {nearness:F2}, new {novelty:F2}, room {room:F2},"
                + $" safe {caution:F2}, purse {purse:F2}, standing {stopped:F2}, revel {revel:F2}, ground {ground:F2},"
                + $" charter {charter:F2}";

            return 0.0;
        }

        // Who the bot is, applied to the whole score rather than folded into the root. See BotCalling.
        var calling = BotCalling.Worth(body, deed);

        // And the toll of the guild whose land it is, applied as a toll is: a share off the pay, outside the root. One on
        // open ground, on its own land and for work that does not brave the ground. See BotToll.
        var toll = BotToll.Factor(body, map, deed);

        // And who the bot is by nature: greedy, cautious, a homebody... One for work none of its traits touches (07.10.2026,
        // traits17). Outside the root as the calling is, so the lean it names is the lean it gives. See BotTraits.Nature.
        var nature = BotTraits.Nature(body, deed, out var natureBit);

        // And the hour of the day against the bot's own timetable: the evening's tavern, the night's fire, the dawn's market
        // (PLAN-life A1). Outside the root as the nature is, held in ×0.5–×2, and never a markdown on a broke bot's paid work —
        // a factor without a floor is a veto. One for most work at most hours. See BotDay.Factor.
        var day = BotDay.Factor(body, deed, out var dayWhy);

        var score = estimate * Math.Pow(product, 1.0 / Math.Max(1, Root)) * calling * toll * nature * day;

        weigh = new BotWeigh(estimate, nearness, novelty, room, caution, purse, score, stopped, revel, charter, ground, calling, toll, nature, natureBit, day, dayWhy);

        return score;
    }

    /// <summary>
    /// Minutes of walking from one place to another at a walking pace, by the same arithmetic <see cref="Weigh"/>
    /// uses for nearness. What a piece of work reckons for its walk when it is taken on.
    /// </summary>
    internal static double Travel(Point3D from, Point3D to) =>
        Tiles(from, to) * (double)BotWalk.StepDelayMs(false) / 60000.0;

    /// <summary>
    /// Distance the way the engine measures adjacency — the larger of the two axes — so it agrees with
    /// <see cref="BotArrival"/> and with the planner about what "one tile away" means.
    /// </summary>
    private static int Tiles(Point3D from, Point3D to)
    {
        var dx = Math.Abs(from.X - to.X);
        var dy = Math.Abs(from.Y - to.Y);

        return dx > dy ? dx : dy;
    }
}
