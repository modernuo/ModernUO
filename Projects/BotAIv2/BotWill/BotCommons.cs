using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What the population as a whole has found out about what pays where.
///
/// <para>
/// <b>Thirty-three bots were learning one island thirty-three times.</b> <see cref="BotLedger"/> is per bot
/// and its own note says why — "that a hundred bots have exhausted a field says nothing about whether this
/// one has seen it" — which is exactly right about <em>seeing</em> and exactly wrong about the field. Two
/// different facts had been folded into one record: "I could not get there from here", which is a fact about
/// the bot and must stay private, and "there is nothing left in that seam", which is a fact about the seam
/// and belongs to everybody. This is the second of those, and only the second.
/// </para>
///
/// <para>
/// <b>It is shaped like <see cref="BotPeril"/> on purpose.</b> Squares rather than points, because a payout
/// at tile resolution is thirty facts each worth nothing; a frequency rather than a total, because last
/// week's gold is a fact about last week; and shard-wide, because the whole value of it is that bot number
/// thirty starts where bot number one finished. That map has worked for danger since the day it was written
/// and there was never a reason for money to be different.
/// </para>
///
/// <para>
/// <b>Own experience always wins, and that ordering is the whole safety of this.</b> A bot that has worked a
/// place knows more about it than the population does — more recent, and about this bot's own skill — so
/// <see cref="BotLedger.Expect"/> reaches here only when it has nothing of its own. What this replaces is not
/// a bot's judgement; it is the hand-written constant a bot would otherwise have used for its first guess.
/// </para>
///
/// <para>
/// <b>Nothing here is a decision.</b> It answers a question the auction asks and takes no view about what
/// anybody should do — the same contract the ledger keeps. A shared board that could compel would be the one
/// thing this project has refused since its first week.
/// </para>
/// </summary>
public static class BotCommons
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCommons));

    /// <summary>
    /// How coarse a patch is. The ledger's own band, deliberately: two records of the same ground keyed
    /// differently would disagree at the edges for ever, and the edges are where every argument happens.
    /// </summary>
    public static int Band => BotLedger.BandSize;

    /// <summary>
    /// How long it takes half of what a place was worth to fade.
    ///
    /// An hour. A seam that paid this morning may be empty now, and a market that paid nothing this morning
    /// may have a buyer standing in it — long enough that a night's work is not thrown away between shifts,
    /// short enough that the board describes today.
    /// </summary>
    public static int HalfLifeMs { get; set; } = 3600000;

    /// <summary>
    /// How much of a fresh outcome replaces what the board held. A fifth: slower than one bot's own ledger,
    /// which moves a third at a time, because this is many bots' evidence and one bad trip should not
    /// overturn it.
    /// </summary>
    public static double Smoothing { get; set; } = 0.2;

    /// <summary>
    /// How many outcomes a patch needs before the board is trusted as much as the caller's own guess.
    ///
    /// The same shape the ledger uses on itself: below this the answer is a blend, so one bot's first bad
    /// afternoon in a place does not become the population's opinion of it.
    /// </summary>
    public static int Confidence { get; set; } = 4;

    /// <summary>Weight the caller's own prior keeps while the board is still learning. See <see cref="Expect"/>.</summary>
    public static double PriorWeight { get; set; } = 2.0;

    /// <summary>
    /// How many outcomes a trade needs before its measured worth outweighs the number in the source.
    ///
    /// Far higher than a patch's, and deliberately: a patch is one corner of an island and a trade is every
    /// bot that has ever done it. Twenty-five is a few minutes of a busy shard for a common trade and a whole
    /// evening for a rare one, which is about right — the rare ones are exactly where one unlucky afternoon
    /// should not rewrite the number.
    /// </summary>
    public static int TradeConfidence { get; set; } = 25;

    /// <summary>
    /// The least a corrected claim may fall to, as a share of what the deed asserts.
    ///
    /// <para>
    /// <b>A correction that reaches nought does not lower a trade, it deletes it — and this shard has written
    /// that lesson down before.</b> A nought in the auction is a refusal, so the first evening this ran, the
    /// measured worth of prowling converged on nought, because prowling pays nothing: it is a walk, and its
    /// whole design is to be "scored at almost nothing and the right answer anyway" when there is nothing
    /// else. Two hundred and five prowls were taken at a corrected claim of nought — the population kept
    /// doing it only because everything else had been flattened too, and the one trade whose price was a
    /// deliberate statement of relative worth had had that statement measured away.
    /// </para>
    ///
    /// <para>
    /// <b>The prior is a designer's judgement about worth and the measurement is an observation about gold,
    /// and they are not the same quantity.</b> Buying a scroll is a cost, a lesson is paid for in skill, a
    /// patrol is worth something nobody is invoiced for. Measurement may say a trade is worth a quarter of
    /// what it claims. It may not say the trade does not exist.
    /// </para>
    ///
    /// <para>
    /// A quarter, the same floor <c>BotAppraisal</c> uses for crowding and for an empty purse, and for the
    /// same reason each time: being short of something should change what a bot prefers, never what it is
    /// allowed to consider.
    /// </para>
    /// </summary>
    public static double LeastShare { get; set; } = 0.25;

    /// <summary>Most patches remembered at once. Beyond this the stalest is forgotten.</summary>
    public static int MostPatches { get; set; } = 4096;

    private sealed class Patch
    {
        public string Kind;

        public Map Map;

        public int X;

        public int Y;

        public double Measured;

        public int Settled;

        public long TouchedTick;

        /// <summary>How many of the outcomes behind this came from a bot with a mind. See <see cref="Told"/>.</summary>
        public int Minded;
    }

    /// <summary>
    /// What one trade turned out to be worth across the whole island, against what it claims to be worth.
    ///
    /// <para>
    /// <b>Every <c>Prior</c> in this project is a number somebody typed.</b> A sweep asserts forty-five a
    /// minute, an unload a hundred and twenty, a prowl eight — nine-tenths of what the auction weighs is
    /// those constants, and not one of them has ever been checked against what the work actually paid. They
    /// were reasonable when written and the shard has changed underneath every one of them.
    /// </para>
    ///
    /// <para>
    /// <b>The three thinking bots found this out on their own, in words.</b> Cedric wrote itself the rule "if
    /// Scribe forecast exceeds 100 gold per minute, reduce expected value to 60% of forecast" — which is this
    /// record, discovered by a language model and applicable only to itself. What is kept here is the same
    /// discovery in a form the other thirty can read.
    /// </para>
    /// </summary>
    private sealed class Trade
    {
        /// <summary>What the deeds of this kind assert, smoothed. Constants differ between deeds of a trade.</summary>
        public double Claimed;

        /// <summary>What they actually came to.</summary>
        public double Measured;

        public int Settled;

        /// <summary>Of those, how many were worked by a bot with a mind. See <see cref="MindWeight"/>.</summary>
        public int Minded;

        public long TouchedTick;
    }

    private static readonly Dictionary<string, Trade> _trades = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<(string Kind, int Map, int X, int Y), Patch> _patches = [];

    /// <summary>
    /// How much more an outcome counts towards correcting a trade when a mind chose it.
    ///
    /// <para>
    /// <b>A judgement, and worth naming as one.</b> An outcome is an outcome and a mind's gold spends the
    /// same — the argument for the weight is not that the measurement is better but that the <em>choice</em>
    /// was: a mind takes work having reviewed how its last forecasts turned out, so its outcomes are less
    /// contaminated by an auction repeatedly picking the same badly-priced thing. Two, which is enough that
    /// three bots move a number thirty bots also feed, and small enough that they cannot invent one.
    /// </para>
    ///
    /// <para>
    /// This is the whole of "the minds teach the instincts", and it is deliberately the whole of it. A mind's
    /// rules are English sentences written by a language model; they are applied where they can be
    /// understood, which is in that mind's own prompt. What crosses to the other thirty is the part that is a
    /// number.
    /// </para>
    /// </summary>
    public static double MindWeight { get; set; } = 2.0;

    /// <summary>Trades whose claim has been corrected by what they paid, and by how much in total.</summary>
    public static long Corrections { get; private set; }

    /// <summary>Outcomes folded in, and how many of them came from a thinking bot.</summary>
    public static long Noted { get; private set; }

    public static long Taught { get; private set; }

    /// <summary>Times a bot with no experience of a place was answered out of the population's.</summary>
    public static long Asked { get; private set; }

    public static int Patches => _patches.Count;

    /// <summary>
    /// A piece of work ended here and this is what it came to per minute.
    ///
    /// <para>
    /// Called beside <see cref="BotLedger.Note"/> and never instead of it: a bot keeps its own record and the
    /// population keeps this one, and the two answer different questions.
    /// </para>
    /// </summary>
    /// <param name="told">Whether the bot behind this outcome has a mind. Only for the report — see Describe.</param>
    public static void Note(string kind, Map map, Point3D where, double perMinute, bool told)
    {
        if (kind == null || map == null || map == Map.Internal)
        {
            return;
        }

        var now = Core.TickCount;
        var key = Key(kind, map, where);

        if (!_patches.TryGetValue(key, out var patch))
        {
            if (_patches.Count >= MostPatches)
            {
                Forget(now);
            }

            patch = new Patch { Kind = kind, Map = map, X = key.X, Y = key.Y, TouchedTick = now };
            _patches[key] = patch;
        }

        // Faded to now before the new outcome is folded in, or a patch nobody has touched since last night
        // would average today's takings against a number that has not been true for hours.
        var held = Faded(patch, now);

        patch.Measured = patch.Settled == 0 ? perMinute : held + (perMinute - held) * Smoothing;
        patch.TouchedTick = now;

        if (patch.Settled < int.MaxValue)
        {
            patch.Settled++;
        }

        Noted++;

        if (told)
        {
            patch.Minded++;
            Taught++;
        }
    }

    /// <summary>
    /// What the population reckons this work is worth here, blended with the caller's own prior.
    ///
    /// <para>
    /// <b>Blended rather than substituted, and the weight is the board's own experience.</b> A patch with one
    /// outcome behind it is a rumour and is treated as one; a patch with a dozen is most of the answer. That
    /// is the same arithmetic the ledger uses on a single bot's history, for the same reason — it is the only
    /// shape that lets a record be useful early without being believed early.
    /// </para>
    /// </summary>
    public static double Expect(string kind, Map map, Point3D where, double prior)
    {
        if (prior <= 0.0 || kind == null || map == null)
        {
            return prior;
        }

        if (!_patches.TryGetValue(Key(kind, map, where), out var patch) || patch.Settled == 0)
        {
            return prior;
        }

        Asked++;

        var settled = Math.Min(patch.Settled, Confidence);

        return (prior * PriorWeight + Faded(patch, Core.TickCount) * settled) / (PriorWeight + settled);
    }

    /// <summary>
    /// A piece of work of this kind ended anywhere on the island, claiming one thing and paying another.
    ///
    /// <para>
    /// Kept apart from the patch record above because it answers a different question: that one is "is this
    /// place worth going to", this one is "is this trade worth what it says it is". A patch fades in an hour
    /// and a trade's true worth does not — a sweep is a sweep tomorrow — so this decays far more slowly and
    /// on far more evidence.
    /// </para>
    /// </summary>
    public static void Claimed(string kind, double claim, double got, bool told)
    {
        if (kind == null || claim <= 0.0)
        {
            return;
        }

        var now = Core.TickCount;

        if (!_trades.TryGetValue(kind, out var trade))
        {
            trade = new Trade { TouchedTick = now };
            _trades[kind] = trade;
        }

        // A mind's outcome counts for more, which is the whole of what those three give the other thirty.
        // See MindWeight — and note that it moves the average, it does not replace it.
        var weight = Smoothing * (told ? MindWeight : 1.0);

        trade.Claimed = trade.Settled == 0 ? claim : trade.Claimed + (claim - trade.Claimed) * Smoothing;
        trade.Measured = trade.Settled == 0 ? got : trade.Measured + (got - trade.Measured) * Math.Min(1.0, weight);
        trade.TouchedTick = now;

        if (trade.Settled < int.MaxValue)
        {
            trade.Settled++;
        }

        if (told)
        {
            trade.Minded++;
        }
    }

    /// <summary>
    /// The claim a deed makes, corrected by what work of that kind has actually paid across the island.
    ///
    /// <para>
    /// <b>Asked before the place is considered at all, because it is a different question.</b>
    /// <see cref="Expect"/> answers "what is this ground worth"; this answers "what is this trade worth", and
    /// the second is what the constant in the source file was always trying to say. A shard that measures its
    /// own assertions needs no one to come back and retune them by hand — which is the only version of
    /// self-teaching that survives contact with a number nobody has checked since it was typed.
    /// </para>
    ///
    /// <para>
    /// Weighted by evidence, exactly as everything else here is: a trade worked twice keeps almost all of its
    /// claim, and one worked a hundred times is mostly what it paid. And floored at nothing — a trade the
    /// shard has found worthless still scores worthless rather than negative, because a negative here would
    /// be a veto, and this project has paid for a multiplier without a floor before.
    /// </para>
    /// </summary>
    public static double Corrected(string kind, double claim)
    {
        if (claim <= 0.0 || kind == null || !_trades.TryGetValue(kind, out var trade) || trade.Settled == 0)
        {
            return claim;
        }

        Corrections++;

        var settled = Math.Min(trade.Settled, TradeConfidence);
        var corrected = (claim * PriorWeight + trade.Measured * settled) / (PriorWeight + settled);

        // Floored against the claim itself, never against nought. See LeastShare.
        return Math.Max(claim * LeastShare, corrected);
    }

    /// <summary>
    /// A posted claim corrected by the share of such claims that work of its kind has actually earned. See
    /// <see cref="BotDeed.Posted"/>.
    ///
    /// <para>
    /// The same weighing by evidence as <see cref="Corrected"/>, applied to a ratio instead of a level: a trade's claims and
    /// takings are both smoothed averages of the same outcomes, so their quotient is how much of what it claims it gets. A
    /// trade that earns everything it posts keeps every claim whole whatever the claims are; one whose errands are let go
    /// half the time gets half. Capped at twice the claim, so loot picked up on the way cannot talk an errand into being
    /// worth more than double its reward, and floored like the other at <see cref="LeastShare"/>.
    /// </para>
    /// </summary>
    public static double Realised(string kind, double claim)
    {
        if (claim <= 0.0 || kind == null || !_trades.TryGetValue(kind, out var trade) || trade.Settled == 0 || trade.Claimed <= 0.0)
        {
            return claim;
        }

        Realisations++;

        var settled = Math.Min(trade.Settled, TradeConfidence);
        var share = Math.Clamp(trade.Measured / trade.Claimed, 0.0, 2.0);
        var corrected = claim * (PriorWeight + share * settled) / (PriorWeight + settled);

        return Math.Max(claim * LeastShare, corrected);
    }

    /// <summary>Posted claims corrected by their share earned. See <see cref="Realised"/>.</summary>
    public static long Realisations { get; private set; }

    /// <summary>
    /// What every trade claims against what it pays, worst overstatement first. For the fifth tab.
    /// </summary>
    public static List<(string Kind, double Claimed, double Measured, int Settled, int Minded)> Gaps(int most)
    {
        List<(string Kind, double Claimed, double Measured, int Settled, int Minded)> found = [];

        foreach (var (kind, trade) in _trades)
        {
            found.Add((kind, trade.Claimed, trade.Measured, trade.Settled, trade.Minded));
        }

        // By how far the claim overstates, because an overstatement is what sends the whole population at
        // work that pays nothing, and an understatement only means somebody is pleasantly surprised.
        found.Sort((left, right) => (right.Claimed - right.Measured).CompareTo(left.Claimed - left.Measured));

        if (most > 0 && found.Count > most)
        {
            found.RemoveRange(most, found.Count - most);
        }

        return found;
    }

    /// <summary>
    /// The richest ore anybody has taken out of a patch, by the engine's own ordering of metals.
    ///
    /// <para>
    /// <b>A vein's nominal requirement and what actually comes out of the hillside are two different
    /// facts.</b> The seam list already carries the first and already prefers a harder vein to an easier one
    /// — that much has worked for a while. What nobody wrote down was the second: which corners of the map
    /// have been paying in bronze and which in iron. Every miner rediscovered it alone, forgot it on the next
    /// restart, and the mountains kept their secret.
    /// </para>
    ///
    /// <para>
    /// <b>Ranked by <c>CraftResource</c>, which is the engine's list in order.</b> Iron is nought and
    /// valorite is the last of them, so the enum's own value is the ranking and there is no table here to
    /// disagree with the one in the world.
    /// </para>
    /// </summary>
    private static readonly Dictionary<(int Map, int X, int Y), (int Rank, int Loads)> _seams = [];

    /// <summary>Somebody took ore out of the ground here, and this is the best of it.</summary>
    public static void Dug(Map map, Point3D where, CraftResource resource)
    {
        if (map == null || map == Map.Internal)
        {
            return;
        }

        var rank = (int)resource;
        var key = (map.MapID, where.X / Band, where.Y / Band);

        if (_seams.TryGetValue(key, out var held))
        {
            _seams[key] = (Math.Max(held.Rank, rank), held.Loads + 1);

            return;
        }

        _seams[key] = (rank, 1);
        Loads++;
    }

    /// <summary>How rich this ground has been, or nought where nobody has dug. Iron reads nought too.</summary>
    public static int Richest(Map map, Point3D where) =>
        map != null && _seams.TryGetValue((map.MapID, where.X / Band, where.Y / Band), out var held) ? held.Rank : 0;

    /// <summary>Patches anybody has dug, for the report.</summary>
    public static long Loads { get; private set; }

    /// <summary>What the board holds about one patch, or nought. For the report and for nothing else.</summary>
    public static double Reading(string kind, Map map, Point3D where) =>
        map != null && kind != null && _patches.TryGetValue(Key(kind, map, where), out var patch)
            ? Faded(patch, Core.TickCount)
            : 0.0;

    /// <summary>
    /// The best-paying patches the population knows about, richest first.
    ///
    /// Built fresh rather than kept sorted: it is asked when somebody opens a window, and keeping an order
    /// that nothing reads between times is bookkeeping nobody is paid for.
    /// </summary>
    public static List<(string Kind, Map Map, Point3D Where, double PerMinute, int Settled, int Minded)> Best(int most)
    {
        var now = Core.TickCount;

        List<(string Kind, Map Map, Point3D Where, double PerMinute, int Settled, int Minded)> found = [];

        foreach (var patch in _patches.Values)
        {
            var worth = Faded(patch, now);

            if (worth <= 0.0)
            {
                continue;
            }

            found.Add((patch.Kind, patch.Map, Middle(patch), worth, patch.Settled, patch.Minded));
        }

        found.Sort((left, right) => right.PerMinute.CompareTo(left.PerMinute));

        if (most > 0 && found.Count > most)
        {
            found.RemoveRange(most, found.Count - most);
        }

        return found;
    }

    /// <summary>
    /// The best-paying patch the population knows of for one trade, within reach of a place, or nothing.
    ///
    /// <para>
    /// <b>This exists because a sampled candidate almost never lands on the one patch that matters.</b>
    /// <c>BotHunter.Hunting</c> draws eight points at random out of a box five hundred tiles across and
    /// scores them, and the scoring is right — a bot's own ledger first, the population's memory behind it —
    /// but scoring can only rank what was offered, and eight darts thrown at a quarter of a million tiles
    /// will miss a graveyard almost every time. The file already makes this argument about danger and acts
    /// on it: the worst peril square is put on the list <em>by name</em> because "the place most likely to
    /// have a fight in it has to be put on the list by name or it will usually not be on the list at all".
    /// Where the fighting has actually paid deserves the same, and nothing was asking.
    /// </para>
    ///
    /// <para>
    /// The height is not settled here. What comes back is a patch's middle, which is arithmetic, and
    /// arithmetic has no height — the caller settles it on the ground the way <c>BotHunter.Noisy</c> already
    /// does. Returning an invented nought from here would be the fault that put <c>(x, y, 0)</c> across the
    /// whole peril map, wearing a different hat.
    /// </para>
    /// </summary>
    public static Point3D Richest(string kind, Map map, Point3D from, int within)
    {
        if (kind == null || map == null || map == Map.Internal)
        {
            return Point3D.Zero;
        }

        var now = Core.TickCount;
        var best = Point3D.Zero;
        var bestWorth = 0.0;

        foreach (var patch in _patches.Values)
        {
            if (patch.Map != map || !string.Equals(patch.Kind, kind, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var worth = Faded(patch, now);

            if (worth <= bestWorth)
            {
                continue;
            }

            var middle = Middle(patch);

            // The larger of the two axes, which is how everything else on this shard measures a distance.
            if (Math.Max(Math.Abs(middle.X - from.X), Math.Abs(middle.Y - from.Y)) > within)
            {
                continue;
            }

            bestWorth = worth;
            best = middle;
        }

        return best;
    }

    private static double Faded(Patch patch, long now)
    {
        var since = now - patch.TouchedTick;

        if (since <= 0 || patch.Measured <= 0.0)
        {
            return patch.Measured;
        }

        return patch.Measured * Math.Pow(0.5, since / (double)HalfLifeMs);
    }

    /// <summary>Room is made by forgetting the stalest, which is the one least likely to still be true.</summary>
    private static void Forget(long now)
    {
        (string Kind, int Map, int X, int Y) stalest = default;
        var oldest = long.MinValue;
        var found = false;

        foreach (var (key, patch) in _patches)
        {
            var idle = now - patch.TouchedTick;

            if (idle <= oldest)
            {
                continue;
            }

            oldest = idle;
            stalest = key;
            found = true;
        }

        if (found)
        {
            _patches.Remove(stalest);
        }
    }

    private static (string Kind, int Map, int X, int Y) Key(string kind, Map map, Point3D where) =>
        (kind, map?.MapID ?? -1, where.X / Band, where.Y / Band);

    private static Point3D Middle(Patch patch) => new(patch.X * Band + Band / 2, patch.Y * Band + Band / 2, 0);

    /// <summary>
    /// Everything on the board, for <see cref="BotCommonsStore"/>: each record with its age rather than its tick, so
    /// the next process can fade it from where it was.
    /// </summary>
    internal static void Save(IGenericWriter writer)
    {
        var now = Core.TickCount;

        writer.WriteEncodedInt(_patches.Count);

        foreach (var (key, patch) in _patches)
        {
            writer.Write(key.Kind);
            writer.WriteEncodedInt(key.Map);
            writer.WriteEncodedInt(key.X);
            writer.WriteEncodedInt(key.Y);
            writer.Write(patch.Measured);
            writer.WriteEncodedInt(patch.Settled);
            writer.WriteEncodedInt(patch.Minded);
            writer.Write(Math.Max(0L, now - patch.TouchedTick));
        }

        writer.WriteEncodedInt(_trades.Count);

        foreach (var (kind, trade) in _trades)
        {
            writer.Write(kind);
            writer.Write(trade.Claimed);
            writer.Write(trade.Measured);
            writer.WriteEncodedInt(trade.Settled);
            writer.WriteEncodedInt(trade.Minded);
            writer.Write(Math.Max(0L, now - trade.TouchedTick));
        }

        writer.WriteEncodedInt(_seams.Count);

        foreach (var (key, seam) in _seams)
        {
            writer.WriteEncodedInt(key.Map);
            writer.WriteEncodedInt(key.X);
            writer.WriteEncodedInt(key.Y);
            writer.WriteEncodedInt(seam.Rank);
            writer.WriteEncodedInt(seam.Loads);
        }
    }

    /// <summary>The board read back, each record touched "that long ago". Returns what was read, for the log.</summary>
    internal static (int Patches, int Trades, int Seams) Load(IGenericReader reader)
    {
        var now = Core.TickCount;
        var maps = Map.Maps;

        var patches = reader.ReadEncodedInt();

        for (var i = 0; i < patches; i++)
        {
            var kind = reader.ReadString();
            var facet = reader.ReadEncodedInt();
            var x = reader.ReadEncodedInt();
            var y = reader.ReadEncodedInt();
            var measured = reader.ReadDouble();
            var settled = reader.ReadEncodedInt();
            var minded = reader.ReadEncodedInt();
            var age = reader.ReadLong();
            var map = maps is { Length: > 0 } && facet >= 0 && facet < maps.Length ? maps[facet] : null;

            if (kind == null || map == null || map == Map.Internal || _patches.Count >= MostPatches)
            {
                continue;
            }

            _patches[(kind, facet, x, y)] = new Patch
            {
                Kind = kind,
                Map = map,
                X = x,
                Y = y,
                Measured = measured,
                Settled = settled,
                Minded = minded,
                TouchedTick = now - Math.Max(0L, age)
            };
        }

        var trades = reader.ReadEncodedInt();

        for (var i = 0; i < trades; i++)
        {
            var kind = reader.ReadString();
            var claimed = reader.ReadDouble();
            var measured = reader.ReadDouble();
            var settled = reader.ReadEncodedInt();
            var minded = reader.ReadEncodedInt();
            var age = reader.ReadLong();

            if (kind == null)
            {
                continue;
            }

            _trades[kind] = new Trade
            {
                Claimed = claimed,
                Measured = measured,
                Settled = settled,
                Minded = minded,
                TouchedTick = now - Math.Max(0L, age)
            };
        }

        var seams = reader.ReadEncodedInt();

        for (var i = 0; i < seams; i++)
        {
            var facet = reader.ReadEncodedInt();
            var x = reader.ReadEncodedInt();
            var y = reader.ReadEncodedInt();
            var rank = reader.ReadEncodedInt();
            var loads = reader.ReadEncodedInt();

            _seams[(facet, x, y)] = (rank, loads);
        }

        return (patches, trades, seams);
    }

    public static string Describe() =>
        _patches.Count == 0
            ? "the population has not learned anything about anywhere yet"
            : $"{_patches.Count} patches and {_trades.Count} trades known from {Noted} outcomes, {Taught} of them from a bot with a mind; asked {Asked} times by somebody who had never been there and {Corrections} claims corrected by what the work really paid; {_seams.Count} patches dug over";

    private static readonly List<(string Kind, int Map, int X, int Y)> _forgiving = [];

    /// <summary>
    /// Everything the population has learned about one kind of work — every patch and the trade's own correction —
    /// struck out, so the next bot asked judges it on its promise again. For the door.
    ///
    /// <para>
    /// <b>For a memory that was true of the captain and got filed against the work.</b> Two lessons on 20.09.2026
    /// were paid for and taught nothing, because the captains walked off the field for a better price; the outcomes
    /// went in here as drill-in at −65 and −209 a minute, and from then on every bot on the island read "drill-in is
    /// expected to pay −4.7/min here" and nobody went to school again — 1,784 places offered in one session, none
    /// taken. The captain is mended (BotLesson.Steadfast); the memory of what he did is not a fact about the work,
    /// and there was no way to say so without a restart of the world.
    /// </para>
    /// </summary>
    public static int Forgive(string kind)
    {
        if (string.IsNullOrEmpty(kind))
        {
            return 0;
        }

        _forgiving.Clear();

        foreach (var key in _patches.Keys)
        {
            if (string.Equals(key.Kind, kind, StringComparison.OrdinalIgnoreCase))
            {
                _forgiving.Add(key);
            }
        }

        for (var i = 0; i < _forgiving.Count; i++)
        {
            _patches.Remove(_forgiving[i]);
        }

        var patches = _forgiving.Count;

        _forgiving.Clear();

        if (_trades.Remove(kind))
        {
            patches++;
        }

        return patches;
    }

    public static void Forget()
    {
        _patches.Clear();
        _trades.Clear();
        _seams.Clear();
        Loads = 0;
        Corrections = 0;
        Realisations = 0;
        Noted = 0;
        Taught = 0;
        Asked = 0;
    }
}
