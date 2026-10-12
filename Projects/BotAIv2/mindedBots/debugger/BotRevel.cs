using System;
using System.Collections.Generic;
using Server.BotAI.V2;
using Server.Guilds;
using Server.Logging;
using Server.Mobiles;
using Server.Multis;
using Server.Text;

namespace Server.BotAI.Mind;

/// <summary>
/// Something for the population to do that nobody planned: a bounty, a hunt, a contest.
///
/// <para>
/// <b>Ordered by Patrick on 08.09.2026 — the watcher thinks, so let it think of something worth doing.</b>
/// Argus already reads the whole shard every ten minutes and forms an opinion about it. This turns that
/// opinion into an event: it looks at what the population is short of, what it has too much of, who is idle
/// and who is rich, and declares a revel.
/// </para>
///
/// <para>
/// <b>The design rule that makes this possible at all: a revel invents no mechanism.</b> Bots know nothing
/// of events and never will. What a revel does is move two things they already answer to — the price of a
/// kind of work, and the world itself. Declare rock worth triple for twelve minutes and the miners go;
/// put an orc camp on the ridge and the hunters go. Nothing is scheduled, nothing is assigned, and a revel
/// that nobody enters simply pays nobody, which is itself a measurement.
/// </para>
///
/// <para>
/// <b>The prize comes out of a fixed treasury, and that is deliberate.</b> Money minted for a party is
/// still money minted: this shard has already paid once for coin entering the world faster than it left —
/// see the note on shopkeepers' shelves. So the crown has <see cref="Treasury"/> for the session and no
/// more; when it is spent, revels go on being declared and are paid in the only other currency the
/// population has, which is a fight worth having.
/// </para>
/// </summary>
public static class BotRevel
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRevel));

    /// <summary>How often the watcher may think about declaring one.</summary>
    public static int EveryMs { get; set; } = 900000;

    /// <summary>How long a revel runs before it is judged and paid.</summary>
    public static int HoldsMs { get; set; } = 720000;

    /// <summary>
    /// What the crown starts the session holding.
    ///
    /// It is an opening balance now rather than a ceiling: see <see cref="Purse"/> and <see cref="Tax"/>.
    /// </summary>
    public static int Treasury { get; set; } = 6000;

    /// <summary>
    /// What the crown has left to spend, this moment.
    ///
    /// <para>
    /// <b>Patrick's order of 09.09.2026: the watcher is to look for ways to fill its own treasury.</b> It had
    /// six thousand for the session and no way to get any more, so a shard left running all night stopped
    /// being able to pay for anything after the fifth revel and every one after that was a price rise with no
    /// purse behind it.
    /// </para>
    ///
    /// <para>
    /// The way it fills is a tax on the guilds, which mints nothing — see <see cref="Tax"/>. Money goes from
    /// the guilds to the crown and comes back as prizes, so the same coin goes round instead of more of it
    /// arriving. This shard has already paid once for a faucet.
    /// </para>
    /// </summary>
    public static int Purse { get; private set; } = 6000;

    /// <summary>How often the crown collects. A quarter of an hour, like the thinking.</summary>
    public static int TaxMs { get; set; } = 900000;

    /// <summary>
    /// What share of a guild's spare money the crown takes each time.
    ///
    /// Small on purpose. A tax a guild notices is a tax that stops it buying a hall, and the halls are the
    /// thing this population is actually saving for.
    /// </summary>
    public static double TaxShare { get; set; } = 0.06;

    /// <summary>What share of a revel's prize goes to the winning guild rather than the winning bot.</summary>
    public static double BandShare { get; set; } = 0.5;

    /// <summary>Coin the crown has taken in tax this session.</summary>
    public static long Collected { get; private set; }

    /// <summary>Coin the crown gave for the rumours' chests, and coin that came back from chests nobody found (07.10.2026, events17).</summary>
    public static long Lent { get; private set; }

    public static long Repaid { get; private set; }

    /// <summary>Up to <paramref name="gold"/> out of the purse for a rumour's chest; what it gave. See <c>BotRumour</c>.</summary>
    public static int Lend(int gold)
    {
        var given = Math.Clamp(gold, 0, Math.Max(0, Purse));

        Purse -= given;
        Lent += given;

        return given;
    }

    /// <summary>A chest's coin back into the purse. See <c>BotRumour</c>.</summary>
    public static void Repay(int gold)
    {
        if (gold <= 0)
        {
            return;
        }

        Purse += gold;
        Repaid += gold;
    }

    /// <summary>Guilds paid for winning a revel between them.</summary>
    public static long Bands { get; private set; }

    /// <summary>The most one revel may promise.</summary>
    public static int MostPrize { get; set; } = 1200;

    /// <summary>
    /// Pieces of the revel's work, done by everybody who entered, that earn the whole prize; fewer earn that share of it,
    /// and the rest stays in the purse. Nought pays the whole prize for any work at all.
    ///
    /// <para>
    /// <b>The prize is paid after the revel and moves nobody while it runs</b> — a bot is drawn by the trade's ×3, never by a
    /// purse it cannot see — so what it buys is a reward for what was done, and it was paid whole for anything: "The revel
    /// for herbs is won by Orin with 1 of them, and 600gp is paid" at 11:51 on 17.09.2026, a revel Hermes declared in a
    /// trade almost nobody works. Twenty is a revel a dozen bots entered for a few pieces each.
    /// </para>
    /// </summary>
    public static int EnoughForPrize { get; set; } = 20;

    /// <summary>How much dearer the named work is while the revel runs.</summary>
    public static double Bonus { get; set; } = 3.0;

    /// <summary>Revels declared.</summary>
    public static long Declared { get; private set; }

    /// <summary>Revels that ended with somebody having done the work.</summary>
    public static long Won { get; private set; }

    /// <summary>Revels nobody entered.</summary>
    public static long Ignored { get; private set; }

    /// <summary>Coin paid out in prizes.</summary>
    public static long Paid { get; private set; }

    /// <summary>Camps put up for a hunt.</summary>
    public static long Camps { get; private set; }

    /// <summary>What is being rewarded now, or null between revels.</summary>
    public static string Kind { get; private set; }

    /// <summary>What the crown will pay the winner.</summary>
    public static int Prize { get; private set; }

    /// <summary>What Argus said this one was for. For the report and for Patrick.</summary>
    public static string Why { get; private set; }

    /// <summary>What it called out to the island when this one began.</summary>
    public static string Said { get; private set; }

    /// <summary>Where the thing itself is: the camp, the waves' field, or the square it was called in. See <see cref="Venue"/>.</summary>
    public static Point3D Where { get; private set; }

    /// <summary>The place the revel running now, or the last one, was held: chosen by <see cref="BotVenues"/>.</summary>
    public static BotVenues.Venue Venue { get; private set; }

    /// <summary>A place's phrase with its first letter made a capital, for the start of a line.</summary>
    private static string Capital(string text) =>
        string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text[1..];

    /// <summary>
    /// The closest a camp may be pitched to where the population lives.
    ///
    /// Not nought: a camp on top of the bots is not somewhere to go, it is something that happens to them,
    /// and the point of letting the watcher choose a spot is that going there is part of the contest.
    /// </summary>
    public static int NearestCamp { get; set; } = 30;

    /// <summary>
    /// The furthest a camp may be pitched from where the population lives.
    ///
    /// <para>
    /// <b>Bounded because the unbounded version is on record.</b> The first hunt called by hand put its wave
    /// at (1980, 1475), five hundred and forty tiles from home, where no bot would ever have met it. The
    /// answer then was to take the choice away entirely and always use the population's own point; the
    /// answer now is to give the choice back with an edge on it, because a revel that is always in the same
    /// place is not a place at all.
    /// </para>
    ///
    /// <para>
    /// Well inside <c>BotPopulation.Roam</c>, which is what a bot will walk for. A camp the population is
    /// not willing to walk to is the (1980, 1475) camp with a smaller number.
    /// </para>
    /// </summary>
    public static int FurthestCamp { get; set; } = 120;

    /// <summary>Times the watcher named a spot of its own rather than leaving it to the population's point.</summary>
    public static long Placed { get; private set; }

    /// <summary>
    /// Times a named spot had to be pulled back into the ring before it could be used.
    ///
    /// Its own bucket, and the one to read: the watcher choosing badly and the watcher not choosing at all
    /// look identical in <see cref="Camps"/>, and only this says which. Climbing steadily means the ring is
    /// not in the prompt clearly enough, not that the model is bad at maps.
    /// </summary>
    public static long Hauled { get; private set; }

    /// <summary>Which map that is on.</summary>
    public static Map Ground { get; private set; }

    /// <summary>Whether a camp was raised for this one.</summary>
    public static bool Pitched { get; private set; }

    /// <summary>The ones already over, newest first. Kept short: this is a window, not an archive.</summary>
    private static readonly List<string> _past = [];

    private static long _startedTick;

    private static long _thoughtTick;

    private static readonly Dictionary<Serial, int> _tally = [];

    /// <summary>The same score kept by guild, so a revel is a contest between bands as well as between bots.</summary>
    private static readonly Dictionary<string, int> _bands = [];

    private static long _taxedTick;

    private static bool _taxed;

    /// <summary>Whether the standings have been called out for the revel now running.</summary>
    private static bool _halfWay;

    /// <summary>
    /// What every trade's revels have actually come to: how often declared, how many took it up, what it
    /// paid out.
    ///
    /// <para>
    /// <b>Patrick's order of 09.09.2026: the watcher is to follow its own events closely.</b> It was
    /// declaring them and forgetting them — the log had every revel and no way to compare two. This is the
    /// smallest thing that answers "did that work": it goes into the question the model is asked, so the
    /// next choice is made knowing that the last three mining revels drew nobody.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, (int Declared, int Entrants, int Paid)> _ledger = [];

    /// <summary>Whether a revel is running this moment.</summary>
    public static bool Running => Kind != null && Core.TickCount - _startedTick < HoldsMs;

    /// <summary>The revel in force in a sentence, or null. For the live window (02.10.2026).</summary>
    public static string Current() =>
        !Running
            ? null
            : $"a revel for {Kind}: ×{Bonus} for {Math.Max(0, HoldsMs - (Core.TickCount - _startedTick)) / 60000} more minutes, prize {Prize}gp, {Venue?.Phrase ?? "at"} ({Where.X}, {Where.Y}){(Pitched ? ", the camp pitched" : "")}";

    /// <summary>
    /// How much dearer this kind of work is because of the revel, or one when it is not the named kind.
    ///
    /// Read by <c>BotAppraisal</c>, which is the one place a price is decided — so a revel is a multiplier
    /// in exactly the same shape as crowding, novelty and caution, and competes with them honestly.
    /// </summary>
    public static double Worth(string kind) =>
        Running && string.Equals(kind, Kind, StringComparison.OrdinalIgnoreCase) ? Bonus : 1.0;

    /// <summary>Whether enough time has passed to think about declaring one.</summary>
    public static bool Due()
    {
        if (Running || Core.TickCount - _thoughtTick < EveryMs)
        {
            return false;
        }

        _thoughtTick = Core.TickCount;

        return true;
    }

    /// <summary>
    /// Somebody finished a piece of work. Counted only while a revel is running and only for its own kind.
    ///
    /// Called from <c>BotWill</c> on every completion, so it must stay a dictionary write and nothing more.
    /// </summary>
    public static void Did(Mobile bot, string kind)
    {
        if (bot == null || !Running || !string.Equals(kind, Kind, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _tally[bot.Serial] = _tally.TryGetValue(bot.Serial, out var had) ? had + 1 : 1;

        // And the same score under the bot's guild. A revel is two contests at once — one bot against every
        // other, one band against every other — and the second is the one Patrick asked for: an event the
        // guilds themselves take part in, without a war between them.
        if (bot.Guild is Guild guild)
        {
            _bands[guild.Name] = _bands.TryGetValue(guild.Name, out var many) ? many + 1 : 1;
        }
    }

    /// <summary>
    /// The crown collects. Called on the watcher's beat; does nothing but compare two ticks, nearly always.
    ///
    /// <para>
    /// Taken from what a guild can spare rather than from what it has — <c>BotEstate.Tax</c> leaves every
    /// member its keep — so a tax never takes a bot's last coin, and a poor guild pays nothing at all.
    /// </para>
    /// </summary>
    public static void Tax()
    {
        var now = Core.TickCount;

        // <b>The first window is skipped, and that is not tidiness.</b> A deadline field left at nought means
        // "infinitely overdue", so the very first beat of the shard collected a tax — 715gp off five bots at
        // 01:22:05 on 09.09.2026, out of the float they are born with and had not yet earned. Seeded from a
        // real tick instead, which is the same rule this project keeps for every other clock.
        if (!_taxed)
        {
            _taxed = true;
            _taxedTick = now;

            return;
        }

        if (now - _taxedTick < TaxMs)
        {
            return;
        }

        _taxedTick = now;

        var got = 0;

        foreach (var guild in BotGuilds.Standing)
        {
            // What the members can spare, never the chest: the guild's savings for its hall are not the crown's to tax (29.09.2026).
            var spare = BotEstate.Spare(guild);

            if (spare <= 0)
            {
                continue;
            }

            got += BotEstate.Tax(guild, (int)(spare * TaxShare));
        }

        if (got <= 0)
        {
            return;
        }

        Purse += got;
        Collected += got;

        logger.Information(
            "The crown collected {Got}gp from the guilds; its purse stands at {Purse}gp",
            got,
            Purse
        );
    }

    /// <summary>
    /// Declares one. Everything here is the model's answer, bounded by this shard's own rules.
    /// </summary>
    /// <param name="kind">The trade being rewarded, as the ledger spells it.</param>
    /// <param name="prize">What the winner gets, capped at what the crown has left.</param>
    /// <param name="say">What Argus calls out to the island.</param>
    /// <param name="why">Its own reasoning, for the log.</param>
    /// <param name="camp">Whether to put an orc camp up as part of it.</param>
    /// <param name="where">Where that camp goes, or Zero for a place the venues choose.</param>
    /// <param name="exact">The spot is a person's at the door, and is taken as named; the marshal's is tested first (<see cref="BotVenues.Accept"/>).</param>
    public static string Declare(string kind, int prize, string say, string why, bool camp, Point3D where, bool exact = false)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            return "no trade named, so nothing was declared.";
        }

        // <b>One revel at a time, and a declaration over a running one used to wipe it.</b> Declaring set the clock and
        // cleared the tally whatever was running: Hermes declared mining ×3 at 11:05:30 on 17.09.2026 and mining ×3 again
        // at 11:15:42, two minutes before the first would have paid, and every piece of mining done for the first was
        // struck off. The marshal's form no longer offers a revel while one runs; this is the verb's own refusal, for the
        // door as well.
        if (Running)
        {
            return $"a revel of {Kind} is already on for {Math.Max(1, (HoldsMs - (Core.TickCount - _startedTick)) / 60000)} more minutes; nothing was declared.";
        }

        // One that has run its time and waits for the watcher's beat is settled first, so its winner is paid before its
        // tally is cleared.
        if (Kind != null)
        {
            Settle();
        }

        var body = BotVigil.Body;
        var map = body?.Map;

        if (map == null || map == Map.Internal)
        {
            return "the watcher is nowhere, so it can declare nothing.";
        }

        Kind = kind.Trim().ToLowerInvariant();
        Prize = Math.Clamp(prize, 0, Math.Min(MostPrize, Purse));
        Why = why;
        Said = say;
        _startedTick = Core.TickCount;
        _tally.Clear();
        _bands.Clear();
        _halfWay = false;
        Declared++;

        var seen = _ledger.TryGetValue(Kind, out var had) ? had : default;

        _ledger[Kind] = (seen.Declared + 1, seen.Entrants, seen.Paid);

        // <b>Where the bots are, and not where the watcher happens to be standing.</b> Argus follows bots about and can be half
        // the island away: the first hunt called by hand put its wave at (1980, 1475), five hundred and forty tiles from home,
        // where no bot would ever have met it. And bots do not travel to a revel — they only ever see the price of a trade go
        // up — so a revel's waves must stand where hunters already are.
        //
        // <b>And not always the same place (02.10.2026).</b> Until build 389 that meant the population's home, every time: 27 of
        // the 29 hunt revels from the wipe of 01.10.2026 to 11:00 on 02.10.2026 stood every wave at (1440, 1470), and the
        // marshal's two camps at (1356, 1384) and two tiles from it. Patrick: "I always see the debuggers holding feasts and events
        // in one and the same place". The place is chosen now (BotVenues): a town's square for a revel of a trade, the
        // countryside off a bot working in the open for a hunt's waves or a camp — safe, reached, not one of the last few, and
        // drawn by where the bots are. The marshal's own spot for a camp is kept when it passes the same tests.
        Ground = BotPopulation.Home ?? map;
        Pitched = false;

        var field = camp || BotWaves.IsHunt(Kind);
        BotVenues.Venue venue = null;

        if (camp && !exact && !BotVenues.Running)
        {
            // Venues switched off: the camp as before build 390, never on top of home (Ring).
            if (where != Point3D.Zero)
            {
                Placed++;
            }

            venue = BotVenues.Exact(BotVenues.Use.Field, Ground, Ring(BotPopulation.Where, where == Point3D.Zero ? BotPopulation.Where : where));
        }
        else if (camp && where != Point3D.Zero)
        {
            Placed++;
            venue = exact ? BotVenues.Exact(BotVenues.Use.Field, Ground, where) : BotVenues.Accept(Ground, where);

            if (venue == null)
            {
                Hauled++;
            }
        }

        venue ??= BotVenues.Choose(field ? BotVenues.Use.Field : BotVenues.Use.Feast, Ground);
        Venue = venue;
        Where = venue.At;

        var raised = "";

        if (camp)
        {
            if (Footing(Ground, Where, out var pitchAt, out var z))
            {
                var pitch = new OrcCamp();

                pitch.MoveToWorld(new Point3D(pitchAt.X, pitchAt.Y, z), Ground);
                pitch.DecayDelay = TimeSpan.FromMilliseconds(HoldsMs);
                Camps++;

                Where = new Point3D(pitchAt.X, pitchAt.Y, z);
                BotVenues.Moved(venue, Where);
                Pitched = true;

                raised = $" A camp stands at ({pitchAt.X}, {pitchAt.Y}), {venue.Phrase}.";
            }
        }

        // Said in the world channel, by the herald standing at the place, because a revel nobody hears is a price change with
        // extra steps — and because Patrick watches the shard with his own eyes and should be able to see one start, and where.
        BotVenues.Herald(venue, BotVenues.HeraldHoldMs);
        BotVenues.Announce(
            $"{Capital(venue.Phrase)}: {(string.IsNullOrWhiteSpace(say) ? $"a {Kind} revel, worth ×{Bonus} for {HoldsMs / 60000} minutes, {Prize}gp to whoever does most." : say.Trim())}"
        );

        logger.Information(
            "A revel is declared: {Kind} is worth ×{Bonus} for {Minutes} minutes, prize {Prize}gp, held {Venue} ({X},{Y}) — {Why}{Raised}",
            Kind,
            Bonus,
            HoldsMs / 60000,
            Prize,
            venue.Phrase,
            Where.X,
            Where.Y,
            Why,
            raised
        );

        BotDebugLog.Rule();
        BotDebugLog.Write($"REVEL: {Kind} ×{Bonus} for {HoldsMs / 60000} minutes, prize {Prize}gp, {venue.Phrase} ({Where.X}, {Where.Y})");
        BotDebugLog.Block("  because:", Why ?? "no reason given");
        BotDebugLog.Rule();

        return $"declared: {Kind} is worth ×{Bonus} for {HoldsMs / 60000} minutes, prize {Prize}gp, held {venue.Phrase} ({Where.X}, {Where.Y}).{raised}";
    }

    /// <summary>
    /// Judges a revel whose time is up: finds who did most of the named work and pays them.
    ///
    /// <para>
    /// Called on the watcher's own beat. A revel nobody entered pays nobody and says so — that is the
    /// honest outcome and the one worth counting, because it means the price the crown offered was not
    /// enough to move anybody, which is a fact about the shard rather than about the revel.
    /// </para>
    /// </summary>
    public static void Settle()
    {
        if (Kind == null)
        {
            return;
        }

        if (Core.TickCount - _startedTick < HoldsMs)
        {
            Standings();

            return;
        }

        var was = Kind;
        var prize = Prize;
        var band = Band();
        var entered = _tally.Count;

        Kind = null;
        Prize = 0;

        Mobile best = null;
        var bestDid = 0;

        var done = 0;

        foreach (var (serial, did) in _tally)
        {
            done += did;

            if (did <= bestDid)
            {
                continue;
            }

            var who = World.FindMobile(serial);

            if (who is { Deleted: false })
            {
                best = who;
                bestDid = did;
            }
        }

        _tally.Clear();

        if (best == null)
        {
            Ignored++;
            _bands.Clear();
            Remember($"{was}: nobody came, {prize}gp unclaimed");

            logger.Information("The revel for {Kind} ended with nobody having done any", was);
            BotVigil.Body?.Say($"The {was} revel ends. Nobody came.");

            return;
        }

        Won++;

        // In proportion to what the revel moved. See EnoughForPrize.
        if (EnoughForPrize > 0 && done < EnoughForPrize)
        {
            prize = prize * done / EnoughForPrize;
        }

        var was2 = _ledger.TryGetValue(was, out var seen2) ? seen2 : default;

        _ledger[was] = (was2.Declared, was2.Entrants + entered, was2.Paid + prize);

        // The bot's own share first, then the band's. Two prizes out of one purse, because a revel that only
        // ever rewards the best individual teaches a guild nothing about being a guild.
        var single = band == null ? prize : (int)(prize * (1.0 - BandShare));

        if (single > 0 && Banker.Deposit(best, single))
        {
            Paid += single;
            Purse = Math.Max(0, Purse - single);
        }

        if (band != null)
        {
            var pot = prize - single;
            var shared = Share(band, pot);

            if (shared > 0)
            {
                Bands++;
                Paid += shared;
                Purse = Math.Max(0, Purse - shared);

                BotVigil.Body?.Say($"{band.Name} did most of it between them. {shared}gp split among them.");
            }
        }

        _bands.Clear();

        logger.Information(
            "The revel for {Kind} is won by {Name} with {Did} of them, {Done} done in all, and {Prize}gp is paid; {Left}gp left in the treasury",
            was,
            best.Name,
            bestDid,
            done,
            prize,
            Math.Max(0, Treasury - (int)Paid)
        );

        BotVigil.Body?.Say($"{best.Name} wins the {was} revel with {bestDid}. {prize}gp to the winner.");

        Remember($"{was}: {best.Name} won with {bestDid}, {prize}gp paid");
    }

    /// <summary>
    /// Half way through a revel, the watcher calls out who is winning — by name, and by guild.
    ///
    /// <para>
    /// Patrick's order of 09.09.2026: the watcher deals with the guild leaders. This is where it does it in
    /// the world rather than in a log — the shard is watched from a client, and a contest nobody is told the
    /// state of is a contest nobody is in.
    /// </para>
    /// </summary>
    private static void Standings()
    {
        if (_halfWay || Core.TickCount - _startedTick < HoldsMs / 2)
        {
            return;
        }

        _halfWay = true;

        var band = Band();

        if (band == null)
        {
            BotVigil.Body?.Say($"Half the {Kind} revel gone and not one of you has taken it up.");

            return;
        }

        var leader = band.Leader?.Name ?? "somebody";

        BotVigil.Body?.Say($"{band.Name} leads the {Kind} revel. {leader}, hold it.");
    }

    /// <summary>
    /// What the revels have taught, in one line, for the question the model is asked.
    ///
    /// A trade with declarations and no entrants is the row that matters: it is a price the population
    /// walked past, and declaring it a sixth time is the watcher not reading its own results.
    /// </summary>
    public static string Ledger()
    {
        if (_ledger.Count == 0)
        {
            return "none have been held yet";
        }

        var say = Server.Text.ValueStringBuilder.Create(256);

        try
        {
            foreach (var (kind, (declared, entrants, paid) ) in _ledger)
            {
                if (say.Length > 0)
                {
                    say.Append("; ");
                }

                say.Append(kind);
                say.Append(": ");
                say.Append(declared.ToString());
                say.Append(" held, ");
                say.Append(entrants.ToString());
                say.Append(" took it up, ");
                say.Append(paid.ToString());
                say.Append("gp paid");
            }

            return say.ToString();
        }
        finally
        {
            say.Dispose();
        }
    }

    /// <summary>Who leads each guild, for the herald's own use.</summary>
    public static string Leaders()
    {
        var say = Server.Text.ValueStringBuilder.Create(192);

        try
        {
            foreach (var guild in BotGuilds.Standing)
            {
                if (say.Length > 0)
                {
                    say.Append(", ");
                }

                say.Append(guild.Name);
                say.Append(" under ");
                say.Append(guild.Leader?.Name ?? "nobody");
            }

            return say.Length == 0 ? "there are no guilds" : say.ToString();
        }
        finally
        {
            say.Dispose();
        }
    }

    /// <summary>Which guild did most of the work, or null if none of them did any.</summary>
    private static Guild Band()
    {
        Guild best = null;
        var bestDid = 0;

        foreach (var (name, did) in _bands)
        {
            if (did <= bestDid)
            {
                continue;
            }

            var guild = BotGuilds.Find(name);

            if (guild != null)
            {
                best = guild;
                bestDid = did;
            }
        }

        return best;
    }

    /// <summary>Splits a pot between everybody in a guild, and says how much actually left the purse.</summary>
    private static int Share(Guild guild, int pot)
    {
        if (guild?.Members == null || pot <= 0)
        {
            return 0;
        }

        var many = 0;

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is BotMobile { Deleted: false })
            {
                many++;
            }
        }

        if (many == 0)
        {
            return 0;
        }

        var each = Math.Max(1, pot / many);
        var given = 0;

        for (var i = 0; i < guild.Members.Count && given + each <= pot; i++)
        {
            if (guild.Members[i] is BotMobile { Deleted: false } member && Banker.Deposit(member, each))
            {
                given += each;
            }
        }

        return given;
    }

    /// <summary>Files one finished revel where the dashboard can show it. Newest first, five at most.</summary>
    private static void Remember(string line)
    {
        _past.Insert(0, line);

        while (_past.Count > 5)
        {
            _past.RemoveAt(_past.Count - 1);
        }
    }

    /// <summary>
    /// The notice the dashboard reads, taken as a copy.
    ///
    /// <para>
    /// Everything here is already kept for the log or for the arithmetic; the one thing this adds is who is
    /// currently winning, which is worked out on demand because it is a question nobody asks except a person
    /// looking at the window.
    /// </para>
    /// </summary>
    public static BotNotice Notice()
    {
        var body = BotVigil.Body;
        var running = Running;

        Mobile best = null;
        var bestDid = 0;

        if (running)
        {
            foreach (var (serial, did) in _tally)
            {
                if (did <= bestDid)
                {
                    continue;
                }

                var who = World.FindMobile(serial);

                if (who is { Deleted: false })
                {
                    best = who;
                    bestDid = did;
                }
            }
        }

        return new BotNotice
        {
            Running = running,
            Kind = Kind,
            Prize = Prize,
            Bonus = Bonus,
            Said = Said,
            Why = Why,
            EndsIn = running ? HoldsMs - (Core.TickCount - _startedTick) : 0,
            NextIn = Math.Max(0, EveryMs - (Core.TickCount - _thoughtTick)),
            Entered = _tally.Count,
            Leading = best?.Name,
            LeadingDid = bestDid,
            Map = Ground,
            Where = Where,
            Camp = Pitched,
            WatcherMap = body?.Map,
            Watcher = body?.Location ?? Point3D.Zero,
            WatcherName = body?.Name,
            Declared = Declared,
            Won = Won,
            Ignored = Ignored,
            Paid = Paid,
            Treasury = Treasury,
            Purse = Purse,
            Collected = Collected,
            Bands = Bands,
            Past = _past.ToArray(),
            Wave = BotWaves.Wave,
            Standing = BotWaves.Standing,
            Waves = BotWaves.Describe(),
            Ledger = Ledger()
        };
    }

    /// <summary>What the population has been put through, for the summary.</summary>
    public static string Describe() =>
        Declared == 0
            ? $"no revels have been declared; the crown holds {Purse}gp"
            : $"{Declared} revels declared, {Won} won and {Ignored} ignored, {Bands} of them by a whole band; "
            + $"{Paid}gp paid out and {Collected}gp taken in tax, the crown holding {Purse}gp; "
            + $"{Camps} camps raised for them ({Placed} on a spot the watcher chose, {Hauled} of those moved to a fitting place, "
            + $"{Inland} moved off the water, {Awash} refused for standing in it, {Walled} refused for standing in a town, {Unpitched} that found no ground to stand on)"
            + (Running ? $"; running now: {Kind} at ×{Bonus} for {Prize}gp, {Venue?.Phrase ?? "at home"}" : "");

    /// <summary>A world reload is a different world.</summary>
    public static void Forget()
    {
        Kind = null;
        Prize = 0;
        Why = null;
        Said = null;
        Where = Point3D.Zero;
        Venue = null;
        Ground = null;
        Pitched = false;
        _past.Clear();
        _tally.Clear();
        Declared = 0;
        Won = 0;
        Ignored = 0;
        Paid = 0;
        Camps = 0;
        Placed = 0;
        Hauled = 0;
        Unpitched = 0;
        Inland = 0;
        Awash = 0;
        Walled = 0;
        Collected = 0;
        Bands = 0;
        Purse = Treasury;
        _bands.Clear();
        _ledger.Clear();
        _halfWay = false;
        _taxed = false;
    }

    /// <summary>
    /// The answer the model is allowed to give.
    ///
    /// <para>
    /// The trade is an enumeration rather than free text, and that is the whole safety of this: a model that
    /// may name any string can name one no proposer answers to, and the revel would then be a price rise on
    /// nothing. These are the ledger's own words for work the population actually takes.
    /// </para>
    /// </summary>
    /// <para>
    /// <b>Plain types only, and that is not fastidiousness.</b> The first version of this used <c>enum</c>,
    /// <c>integer</c> and <c>minimum</c>/<c>maximum</c>, and the model answered HTTP 500 to every ask — the
    /// note on BotDebugNote.Schema says as much about this transport: not every JSON Schema keyword is
    /// honoured there. So the shape is strings, numbers and a boolean, and the list of allowed trades is
    /// enforced by <see cref="Known"/> on the way in, where a bad answer costs a log line instead of a
    /// silent nothing every quarter of an hour.
    /// </para>
    public const string Schema =
        """
        {"type":"object","properties":{"kind":{"type":"string"},"prize":{"type":"number"},"camp":{"type":"boolean"},"x":{"type":"number"},"y":{"type":"number"},"say":{"type":"string","minLength":20},"why":{"type":"string","minLength":40}},"required":["kind","prize","camp","say","why"]}
        """;

    /// <summary>
    /// The trades a revel may name. Enforced here rather than in the schema — see Schema for why.
    ///
    /// A word no proposer answers to would make the revel a price rise on nothing, which is the one failure
    /// mode of this whole idea that would be invisible: the bonus applies, nobody notices, nobody is paid.
    ///
    /// <para>
    /// <b>Not "prowl", since 14.09.2026.</b> A prowl is a walk to wherever a fight might be, and it pays in the fights
    /// it finds rather than in goods. At 20:35 that evening the watchers declared prowl worth three times as much
    /// because it "has been taken many times but hasn't produced any goods or skills" — true of every prowl by
    /// design — and companies went looking for fights across the whole map; they found plague beasts north-east of
    /// Britain, and twenty-eight bots died in that field in the next twenty minutes. A hunt revel with a camp puts
    /// the fighting where it was chosen to be, which is the controlled form of the same idea.
    /// </para>
    /// </summary>
    public static readonly string[] Trades =
    [
        "mine", "chop", "hunt", "cook", "sew", "forge", "brew",
        "inscribe", "herbs", "forage", "peddle", "plunder", "liberate"
    ];

    /// <summary>Whether this is a trade the population actually takes.</summary>
    public static bool Known(string kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            return false;
        }

        var want = kind.Trim();

        for (var i = 0; i < Trades.Length; i++)
        {
            if (string.Equals(Trades[i], want, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Times a chosen spot had no ground a camp could stand on, even after looking around it.
    ///
    /// <b>Its own bucket, because the first version of this said nothing at all.</b> A spot that cannot be
    /// settled produced a revel with no camp and a sentence that did not mention one, which reads exactly
    /// like a revel that never wanted a camp. The first spot the watcher ever chose landed in water and the
    /// summary reported "0 camps raised (1 on a spot the watcher chose)" - true, unhelpful, and only legible
    /// because the two numbers happened to be printed side by side.
    /// </summary>
    public static long Unpitched { get; private set; }

    /// <summary>
    /// Camps moved off wet ground onto dry, and the number that says the water rule is doing something.
    ///
    /// <b>Its own bucket rather than a share of <see cref="Hauled"/>.</b> Hauled means the watcher misjudged
    /// the distance; this means it named somewhere on a coast. They are different mistakes by the model and
    /// folding them together would hide whichever is rarer.
    /// </summary>
    public static long Inland { get; private set; }

    /// <summary>
    /// Spots refused because every candidate around them had water inside the camp's footprint.
    ///
    /// Separate from <see cref="Unpitched"/> on purpose: "no floor" and "floor, but a shore" are two
    /// different things to be told, and a rule that filed its refusals in an older rule's bucket would read
    /// as the map having got worse. See the note on <see cref="Footing"/>.
    /// </summary>
    public static long Awash { get; private set; }

    /// <summary>
    /// Spots refused because a town stood in the camp's footprint.
    ///
    /// Its own bucket beside <see cref="Awash"/> and <see cref="Unpitched"/>: "no floor", "a shore" and "a
    /// street" are three different things to be told, and a rule that filed its refusals in an older rule's
    /// bucket would read as the island having got wetter.
    /// </summary>
    public static long Walled { get; private set; }

    /// <summary>
    /// How far around a camp's middle the ground must be out of the water.
    ///
    /// <b>Seven because <c>OrcCamp</c> says seven</b> — <c>Utility.RandomMinMax(-7, 7)</c> for each of its
    /// orcs and its captain, and a standard at (-5, -4). It is the reach of what the camp puts on the
    /// ground, not a guess about how much room a camp looks like it wants.
    /// </summary>
    public static int CampDry { get; set; } = 7;

    /// <summary>How far out <see cref="Footing"/> will look for ground the camp can have.</summary>
    public static int CampSweep { get; set; } = 32;

    /// <summary>How coarsely it looks: eight bearings at each of these intervals.</summary>
    public static int CampStep { get; set; } = 4;

    /// <summary>
    /// Somewhere near the asked-for spot that a camp can actually stand on.
    ///
    /// <para>
    /// One blocked tile is not a refusal. <c>BotStep.Settle</c> answers about a single square, and a coast,
    /// a rock or a road will fail it while perfectly good ground sits four tiles over; the watcher is
    /// choosing a place, not a square, and holding it to the square is holding it to a map it cannot see.
    /// So the asked-for tile is tried first and then rings out from it in eight directions.
    /// </para>
    ///
    /// <para>
    /// <b>And dry ground, not merely standing ground — Patrick found orcs in the sea on 10.09.2026.</b>
    /// Settling refuses water, because water land is impassable, so the camp itself always stood on land.
    /// What it never asked about was the fifteen tiles the camp scatters over: <c>OrcCamp</c> puts each of
    /// its three orcs, its captain and its prisoner down at a random offset up to seven tiles out, through
    /// <c>BaseCamp.AddMobile</c>, which takes the average height of that tile and moves the creature there
    /// with no test of any kind. A camp sited six tiles from a shore is therefore a camp with part of its
    /// garrison standing in the water — and an orc cannot swim, so it is blocked in all eight directions
    /// for as long as the camp lasts, while the prisoner is <c>CantWalk</c> and cannot leave at all.
    /// So the whole footprint is asked about, through <see cref="BotStep.Dry"/>, and a wet one is walked
    /// away from exactly as an unstandable one is.
    /// </para>
    ///
    /// <para>
    /// The height is always the map's, never arithmetic. See the note on <see cref="Ring"/>: an invented Z
    /// is a place nothing can stand on, and that is a mistake this project has already paid for once.
    /// </para>
    ///
    /// <para>
    /// Public because <see cref="BotHand"/> pitches camps by hand through the same door and must site them
    /// by the same rule; a second copy of this would have disagreed with the first within the week.
    /// </para>
    /// </summary>
    public static bool Footing(Map map, Point3D near, out Point3D found, out sbyte z)
    {
        found = near;

        var wet = false;
        var town = false;

        if (BotStep.Settle(map, near.X, near.Y, out z))
        {
            if (Outdoors(map, near.X, near.Y))
            {
                if (BotStep.Dry(map, near.X, near.Y, CampDry))
                {
                    return true;
                }

                wet = true;
            }
            else
            {
                town = true;
            }
        }

        for (var step = CampStep; step <= CampSweep; step += CampStep)
        {
            for (var dir = 0; dir < 8; dir++)
            {
                var angle = dir * Math.PI / 4.0;
                var x = near.X + (int)Math.Round(Math.Cos(angle) * step);
                var y = near.Y + (int)Math.Round(Math.Sin(angle) * step);

                if (!BotStep.Settle(map, x, y, out z))
                {
                    continue;
                }

                if (!Outdoors(map, x, y))
                {
                    town = true;

                    continue;
                }

                if (!BotStep.Dry(map, x, y, CampDry))
                {
                    wet = true;

                    continue;
                }

                found = new Point3D(x, y, z);

                if (wet)
                {
                    Inland++;
                }

                return true;
            }
        }

        if (town)
        {
            Walled++;
        }
        else if (wet)
        {
            Awash++;
        }
        else
        {
            Unpitched++;
        }

        return false;
    }

    /// <summary>
    /// Whether a camp may stand here at all, which on this island means: not in a town.
    ///
    /// <para>
    /// <b>Patrick found an orc camp inside Britain on 11.09.2026.</b> Settling answers about the floor and
    /// <see cref="BotStep.Dry"/> about the water, and neither of them has any opinion about a guarded region
    /// — so the ring search would happily put three orcs, a captain and a prisoner on a street corner, where
    /// the watch kills them before anybody walks over and the whole revel is a joke. <c>BotPlot</c> has
    /// refused town ground for halls since it was written, for the same reason and in the same words; this
    /// is that rule, applied to the other thing this shard puts on the ground.
    /// </para>
    ///
    /// <para>
    /// Asked of the whole footprint rather than of the middle tile, because a camp on the town's edge puts
    /// half its garrison inside the walls — the same argument as the water, and the same reach.
    /// </para>
    /// </summary>
    private static bool Outdoors(Map map, int x, int y)
    {
        for (var dy = -CampDry; dy <= CampDry; dy += CampDry)
        {
            for (var dx = -CampDry; dx <= CampDry; dx += CampDry)
            {
                if (Region.Find(new Point3D(x + dx, y + dy, 0), map)?.IsPartOf<Server.Regions.GuardedRegion>() == true)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// A spot the watcher asked for, pulled into the ring around where the population lives.
    ///
    /// <para>
    /// Direction is the watcher's and is never overridden — that is the whole of what it is choosing. Only
    /// the distance is corrected, and only when it falls outside <see cref="NearestCamp"/> to
    /// <see cref="FurthestCamp"/>, in which case the point slides along its own bearing to the nearest edge
    /// of the ring. A model that names somewhere sensible is obeyed exactly; one that names the far side of
    /// the island gets the right direction at a distance bots will actually walk.
    /// </para>
    ///
    /// <para>
    /// The height is deliberately not carried over. Whatever the watcher says about Z is arithmetic about a
    /// map it cannot see, and an invented height is a place nothing can stand on; the caller settles it
    /// against the map, which is the only thing that knows.
    /// </para>
    /// </summary>
    public static Point3D Ring(Point3D home, Point3D asked)
    {
        double dx = asked.X - home.X;
        double dy = asked.Y - home.Y;
        var span = Math.Sqrt(dx * dx + dy * dy);

        // Straight on top of home carries no bearing to preserve, so one is chosen rather than dividing by
        // nothing: due east at the near edge, which is as arbitrary as it is harmless.
        if (span < 1.0)
        {
            Hauled++;

            return new Point3D(home.X + NearestCamp, home.Y, home.Z);
        }

        var want = Math.Clamp(span, NearestCamp, FurthestCamp);

        if (Math.Abs(want - span) < 1.0)
        {
            return new Point3D(asked.X, asked.Y, home.Z);
        }

        Hauled++;

        return new Point3D(
            home.X + (int)Math.Round(dx / span * want),
            home.Y + (int)Math.Round(dy / span * want),
            home.Z
        );
    }

    /// <summary>What the watcher is told before it is asked to think of one.</summary>
    public static string System(string name) =>
        $"You are {name}, the watcher of a shard of autonomous bots. Every so often you may declare a revel: "
        + "a contest that makes one kind of work worth three times as much for twelve minutes, with a prize "
        + "of gold to whoever does most of it. You are looking at the population's own numbers. Choose work "
        + "that is being neglected, or that the island is short of, or that would put the bots somewhere "
        + "interesting — and set 'camp' true when you want an orc camp raised for them to fight over. "
        + "When you raise a camp you may also choose where it stands: give 'x' and 'y' as map coordinates in the "
        + "countryside, out of town, near where bots are working. A spot on dangerous or deadly ground, on ground no "
        + "walk reaches, with no bot near it, or close to one of the last few places an event was held is moved to a "
        + "fitting place, and being moved is a sign you have misjudged the map. Leave x and y out to have a fitting "
        + "place chosen for you; a revel of a trade is called in a town square chosen the same way. "
        + "Answer 'nothing' when the shard is busy and healthy and needs no prodding; that is a good answer "
        + "and you should give it often. 'say' is what you shout to the island, in the voice of a herald. "
        + "'why' is your reasoning, for the log. 'kind' must be exactly one of: "
        + string.Join(", ", Trades) + ", or the word nothing. 'prize' is a whole number of gold from 0 to "
        + MostPrize + ". Half of what you offer goes to the single bot that does most of it and half to the "
        + "guild that does most of it between them, so a revel is a contest between the bands as well. "
        + $"The guilds are: {Leaders()}. Your treasury holds {Purse} gold and is filled by a small tax on "
        + "those guilds, so a prize you do not need to offer is a prize you keep. What your past revels came "
        + $"to: {Ledger()} — a kind that has been held more than once and drawn nobody is a price the "
        + "population walked past, and declaring it again is not reading your own results.";
}

/// <summary>
/// What the model answered when asked to think of a revel.
///
/// A record of its own rather than a tuple, for the same reason <c>BotDebugNote</c> is one: the fields are
/// what the schema promises, and a mismatch between the two should be a compile error rather than a silent
/// null at two in the morning.
/// </summary>
public sealed class BotRevelPlan
{
    /// <summary>The trade being rewarded, or "nothing".</summary>
    public string Kind { get; init; }

    /// <summary>What the winner gets. Capped again on the way in — a schema is a request, not a guarantee.</summary>
    public int Prize { get; init; }

    /// <summary>Whether an orc camp goes up as part of it.</summary>
    public bool Camp { get; init; }

    /// <summary>
    /// Where the watcher wants that camp, or <c>Point3D.Zero</c> for beside the population.
    ///
    /// Zero is the honest reading of "it did not say": a model that omits the fields and a model that names
    /// the origin of the map are both telling us it has no opinion about the place, and both should get the
    /// default rather than a camp in the sea.
    /// </summary>
    public Point3D Spot { get; init; }

    /// <summary>What the watcher shouts to the island.</summary>
    public string Say { get; init; }

    /// <summary>Its reasoning, for the log.</summary>
    public string Why { get; init; }

    /// <summary>Reads one, or null when the answer cannot be made sense of.</summary>
    public static BotRevelPlan Read(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                return null;
            }

            return new BotRevelPlan
            {
                Kind = root.TryGetProperty("kind", out var kind) ? kind.GetString() : null,
                Prize = root.TryGetProperty("prize", out var prize) && prize.TryGetInt32(out var got) ? got : 0,
                Camp = root.TryGetProperty("camp", out var camp) && camp.ValueKind == System.Text.Json.JsonValueKind.True,
                Say = root.TryGetProperty("say", out var say) ? say.GetString() : null,
                Why = root.TryGetProperty("why", out var why) ? why.GetString() : null,
                // Read as a pair or not at all: an x with no y is half a point, and half a point on a map
                // is a worse answer than no point, because it looks like one.
                Spot = root.TryGetProperty("x", out var x) && x.TryGetInt32(out var ax)
                       && root.TryGetProperty("y", out var y) && y.TryGetInt32(out var ay)
                    ? new Point3D(ax, ay, 0)
                    : Point3D.Zero
            };
        }
        catch
        {
            return null;
        }
    }
}
