using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// Finding something worth fighting, and finding what it left behind.
///
/// <para>
/// <b>Every judgement here is already written.</b> <c>BotThreat</c> knows what fighting power is (how long a
/// thing lasts multiplied by how hard it hits), what counts as hostile, and what our side comes to including
/// whoever happens to be standing nearby. This file adds one question to that: of the things I could beat,
/// which is the biggest. Nothing about combat is re-decided.
/// </para>
///
/// <para>
/// <b>The biggest I can beat, not the nearest.</b> Distance is already priced by the appraisal's nearness
/// factor, so a hunter that preferred the closest rat would be paid twice for being lazy. And the whole shape
/// of the first version's death loop was fighting things it could not beat, so the test is the same tolerance
/// the flight decision uses — it must lose by the same arithmetic it would flee by.
/// </para>
///
/// <para>
/// <b>No memory of where the monsters are.</b> Unlike a vein or a shop, a creature walks and respawns, so a
/// remembered one is a lie within a minute. The sweep is fresh every time and that is the cost of this
/// proposer; which patches of ground pay is remembered instead, by the ledger, which is the right place for a
/// fact about ground.
/// </para>
/// </summary>
public static class BotQuarry
{
    /// <summary>
    /// How far a hunter looks for something to fight.
    ///
    /// Thirty tiles: about a screen and a half, far enough that a bot standing in town can see the field
    /// outside it, near enough that the walk is a walk rather than an expedition. Hunting further afield is a
    /// different mechanic and it is the one that killed the first version.
    /// </summary>
    public static int Reach { get; set; } = 30;

    /// <summary>
    /// How close something has to be before it is worth putting other work down for.
    ///
    /// <para>
    /// <b>The reach and this are two different questions and were being answered by one number.</b> "What
    /// could I go and fight" is a thirty-tile question — a bot standing in town can see the field outside it
    /// — but "is this worth abandoning a dig for" is not, and using the reach for both meant either a bot
    /// that ignored the ogre beside it for half a minute, or one that dropped everything for a rat three
    /// screens away. Twelve tiles: outside a bow's ten and well outside a caster's eight, so anything this
    /// close is already close enough to be a fight whether the bot agrees to it or not.
    /// </para>
    /// </summary>
    public static int Notice { get; set; } = 12;

    /// <summary>
    /// How much stronger than itself a bot will deliberately set out after.
    ///
    /// <para>
    /// <b>This number was upside down and it is why ogres were invisible.</b> The test read
    /// <c>power × Tolerance &gt; ours</c> — that is, a bot would only go after something it was one and a half
    /// times <em>stronger</em> than — while the standing decision it claimed to be copying,
    /// <see cref="BotThreat.Decide"/>, commits to anything up to one and a half times stronger than the bot.
    /// The two differ by a factor of two and a quarter, and an ogre sits squarely in the gap: 1080 against a
    /// bot's 1116, so the flight rule says fight and the hunting rule demanded 1620 and found nothing. The
    /// population spent its evenings on rats and sheep because the only things it was allowed to want were
    /// things it outclassed two to one.
    /// </para>
    ///
    /// <para>
    /// One, rather than the tolerance itself, and the difference is deliberate: going out looking for
    /// something is not the same act as refusing to abandon a fight already happening, and a bot that walks
    /// half a screen to pick a fight it can only just survive has no margin left for the second creature.
    /// At one, anything up to the bot's own strength is fair game — which is the ogre, and is not the
    /// graveyard spectre at 2120. Move it in <c>bot-hunt.json</c> if the population should be braver.
    /// </para>
    /// </summary>
    public static double Daring { get; set; } = 1.0;

    /// <summary>How near the corpse a bot has to be to go through it.</summary>
    public static int LootReach { get; set; } = 2;

    /// <summary>
    /// How long a claim on a quarry stands before anybody else may take it.
    ///
    /// <b>Short, and renewed by pursuit rather than granted once.</b> A claim is made when a bot decides to go
    /// for something — that is the whole point, since a claim made on contact comes too late to stop everybody
    /// converging — and it is refreshed on every beat the bot is still walking towards it or hitting it. So a
    /// bot that gets there keeps its claim for as long as the fight lasts, and one that wanders off, dies or
    /// changes its mind stops renewing and the thing is free again within the window.
    /// </summary>
    public static int ClaimMs { get; set; } = 45000;

    /// <summary>
    /// Who is fighting what.
    ///
    /// <para>
    /// <b>The one thing in this project that is keyed by serial rather than held on the bot</b>, and it has to
    /// be: it is a fact about a creature, and we do not own creatures. It is kept small by construction —
    /// entries go the moment the fight ends, and a stale one expires — rather than by a sweep.
    /// </para>
    /// </summary>
    private static readonly Dictionary<Serial, (Serial Hunter, long Tick)> _claims = [];

    /// <summary>
    /// Claims a quarry for this bot, or renews a claim it already holds. The first to set about something owns
    /// what comes off it — and, more to the point, is the only one who goes for it at all.
    /// </summary>
    public static void Claim(Mobile hunter, Mobile quarry)
    {
        if (hunter == null || quarry == null)
        {
            return;
        }

        if (!Held(quarry, out var by) || by == hunter.Serial)
        {
            _claims[quarry.Serial] = (hunter.Serial, Core.TickCount);
        }
    }

    /// <summary>Whether this bot may take what comes off this quarry: its own claim, or nobody's.</summary>
    public static bool Ours(Mobile hunter, Mobile quarry) =>
        hunter != null && quarry != null && (!Held(quarry, out var by) || by == hunter.Serial);

    /// <summary>The fight is over, however it ended. The claim goes with it.</summary>
    public static void Release(Mobile quarry)
    {
        if (quarry != null)
        {
            _claims.Remove(quarry.Serial);
        }
    }

    /// <summary>Whether somebody holds a standing claim on this, and who.</summary>
    private static bool Held(Mobile quarry, out Serial by)
    {
        by = Serial.Zero;

        if (!_claims.TryGetValue(quarry.Serial, out var claim))
        {
            return false;
        }

        if (Core.TickCount - claim.Tick >= ClaimMs)
        {
            _claims.Remove(quarry.Serial);

            return false;
        }

        by = claim.Hunter;

        return true;
    }

    /// <summary>
    /// How long a quarry nobody could reach is left alone.
    ///
    /// A creature can be perfectly visible and completely unreachable — across water, behind a fence, on a
    /// roof, on the far side of a wall — and the sweep that finds it has no idea. Without this the population
    /// rediscovers it every few seconds: one bot claims it, walks at it until the journey gives up, drops it,
    /// the claim lapses, and the next bot does the same. Two minutes is long enough for the thing to wander
    /// somewhere reachable and short enough that a temporary obstacle is forgiven.
    /// </summary>
    public static int ShunMs { get; set; } = 120000;

    /// <summary>
    /// How long something a whole company could not kill is left alone.
    ///
    /// <para>
    /// <b>Much longer than a walk that failed, because a different thing was learned.</b> A quarry nobody
    /// could reach may have wandered somewhere reachable a minute later; a quarry four bots stood round for
    /// ninety seconds without moving its health has told the population something about itself that two
    /// minutes does not undo. Left at two, the shard spent the evening of 23.08.2026 re-forming companies
    /// against the same wraith every two minutes — thirty-five attempts, no kill, and every bot involved
    /// bound to the Bound rung for the duration each time.
    /// </para>
    /// </summary>
    public static int HopelessMs { get; set; } = 900000;

    /// <summary>When each shunned quarry may be looked at again. The expiry, not the moment it was set.</summary>
    private static readonly Dictionary<Serial, long> _shunned = [];

    /// <summary>
    /// Nobody could get to it. Leave it alone for a while — and for twice as long each time it is found unreachable
    /// again without having moved.
    ///
    /// <para>
    /// <b>Two minutes is a fair sentence for a creature and a standing invitation for one that never moves.</b> A zombie at
    /// (1375, 1465, 30) in Britain Graveyard, on top of something, was taken by eight different hunters on build 44 — at
    /// 01:17, 01:19, 01:21, 01:23, 01:25, 01:27, 01:30 and 01:39 — each failing "no way through" and each shunning it for
    /// exactly the two minutes after which the next hunter was handed it. What a second failure proves that the first did
    /// not is that the thing is still where nobody could reach it, so the sentence doubles for as long as that stays true,
    /// up to <see cref="HopelessMs"/>; a creature that has moved starts again at two minutes.
    /// </para>
    /// </summary>
    public static void Shun(Mobile quarry) => Shun(quarry, Sentence(quarry));

    /// <summary>How near its last unreachable spot a creature has to stand to count as not having moved. See <see cref="Shun(Mobile)"/>.</summary>
    public static int StillWithin { get; set; } = 2;

    /// <summary>Shuns longer than the first, because the creature was still where it could not be reached before.</summary>
    public static long Reshunned { get; private set; }

    /// <summary>How many times running each creature has been found unreachable, and where it stood the last time.</summary>
    private static readonly Dictionary<Serial, (int Times, Point3D At)> _unreached = [];

    private static int Sentence(Mobile quarry)
    {
        if (quarry == null)
        {
            return ShunMs;
        }

        var times = _unreached.TryGetValue(quarry.Serial, out var last) && quarry.InRange(last.At, StillWithin)
            ? last.Times + 1
            : 1;

        // A table of every creature ever found unreachable would outlive most of them; clearing it now and then costs
        // one creature one doubling.
        if (_unreached.Count >= 4096)
        {
            _unreached.Clear();
        }

        _unreached[quarry.Serial] = (times, quarry.Location);

        if (times == 1)
        {
            return ShunMs;
        }

        Reshunned++;

        return (int)System.Math.Min((long)ShunMs << System.Math.Min(times - 1, 8), HopelessMs);
    }

    /// <summary>Leave it alone for as long as whoever found that out thinks it is worth.</summary>
    public static void Shun(Mobile quarry, int ms)
    {
        if (quarry != null)
        {
            var until = Core.TickCount + ms;

            // A longer sentence is never shortened by a later, lesser one: the hunter's own two minutes
            // would otherwise overwrite a company's quarter of an hour the first time one bot walked past.
            if (!_shunned.TryGetValue(quarry.Serial, out var standing) || until - standing > 0)
            {
                _shunned[quarry.Serial] = until;
            }
        }
    }

    /// <summary>
    /// Whether this one is being left alone at the moment.
    ///
    /// <para>
    /// <b>The twin of <see cref="Crowded"/>, and it was hidden for the same reason and cost the same
    /// thing.</b> A fight that finds no way to its quarry writes this list — see <c>BotSlay.Bend</c> — and
    /// until now only the hunt's own target-picking ever read it. So a bot with something unreachable
    /// hitting it was handed that fight by the defender, failed to path to it, and was handed it again five
    /// seconds later: Gerda ran that circle eleven times on one skeleton, all of them printing "no way
    /// through to a skeleton", none of them able to remember the last one.
    /// </para>
    ///
    /// <para>
    /// Unlike a crowd, this one is honestly shard-wide: "nobody could get to it" is as true of a company as
    /// of one bot, which is exactly the distinction the two lists were separated to keep.
    /// </para>
    /// </summary>
    public static bool Shunned(Mobile quarry)
    {
        if (!_shunned.TryGetValue(quarry.Serial, out var until))
        {
            return false;
        }

        // Subtraction, never a bare comparison: these counters start enormous and wrap. See dev-docs/tick-counts.md.
        if (Core.TickCount - until < 0)
        {
            return true;
        }

        _shunned.Remove(quarry.Serial);

        return false;
    }

    /// <summary>
    /// How long something that turned out to be standing in a crowd is left to lone hunters.
    ///
    /// Short, because a crowd is the most temporary fact on this list: creatures wander apart, and the one
    /// that made the odds hopeless may be dead by the time this lapses.
    /// </summary>
    public static int CrowdMs { get; set; } = 120000;

    private static readonly Dictionary<Serial, long> _crowded = [];

    /// <summary>
    /// How long a fight stays handed over before a walking bot will stop for it again.
    ///
    /// <para>
    /// Ten seconds, and it is short because it is not a verdict about the creature — it is a note that this
    /// bot has already offered the auction this fight once and the auction did nothing with it.
    /// </para>
    /// </summary>
    public static int HandMs { get; set; } = 10000;

    private static readonly Dictionary<Serial, long> _handed = [];

    /// <summary>
    /// A walking bot stopped for this creature so that something else could deal with it.
    ///
    /// <para>
    /// <b>A hand-over with no receiver is a loop, and this is the note that makes it stop after one lap.</b>
    /// A prowl ends the moment something picks a fight with the bot, on the reasoning that a bot out looking
    /// for a fight does not get to walk past one. But the auction only offers a fight the bot would
    /// <em>choose</em>, and something that has chosen the bot is very often not that — so the errand ended,
    /// nothing took the fight, the auction handed the same errand straight back, and it ended again. On
    /// 02.09.2026 between 23:18 and 23:31 that came to 345 prowls taken and 189 of them ending this way,
    /// with no fight recorded in between at all: Edda went round it eight times inside three seconds.
    /// </para>
    ///
    /// <para>
    /// So the offer is made once. If the same creature is still on the bot when the walk resumes, walking on
    /// <em>is</em> the answer — a bot nobody will fight for is a bot that should be leaving.
    /// </para>
    /// </summary>
    public static void Hand(Mobile quarry)
    {
        if (quarry != null)
        {
            _handed[quarry.Serial] = Core.TickCount + HandMs;
        }
    }

    /// <summary>Whether this fight has already been offered to the auction and left there.</summary>
    public static bool Handed(Mobile quarry)
    {
        if (quarry == null || !_handed.TryGetValue(quarry.Serial, out var until))
        {
            return false;
        }

        if (Core.TickCount - until < 0)
        {
            return true;
        }

        _handed.Remove(quarry.Serial);

        return false;
    }

    /// <summary>
    /// One bot walked to it and found the odds against it. Kept away from lone hunters — and from nobody else.
    ///
    /// <para>
    /// <b>A list of its own, and merging it with <see cref="Shun"/> cost a whole evening's companies.</b> The
    /// two facts sound alike and are opposite in their consequence. "Nobody could get to it" is true of a
    /// company as much as of one bot, so it belongs on the shared list. "One bot is outnumbered around it" is
    /// the definition of what a <em>company</em> is for — and put on the shared list it hid, from
    /// <see cref="Company"/>, exactly the creatures <see cref="BotMuster"/> exists to call companies against.
    /// Squads formed fifteen times in twenty minutes before, and nought times in the ten minutes after. One
    /// list cannot answer two questions.
    /// </para>
    /// </summary>
    public static void Crowd(Mobile quarry)
    {
        if (quarry != null)
        {
            _crowded[quarry.Serial] = Core.TickCount + CrowdMs;
        }
    }

    /// <summary>
    /// Whether one bot should be left to find something else.
    ///
    /// <para>
    /// <b>Written by the fight and read by only one of the two things that pick targets, which is how a
    /// note becomes useless.</b> Choosing quarry to hunt asked this; going to somebody's aid did not, and
    /// nothing said so. On the night of 25.08.2026 that split the population's failures down the middle: on
    /// one and the same creature, at one and the same time, the hunt gave up 7 times and rescue 103 — and
    /// across the night rescue was 70 to 96 per cent of every failure on the shard, with three bots of
    /// sixteen doing nothing else for an hour. A fact worth recording is worth recording for everybody who
    /// acts on it.
    /// </para>
    /// </summary>
    public static bool Crowded(Mobile quarry)
    {
        if (!_crowded.TryGetValue(quarry.Serial, out var until))
        {
            return false;
        }

        if (Core.TickCount - until < 0)
        {
            return true;
        }

        _crowded.Remove(quarry.Serial);

        return false;
    }

    /// <summary>
    /// What a kind of creature has actually paid, per kill, averaged over every one this population has
    /// brought down.
    ///
    /// <para>
    /// <b>Strength is not worth, and picking the strongest thing you can beat quietly assumes it is.</b> A
    /// bull is tougher than a lizardman and carries nothing; a goat is tougher than a rat and carries nothing
    /// either. Left to raw power the population hunted the local farms — seven of nine kills in one stretch
    /// were sheep, goats, pigs and eagles — earned skill, and brought in no gold at all, which matters
    /// because a monster's purse is the only place gold enters this world.
    /// </para>
    ///
    /// <para>
    /// Measured rather than declared, like everything else here: nothing anywhere lists what a creature
    /// carries, and a table of it would be wrong the first time somebody edits a loot pack. An untried kind is
    /// given <see cref="Untried"/> so that it gets tried — which is the whole of exploration, and the same
    /// trick the ledger plays with unfamiliar ground.
    /// </para>
    /// </summary>
    private static readonly Dictionary<Type, (long Gold, int Kills)> _paid = [];

    /// <summary>
    /// What a kind nobody has killed yet is assumed to be worth.
    ///
    /// Above a farm animal's measured nothing and below a real monster's purse, so an unknown thing is worth
    /// one look and stops being chosen if that look comes back empty.
    /// </summary>
    public static double Untried { get; set; } = 25.0;

    /// <summary>Notes what came off one of these. Called by whoever went through the corpse.</summary>
    public static void Paid(Type kind, int gold)
    {
        if (kind == null)
        {
            return;
        }

        _paid.TryGetValue(kind, out var sofar);

        _paid[kind] = (sofar.Gold + Math.Max(0, gold), sofar.Kills + 1);

        // One of this kind fell to us: the other half of what it costs us in lives. See Price.
        Killed(kind);
    }

    // ---- What a kind costs us in lives (02.10.2026, build next; Patrick's option 2: "hunt only what you beat") -----------------

    /// <summary>
    /// Whether a kind's measured deaths of ours against kills of it price what it takes to beat it. Off, every kind is weighed at
    /// its strength alone (<see cref="BotThreat.Power"/>), as before.
    /// </summary>
    public static bool PricesRisk { get; set; } = true;

    /// <summary>How long it takes a kind's kills and deaths to count for half. An hour: what it did lately, not on the first night.</summary>
    public static int RiskHalfMs { get; set; } = 3600000;

    /// <summary>Deaths of ours to a kind, as the hour weighs them, before its record prices anything: three, so one unlucky fall is not a ban.</summary>
    public static double RiskLeast { get; set; } = 3.0;

    /// <summary>Deaths of ours per kill of it that a kind may cost before its price rises: a quarter, one bot for every four of them.</summary>
    public static double RiskFair { get; set; } = 0.25;

    /// <summary>How steeply the price rises past <see cref="RiskFair"/>: twice the deaths per kill over it.</summary>
    public static double RiskSteep { get; set; } = 2.0;

    /// <summary>The most a kind's strength is multiplied by for its record. Eight.</summary>
    public static double RiskMost { get; set; } = 8.0;

    private sealed class Record
    {
        public double Kills;

        public double Deaths;

        public long Tick;
    }

    private static readonly Dictionary<Type, Record> _records = [];

    /// <summary>Quarry a lone hunter, a company and a zone's look passed over because the kind's record priced it past them.</summary>
    public static long PricedOut { get; private set; }

    public static long CompanyPricedOut { get; private set; }

    /// <summary>Creatures a company's look passed over for lying between the company's choice gate and the standing tolerance (06.10.2026, company16). See <c>BotSquad.ChooseOdds</c>.</summary>
    public static long CompanyChoiceOut { get; private set; }

    public static long ZonePricedOut { get; private set; }

    /// <summary>Kinds whose pay a poor hunter's zone look left out for being priced past it. See <see cref="ZonePays"/>.</summary>
    public static long PayUnheeded { get; private set; }

    /// <summary>A kind's record brought up to now: both counts halved for every <see cref="RiskHalfMs"/> since it was last touched.</summary>
    private static Record Aged(Type kind, long now, bool make)
    {
        if (!_records.TryGetValue(kind, out var record))
        {
            if (!make)
            {
                return null;
            }

            if (_records.Count > 1024)
            {
                _records.Clear();
            }

            record = new Record { Tick = now };
            _records[kind] = record;

            return record;
        }

        var span = now - record.Tick;

        if (span > 0 && RiskHalfMs > 0)
        {
            var keep = Math.Pow(0.5, span / (double)RiskHalfMs);

            record.Kills *= keep;
            record.Deaths *= keep;
            record.Tick = now;
        }

        return record;
    }

    private static void Killed(Type kind)
    {
        if (kind == null)
        {
            return;
        }

        Aged(kind, Core.TickCount, true).Kills += 1.0;
    }

    /// <summary>One of ours fell to <paramref name="killer"/>. Read from <c>BotKept.Fell</c>, as the city's prices on heads are; a bot, a pet or a summons is not a kind.</summary>
    public static void Fell(Mobile killer)
    {
        if (killer is not BaseCreature { Deleted: false, Controlled: false, Summoned: false } creature || killer is BotMobile)
        {
            return;
        }

        Aged(creature.GetType(), Core.TickCount, true).Deaths += 1.0;
    }

    /// <summary>
    /// What a kind's strength is multiplied by before a bot or a company may choose it: one, until it has killed
    /// <see cref="RiskLeast"/> of ours inside the hour's weighing; then one plus <see cref="RiskSteep"/> times its deaths of ours per
    /// kill of it over <see cref="RiskFair"/>, up to <see cref="RiskMost"/>.
    ///
    /// <para>
    /// <b>Measured before it (builds 390–392, 14:21–16:12, the <c>Kept</c> lines against the company downs and solo kills):</b> 264
    /// bots fell to creatures, 114 of them to five kinds; Reaper 27 of ours for 9 of them (122gp a kill), Wisp 28 for 1, Air
    /// Elemental 24 for 8, Gazer 15 for 6, Giant Spider 20 for 14, Gargoyle 19 for 14 — while Ogre cost 6 for 33 and Ettin 18 for
    /// 25. The fallen were a median ten times weaker than what killed them (killer's strength over the bot's and its able
    /// company's: 9.8 for the 112 who died enlisted), and not one of the five kinds' deaths was a lone hunt. The choice weighed the
    /// kind's strength against the company's at ×1.5 and never asked what the kind had cost; at about 100gp a death (pocket and
    /// supplies) a Reaper's 122gp was paid for with 300gp of ours.
    /// </para>
    ///
    /// <para>
    /// <b>A kind the bot does not beat is not chosen, however high its bounty or loot.</b> The price is a multiplier on the kind's
    /// strength, asked wherever a hunter, a company or a zone's look weighs strength against strength, so a company still takes
    /// what a lone bot may not, and a strong enough one still takes a Reaper. The bounty is never in it: what a kind pays only
    /// ranks the kinds a bot beats. As the hour forgets, a kind is tried again; three more deaths price it again.
    /// </para>
    /// </summary>
    public static double Price(Type kind)
    {
        if (!PricesRisk || kind == null)
        {
            return 1.0;
        }

        var record = Aged(kind, Core.TickCount, false);

        return record == null ? 1.0 : Formula(record.Deaths, record.Kills);
    }

    /// <summary>
    /// The price a record of <paramref name="deaths"/> of ours to <paramref name="kills"/> of a kind comes to: one under
    /// <see cref="RiskLeast"/> deaths, then one plus <see cref="RiskSteep"/> times the deaths per kill over <see cref="RiskFair"/>,
    /// up to <see cref="RiskMost"/>. The island's record (<see cref="Price"/>) and the companies' (<c>BotCompanyRecord</c>) are
    /// priced by the same arithmetic.
    /// </summary>
    public static double Formula(double deaths, double kills)
    {
        if (deaths < RiskLeast)
        {
            return 1.0;
        }

        var perKill = deaths / (kills + 1.0);

        return Math.Clamp(1.0 + RiskSteep * (perKill - RiskFair), 1.0, Math.Max(1.0, RiskMost));
    }

    /// <summary>The strength a bot or a company must bring to choose this creature: its own, times its kind's price.</summary>
    public static double Priced(BaseCreature creature) =>
        creature == null ? 0.0 : BotThreat.Power(creature) * Price(creature.GetType());

    /// <summary>
    /// The highest price among a zone's living kinds: what the zone's fight is multiplied by when it is weighed. See <c>BotHunter.Zoned</c>.
    /// <paramref name="company"/>: weighed for a company, each kind at its company price (<c>BotCompanyRecord.Price</c>, 03.10.2026).
    /// </summary>
    public static double ZonePrice(BotZoneKindCount[] kinds, bool company = false)
    {
        var most = 1.0;

        if (!PricesRisk || kinds == null)
        {
            return most;
        }

        for (var i = 0; i < kinds.Length; i++)
        {
            if (kinds[i].Live <= 0 || KindOf(kinds[i].Kind) is not { } type)
            {
                continue;
            }

            most = Math.Max(most, company ? BotCompanyRecord.Price(type) : Price(type));
        }

        return most;
    }

    /// <summary>A zone's look passed over a zone or a creature for its kind's price. See <c>BotHunter.Zoned</c>.</summary>
    internal static void ZoneRefused() => ZonePricedOut++;

    private static Type KindOf(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (!_kindTypes.TryGetValue(name, out var type))
        {
            type = AssemblyHandler.FindTypeByName(name);
            _kindTypes[name] = type;
        }

        return type;
    }

    /// <summary>The kinds priced now, dearest first, for the summary.</summary>
    private static string DescribePrices()
    {
        if (!PricesRisk)
        {
            return "off";
        }

        var now = Core.TickCount;
        List<(string Name, double Price, double Deaths, double Kills)> priced = [];

        foreach (var (kind, _) in _records)
        {
            var record = Aged(kind, now, false);
            var price = Price(kind);

            if (record != null && price > 1.0)
            {
                priced.Add((kind.Name, price, record.Deaths, record.Kills));
            }
        }

        if (priced.Count == 0)
        {
            return "none";
        }

        priced.Sort(static (a, b) => b.Price.CompareTo(a.Price));

        List<string> named = [];

        for (var i = 0; i < priced.Count && i < 8; i++)
        {
            named.Add($"{priced[i].Name} x{priced[i].Price:F1} ({priced[i].Deaths:F0} of ours to {priced[i].Kills:F0} of them)");
        }

        return string.Join(", ", named);
    }

    /// <summary>Gold paid for one of these besides what the corpse held — the city's price on its head — without another kill. See BotCity.Collect.</summary>
    public static void Rewarded(Type kind, int gold)
    {
        if (kind == null || gold <= 0)
        {
            return;
        }

        _paid.TryGetValue(kind, out var sofar);

        _paid[kind] = (sofar.Gold + gold, sofar.Kills);
    }

    /// <summary>
    /// What a standing order for something a carcass yields adds to that creature's worth.
    ///
    /// <para>
    /// Two hundred, which is well above what any ordinary kill on this island pays, and deliberately so: the
    /// point is not to nudge the odds but to turn the population's attention. When the board is asking for
    /// feathers, birds stop being the worthless thing a hunter walks past.
    /// </para>
    /// </summary>
    public static double Bounty { get; set; } = 200.0;

    /// <summary>Kills chosen because the board was asking for what the carcass carries. For the summary.</summary>
    public static long Sent { get; private set; }

    /// <summary>What this creature's meat becomes on the corpse. The engine's own mapping, read not copied.</summary>
    private static Type Butchered(MeatType meat) =>
        meat switch
        {
            MeatType.Bird    => typeof(RawBird),
            MeatType.LambLeg => typeof(RawLambLeg),
            _                => typeof(RawRibs)
        };

    /// <summary>
    /// How much this creature is worth over its purse because somebody has put money down for what it is
    /// made of.
    ///
    /// <para>
    /// <b>This is the last link of the chain Patrick asked for, and without it the chain is open at both
    /// ends.</b> An archer runs low and puts arrows on the board; a fletcher reads the board and can make
    /// them out of wood and feathers; wood is money, and a feather is a thing that only exists because
    /// somebody killed a bird. Nothing anywhere connected "the board wants feathers" to "go and hunt
    /// something with feathers on it", so the fletchers would have stood idle beside a full order book while
    /// the hunters walked past chickens all day looking for something that paid.
    /// </para>
    ///
    /// <para>
    /// Asked of the engine's own carcass properties rather than of a table of creatures — <c>Feathers</c>,
    /// <c>Hides</c> and <c>Wool</c> are on <c>BaseCreature</c> and every animal on the shard fills them in.
    /// A table would have been right on the day it was written and quietly wrong after the first new
    /// creature.
    /// </para>
    /// </summary>
    public static double Sought(BaseCreature creature, Mobile hunter = null)
    {
        if (creature == null)
        {
            return 0.0;
        }

        var worth = 0.0;

        // <b>And the hunter's own guild's board, beside the market.</b> Patrick's order of 15.09.2026: where a
        // material a guild's maker has asked for comes off a creature, the hunters go for that creature. The
        // board is a request with no money down, so it is read here as the market's wants are and paid the same
        // bounty; see BotCharter.Wants.
        var hides = Demanded(typeof(Hides)) || BotCharter.Wants(hunter, "hides");
        // And a fletcher that lately found no feather anywhere, when it could not pay to post the want (30.09.2026). See
        // BotFletcher.Starved: the same demand without the money, read for the ten minutes it stays true.
        var feathers = Demanded(typeof(Feather)) || Demanded(typeof(Arrow)) || BotCharter.Wants(hunter, "feathers") || BotFletcher.Starved;
        var wool = Demanded(typeof(Wool)) || BotCharter.Wants(hunter, "wool");
        var meat = BotCharter.Wants(hunter, "meat");

        // <b>A want for arrows is a want for feathers, one step on.</b> The woodcutter's proposer has read it
        // that way since the day it was written — "logs directly, or arrows, which are logs one step further
        // on, and which is what an archer actually asks for" — and this side never learned the same sentence.
        // It stopped mattering only while archers could not ask: on the evening of 04.09.2026 they began to,
        // and 179 arrow orders stood on the board at 18:02 while this counter had been frozen at 122 for
        // three half-hourly readings. An archer asking for arrows is the demand; nobody was carrying it back
        // to the one act that puts a feather into the world.
        if (creature.Feathers > 0 && feathers)
        {
            worth += Bounty;
        }

        if (creature.Hides > 0 && hides)
        {
            worth += Bounty;
        }

        if (creature.Wool > 0 && wool)
        {
            worth += Bounty;
        }

        // <b>Meat, added after this counter froze at 45 for four readings running.</b> The note above records
        // the identical freeze on feathers — 179 arrow orders standing while the count sat at 122 for three
        // half-hourly readings — and this is the same omission on the material the shard now asks for most.
        // A cook could put money down for ribs from 05.09.2026 and 68 such wants were filled, but nothing
        // carried that demand back to the one act that puts meat into the world, so the board steered hunts
        // for hide and feather and stopped dead the moment those were satisfied.
        //
        // The kind matters: a bird carves into RawBird and a sheep into RawLambLeg, and an order for ribs is
        // not filled by either. Asked of the creature's own MeatType, which is what the engine's carve reads.
        if (creature.Meat > 0 && (meat || Demanded(Butchered(creature.MeatType))))
        {
            worth += Bounty;
        }

        return worth;
    }

    /// <summary>Whether anybody has money down on the board for this, right now.</summary>
    private static bool Demanded(Type kind)
    {
        var wants = BotMarket.Wants;

        for (var i = 0; i < wants.Count; i++)
        {
            if (wants[i].IsOpen && wants[i].Kind == kind)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>What one of these is worth, on the evidence. <see cref="Untried"/> when there is none.</summary>
    public static double Pays(Type kind) =>
        kind != null && _paid.TryGetValue(kind, out var known) && known.Kills > 0
            ? (double)known.Gold / known.Kills
            : Untried;

    // ---- The poor hunter's look for what pays (02.10.2026, build 389; Patrick's option 2) -------------------------------------

    /// <summary>Whether a bot under <c>BotHunter.PoorBelow</c> looks <see cref="PayReach"/> times as far first, for a kind that pays.</summary>
    public static bool PayFirst { get; set; } = true;

    /// <summary>How many times <see cref="Reach"/> the poor hunter's first look goes.</summary>
    public static int PayReach { get; set; } = 2;

    /// <summary>Kills of a kind, all for nothing, after which it is known to carry nothing.</summary>
    public static int PayKnown { get; set; } = 3;

    /// <summary>The least and the most a zone's score is multiplied by for what its kinds pay. See <see cref="ZonePays"/>.</summary>
    public static double ZoneLeast { get; set; } = 0.5;

    public static double ZoneMost { get; set; } = 2.0;

    public static long PaidFirst { get; private set; }

    public static long BarrenPassed { get; private set; }

    public static long ZoneWeighed { get; private set; }

    /// <summary>Quarry a caster passed over as not paying for <c>BotSlay.CastFloor</c> casts. See <c>BotSlay.Thrifty</c>.</summary>
    public static long Dear { get; private set; }

    /// <summary>Whether this kind has been killed <see cref="PayKnown"/> times and carried nothing every time.</summary>
    public static bool Barren(Type kind) =>
        kind != null && _paid.TryGetValue(kind, out var known) && known.Kills >= PayKnown && known.Gold <= 0;

    /// <summary>
    /// The hunter's choice: the ordinary look, and for a bot under <c>BotHunter.PoorBelow</c> whose ordinary look found nothing or a
    /// kind known to carry nothing, a look <see cref="PayReach"/> times as far for the best quarry that is not of such a kind.
    /// </summary>
    public static BaseCreature Paying(IBotWilful bot, Mobile body)
    {
        var near = Best(body, Reach);

        // The wider look, four times the ground of a loop every hunter runs at every review, only when the near look found
        // nothing or a kind known to carry nothing, and only for the poor.
        if (!PayFirst || body == null || near != null && !Barren(near.GetType()) || BotYield.Spendable(body) >= BotHunter.PoorBelow)
        {
            return near;
        }

        var wide = Best(body, Reach * Math.Max(1, PayReach), true);

        if (wide == null)
        {
            return near;
        }

        PaidFirst++;

        return wide;
    }

    private static readonly Dictionary<string, Type> _kindTypes = [];

    /// <summary>
    /// What a zone's kinds pay, as a factor on the zone's score for a poor hunter: half for a zone of kinds known to carry nothing,
    /// up to <see cref="ZoneMost"/> for one whose kinds have paid well, one for anybody not poor and for a zone whose kinds nobody
    /// has priced. Weighted by how many of each kind live there. "Prowl to zones that pay" (Patrick's option 2).
    /// </summary>
    public static double ZonePays(BotZoneKindCount[] kinds, Mobile body)
    {
        if (!PayFirst || kinds == null || kinds.Length == 0 || body == null || BotYield.Spendable(body) >= BotHunter.PoorBelow)
        {
            return 1.0;
        }

        var weight = 0;
        var pays = 0.0;

        for (var i = 0; i < kinds.Length; i++)
        {
            var type = KindOf(kinds[i].Kind);

            if (type == null || !_paid.TryGetValue(type, out var known) || known.Kills <= 0)
            {
                continue;
            }

            // <b>A bounty must not lure the weak (02.10.2026, build next).</b> The city's price on a head counts in what a kind pays
            // (Rewarded), and the kinds that carry one are the kinds that kill us; a kind its record prices past a poor hunter's zone
            // look is left out of what the zone pays. See Price.
            if (Price(type) > 1.0)
            {
                PayUnheeded++;

                continue;
            }

            var live = Math.Max(1, kinds[i].Live);

            weight += live;
            pays += live * ((double)known.Gold / known.Kills);
        }

        if (weight <= 0)
        {
            return 1.0;
        }

        ZoneWeighed++;

        return Math.Clamp(0.5 + pays / weight / Math.Max(1.0, Untried), ZoneLeast, ZoneMost);
    }

    /// <summary>Everything forgotten. The world these serials belong to is being replaced.</summary>
    public static void Forget()
    {
        _claims.Clear();
        _shunned.Clear();
        _unreached.Clear();
        _crowded.Clear();
        _paid.Clear();
        Sent = 0;
        InTown = 0;
        OnGuarded = 0;
        ChosenAttacking = 0;
        ChosenFighting = 0;
        ChosenGrazing = 0;
        ChosenFightingBack = 0;
        PaidFirst = 0;
        BarrenPassed = 0;
        ZoneWeighed = 0;
        Dear = 0;
        _records.Clear();
        BotCompanyRecord.Forget();
        PricedOut = 0;
        CompanyPricedOut = 0;
        CompanyChoiceOut = 0;
        ZonePricedOut = 0;
        PayUnheeded = 0;
    }

    /// <summary>What the population has learned about who is worth fighting. For the summary.</summary>
    public static string Describe()
    {
        var kinds = 0;
        var paying = 0;

        foreach (var (_, known) in _paid)
        {
            kinds++;

            if (known.Gold > 0)
            {
                paying++;
            }
        }

        return $"{kinds} kinds of creature killed and priced, {paying} of them worth the trouble; {Walled} passed over by a company as shut off from where it stood and {Afloat} as swimming where nobody can stand; {Penned} passed over by a lone hunter as standing in a pocket shut off from it, {Unwelcome} as standing on refused ground and {Daunted} as standing on ground asking more strength than it brought ({(ReadsGround ? "passed over" : "only counted")}); {Reshunned} left alone for longer for being found unreachable again where they stood before; hunters sent after {ChosenAttacking} creatures at their throat, {ChosenFighting} that fight whoever comes near and {ChosenGrazing} of the grazing kind ({ChosenFightingBack} of them of a kind that fights back when struck) ({(HostileFirst ? "hostile first" : "by pay alone")}); a poor hunter (under {BotHunter.PoorBelow}gp) looks {PayReach} times as far first for a kind that pays: {PaidFirst} found one, {BarrenPassed} of a kind known to carry nothing passed over in that look, {ZoneWeighed} zone looks weighed by what their kinds pay{(PayFirst ? "" : " (off)")}; {Dear} quarry passed over by a caster as not paying for {BotSlay.CastFloor} casts; priced for the lives they cost (strength × the kind's price, {RiskLeast:F0} deaths of ours inside the hour's weighing before any price): {PricedOut} lone hunters' choices changed, {CompanyPricedOut} creatures passed over in a company's look ({CompanyChoiceOut} more between the company's choice gate and the tolerance), {ZonePricedOut} zones or their creatures passed over in a hunter's zone look, {PayUnheeded} kinds' pay (bounties included) left out of a poor hunter's zone weighing; prices now: {DescribePrices()}";
    }

    /// <summary>
    /// The biggest thing within reach that this bot could take, or null.
    ///
    /// <para>
    /// <b>Judged against what this bot can do alone, and that had to change when claims arrived.</b> It used
    /// to be judged against <c>OurPower</c> — everybody able standing nearby — which was a fair sum while any
    /// number of bots might pile onto the same creature. Claims ended that on purpose: one bot goes, and the
    /// others are pointed at something else. So the company that the sum was counting is company that will
    /// not be there, and the arithmetic was writing cheques the fight could not cash — a spectre at 2120
    /// against three bots' 3348 reads as winnable and is then fought by one bot worth 1116. Five deaths in
    /// nine minutes, all of them at the graveyard, all of them alone against something chosen for a crowd.
    /// </para>
    ///
    /// <para>
    /// Standing beside somebody still helps and still costs nothing to arrange — <see cref="BotThreat.Decide"/>
    /// sums the neighbours when deciding whether to stand and fight something that has already attacked, and
    /// there the company is real, because it is being attacked too. What a bot may go looking for is a
    /// different question, and it is answered by what it can beat by itself.
    /// </para>
    /// </summary>
    public static BaseCreature Best(Mobile bot, int range) => Best(bot, range, false);

    /// <summary>
    /// The same choice, and with <paramref name="payingOnly"/> a kind known to carry nothing is passed over: the poor hunter's wider
    /// look. See <see cref="Paying"/> (02.10.2026, build 389; Patrick's option 2).
    /// </summary>
    public static BaseCreature Best(Mobile bot, int range, bool payingOnly)
    {
        var map = bot?.Map;

        if (map == null || map == Map.Internal)
        {
            return null;
        }

        var ours = BotThreat.Power(bot);

        if (ours <= 0.0)
        {
            return null;
        }

        // The lone bot's own daring: the standing one leaned by a cautious or reckless nature, never past the flight rule's
        // tolerance (07.10.2026, traits17). See BotTraits.Daring.
        var daring = BotTraits.Daring(bot);

        // What the ground is asked against: the hunter and any company it is in, as a hunting ground is. See ReadsGround.
        var strength = BotQuad.Strength(bot);

        // What one cast costs this bot, when it casts: the price its quarry has to pay for. See the loop below.
        var casts = BotSlay.CastsByGold && bot is BotMobile { Class.Role: BotRole.Caster } && BotStrike.Can(bot)
            ? BotProvision.CastCost(bot, BotStrike.Best(bot))
            : 0;

        BaseCreature best = null;
        var bestPower = 0.0;
        var bestPays = -1.0;
        var bestRank = -1;

        foreach (var creature in map.GetMobilesInRange<BaseCreature>(bot.Location, range))
        {
            if (!BotThreat.Hostile(bot, creature) || !BotPopulation.Within(map, creature.Location, bot))
            {
                continue;
            }

            // <b>Outside the towns, the quarry as well as the hunter (01.10.2026, build next; Patrick: "hunting means they hunt
            // everything that can be killed or is hostile to them, outside the towns").</b> The town test was on the hunter only,
            // so a hunter at the edge of town took the dogs and cats in the street: 432 of the 1624 solo kills on build 384
            // (00:42–10:30) were inside a town, 264 of them the grazing kind, at nought gold each.
            if (OutsideTowns && creature.Region?.IsPartOf<TownRegion>() == true)
            {
                InTown++;

                continue;
            }

            // <b>Nor a hostile creature on guarded ground (08.10.2026, guards30).</b> Its first blow back makes it a menace
            // (GuardedRegion.IsMenace), a townsman within eight tiles calls the guards on it, and the guard leaves no corpse
            // and pays no kill: a hunt there ends in nothing. Guarded ground is wider than the town (Yew's reaches far into
            // the woods), so this is asked apart from the town test; the grazing kind is no menace and stays quarry, as the
            // company's clearer and the delve already pass over guarded ground (BotClearer, BotDelve).
            if (OutsideTowns && GuardedRegion.IsHostileCreature(creature) && BotHaven.Guarded(map, creature.Location))
            {
                OnGuarded++;

                continue;
            }

            // Somebody else got to it first. This is what stops six bots converging on one skeleton and five
            // of them arriving at an empty corpse — which is not merely wasteful, it is five bots that spent a
            // walk each to be paid nothing, and the ledger learning that hunting here does not pay.
            //
            // Crowded is asked here and nowhere else: a lone hunter has already walked to this one and found
            // the numbers against it. A company is still offered the same creature, and should be — see Crowd.
            if (!Ours(bot, creature) || Shunned(creature) || Crowded(creature))
            {
                continue;
            }

            var power = BotThreat.Power(creature);

            // Measured the same way round as the flight decision, which it used not to be: there the ratio
            // is the creature's power over ours, and here it was ours over the creature's. See
            // <see cref="Daring"/> — a bot must not walk towards something it would immediately run away
            // from, and it must not refuse everything it could actually take either.
            if (power <= 0.0 || power > ours * daring)
            {
                continue;
            }

            // <b>A poor hunter's wider look is for what pays (02.10.2026, build 389; Patrick's option 2).</b> On build 386, 141 of
            // the 151 solo kills were grazers at nought gold, 3.1gp a corpse; the ten that paid averaged 56. A kind that has been
            // killed PayKnown times for nothing is not what a bot under BotHunter.PoorBelow walks twice as far for.
            if (payingOnly && Barren(creature.GetType()))
            {
                BarrenPassed++;

                continue;
            }

            // <b>A caster takes no quarry that does not pay for two casts (02.10.2026, build 389; Patrick's option 4).</b> Five Harm
            // scrolls at eleven gold on a cougar carrying nothing was the Mage's hour on build 386, -59gp a bot-hour: the dearest
            // consumable on the island thrown at the one kind that never pays it back. Not asked of a creature already at the
            // bot's throat, and a board's bounty on what the carcass yields (Sought) counts as pay. See BotSlay.Thrifty.
            if (casts > 0 && !ReferenceEquals(creature.Combatant, bot)
                && Pays(creature.GetType()) + Sought(creature, bot) < casts * BotSlay.CastFloor)
            {
                Dear++;

                continue;
            }

            // What it pays decides; how big it is only breaks a tie. That ordering is the whole of the fix:
            // among things a bot can beat, the useful question is which one carries something.
            var pays = Pays(creature.GetType()) + Sought(creature, bot);

            // <b>What is hostile to the hunter comes first (01.10.2026, build next; Patrick: "hunting means they hunt everything
            // that can be killed or is hostile to them, outside the towns").</b> A creature at this bot's throat, then one that
            // fights whoever comes near, then the grazing kind; within each, what it pays and then its size, as before. On build 384
            // the hunt's finishes were rats 101, dogs 74, cats 69, sheep 58, boars 52 — the board's bounty on meat and hides
            // choosing the grazing kind over the orc beside it.
            // <b>And what fights back above what does not (01.10.2026, build next; the Sonnet analyst of 386).</b> The engine makes
            // every bear, wolf, cougar, boar and hart FightMode.Aggressor — it fights what strikes it — and the cows, sheep,
            // pigs and horses FightMode.None; both read as the grazing kind here, so a wolf ranked with a cow. Patrick's order
            // puts the aggressive first and the harmless second; what fights back is the less harmless half of the second.
            var rank = !HostileFirst ? 0
                : ReferenceEquals(creature.Combatant, bot) ? 3
                : Fights(creature) ? 2
                : FightsBack(creature) ? 1
                : 0;

            if (best != null
                && (rank < bestRank || rank == bestRank && (pays < bestPays || pays == bestPays && power <= bestPower)))
            {
                continue;
            }

            // <b>And at what its kind has cost us (02.10.2026, build next; Patrick: "hunt only what you beat").</b> A kind whose record
            // prices it past this bot is not chosen, however much it pays or what the city has put on its head. Asked of a creature
            // about to become the best, as the ground is, so the count is of choices changed. See Price.
            if (power * Price(creature.GetType()) > ours * daring)
            {
                PricedOut++;

                continue;
            }

            // <b>The two questions the company's choice asks, which the lone hunter's never did.</b> On build 58 the roof
            // of Britain Graveyard was proved a pocket at 13:57:36 around (1369, 1462, 30) and at 13:58:06 around
            // (1381, 1478, 30), and six hunts after that walked at the zombie and the skeleton standing in them and failed
            // "no way through" — one hunter each time the last one's shun lapsed, 14:04 to 14:40 — sixteen hunts into the
            // graveyard's roofs in the window. Asked only of a creature about to become the best, because this loop sees
            // every creature in reach of every idle fighter. Counted apart since build 60 — see Unwelcome.
            if (HunterReadsRefusals && BotRefused.Refusing(map, creature.Location))
            {
                Unwelcome++;

                continue;
            }

            if (ShutOff(map, bot.Location, creature.Location))
            {
                Penned++;

                continue;
            }

            if (BotQuad.MuscleNear(map, creature.Location, GroundWithin) > strength)
            {
                Daunted++;

                if (ReadsGround)
                {
                    continue;
                }
            }

            best = creature;
            bestPower = power;
            bestPays = pays;
            bestRank = rank;
        }

        return best;
    }

    /// <summary>Whether a creature fights whoever comes near, rather than only what strikes it. See <see cref="Best"/> and <c>BotHunter.Propose</c>.</summary>
    internal static bool Fights(BaseCreature creature) =>
        creature.FightMode is not (FightMode.None or FightMode.Aggressor) || creature.Combatant != null;

    /// <summary>Whether a creature of the grazing kind fights what strikes it (the engine's Aggressor), rather than nothing. See <see cref="Best"/>.</summary>
    internal static bool FightsBack(BaseCreature creature) => creature.FightMode == FightMode.Aggressor;

    /// <summary>Whether a hunter passes over quarry standing in a town. A switch. See <see cref="Best"/>.</summary>
    public static bool OutsideTowns { get; set; } = true;

    /// <summary>Creatures a hunter's choice passed over for standing in a town. Counts creatures looked at, not hunts.</summary>
    public static long InTown { get; private set; }

    /// <summary>Hostile creatures a hunter's choice passed over for standing on guarded ground. Creatures looked at, not hunts.</summary>
    public static long OnGuarded { get; private set; }

    /// <summary>Whether a hunter's choice ranks what is hostile to it above the grazing kind. A switch. See <see cref="Best"/>.</summary>
    public static bool HostileFirst { get; set; } = true;

    /// <summary>Quarry a hunter was sent after, by kind: at its throat, a fighter, the grazing kind. See <see cref="Chosen"/>.</summary>
    public static long ChosenAttacking { get; private set; }

    public static long ChosenFighting { get; private set; }

    public static long ChosenGrazing { get; private set; }

    /// <summary>
    /// <b>Counted for the quarry a hunter is sent after, not for every creature weighed (01.10.2026).</b> <see cref="Sought"/>
    /// counted itself on every call, and it is asked of every creature in reach of every idle fighter on every look: "102762 kills
    /// were chosen because the board wanted what the carcass carries" in the first ten minutes of 378, against 60 hunts ended.
    /// Called by <c>BotHunter</c> when it claims its quarry.
    /// </summary>
    public static void Chosen(BaseCreature quarry, Mobile hunter)
    {
        if (Sought(quarry, hunter) > 0.0)
        {
            Sent++;
        }

        if (quarry == null)
        {
            return;
        }

        if (ReferenceEquals(quarry.Combatant, hunter))
        {
            ChosenAttacking++;
        }
        else if (Fights(quarry))
        {
            ChosenFighting++;
        }
        else
        {
            ChosenGrazing++;

            if (FightsBack(quarry))
            {
                ChosenFightingBack++;
            }
        }
    }

    /// <summary>Of the grazing kind sent after, those that fight back when struck (bears, wolves, boars). See <see cref="Chosen"/>.</summary>
    public static long ChosenFightingBack { get; private set; }

    /// <summary>
    /// Whether there is something here this bot should be setting about rather than walking away from.
    ///
    /// <para>
    /// <b>One implementation, because two of them deadlocked the bot that had them.</b> The hunter refuses to
    /// offer a fight to a bot that is already outnumbered and offers it a walk instead; the walk ends itself
    /// the moment anything worth fighting is in reach. Written separately, those two are a bot that is told
    /// to leave and told there is no reason to leave, several times a second — measured at ten round trips a
    /// second in the log, every one of them a finished undertaking and a fresh auction. The rule is the same
    /// rule in both places, so it lives in one.
    /// </para>
    /// </summary>
    public static bool Worthwhile(Mobile bot) =>
        bot != null
        && BotThreat.Decide(bot, BotMobile.NoticeRange) != BotStand.Outmatched
        && Best(bot, Reach) != null;

    /// <summary>
    /// The biggest thing within reach that this bot could <b>not</b> take alone but a company standing here
    /// could, or null.
    ///
    /// <para>
    /// <b>The gap <see cref="Best"/> deliberately refuses is exactly what this returns.</b> That one judges
    /// against what a bot can do by itself, and it has to: claims send one bot at a thing, so counting the
    /// neighbours there was writing cheques the fight could not cash. But the creatures that fall in the gap
    /// — too much for one, comfortable for four — are the ones worth the most and the ones this population
    /// has never once fought. Nothing was wrong with refusing them one at a time. What was missing was
    /// anybody asking the other question.
    /// </para>
    ///
    /// <para>
    /// Sorted by strength rather than by what the kind has paid, which is the opposite of <see cref="Best"/>
    /// and for a plain reason: a company is expensive — five bots stop doing everything else — so the case
    /// for calling one is that the thing is big, and a rat that happens to have a good record is not a case
    /// for anything. What it turns out to carry is measured on the way past like everything else.
    /// </para>
    /// </summary>
    /// <summary>Creatures a company passed over because the reach ledger had proved them shut off from where it stood.</summary>
    public static long Walled { get; private set; }

    /// <summary>
    /// Creatures a lone hunter passed over because the creature itself stood in a pocket the reach ledger had proved shut
    /// off from where the hunter stood. See <see cref="Best"/> and <see cref="ShutOff"/>. Counts the choices it changed,
    /// not the creatures looked at.
    /// </summary>
    public static long Penned { get; private set; }

    /// <summary>
    /// Creatures a lone hunter passed over for standing on ground the refusal memory holds. Apart from
    /// <see cref="Penned"/>, because build 59 counted both refusals in one number and could not say which did the
    /// graveyard's work — or cost the hunts. Asked only while <see cref="HunterReadsRefusals"/> is on.
    /// </summary>
    public static long Unwelcome { get; private set; }

    /// <summary>
    /// Whether a lone hunter reads the refusal memory about its quarry, as a company does. A switch, so it can be taken off
    /// a running shard and put back: build 59, which added it, finished 316 hunts in its window against 337 to 440 in the
    /// day's others — one window, which is why this is a switch and not a revert.
    /// </summary>
    public static bool HunterReadsRefusals { get; set; } = true;

    /// <summary>
    /// Whether a hunter reads the strength its quarry's ground asks, as a hunting ground is read when it is chosen.
    ///
    /// <para>
    /// <b>Patrick's order of 03.09.2026, asked where it was not.</b> Below the strength a square asks a bot does not go there,
    /// whatever the work is worth — asked of a hunting ground (<c>BotQuad.Dares</c> in <c>BotHunter</c>) and never of a
    /// creature a bot sets about where it stands, which this weighed against the hunter alone. Build 107 counted it: 28 of
    /// 280 hunts begun between 14:41 and 15:16 on 17.09.2026 were at a quarry on ground asking more than the hunter brought,
    /// while the day's lone hunting deaths with a reading beside them were mostly there — Faron Ashdown at (1971, 819) after
    /// a scorpion, ground asking 10000 of his 2513; Ilsa at (723, 1925) after a giant spider, 5000 of 2697; Edda Ashdown led
    /// by six hunts into the brigands' camp, 9500 of 2450. Britain Graveyard, the busiest hunting ground, read 0.90 to 1.00
    /// at the same hour and asks nothing. A switch, for the reason <see cref="HunterReadsRefusals"/> is one.
    /// </para>
    /// </summary>
    public static bool ReadsGround { get; set; } = true;

    /// <summary>
    /// How far round the quarry the ground is read: the most any square within this many tiles asks. Twelve, the distance
    /// at which a creature notices somebody; nought reads the quarry's own square alone.
    ///
    /// <para>
    /// <b>A fight is drawn onto the square next door.</b> On build 108 Wystan prowled to (864, 1696), beside the brigands'
    /// camp, hunted Renaldo on the camp's neighbouring square — which asked nothing, so the quarry's own square let him —
    /// and died at (840, 1672) on the camp's, which asked 6000 of his 2052 (15:53:25, 17.09.2026).
    /// </para>
    /// </summary>
    public static int GroundWithin { get; set; } = 12;

    /// <summary>Creatures a lone hunter passed over for standing on ground that asks more strength than it brings. Counts choices changed.</summary>
    public static long Daunted { get; private set; }

    /// <summary>
    /// Whether the creature itself stands in a pocket the reach ledger has proved shut off from where the bot stands.
    ///
    /// <para>
    /// <b>The creature's own cell, not the ground around it.</b> <c>BotReach.Ask</c> sweeps the cells within the arrival
    /// it is given — up to two tiles — and answers Unknown at the first settled cell nobody has filed, and the street under
    /// a roof's edge is such a cell. So asked with a hunter's reach it could not call a creature on a narrow roof shut off:
    /// on build 59 the 72-tile roof pocket of Britain Graveyard was proved at 15:09:48 around the tile a zombie stood on,
    /// and at 15:27:33 Oswin walked at the zombie on that same tile and failed "no way through". A creature on a roof is
    /// fought on the roof; whether a body could stand within two tiles of it in the street below is not the question.
    /// Asked the same way for a lone hunter and for a company, so the two cannot come apart again.
    /// </para>
    /// </summary>
    private static bool ShutOff(Map map, Point3D from, Point3D at) =>
        BotReach.Ask(map, from, at, BotArrival.Exactly) == BotReachVerdict.Sealed;

    /// <summary>Creatures a company passed over for swimming where no body can stand. See <see cref="Company(Mobile, int, out CompanyRefusal)"/>.</summary>
    public static long Afloat { get; private set; }

    public static BaseCreature Company(Mobile bot, int range) => Company(bot, range, 0.0, out _);

    /// <summary>The same at the given odds: a company's own choice gate (<c>BotSquad.ChoiceOdds</c>, 06.10.2026). Nought is <c>BotThreat.Tolerance</c>.</summary>
    public static BaseCreature Company(Mobile bot, int range, double odds) => Company(bot, range, odds, out _);

    public static BaseCreature Company(Mobile bot, int range, out CompanyRefusal why) => Company(bot, range, 0.0, out why);

    /// <summary>Why no company could be called, when none could. See <see cref="Company"/>.</summary>
    public enum CompanyRefusal
    {
        /// <summary>One was found. Nothing was refused.</summary>
        None,

        /// <summary>Nobody of ours is near enough to make a company at all.</summary>
        Alone,

        /// <summary>Everything here is already one bot's work.</summary>
        AllSmall,

        /// <summary>Something is here and it is beyond what everybody standing here could take.</summary>
        AllTooBig,

        /// <summary>Nothing hostile is here, or it is claimed or being left alone.</summary>
        Nothing
    }

    /// <summary>
    /// The same, saying which gate closed when it hands back nothing.
    ///
    /// <para>
    /// <b>Three different findings shared one silence, and the middle one was the answer.</b> The plain
    /// overload returns null for "there is nobody with me", "everything here is small" and "the only thing
    /// here would kill all of us", and <see cref="BotMuster"/> filed all three under <em>found nothing beyond
    /// one bot</em> — nine hundred and eighty-one of them in twenty minutes, which I read out as proof that
    /// the ground held nothing worth a company. It was not proof of anything. The very first check returns
    /// before a single creature is looked at, so a field full of ogres and a field full of rats produce the
    /// identical number, and the population walked past ogres all evening while the log agreed there was
    /// nothing to fight.
    /// </para>
    /// </summary>
    public static BaseCreature Company(Mobile bot, int range, double odds, out CompanyRefusal why)
    {
        why = CompanyRefusal.Nothing;

        var map = bot?.Map;

        if (map == null || map == Map.Internal)
        {
            return null;
        }

        // The company's gate: the choice odds when the caller is a company choosing (06.10.2026, company16), the standing tolerance
        // otherwise. Never looser than the tolerance.
        var gate = odds > 0.0 ? Math.Min(odds, BotThreat.Tolerance) : BotThreat.Tolerance;

        var alone = BotThreat.Power(bot);

        if (alone <= 0.0)
        {
            return null;
        }

        var together = BotThreat.OurPower(bot, range);

        // Nobody of ours is near enough to be counted. There is no company to call.
        if (together <= alone)
        {
            why = CompanyRefusal.Alone;

            return null;
        }

        BaseCreature best = null;
        var bestPower = 0.0;
        var bestLetIn = false;
        var small = 0;
        var big = 0;

        foreach (var creature in map.GetMobilesInRange<BaseCreature>(bot.Location, range))
        {
            if (!BotThreat.Hostile(bot, creature) || !BotPopulation.Within(map, creature.Location, bot))
            {
                continue;
            }

            if (!Ours(bot, creature) || Shunned(creature))
            {
                continue;
            }

            // <b>And not a creature out in the water, which was most of the fights a company never got near.</b>
            // Over the night of 14.09.2026 companies broke off "we never got near it" 131, 65, 47 and 75 times a
            // session, and the commonest names were a water elemental, a sea serpent, a deep sea serpent and a
            // kraken. Squad 18 on build 42 was handed six of them in a row from 23:46 on a beach east of Britain,
            // every one 37 to 109 tiles off, while its five members stood on their stations for four minutes, Lorcan
            // was carried home and Fenna let go. Each is shunned after its half-minute, and the next swimmer is
            // offered: a swimmer on a tile no body can settle on is a fight nobody reaches from any side.
            if (creature.CanSwim && !BotStep.Settle(map, creature.X, creature.Y, out _))
            {
                Afloat++;

                continue;
            }

            // <b>And not one the ledger has proved is shut off from here.</b> The lone hunter's choice asks
            // this too since build 59 (see Best) — before that it asked neither, whatever this line used to say; the
            // company's did not, and a company would be pointed at a creature in a
            // fenced yard north of Britain, every member's station refused a dozen times, and the leader
            // carried home — six companies on the night of 13.09.2026, two of them inside three minutes at
            // 05:09 and 05:12, drawn back by the same quarry at 1319,1047 that the far side had already
            // called Enclosed. What is free is what somebody has already proved.
            if (ShutOff(map, bot.Location, creature.Location))
            {
                Walled++;

                continue;
            }

            // And not ground the refusal memory or the barred list says no to — the hunter's own choice asks
            // this at three places; the company's did not, so a box in bot-movement.json fenced hunters and
            // not companies.
            if (BotRefused.Refusing(map, creature.Location))
            {
                Walled++;

                continue;
            }


            var power = BotThreat.Power(creature);

            // Weighed at what its kind has cost us (02.10.2026, build next; Patrick: "a company may take what a solo bot may not",
            // and nobody takes what it does not beat). See Price. One for every kind that has not killed past its share.
            var priced = power * Price(creature.GetType());

            // Already one bot's work. Calling four more to a thing one of them was going to take anyway is
            // how the first version ended up with six bots declaring a band against a skeleton. Asked at the island's price, the
            // lone hunter's own (03.10.2026): a kind the company record prices lower is still no lone bot's work.
            if (priced <= alone * Daring)
            {
                small++;

                continue;
            }

            // Beyond the company too. The tolerance is the one the standing decision uses, because that is
            // the decision the squad will actually be making once it is in the fight. At the kind's company price (03.10.2026,
            // Patrick: "if they kill it, let them gather"): what companies as strong as this one have beaten. See BotCompanyRecord.
            var forCompany = power * BotCompanyRecord.Price(creature.GetType());

            if (forCompany > together * gate)
            {
                big++;

                // Within the company's strength by the kind's own, beyond it by what the kind has cost us: the price refused it.
                if (power <= together * gate)
                {
                    CompanyPricedOut++;
                }

                // Within the standing tolerance, beyond the company's own choice gate (company16): the band 1.0–1.5 the deaths were in.
                if (forCompany <= together * BotThreat.Tolerance)
                {
                    CompanyChoiceOut++;
                }

                continue;
            }

            if (power <= bestPower)
            {
                continue;
            }

            best = creature;
            bestPower = power;

            // The island's price would have turned it away; companies' own fights let it in. Counted for the one chosen.
            bestLetIn = priced > together * gate;
        }

        if (best != null)
        {
            why = CompanyRefusal.None;

            if (bestLetIn)
            {
                BotCompanyRecord.Looked();
            }
        }
        else if (big > 0)
        {
            why = CompanyRefusal.AllTooBig;
        }
        else if (small > 0)
        {
            why = CompanyRefusal.AllSmall;
        }

        return best;
    }

    /// <summary>
    /// The corpse of this creature, if it is lying where it fell.
    ///
    /// Found by looking rather than by being told, because the engine's kill hook lives on the creature and we
    /// do not own creatures. Cheap because the place is known: two tiles around where the thing was standing
    /// the last time it was alive.
    /// </summary>
    public static Corpse Remains(Map map, Point3D where, Mobile fallen)
    {
        if (map == null || map == Map.Internal || fallen == null)
        {
            return null;
        }

        foreach (var item in map.GetItemsInRange(where, LootReach))
        {
            if (item is Corpse corpse && !corpse.Deleted && corpse.Owner == fallen)
            {
                return corpse;
            }
        }

        return null;
    }
}
