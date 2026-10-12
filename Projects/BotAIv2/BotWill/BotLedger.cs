using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// What this bot has learned about what pays. One number per kind of work per patch of ground, measured
/// rather than configured.
///
/// <para>
/// <b>This is the whole of learning in this project, and it is arithmetic.</b> A piece of work finishes, its
/// takings are divided by the minutes it took, and the figure is folded into what was already known about
/// that kind of work in that place. Next time, that is the estimate. No model, no training, nothing to
/// serialise, and it recovers on its own when the world changes — a mined-out patch stops paying and its
/// figure falls with it.
/// </para>
///
/// <para>
/// <b>It is also where punishment lives, and why there is no punishment mechanism.</b> The first version
/// punished by prohibition: a bot judged stuck had its errand cancelled and was barred from trading for five
/// minutes — which, because standing at a counter reads as being stuck, punished it for trading. Here a
/// failure is a low number in the row for that work in that place, so the bot stops choosing it, and tries
/// again later when the row has faded. Nothing is ever forbidden.
/// </para>
///
/// <para>
/// <b>Per bot, not per population</b>, like the first version's record of the squares a bot kept returning
/// to. That a hundred bots have exhausted a field says nothing about whether this one has seen it, and what
/// is being modelled is one bot's own experience.
/// </para>
/// </summary>
public sealed class BotLedger
{
    /// <summary>
    /// How coarse a patch of ground is. Sixty-four tiles: fine enough that a mine and the city beside it are
    /// different places, coarse enough that a bot does not have to learn about every vein separately.
    /// </summary>
    public static int BandSize { get; set; } = 64;

    /// <summary>
    /// How many rows one bot keeps. Bounded because this lives on the bot for the rest of its life and a bot
    /// that has crossed a continent has seen a great many patches of ground; the least recently thought about
    /// go first.
    /// </summary>
    public static int MaxPlaces { get; set; } = 48;

    /// <summary>
    /// How much the proposer's own claim is worth, measured in settlements. At two, one real outcome already
    /// counts for a third of the estimate and five outcomes have all but replaced the claim.
    /// </summary>
    public static double PriorWeight { get; set; } = 2.0;

    /// <summary>
    /// How many settlements a row is allowed to count for. Capped so the claim never falls quite silent: a
    /// vein that has become rich, an order that now pays double, a monster that has moved in — the world can
    /// change under a row, and a row with infinite confidence in itself could never hear about it.
    /// </summary>
    public static int Confidence { get; set; } = 12;

    /// <summary>How much of a fresh outcome replaces what was known. A third: responsive, not jumpy.</summary>
    public static double Smoothing { get; set; } = 0.35;

    /// <summary>
    /// How long it takes for half of "I have done this here rather a lot lately" to wear off. Fifteen
    /// minutes, which is what makes repetition a passing discount rather than a permanent ban.
    /// </summary>
    public static int SpinHalfLifeMs { get; set; } = 900000;

    /// <summary>
    /// How long a place stays under suspicion the <b>first</b> time work there ends badly. Long enough for
    /// whatever it was to move on; short enough that the map does not fill up with ground nobody will go
    /// back to.
    /// </summary>
    public static int CautionMs { get; set; } = 300000;

    /// <summary>
    /// The longest a place may be held under suspicion, however many times it has disappointed.
    ///
    /// <para>
    /// <b>The window doubles with every fresh failure at the same place, and that is the difference between
    /// bad luck and a fact.</b> A flat five minutes says "try again in five minutes" to a mine that happened
    /// to be empty and to a counter that cannot be reached at all — and the second of those is answered by
    /// walking into the same wall twelve times an hour for the life of the shard. One shopkeeper's counter
    /// at (1425, 1688) took 56 attempts in one hour on 26.08.2026 and 76 in a night before that, from every
    /// bot in turn, because each failure reset the clock to exactly the same five minutes.
    /// </para>
    ///
    /// <para>
    /// An hour at the ceiling, and it is still not a ban: suspicion is a discount on the score and not a
    /// refusal, and one piece of work that finishes there clears the whole thing — see <see cref="Worked"/>.
    /// A place really has to keep failing to earn the ceiling.
    /// </para>
    /// </summary>
    public static int MostCautionMs { get; set; } = 3600000;

    private sealed class Tally
    {
        public double Measured;

        public int Settled;

        /// <summary>How many times lately, before fading. See <see cref="Faded"/>.</summary>
        public double Spins;

        public long TouchedTick;

        /// <summary>
        /// Whether this row has ever gone badly. A flag rather than "is the deadline still zero": on some
        /// hosts a tick count is the machine's uptime counter passed straight through, so it starts enormous
        /// and can wrap negative, and zero is a legitimate reading. See <c>dev-docs/tick-counts.md</c>.
        /// </summary>
        public bool Cautioned;

        public long CautiousUntil;

        /// <summary>Failures here since the last one that worked. What makes the window grow.</summary>
        public int Cautions;
    }

    private readonly Dictionary<(string Kind, int Map, int X, int Y), Tally> _tallies = [];

    /// <summary>How many rows are being kept. For the log, and for noticing a bot that has seen everything.</summary>
    public int Places => _tallies.Count;

    /// <summary>
    /// Writes what this bot has learned about what pays where, so that a restart does not make it a novice.
    ///
    /// <para>
    /// <b>The clocks are deliberately left out.</b> <c>TouchedTick</c> and <c>CautiousUntil</c> are
    /// <c>Core.TickCount</c>, which starts again with the process — a saved tick read back is a time that never
    /// comes or one already long past, and either way it is a lie. What is worth keeping is the reading itself:
    /// what a place paid, over how many settled outcomes, and how often it had to be warned about. A caution is a
    /// thing of the last few minutes and is allowed to lapse with the shard.
    /// </para>
    /// </summary>
    public void Save(IGenericWriter writer)
    {
        writer.Write(_tallies.Count);

        foreach (var (key, tally) in _tallies)
        {
            writer.Write(key.Kind ?? "");
            writer.Write(key.Map);
            writer.Write(key.X);
            writer.Write(key.Y);
            writer.Write(tally.Measured);
            writer.Write(tally.Settled);
            writer.Write(tally.Spins);
            writer.Write(tally.Cautions);
        }
    }

    /// <summary>Reads back what <see cref="Save"/> wrote, with the clocks started afresh.</summary>
    public void Load(IGenericReader reader)
    {
        var many = reader.ReadInt();
        var now = Core.TickCount;

        for (var i = 0; i < many; i++)
        {
            var kind = reader.ReadString();
            var map = reader.ReadInt();
            var x = reader.ReadInt();
            var y = reader.ReadInt();

            var tally = new Tally
            {
                Measured = reader.ReadDouble(),
                Settled = reader.ReadInt(),
                Spins = reader.ReadDouble(),
                Cautions = reader.ReadInt(),
                TouchedTick = now
            };

            if (!string.IsNullOrEmpty(kind) && _tallies.Count < MaxPlaces)
            {
                _tallies[(kind, map, x, y)] = tally;
            }
        }
    }

    /// <summary>
    /// What this bot should expect from this work here, in gold-equivalent per minute: the proposer's claim
    /// until there is experience, then increasingly the experience.
    ///
    /// <para>
    /// Unknown ground gets the claim untouched, which is the whole of exploration in this design — a place
    /// never tried is judged on its promise and therefore gets tried once. No randomness is needed for it,
    /// and the absence of randomness is worth something: a population that does the same thing twice from
    /// the same facts can be diagnosed by reading.
    /// </para>
    /// </summary>
    public double Expect(string kind, Map map, Point3D where, double prior)
    {
        if (prior <= 0.0)
        {
            return 0.0;
        }

        if (!_tallies.TryGetValue(Key(kind, map, where), out var tally) || tally.Settled == 0)
        {
            // <b>Nothing of its own, so what the population knows — and this is the one line that turns
            // thirty-three private records into one shard that has learned something.</b> The paragraph above
            // is still true where it says unknown ground is judged on its promise and therefore gets tried
            // once; what has changed is whose promise. A hand-written constant is a guess made by somebody
            // who has never been there, and every bot on the island used to start from it every time, even
            // where twenty of its neighbours had already found out. See BotCommons — it is deliberately
            // reached only here, where this bot has no experience, so a bot's own history always wins.
            return BotCommons.Expect(kind, map, where, prior);
        }

        var settled = Math.Min(tally.Settled, Confidence);

        return (prior * PriorWeight + tally.Measured * settled) / (PriorWeight + settled);
    }

    /// <summary>How much this bot has been doing this here lately, faded by time.</summary>
    public double Spins(string kind, Map map, Point3D where) =>
        _tallies.TryGetValue(Key(kind, map, where), out var tally) ? Faded(tally, Core.TickCount) : 0.0;

    /// <summary>Whether this work here ended badly recently enough to be worth avoiding.</summary>
    public bool Cautious(string kind, Map map, Point3D where) =>
        _tallies.TryGetValue(Key(kind, map, where), out var tally)
        && tally.Cautioned
        && Core.TickCount - tally.CautiousUntil < 0;

    /// <summary>
    /// A piece of work has ended and this is what it came to per minute. Folded in, counted as a repetition,
    /// and the row is marked as thought about.
    /// </summary>
    public void Note(string kind, Map map, Point3D where, double perMinute)
    {
        var now = Core.TickCount;
        var tally = Row(kind, map, where, now);

        tally.Measured = tally.Settled == 0
            ? perMinute
            : tally.Measured + (perMinute - tally.Measured) * Smoothing;

        if (tally.Settled < int.MaxValue)
        {
            tally.Settled++;
        }

        // Faded first, then incremented, or a bot that worked the same patch every ten minutes for an hour
        // would read as having worked it six times in a row.
        tally.Spins = Faded(tally, now) + 1.0;
        tally.TouchedTick = now;

        Forget();
    }

    /// <summary>
    /// This work here ended badly. Treat the place with suspicion for a while — and for longer each time it
    /// happens again without anything having worked there in between. See <see cref="MostCautionMs"/>.
    /// </summary>
    public void Beware(string kind, Map map, Point3D where)
    {
        var now = Core.TickCount;
        var tally = Row(kind, map, where, now);

        if (tally.Cautions < 30)
        {
            tally.Cautions++;
        }

        // Doubling, in milliseconds, without ever overflowing the shift: past thirty failures the ceiling is
        // reached many times over and the arithmetic stops mattering.
        var span = Math.Min((long)CautionMs << Math.Min(tally.Cautions - 1, 20), MostCautionMs);

        tally.Cautioned = true;
        tally.CautiousUntil = now + span;
        tally.TouchedTick = now;

        Forget();
    }

    /// <summary>
    /// This work here finished. Whatever was wrong with the place is over.
    ///
    /// <para>
    /// <b>The other half of a growing suspicion, and without it the growth would be a ratchet.</b> A mine
    /// that is empty this afternoon is full tomorrow, and a doubling window with nothing to reset it would
    /// have the bot avoiding good ground for an hour on the strength of four bad visits last week. One piece
    /// of work that finishes is the only evidence needed, and it is complete evidence.
    /// </para>
    /// </summary>
    public void Worked(string kind, Map map, Point3D where)
    {
        if (!_tallies.TryGetValue(Key(kind, map, where), out var tally))
        {
            return;
        }

        tally.Cautions = 0;
        tally.Cautioned = false;
    }

    private Tally Row(string kind, Map map, Point3D where, long now)
    {
        var key = Key(kind, map, where);

        if (_tallies.TryGetValue(key, out var tally))
        {
            return tally;
        }

        tally = new Tally { TouchedTick = now };
        _tallies[key] = tally;

        return tally;
    }

    private static (string Kind, int Map, int X, int Y) Key(string kind, Map map, Point3D where) =>
        (kind, map?.MapID ?? -1, where.X / BandSize, where.Y / BandSize);

    private static double Faded(Tally tally, long now)
    {
        if (tally.Spins <= 0.0)
        {
            return 0.0;
        }

        var elapsed = now - tally.TouchedTick;

        return elapsed <= 0 ? tally.Spins : tally.Spins * Math.Pow(0.5, elapsed / (double)SpinHalfLifeMs);
    }

    /// <summary>Drops the rows thought about longest ago, down to what one bot may hold.</summary>
    private void Forget()
    {
        while (_tallies.Count > MaxPlaces)
        {
            var oldest = 0L;
            (string Kind, int Map, int X, int Y) worst = default;
            var found = false;

            foreach (var (key, tally) in _tallies)
            {
                // Two stamps are compared by subtracting them, never by <. This shard's hosts can hand the
                // process a tick count that started enormous and has wrapped negative, and a plain
                // comparison of two such values is wrong in exactly the cases nobody can reproduce.
                if (found && tally.TouchedTick - oldest >= 0)
                {
                    continue;
                }

                oldest = tally.TouchedTick;
                worst = key;
                found = true;
            }

            if (!found)
            {
                return;
            }

            _tallies.Remove(worst);
        }
    }

    /// <summary>
    /// The best this bot's own work has paid it a minute, over rows settled at least <paramref name="least"/> times, and the kind of
    /// work; nought and null when none has. The lender's question — "has it a trade that pays" — asked of the bot's own record
    /// rather than of its class (07.10.2026, trades17). A walk of this bot's rows, asked once per borrower look.
    /// </summary>
    public double Best(int least, out string kind)
    {
        kind = null;
        var best = 0.0;

        foreach (var (key, tally) in _tallies)
        {
            if (tally.Settled >= least && tally.Measured > best)
            {
                best = tally.Measured;
                kind = key.Kind;
            }
        }

        return best;
    }

    public override string ToString() => $"{_tallies.Count} places remembered";
}
