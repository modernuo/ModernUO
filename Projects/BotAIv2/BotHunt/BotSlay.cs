using System;
using Server.Logging;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Close, fight, go through what is left. The fighter's chain, and the first work in this project that brings
/// new gold into the world.
///
/// <para>
/// <b>Its stages are its own business, and the third one is the point of it.</b> A hunt that ends when the
/// thing falls over leaves a bot standing next to everything it earned. Mining learned this first — an
/// undertaking that ends at the vein leaves a bot underground holding rock — and it is the same lesson: the
/// work is not the fight, it is the fight <em>and getting paid for it</em>.
/// </para>
///
/// <para>
/// <b>The fighting itself is entirely the engine's.</b> Setting <c>Combatant</c> starts a server-side timer
/// that swings the weapon, rolls to hit, applies damage and wears the blade down — no client involved and
/// nothing simulated. So this file never decides a blow. What it decides is when to stop, and that is the one
/// judgement it must not get wrong.
/// </para>
///
/// <para>
/// <b>It runs away, and it has to, because nothing above it will.</b> The rung for a bot that is losing has no
/// proposer, so the brain's answer to failing health is to hold on to whatever the bot is already doing — and
/// what this bot is already doing is being killed. The first version's sharpest lesson was that flight must
/// outrank everything social; here it also has to outrank the undertaking's own stubbornness. Giving up marks
/// the place with caution, which is exactly the right record: this patch of ground kills me.
/// </para>
/// </summary>
public sealed class BotSlay : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSlay));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "hunt";

    /// <summary>
    /// What a fight is reckoned at per minute before experience corrects it.
    ///
    /// <para>
    /// Deliberately conservative. A fight ought to pay well — coin off the corpse plus real weapon skill,
    /// which at five hundred a point dominates the coin — and a first guess at what a monster carries is a
    /// guess about content nobody here has measured. Sixty puts it level with writing scrolls rather than
    /// above everything, so the ledger gets to raise it from evidence instead of the whole population
    /// stampeding into the field on the strength of a number I chose.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 60.0;

    /// <summary>How long one hunt is expected to take, walk and all.</summary>
    public static double WorkMinutes { get; set; } = 3.0;

    /// <summary>
    /// The share of its health at which a hunter gives the fight up.
    ///
    /// <para>
    /// Forty per cent, and the number matters less than that it exists at all. The first version died four
    /// hundred and forty-three times in a night, a hundred and four of them the same bot in the same tile,
    /// because nothing ever decided that a fight was going badly. A bot that walks away at forty per cent
    /// keeps its gear, its skills and the three minutes that dying costs the ledger.
    /// </para>
    /// </summary>
    public static double FleeAt { get; set; } = 0.4;

    /// <summary>
    /// How far a bot will be drawn from where the chase began before it gives the quarry up.
    ///
    /// <para>
    /// <b>Patrick's order of 09.09.2026: if the quarry runs and the bot cannot reach it, drop it after fifty
    /// tiles.</b> The chase had a clock — <see cref="CapMs"/> — and no length, and a clock does not bound a
    /// bot's position: a quarry that walks away as fast as the bot walks after it is a bot travelling in a
    /// straight line for as long as the cap allows.
    /// </para>
    ///
    /// <para>
    /// It shows in the rescues. Fourteen bots carried home in an hour on 09.09.2026, all of them in bursts of
    /// two to six within one minute; the six at 17:34 were standing in one ten-tile patch at (1208, 2202) —
    /// <b>737 tiles from home, where <c>BotPopulation.Roam</c> is 200</b>. A company had chased something off
    /// the edge of the ground the population is allowed to choose work on, and got stuck there together. The
    /// roam limit governs what a bot may <em>choose</em>; nothing governed what a chase could <em>drag</em> it
    /// into.
    /// </para>
    ///
    /// <para>
    /// Measured from where the chase started rather than from home, because that is the thing this bot
    /// actually decided: the walk out to a quarry is priced by the decision layer and is legitimate, and what
    /// is not legitimate is that walk turning into an unbounded one once the quarry starts running.
    /// </para>
    /// </summary>
    public static int Leash { get; set; } = 50;

    /// <summary>Chases given up for running past the leash.</summary>
    public static long Slipped { get; private set; }

    /// <summary>Hunts begun, lone or in company. The denominator for <see cref="Undaring"/>.</summary>
    public static long Begun { get; private set; }

    /// <summary>
    /// Hunts begun at a quarry standing on ground that asks more strength than the hunter brings — counted, not refused.
    ///
    /// <para>
    /// <b>The ground's wall stands at the prowl and not at the quarry.</b> Patrick's order of 03.09.2026 — below the strength
    /// a square asks, a bot does not go there — is asked of a hunting ground when it is chosen (<c>BotQuad.Dares</c> in
    /// <c>BotHunter</c>), and never of a creature a bot sets about where it stands: <c>BotQuarry.Best</c> weighs the creature
    /// alone. On 17.09.2026, 31 of the day's 91 deaths came out of a hunt, among them Faron Ashdown at (1971, 819) after a
    /// scorpion on ground asking 10000 of his 2513, Ilsa at (723, 1925) after a giant spider on ground asking 5000 of 2697,
    /// and Edda Ashdown, who hunted a wolf, a horse, a hind, a pig, a raven and a boar into the brigands' camp at (841,
    /// 1693) — ground asking 9500 of her 2450 — and fell to a brigand. Counted first, because the ground's reading counts
    /// heads: the graveyard's skeletons would close the island's busiest hunting ground to every lone bot, and how many
    /// hunts a wall there would stop is not yet known.
    /// </para>
    /// </summary>
    public static long Undaring { get; private set; }

    /// <summary>Where the chase began. Point3D.Zero until the first beat of the closing leg.</summary>
    private Point3D _began;

    /// <summary>How far the quarry was from where the chase began: the priced walk out, which the leash is on top of.</summary>
    private int _outward;

    private bool _begun;

    /// <summary>Whether this hunt has been counted begun: a leash measured again after a jump is not a second hunt.</summary>
    private bool _counted;

    /// <summary>Where the bot stood on the last closing beat, to tell a jump from a walk. See <see cref="JumpTiles"/>.</summary>
    private Point3D _last;

    /// <summary>Whether this hunt has reached the fight leg at least once: a quarry already fought is not let go by the leash.</summary>
    private bool _fought;

    private bool _pastLeash;

    /// <summary>Where the quarry's running is measured from: where it was found, or where it stood when the hunt was taken up again.</summary>
    private Point3D _ran;

    /// <summary>
    /// How far beyond the walk out, in leashes, the hunter itself may be drawn after a quarry it is fighting or that runs
    /// inside the leash (01.10.2026, build next). Three: a hundred and fifty tiles, against the 737 that wrote the leash.
    /// </summary>
    public static int FarLeash { get; set; } = 3;

    /// <summary>A move between two beats larger than this is a gate, a carry or a resumption, and the leash is measured again.</summary>
    public static int JumpTiles { get; set; } = 40;

    /// <summary>Leashes measured again after a jump; hunts let go past the far bound; hunts that went past the old leash and were kept.</summary>
    public static long Rebased { get; private set; }

    public static long Dragged { get; private set; }

    public static long Unleashed { get; private set; }

    /// <summary>How full a pack may get with loot before the rest is left on the corpse.</summary>
    public static double FillFraction { get; set; } = 0.8;

    /// <summary>
    /// How often, mid-fight, a bot works out whether it is losing on numbers rather than on health.
    ///
    /// A second. The check is a spatial sweep and this runs on the bot's own turn, which comes round two to
    /// five times a second — so unthrottled it would be the most expensive thing a fighting bot does, for an
    /// answer that cannot change meaningfully inside a swing.
    /// </summary>
    public static int OddsMs { get; set; } = 1000;

    /// <summary>
    /// How long one bot keeps at a quarry whose health will not fall.
    ///
    /// <para>
    /// <b>A squad has had this exit since it was written and a lone hunter never had one at all.</b> The
    /// company gives up after ninety seconds without the target's health moving, and caps a fight at four
    /// minutes, because anything it cannot kill would otherwise hold five bots for ever — that lesson is
    /// written out at length on <see cref="BotSquad.NoProgressMs"/>, and it was never carried across to the
    /// undertaking that holds <em>one</em> bot. So the only ways out of a solo fight were victory, forty per
    /// cent health, or being outnumbered; a quarry that could neither hurt the bot nor be hurt by it matched
    /// none of them. Joss took a zombie at 21:08:51 on 24.08.2026 and was still holding it twenty-two minutes
    /// later, having finished one piece of work all evening — not idle, bound, and reading in every summary
    /// as a bot busy hunting.
    /// </para>
    ///
    /// <para>
    /// Shorter than the squad's, because one bot has less to bring: what five of them cannot shift in ninety
    /// seconds, one of them will not shift in that time either.
    /// </para>
    /// </summary>
    public static int NoProgressMs { get; set; } = 45000;

    /// <summary>
    /// The ceiling on one solo hunt, walking and fighting together.
    ///
    /// Measured from the moment the undertaking was taken rather than from first contact, because the other
    /// way a bot can spend it all walking and still be entitled to the whole of it again on arrival.
    /// </summary>
    public static int CapMs { get; set; } = 150000;

    /// <summary>
    /// How long a bot stands in a fight it cannot land a blow in before it gives the quarry up.
    ///
    /// <para>
    /// <b>"Near enough" and "able to hit it" are different questions, and only the first was ever asked.</b>
    /// Arrival is <c>InRange</c>, which is a flat box on X and Y; whether a blow may land is the engine's,
    /// and it wants line of sight in three dimensions. On a graveyard full of crypts those come apart
    /// constantly: a bot standing on a roof at z=20 is one tile from a skeleton on the ground below it,
    /// takes the fight, sets its combatant, and swings at nothing. Three of Merrick's fights on 24.08.2026
    /// ended at a minute with the target on a hundred per cent of its health, and every number in the line
    /// was correct — the distance, the combatant, the elapsed time. Nothing measured the one thing that was
    /// wrong.
    /// </para>
    ///
    /// <para>
    /// A few seconds rather than instantly, because a creature stepping behind a tombstone breaks the line
    /// for a moment and is not a reason to abandon anything.
    /// </para>
    /// </summary>
    public static int BlindMs { get; set; } = 6000;

    /// <summary>
    /// How long a shooter must have been standing before the engine will let the bow go off.
    ///
    /// <para>
    /// Not a choice — <c>BaseRanged.OnSwing</c> refuses the swing unless <c>LastMoveTime</c> is this old, and
    /// on this era that is a full second. It is written down here because a kite that does not know this
    /// number is a bot that moves for ever and never fires.
    /// </para>
    /// </summary>
    public static int StillMs { get; set; } = 1000;

    /// <summary>
    /// Slack on top of the stillness, to cover the walk itself and the beat it is decided on. A step is a
    /// fifth of a second at a run and the bot is asked for a decision two to five times a second, so a
    /// shooter that starts moving with exactly a second left arrives late and loses the shot it was
    /// repositioning for.
    /// </summary>
    public static int KiteSlackMs { get; set; } = 700;

    /// <summary>
    /// How far above the ground a pair of eyes sits, for asking whether a place a bot is not standing in yet
    /// could see the quarry.
    ///
    /// <para>
    /// Not a choice either: <c>Map.LineOfSight</c> raises both ends by this before tracing, and a line traced
    /// from the floor instead answers a different question — it clips on the first step of ground it crosses
    /// and reports every open field as blind. Taken from the engine so the two agree.
    /// </para>
    /// </summary>
    private const int Eye = 14;

    /// <summary>
    /// How close something may come before a bot that shoots gives ground.
    ///
    /// <para>
    /// <b>An archer had no reason to stand anywhere in particular, so it stood in arm's reach.</b> A bow
    /// carries ten tiles and a crossbow eight, and the approach was written as "beside it" — one tile — so a
    /// bot walked the whole way in and then shot from a distance at which its bow is worth nothing and
    /// everything can hit it back. Watched from a client it reads exactly as what it is: archers firing point
    /// blank.
    /// </para>
    ///
    /// <para>
    /// Three tiles, because two is where a creature is already swinging and four is close enough that a step
    /// backwards is not worth the beat it costs.
    /// </para>
    /// </summary>
    public static int TooClose { get; set; } = 3;

    /// <summary>
    /// How near the quarry has to be before an archer spends its idle window opening the distance.
    ///
    /// <para>
    /// <b>Patrick, 09.09.2026: "why the hell do archers always run away from mobs? They should kite, yes —
    /// but if they can shoot they MUST shoot."</b> He was right and the fault was one condition.
    /// <see cref="Kiting"/> asked whether the quarry was within <c>reach - 1</c>, and a bow reaches ten — so
    /// an archer with a creature <em>nine tiles away</em>, which is the distance it exists to fight at, read
    /// that as "something has closed on me" and backed off. Measured over one session: <b>1,514 kites against
    /// 860 shots</b>, so an archer spent nearly two beats in three walking backwards from things that were
    /// already exactly where it wanted them.
    /// </para>
    ///
    /// <para>
    /// <b>Five, and four was measured wrong within the hour.</b> Four is one past <see cref="TooClose"/>, and
    /// taking it from there was tidy and too tight: over thirty minutes the tally read <b>1,537 archer asks,
    /// every one of them "far enough off to simply shoot", and not one kite</b>. Something inside three tiles
    /// is already handled by the branch further down, so a gate at four left the kite a single tile wide and
    /// it never came up. Half of what Patrick asked for — *they should kite, yes* — had gone inert.
    /// </para>
    ///
    /// <para>
    /// Five is where an archer has lost half the ten tiles its bow is worth, which is a statement about its
    /// advantage rather than about the tidiness of the number. And widening cannot cost a shot by
    /// construction: the kite only runs while the bow is reloading — see the clock in <see cref="Kiting"/> —
    /// so the thing the old nine-tile gate actually wasted was position and time, not arrows.
    /// </para>
    ///
    /// <para>
    /// The retreat itself is unchanged — <see cref="Away"/> still opens to the full reach — so an archer that
    /// is genuinely crowded still backs off as far as it ever did. What changed is that it stops doing it to
    /// things that are not crowding it.
    /// </para>
    /// </summary>
    public static int KiteWithin { get; set; } = 5;

    /// <summary>
    /// Where a bot with this reach would rather stand: comfortably inside its own range and outside anybody
    /// else's. Two short of the weapon's own maximum, so a target drifting a tile does not put it out of the
    /// fight. For a melee weapon this is one, which is where it always was.
    /// </summary>
    private static int Standoff(int reach) => Math.Max(1, reach - 2);

    private enum Leg
    {
        Close,
        Fight,
        Spoils
    }

    private readonly BaseCreature _quarry;

    /// <summary>What this hunt is after. Read when a guard takes it (BotHaven).</summary>
    public BaseCreature Quarry => _quarry;

    private readonly Map _map;

    private readonly Point3D _found;

    private readonly SkillName _trains;

    private Leg _leg;

    private Point3D _fell;

    private int _made;

    private int _taken;

    private int _coins;

    private int _casts;

    /// <summary>How many casts one of this quarry's kind pays for, reckoned once; -1 until asked. See <see cref="Thrifty"/>.</summary>
    private int _castCap = -1;

    private bool _thrifted;

    /// <summary>How much leather came off the last thing this undertaking killed.</summary>
    private int _hides;

    /// <summary>
    /// The spot this bot is presently backing away to, held until it gets there.
    ///
    /// <para>
    /// <b>Recomputed every beat, it was an order that never survived long enough to be obeyed.</b> The
    /// retreat point is worked out from where the quarry is standing, and the quarry moves — so every beat
    /// produced a slightly different destination, BotWill saw an order that did not match the last one,
    /// rebased the journey and threw the plan away. The bot shuffled on the spot for as long as the fight
    /// lasted. That is why a mage with something in its face never seemed to cast: casting comes after the
    /// retreat in this method, and the retreat never finished.
    /// </para>
    ///
    /// <para>
    /// Kept until it is reached or goes bad, so the order is identical beat after beat and the journey lives.
    /// The same lesson as the archer's line-seeking walk, and the same one BotWill's own comment gives beside
    /// the rebase: an undertaking that re-words its walk every beat never gets anywhere.
    /// </para>
    /// </summary>
    private Point3D _backing;

    /// <summary>Whether a cast of ours is waiting for something to be pointed at.</summary>
    private bool _aiming;

    private bool _cast;

    private long _castTick;

    /// <summary>When the odds were last worked out, and whether they ever have been. See <see cref="OddsMs"/>.</summary>
    private long _oddsTick;

    private bool _weighed;

    private bool _complained;

    /// <summary>The lowest health this quarry has been seen at, and when that was. See <see cref="NoProgressMs"/>.</summary>
    private int _lowest = int.MaxValue;

    /// <summary>When the quarry was last somewhere a blow could actually reach. See <see cref="BlindMs"/>.</summary>
    private long _seenTick;

    /// <summary>When this bot's hands were last looked at. See <see cref="BotArms.EveryMs"/>.</summary>
    private long _armedTick;

    private long _progressTick;

    private long _tookTick;

    public BotSlay(BaseCreature quarry, SkillName trains)
    {
        _quarry = quarry;
        _map = quarry.Map;
        _found = quarry.Location;
        _ran = quarry.Location;
        _fell = quarry.Location;
        _trains = trains;

        // Seeded from a real tick, never left at zero: these counters can start enormous and wrap.
        _tookTick = Core.TickCount;
        _progressTick = Core.TickCount;
        _seenTick = Core.TickCount;
        _fightTick = Core.TickCount;
        _armedTick = Core.TickCount - BotArms.EveryMs;
    }

    public override string Kind => Trade;

    /// <summary>Goes where the fighting is on purpose. See <see cref="BotDeed.Braves"/>.</summary>
    public override bool Braves => true;

    /// <summary>
    /// Put down for a rescue or a flight and taken up again, never held against an offer: the quarry is asked about
    /// afresh on the first beat back (dead → spoils, walked off → the chase and its leash). See <see cref="BotDeed.Resumes"/>.
    /// </summary>
    public override bool Resumes => true;

    /// <summary>Kites this hunt made. See <see cref="Kiting"/>: an instrument for the archer that fails "not a scratch".</summary>
    private int _kited;

    /// <summary>Back-off walks this hunt ordered, falls in the quarry's health it saw, and beats it stood for an arrow that was due.</summary>
    private int _backed;

    private int _hits;

    private int _dues;

    /// <summary>
    /// Where the last retreat — a kite or a back-off — was ordered to; nought when none is. A retreat is the one walk this
    /// hunt orders that a later beat must be able to end: BotWill leaves a walk running under every Work answer that follows
    /// it (work that walks and works by turns keeps its walk), so a back-off ordered on one beat was walked through the next
    /// forty. Held as the point rather than a flag, because a flag lied (review A/B, 12.10.2026): a retreat that arrived on
    /// its own left it set, and the next stand on an "after X" walk or a reflex's interruption counted a retreat ended that
    /// was no retreat. The point is matched against the journey's own bottom errand in <see cref="Stand"/>.
    /// </summary>
    private Point3D _retreatTo;

    /// <summary>When the fight leg last began, for <see cref="RetreatMs"/>: a resumed hunt's walk back is not a retreat.</summary>
    private long _fightTick;

    /// <summary>
    /// Whether this fight is not given up for the bot's own health (<see cref="FleeAt"/>) nor for the odds: a stand, taken by a
    /// bot that was refused the fight for the odds and has no flight on offer (12.10.2026, w5; until then the odds ended it on
    /// its first beat). See <c>BotDefender.Stand</c>.
    /// </summary>
    public bool ToTheEnd { get; init; }

    private bool _stood;

    /// <summary>Whether a stand has had the muster urged for it on the odds. Once a stand. See <see cref="Advance"/>.</summary>
    private bool _urged;

    /// <summary>The clocks inside the hunt are put right, or the pause itself reads as a stall and a cap. See <see cref="BotDeed.Steadfast"/>.</summary>
    public override void Resumed(IBotWilful bot)
    {
        var now = Core.TickCount;

        _tookTick = now;
        _progressTick = now;
        _seenTick = now;
        _leg = Leg.Close;

        // The walk back to the quarry from wherever the interruption left the bot is a walk out like the first one, and the
        // leash is measured again from there (01.10.2026, build next). See JumpTiles.
        _begun = false;
        _crowdedTick = 0;
        _retreatTo = Point3D.Zero;

        if (_quarry is { Deleted: false } quarry)
        {
            _ran = quarry.Location;
        }
    }

    /// <summary>
    /// <b>A hunt whose quarry is far away now is not taken up again (01.10.2026, build next).</b> Put down for a company's call,
    /// a flight or a mend, the bot comes back to its hunt wherever that left it — through a gate, down a dungeon, carried home
    /// — and the first closing beat failed it "drew it 2909 tiles past where it was found". A quarry further than the hunter's
    /// reach and a leash from the bot, on another map, or gone, is no longer this hunt's; the work is settled not taken up again.
    /// </summary>
    public override string Stale(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || body.Map != _map)
        {
            return null;
        }

        // A quarry that went down meanwhile is the corpse's place: worth the walk only if it is near.
        var down = Down();
        var at = down ? _fell : _quarry.Location;
        var away = Math.Max(Math.Abs(body.X - at.X), Math.Abs(body.Y - at.Y));

        if (away <= BotQuarry.Reach + Leash)
        {
            return null;
        }

        StaleResumes++;

        return down ? $"{_quarry?.Name ?? "the quarry"} went down {away} tiles off" : $"{_quarry.Name} is {away} tiles off now";
    }

    /// <summary>Hunts begun by a bot holding under <see cref="BotHunter.PoorBelow"/>.</summary>
    public static long PoorBegun { get; private set; }

    /// <summary>Whether a caster's casts at a quarry are capped by what one of its kind pays. See <see cref="Thrifty"/>.</summary>
    public static bool CastsByGold { get; set; } = true;

    /// <summary>The fewest casts a caster throws at any quarry before the cap is asked: enough to learn what the kind carries.</summary>
    public static int CastFloor { get; set; } = 2;

    /// <summary>Hunts left because the casts had spent what the quarry pays.</summary>
    public static long Thrifted { get; private set; }

    /// <summary>Hunts not taken up again because the quarry was far away by then. See <see cref="Stale"/>.</summary>
    public static long StaleResumes { get; private set; }

    /// <summary>What it is after. See <see cref="BotDeed.Foe"/>.</summary>
    public override Mobile Foe => _quarry;

    public override Map Map => _map;

    /// <summary>
    /// Where it was found, for the whole life of the undertaking — never where it has wandered to since.
    ///
    /// This is what the ledger files the outcome under, and the thing being learned is whether hunting this
    /// patch of ground pays. A quarry that led a bot half a mile away and died there would otherwise teach it
    /// that the place it ended up is rich in monsters.
    /// </summary>
    public override Point3D Where => _found;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    /// <summary>
    /// Whatever this bot actually swings, handed in by the proposer out of the bond.
    ///
    /// The class offered six blades and the roll picked one, <em>together with the skill that swings it</em> —
    /// so the answer is a fact about this bot, not about its class. Empty hands are the brawler's whole point,
    /// and wrestling is what those train.
    /// </summary>
    public override SkillName? Trains => _trains;

    public override int Outlay => 0;

    /// <summary>Mostly coin, and this is the only work in the project of which that has ever been true.</summary>
    public override double Coin => 1.0;

    /// <summary>Goods off the corpse that went to the market. The gold is counted as gold.</summary>
    public override int Made => _made;

    /// <summary>
    /// A creature this close will not keep, so this offer may jump the dwell.
    ///
    /// <para>
    /// <b>The whole of the answer to "why did it run straight past that".</b> A bot that has taken something
    /// on is protected from changing its mind for half a minute, and rightly — but the protection was being
    /// read as "do not look", so an ogre could walk across a miner's path and the miner would not be asked
    /// about it until it had finished the vein. A place keeps for half a minute. A creature does not: it is
    /// somewhere else, claimed, or standing over the bot by then.
    /// </para>
    ///
    /// <para>
    /// Bounded by <see cref="BotQuarry.Notice"/> rather than by the reach, because this is the "already in my
    /// lap" question and not the "worth a walk" one, and only while the fight has not started — once it has,
    /// the deed is the bot's own work and needs no help jumping anything.
    /// </para>
    /// </summary>
    public override bool Pressing(IBotWilful bot)
    {
        var body = bot?.Self;

        return body != null && !Down() && body.InRange(_quarry.Location, BotQuarry.Notice);
    }

    public override string Stage => _leg switch
    {
        Leg.Close => $"after {_quarry?.Name ?? "something"}",
        Leg.Fight => _casts > 0
            ? $"fighting {_quarry?.Name ?? "something"} ({_casts} spells)"
            : $"fighting {_quarry?.Name ?? "something"}",
        _ => _taken > 0 ? $"took {_taken} things and {_coins}gp" : "going through the corpse"
    };

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        return _leg switch
        {
            Leg.Close => Closing(bot, body),
            Leg.Fight => Fighting(bot, body),
            _ => Looting(bot, body)
        };
    }

    private BotDoing Closing(IBotWilful bot, Mobile body)
    {
        if (Down())
        {
            _leg = Leg.Spoils;

            return Looting(bot, body);
        }

        // The ceiling applies to the chase as well, and the chase is where it is most needed: a quarry that
        // walks away as fast as the bot walks after it never fails a journey and never comes into reach, so
        // nothing else here would ever end the undertaking.
        if (Core.TickCount - _tookTick >= CapMs)
        {
            BotQuarry.Crowd(_quarry);

            return BotDoing.Failed($"could not catch {_quarry.Name}");
        }

        // Where this chase started, taken on its first beat rather than in the constructor: an undertaking is
        // created wherever the auction happened to run and the walk out to the quarry is a legitimate,
        // priced journey. What the leash bounds is how far the quarry may then drag the bot. See Leash.
        //
        // <b>And the leash is on top of that walk, which it was not.</b> The leash was measured from where the bot stood on
        // this first beat, so the walk out to a quarry found at the edge of the hunt's look was charged to it: on build 96
        // (22:03–22:38, 16.09.2026) 19 hunts failed "drew it 51 tiles and was let go", most within a quarter of a minute,
        // Hollis four times in 48 seconds and Orin Ashdown three times in 29, each failure followed by the offer of another
        // quarry as far away. What the quarry may drag the bot is the leash beyond where the quarry was found.
        if (!_begun)
        {
            _begun = true;
            _began = body.Location;
            _last = body.Location;
            _outward = Math.Max(Math.Abs(_found.X - _began.X), Math.Abs(_found.Y - _began.Y));

            if (!_counted)
            {
                _counted = true;
                Begun++;

                // Counted for the standing rule that a bot without money hunts. See BotHunter.PoorBelow.
                if (BotYield.Spendable(body) < BotHunter.PoorBelow)
                {
                    PoorBegun++;
                }

                // See Undaring: counted, and nothing refused.
                var asked = BotQuad.Muscle(_map, _found);

                if (asked > 0.0 && asked > BotQuad.Strength(body))
                {
                    Undaring++;
                }
            }
        }

        // <b>A jump is not a chase (01.10.2026, build next).</b> A bot carried through a gate, put down for a company's call and
        // taken up again elsewhere, or set on its feet by a rescue moves hundreds of tiles between two beats, and the leash read
        // that as the quarry having dragged it: 49 of the 234 "drew it N tiles past where it was found" on build 384 (00:42–09:55)
        // were 81 to 2912 tiles, 29 of them over a thousand ("a skeleton drew it 2909 tiles", Falk at 01:01:57, 0.2 minutes into
        // the hunt). The leash is measured again from wherever the bot stands after such a jump.
        if (Math.Max(Math.Abs(body.X - _last.X), Math.Abs(body.Y - _last.Y)) > JumpTiles)
        {
            _began = body.Location;
            _outward = Math.Max(Math.Abs(_quarry.X - _began.X), Math.Abs(_quarry.Y - _began.Y));
            Rebased++;
        }

        _last = body.Location;

        var drawn = Math.Max(Math.Abs(body.X - _began.X), Math.Abs(body.Y - _began.Y));

        // <b>The leash is on the quarry's running, not on the hunter's road (01.10.2026, build next; Patrick: "hunting means they
        // hunt everything that can be killed or is hostile to them").</b> It was measured from where the hunter stood, so a long
        // way round to a creature that never moved read as being dragged: on build 384 124 of the 234 leash failures were exactly
        // 51 tiles, and the commonest was one loop — Belthorn, Cirdan and Gerdrun took a ghoul from (1980, 2932) 23 times between
        // 02:38 and 09:44 and every time walked the road north to (1999, 2831) and let it go there, 0.4 minutes in. What the
        // order of 09.09.2026 bounds is a quarry that runs: the creature's own distance from where it was found. And a creature
        // that is fighting this bot, or that this bot has already fought, is not let go for it at all — it is the fight, not a
        // chase. The hunter's own distance from its start keeps a far bound (FarLeash × Leash beyond the walk out), the bound
        // the leash was first written for (a company dragged 737 tiles from home on 09.09.2026), and CapMs bounds the time.
        var strayed = Math.Max(Math.Abs(_quarry.X - _ran.X), Math.Abs(_quarry.Y - _ran.Y));
        var engaged = _fought || ReferenceEquals(_quarry.Combatant, body);

        if (strayed > Leash && !engaged)
        {
            // Filed against the quarry rather than the ground: it is the creature that ran, and the ground it
            // ran over is nobody's fault. Crowd is the same note a chase that timed out leaves.
            BotQuarry.Crowd(_quarry);
            Slipped++;

            return BotDoing.Failed($"{_quarry.Name} ran {strayed} tiles from where it was found and was let go");
        }

        if (drawn > Leash * FarLeash + _outward)
        {
            BotQuarry.Crowd(_quarry);
            Dragged++;

            return BotDoing.Failed($"{_quarry.Name} drew it {drawn - _outward} tiles past where it was found and was let go");
        }

        if (drawn > Leash + _outward)
        {
            // Past the old leash and kept: counted once a hunt, so the change can be read beside the failures it replaced.
            if (!_pastLeash)
            {
                _pastLeash = true;
                Unleashed++;
            }
        }

        _fell = _quarry.Location;

        // A caster's reach is its spells, not the stick in its hands.
        var reach = Math.Max(body.Weapon?.MaxRange ?? 1, BotStrike.Can(body) ? BotStrike.Range : 0);
        var standoff = Standoff(reach);

        if (!body.InRange(_quarry.Location, standoff))
        {
            // Following the thing rather than the place it was: a quarry walks, and a plan aimed at where it
            // used to be is a bot arriving somewhere empty. Stopped at the weapon's own distance rather than
            // at arm's length — walking a bow into melee throws away the only advantage it has.
            return BotDoing.Walk(_map, _quarry, BotArrival.Within(standoff), $"after {_quarry.Name}");
        }

        // <b>The blind clock starts here, and starting it in the constructor cost two nights.</b> It is
        // seeded when the undertaking is created but only ever read once the fight has begun — and between
        // those two moments sits the whole walk to the quarry, which a hunt looking fifty tiles out routinely
        // spends more than BlindMs on. So a bot arrived with its grace period already gone and was judged
        // blind on the first beat of the fight, before it had been given one chance to look.
        //
        // That is why the failures all read the standoff distance and never a tile less: there was no grace
        // left to close the distance in, and the branch that closes it could not be reached at all. It is
        // also why only the bots that keep their distance showed it — a melee bot stops at one tile, where a
        // line practically always exists, so the very next line refreshes this and the stale clock never
        // bites. Seeded at the fight rather than at the intention, the six seconds are six seconds of
        // fighting, which is what they were always meant to be.
        _seenTick = Core.TickCount;
        _fightTick = Core.TickCount;
        _leg = Leg.Fight;
        _fought = true;

        return Fighting(bot, body);
    }

    private BotDoing Fighting(IBotWilful bot, Mobile body)
    {
        if (Down())
        {
            _leg = Leg.Spoils;

            // Down in this bot's own fight: a kill to its credit, towards a veteran's title (07.10.2026, traits17). See BotBiography.Slew.
            BotBiography.Slew(body, _quarry);

            return Looting(bot, body);
        }

        // A retreat that arrived, or was dropped by somebody else, is over: nothing is left for a stand to end.
        if (_retreatTo != Point3D.Zero && body is BotMobile { Journey.Active: false })
        {
            _retreatTo = Point3D.Zero;
        }

        // <b>The exit a solo fight never had.</b> Measured in damage rather than in time, and against the
        // lowest health seen rather than the current health — a creature that regenerates between blows would
        // otherwise read as progress for as long as it lived. See NoProgressMs for what this cost.
        if (_quarry.Hits < _lowest)
        {
            // The first reading is where the quarry started; every later fall is a blow that landed (ours, or a company's).
            if (_lowest != int.MaxValue)
            {
                _hits++;
            }

            _lowest = _quarry.Hits;
            _progressTick = Core.TickCount;
        }

        // Nothing fights with its fists but the brawler. Checked here rather than at birth because a weapon
        // is a condition and not an event — see BotArms — and throttled, because this is asked several times
        // a second and the answer changes about once a session.
        if (Core.TickCount - _armedTick >= BotArms.EveryMs)
        {
            _armedTick = Core.TickCount;

            BotArms.Check(body, bot?.Class);
        }

        // Whether a blow could land at all, asked of the engine rather than of the distance. See BlindMs.
        //
        // <b>Two questions, and printed as one they cost a night.</b> "Cannot land a blow from here" is true
        // of a bow with a tombstone in the way and equally true of a bot the engine will not let swing at
        // all, and those have opposite cures — one is a place to stand, the other is not. On 25.08.2026 the
        // sentence was read as the first, the standoff was taught to close on a lost line, and the next
        // window came back with eighty-one of them: Godric 45 of 48 fights, Merrick 36 of 38, every melee
        // bot nought. A fix aimed at the wrong half of an "and". They are counted apart now, with the
        // distance beside them, because at one tile a broken line is nearly impossible and a refusal is not.
        var sighted = body.InLOS(_quarry);
        var lawful = body.CanBeHarmful(_quarry, false);

        if (sighted && lawful)
        {
            _seenTick = Core.TickCount;
        }
        else if (Core.TickCount - _seenTick >= BlindMs)
        {
            body.Combatant = null;
            body.Warmode = false;

            var apart = (int)body.GetDistanceToSqrt(_quarry.Location);

            var why = (sighted, lawful) switch
            {
                (false, false) => "out of sight and the engine refuses it too",
                (false, true)  => "nothing of it in sight",
                _              => "in plain sight and the engine refuses the blow"
            };

            // <b>"Cured by walking away and being offered it again from somewhere else" — and nothing here
            // ever made it walk away.</b> That sentence stood where the shun call should have been, and it
            // describes a cure nobody administers: the undertaking ends, the very same quarry is the best
            // thing on offer on the very next beat, and the bot takes it again from the very same tile. On
            // 25.08.2026 Joss did that thirty-eight times in one window on one zombie, every refusal from one
            // or two tiles away, with the repetition penalty grinding down by a hundredth a turn while the
            // circle turned every six seconds.
            //
            // Shunned rather than crowded, and the list's own definition settles which: a crowd is one bot
            // being outnumbered, which is an argument for calling a company. This is not that. A creature
            // that cannot be seen from the tile beside it is inside something or on top of something, and
            // "no way to lay a blow on it" is as true for the next bot along as for this one — which is
            // precisely what the shared list means. It lapses in two minutes, so this defers the quarry
            // rather than abandoning it.
            BotQuarry.Shun(_quarry);

            return BotDoing.Failed($"cannot land a blow on {_quarry.Name} at {apart} tiles: {why}");
        }
        else
        {
            // <b>Waiting cannot cure a blocked line, and waiting was the whole of the answer.</b> A bot that
            // keeps its distance stops where the distance tells it to and nothing asked whether it could see
            // what it came for — so an archer parked eight tiles from a skeleton with a tombstone between
            // them sat out the blind clock, gave the quarry up, took it again and walked to another blind
            // spot. On 25.08.2026 Godric lost ten fights out of ten exactly this way while every melee bot in
            // the population lost none, because at one tile there is always a line.
            //
            // Closing is what the waiting was standing in for. BlindMs stays underneath for the case closing
            // cannot fix: a quarry on a roof that nothing on the floor can see.
            //
            // <b>One fixed destination, and the tile-at-a-time version that stood here did nothing at
            // all.</b> It asked for the current distance less one, which is a different destination on every
            // beat — and BotWill rebases the journey whenever the order differs, arrival distance included,
            // throwing the plan away and buying a fresh search each time. The comment beside that rebase warns
            // of exactly this: an undertaking that re-words its walk every beat never gets further than a few
            // tiles. It never got even that far. Every blind failure for two nights reported the standoff
            // distance to the tile, unchanged, and the line "looking for a line" was never once printed
            // against a journey that had gone anywhere.
            //
            // Asking for contact and asking once keeps the order identical beat after beat, so the journey
            // survives and actually carries the bot in. Nothing is given away by aiming at contact: the line
            // check above refreshes the moment one exists, and the standoff and the kite take the bot back
            // out to its proper distance on the very next beat.
            if (!body.InRange(_quarry.Location, 1))
            {
                return BotDoing.Walk(_map, _quarry, BotArrival.Within(1), $"looking for a line on {_quarry.Name}");
            }
        }

        var stalled = Core.TickCount - _progressTick >= NoProgressMs;

        if (stalled || Core.TickCount - _tookTick >= CapMs)
        {
            body.Combatant = null;
            body.Warmode = false;

            // <b>The two exits are two different findings and said so in one sentence.</b> Both printed "would
            // not go down", which is true of a creature nothing could scratch and false of one that was dying
            // steadily when the clock ran out — and the ledger line beside it says "0 coin, 0 made", which is
            // takings and not damage, so nothing anywhere distinguished them. Two casters were cut off at
            // exactly two and a half minutes on a skeleton and a troll, and whether that was a caster doing no
            // damage or a ceiling set too low could not be told from the log at all. How much of it was left
            // is the fact that separates them, so it is the fact that gets printed.
            var left = _quarry.HitsMax > 0 ? _lowest * 100 / _quarry.HitsMax : 0;

            if (stalled)
            {
                // Left to companies rather than to nobody: what has been learned is that one bot cannot shift
                // it, which is an argument for a squad and not against one. Only on this branch — a creature
                // that was steadily dying when the clock ran out is exactly the sort a lone hunter should be
                // offered again.
                BotQuarry.Crowd(_quarry);

                // <b>Said with what the bot had in its hands (29.09.2026).</b> 48 hunts in three hours of build 272 ended
                // here, every one of them an archer's, and the line could not say whether the bow had ammunition, whether
                // the shooter ever stood still long enough to loose, or how far off the thing was. The instrument first.
                //
                // <b>The weapon's own ammunition, not arrows and bolts together (12.10.2026, w4).</b> The sum read a crossbow
                // holding fourteen arrows as loaded in three failures running, the number never falling. And the three
                // numbers the retreat question turns on: back-offs ordered, falls in the quarry's health, beats stood for a
                // due arrow. "Not a scratch" is a stall of NoProgressMs, not a whole fight without a hit: 70 % of the failures
                // measured on 12.10.2026 had hit before the stall.
                var apart = Math.Max(Math.Abs(body.X - _quarry.X), Math.Abs(body.Y - _quarry.Y));

                return BotDoing.Failed(
                    $"{_quarry.Name} would not go down: {left}% of it left and not a scratch in {NoProgressMs / 1000}s "
                    + $"(holding {body.Weapon?.GetType().Name ?? "nothing"}, {Quiver(body)}, {apart} tiles off, {_kited} kites, {_backed} back-offs, "
                    + $"{_hits} hits seen, {_dues} stood for a due arrow, "
                    + $"{(body is BotMobile { Journey.Moving: true } ? "moving" : "still")}, in sight {body.InLOS(_quarry)})"
                );
            }

            return BotDoing.Failed($"ran out of time on {_quarry.Name} with {left}% of it left");
        }

        // Losing. This is the one decision this undertaking makes that nothing else will make for it.
        // <b>Except against a caster, which is fought to the end (30.09.2026, build 358; Patrick: "fight to the end").</b> Since
        // 355 no flight is offered from a mage; giving the fight up here dropped the bot's guard and left it standing under the
        // spells with nothing to do but bandage. The fight against a caster ends when one of them is down.
        // <b>Nor a stand (06.10.2026, react14).</b> A bot that will not be offered a flight and has what is killing it on top of
        // it fights on: giving the fight up here left it with nothing at all. See BotDefender.Stand.
        if (ToTheEnd && body.HitsMax > 0 && body.Hits < body.HitsMax * FleeAt && !_stood)
        {
            _stood = true;
            BotDefender.StoodOn++;
        }

        // At the bot's own flight share: a cautious nature gives up a little earlier, a reckless one later, never under the floor
        // (07.10.2026, traits17). See BotTraits.FleeAt.
        if (!ToTheEnd && body.HitsMax > 0 && body.Hits < body.HitsMax * BotTraits.FleeAt(body) && _quarry is not Server.Mobiles.BaseCreature { AI: Server.Mobiles.AIType.AI_Mage })
        {
            body.Combatant = null;
            body.Warmode = false;

            return BotDoing.Failed($"{_quarry.Name} was winning");
        }

        // <b>Losing on numbers, which is a different fact and used to have nowhere to be noticed.</b> The
        // quarry was weighed once, alone, at the moment it was chosen; what walked in afterwards was never
        // weighed at all. So a bot picked a fight it could win, two more creatures joined in, and the only
        // rule that could end it was health — meaning the bot stood there at full health being surrounded,
        // and started thinking about leaving after it had already lost sixty per cent of itself. This asks
        // the same question the road asks: is this several times over, counting everything here and everybody
        // of ours standing in it.
        if (!_weighed || Core.TickCount - _oddsTick >= OddsMs)
        {
            _weighed = true;
            _oddsTick = Core.TickCount;

            var outmatched = BotThreat.Decide(body, BotMobile.NoticeRange) == BotStand.Outmatched;

            if (outmatched && ToTheEnd)
            {
                // <b>A stand is not ended by the odds (12.10.2026, w5).</b> It is taken because the fight was refused for the odds
                // and no flight was offered (BotDefender.Stand), so ending it here on the same odds handed the bot back to that
                // rung with nothing — which is what the stand was written to close. The muster is still urged for it, once; the
                // crowd note is not written, because the rescuers read it and would pass the stander by (BotRescuer).
                if (!_urged)
                {
                    _urged = true;
                    BotDefender.StoodOnOdds++;
                    BotMuster.Urge(body, _quarry);
                }
            }
            else if (outmatched)
            {
                body.Combatant = null;
                body.Warmode = false;

                // <b>Written down, or the bot walks straight back.</b> A quarry is weighed alone — its own
                // power against ours — and the fight is called off on the numbers standing round it, which
                // is a fact nobody learns until the bot has arrived. So the same creature is chosen again
                // on the next review, walked to again, and refused again: on 24.08.2026 the population
                // finished three hundred and thirty-six walks to look for a fight against twenty-nine
                // fights, and the log reads prowl, hunt, "too many of them", prowl, four times a second.
                //
                // <b>Crowd and not Shun, and the difference cost an evening's companies to find.</b> Put on
                // the shared shun list this also hid the creature from BotQuarry.Company — so the population
                // stopped calling squads entirely, against the one kind of target squads exist for. What was
                // learned here is that <em>one</em> bot cannot have it, which is an argument for a company
                // rather than against one.
                BotQuarry.Crowd(_quarry);
                BotMuster.Urge(body, _quarry);

                return BotDoing.Failed($"too many of them around {_quarry.Name}");
            }
        }

        _fell = _quarry.Location;

        // <b>Without this a mage was given the reach of a quarterstaff — one tile — and walked into contact,
        // which is the one place its whole build cannot work: a blow disturbs every cast a player attempts,
        // and every bot is one. So a caster fights at the distance its spells carry, and backs off when
        // something closes, exactly as an archer does and for a sharper reason.</b>
        // <b>A caster's casts are capped by what the quarry pays (02.10.2026, build 389; Patrick's option 4).</b> See Thrifty: once
        // the casts have spent what one of the kind pays, and the quarry is not fighting back, the hunt is left — the ledger learns
        // the place, the quarry is left alone for a while, and the next look (BotQuarry.Best) passes over kinds that do not pay
        // for the floor's casts.
        if (Thrifty(body))
        {
            BotQuarry.Shun(_quarry, BotQuarry.ShunMs);
            BotQuarry.Release(_quarry);

            return BotDoing.Done($"left {_quarry.Name} alive after {_casts} casts: one of its kind pays {BotQuarry.Pays(_quarry.GetType()):F0}gp, {_castCap} casts' worth");
        }

        var casting = BotStrike.Can(body);

        // <b>Being a caster and having something to cast are two different facts, and the standoff was built
        // on the wrong one.</b> Can() asks whether this bot is the sort that throws spells — a permanent
        // property of its class and its book. Whether it can throw one <em>now</em> is a question about mana
        // and about what is in its pack, and it changes minute to minute. Built on the first, a mage whose
        // book has nothing it can pay for keeps station six tiles from a troll, out of reach of its own
        // staff, and contributes precisely nothing: on 24.08.2026 two fights ended at forty-five seconds with
        // the target on a hundred per cent of its health, which is a bot standing in a fight not being in it.
        //
        // Asked every beat rather than latched, so the moment reagents are back in the pack the bot gives
        // ground again and fights the way it is built to.
        var armed = casting && BotStrike.Best(body) >= 0;

        var reach = Math.Max(body.Weapon?.MaxRange ?? 1, armed ? BotStrike.Range : 0);
        var standoff = Standoff(reach);

        if (!body.InRange(_quarry.Location, reach))
        {
            _leg = Leg.Close;

            return BotDoing.Walk(_map, _quarry, BotArrival.Within(standoff), $"after {_quarry.Name}");
        }

        // <b>Kiting, and the sentence below it is wrong.</b> "The engine keeps swinging the bow while the bot
        // moves — combat is its own timer and does not care that the shooter is walking" is true of a blade
        // and false of a bow on this era: BaseRanged.OnSwing refuses outright unless the shooter has been
        // still for a full second. So an archer that gives ground whenever something is near never fires at
        // all, and one that stands still is a melee fighter holding the wrong weapon.
        //
        // The engine says exactly how long the bow will be idle — NextCombatTime — so the two halves can be
        // taken in turn: spend the idle part opening the distance, and be standing again well before the
        // shot is due. Ordered on 24.08.2026, and it is the only way a bow is worth carrying here.
        if (Kiting(body, reach))
        {
            var opening = Away(body, _quarry, reach);

            if (opening != Point3D.Zero)
            {
                body.Warmode = true;
                body.Combatant = _quarry;

                BotQuarry.Claim(body, _quarry);

                _retreatTo = opening;

                return BotDoing.Walk(_map, opening, BotArrival.Within(1), $"keeping {_quarry.Name} at arm's length");
            }
        }

        // <b>A caster gives ground when it has nothing to throw, and only then.</b>
        //
        // <c>Best</c> is one question with three gates behind it — the book holds the spell, the pool pays for
        // it, and the herbs are in the pack — so "no mana for an attack, or no reagents to cast it" is not two
        // conditions here, it is this one being false. That matters: a mage in contact <em>with</em> mana is
        // trading blows for spells and getting the better of it, while a mage with a dry pool in contact is
        // simply being hit, and the way out of that is distance and a minute.
        //
        // <b>And it had to be written as its own gate, because the arithmetic had quietly closed it.</b>
        // The stand-off is two short of the bot's reach, and a caster with nothing to throw has the reach of
        // the stick in its hand — one tile. One is not greater than three, so the branch below could not be
        // entered by the one bot that most needed it: "backing off" and "keeping at arm's length" both stood
        // at nought across whole sessions, and a mage stood in melee being beaten while the code that would
        // have moved it sat right here.
        var dry = casting && !armed;

        // How far it wants to be from whatever has closed. A dry caster opens to the distance its spells will
        // carry when the mana comes back, not to whatever its stick reaches — backing off one tile is not
        // backing off.
        var yield = dry ? Standoff(BotStrike.Range) : standoff;

        // An archer's arithmetic is untouched: it is not a caster, so it reads the same test it always did.
        var yields = casting ? dry : standoff > TooClose;

        // <b>An archer gives ground only to what it can leave behind (01.10.2026, build next).</b> A bow fires only after a full
        // second standing still (StillMs), and a timber wolf, a bear or a bull steps as fast as a running bot: an archer backing
        // off one never stands for a second, and never shoots. On build 384 (00:42–09:55) 44 hunts and 8 rescues failed "would not
        // go down … not a scratch in 45s (holding Bow, 50–150 arrows, 1–4 tiles off, 3–149 kites, in sight True)" — wolves,
        // bears, bulls, cows, great harts, orcs; never a zombie. Against a quarry no slower than the archer, or once backing off
        // has left it inside TooClose for PlantMs, the archer stands and shoots at the range it has: a bow has no least range.
        var crowded = yields && yield > TooClose && body.InRange(_quarry.Location, TooClose);

        if (crowded && !casting && !GainsGround(body))
        {
            crowded = false;
            Outpaced++;
            Stand(body);
        }

        // <b>An archer whose arrow is due stands and shoots; it backs off only between shots (12.10.2026, w4).</b> The kite
        // above reads the bow's clock — it gives ground only while more than StillMs + KiteSlackMs of the reload is left — and
        // the back-off below never did: the one branch that fires when the quarry is inside TooClose answered Walk with the
        // arrow due, and the walk, left running by every Work beat after it, renewed LastMoveTime every 200 ms while the engine
        // refused the swing every 250. On 11.10.2026 that was 360 of 424 "would not go down" (85 %; 90 % and 89 % on the two
        // runs beside it), 42 an hour, 96 % of them bows: Uldreth Silverleaf shot a black bear once (100 → 51 %), was "backing
        // off" from the beat the next arrow came due (4 s against a 4.0 s delay) and walked 38 s at 3–4 tiles without a second
        // one; Malachi backed off a ghoul 160 tiles in 38 s. Failing hunts moved in 86–90 % of their seconds; 13 of 15 watched
        // were runners, against 2 of 36 planted. The test is the kite's own, so the two halves of the reload are now one rule:
        // the idle part is spent opening the distance, the last 1.7 s standing. Ranged and not casting, as the kite: a blade's
        // "back off" is a dry caster's and needs no stillness. A closer's answer to crowding is its blade, which needs none
        // either, so a captain keeps the crowded branch and only has its walk ended. A kite still running when the kite
        // itself answers Rushed is ended here too.
        if (StandsWhenDue && !casting && body.Weapon is BaseRanged
            && body.NextCombatTime - Core.TickCount <= StillMs + KiteSlackMs)
        {
            if (Stand(body))
            {
                Halted++;
            }

            if (crowded && body is not BotMobile { Class.Closes: true })
            {
                crowded = false;
                _dues++;
                Due++;
            }
        }

        if (!crowded)
        {
            _crowdedTick = 0;
        }

        // Something has closed on a bot that would rather it did not. Give ground, keep shooting.
        //
        // <b>The first thing in this project that walks away from something on purpose.</b> Everything else
        // treats a fight as a place to stand; an archer's whole case for existing is that it is somewhere
        // else.
        if (crowded)
        {
            // <b>A class that closes answers this moment the other way round.</b> Everything above is an
            // archer's arithmetic and it is right for an archer: something is inside the bow's useful
            // distance, so open it again. A captain reads the same fact and draws its sword, and from the
            // line below it is fighting by the shard's ordinary melee code with nothing special about it —
            // the standoff recomputes from the weapon now in its hand, comes out at one tile, and this whole
            // branch stops applying by arithmetic rather than by exception.
            //
            // Asked of the class rather than of the bot's build, because it is a decision about how this
            // sort of fighter answers being closed on, and it must read the same for every captain the
            // population ever raises.
            var closing = BotArms.Suit(body, _quarry, TooClose);

            if (closing)
            {
                // Nowhere to back off to any more, and the held point would otherwise be walked to on the
                // next beat by a bot that has just decided to stand.
                _backing = Point3D.Zero;

                Closed++;
            }
            else
            {
                // Held rather than recomputed: see _backing. A point is dropped once the bot is standing on
                // it, or once it has stopped being far enough from the quarry to be worth walking to.
                if (_backing != Point3D.Zero
                    && (body.InRange(_backing, 1) || !Utility.InRange(_backing, _quarry.Location, yield)))
                {
                    _backing = Point3D.Zero;
                }

                var back = _backing != Point3D.Zero ? _backing : Away(body, _quarry, yield);

                // Backing off that has not opened the distance in PlantMs is not working: an archer stands from here on.
                if (!casting && back != Point3D.Zero)
                {
                    if (_crowdedTick == 0)
                    {
                        _crowdedTick = Core.TickCount;
                    }
                    else if (Core.TickCount - _crowdedTick >= PlantMs)
                    {
                        _planted = true;
                        _crowdedTick = 0;
                        back = Point3D.Zero;
                        Planted++;
                        Stand(body);
                    }
                }

                if (back != Point3D.Zero)
                {
                    _backing = back;

                    body.Warmode = true;
                    body.Combatant = _quarry;

                    BotQuarry.Claim(body, _quarry);

                    _retreatTo = back;
                    _backed++;
                    Backed++;

                    return BotDoing.Walk(_map, back, BotArrival.Within(1), $"backing off {_quarry.Name}");
                }
            }
        }

        // Set every beat, and the setter is a no-op when it has not changed. Combat itself is the engine's
        // from here: one server timer, swinging on the weapon's own delay.
        body.Warmode = true;
        body.Combatant = _quarry;

        // Whoever can throw something, throws it.
        //
        // <b>Melee goes on underneath, deliberately.</b> The engine swings on its own timer whatever else the
        // mobile is doing, so a caster loses nothing by also being in a fight — and a caster out of mana is
        // simply a bot with a stick again, which is the right fallback rather than a special case.
        if (casting)
        {
            // A cast of ours has come round to its target: the click a bot has no client to make. Guarded by
            // having started one, because a cursor on a bot is not necessarily this work's — mining puts one
            // up to point at rock.
            if (_aiming && body.Target != null)
            {
                _aiming = false;

                if (BotStrike.Aim(body, _quarry))
                {
                    _casts++;
                }

                return BotDoing.Work($"casting at {_quarry.Name}");
            }

            // Mid-cast. The engine is holding the delay and movement already knows to stand still for it.
            if (body.Spell != null)
            {
                return BotDoing.Work("casting");
            }

            // Not before the engine will have it. Asking again inside the recovery is refused, and asking
            // every beat is asking eight times a second.
            if (_cast && Core.TickCount - _castTick < BotStrike.CastMs)
            {
                return BotDoing.Work($"between spells at {_quarry.Name}");
            }

            var spell = BotStrike.Best(body);

            if (spell >= 0 && BotStrike.Begin(body, spell))
            {
                _aiming = true;
                _cast = true;
                _castTick = Core.TickCount;

                return BotDoing.Work($"casting at {_quarry.Name}");
            }

            // Said once per fight, because a caster that silently declines to cast is the shape of fault this
            // project has been bitten by all evening: the refusal is a message to a client, and a bot has none.
            //
            // <b>And it has to name the gate that closed, which it did not.</b> The line read "best -1, 35 of
            // 35 mana, book holds 5" — a full pool and a stocked book, both of them fine, and silence about
            // the only thing that was not. That is the rule this project keeps relearning: a message that
            // lists the reasons it checked and finds them all in order is a message whose real reason is not
            // in the list. There are three gates in BotStrike.Ready and the answer is always one of them.
            if (!_complained)
            {
                _complained = true;

                logger.Information(
                    "{Name} could not throw a spell at {What}: {Why} — best {Spell}, {Mana} of {Pool} mana, book holds {Known}",
                    body.Name,
                    _quarry.Name,
                    BotStrike.Why(body),
                    spell,
                    body.Mana,
                    body.ManaMax,
                    BotGrimoire.Count(body)
                );
            }
        }

        // Whoever sets about a thing owns what comes off it. Claimed here rather than when the work was
        // chosen, because choosing is not fighting: a bot that picked a quarry and never reached it has no
        // business locking it away from everybody who could.
        BotQuarry.Claim(body, _quarry);

        return BotDoing.Work($"fighting {_quarry.Name}");
    }

    private BotDoing Looting(IBotWilful bot, Mobile body)
    {
        body.Combatant = null;
        body.Warmode = false;

        if (!body.InRange(_fell, BotQuarry.LootReach))
        {
            return BotDoing.Walk(_map, _fell, BotArrival.Within(BotQuarry.LootReach), "to the corpse");
        }

        // The claim outlives the creature: this is exactly the moment it decides something. A bot that came
        // late to somebody else's fight has fought honestly and earned the skill for it, and it does not get
        // the purse.
        if (!BotQuarry.Ours(body, _quarry))
        {
            return BotDoing.Done($"{_quarry?.Name} was somebody else's fight");
        }

        var corpse = BotQuarry.Remains(_map, _fell, _quarry);

        if (corpse == null)
        {
            // Killed and nothing to show for it: somebody else went through it, or it left nothing. Finished
            // rather than failed — the fight happened and the skill it taught is real.
            return BotDoing.Done("nothing left on it");
        }

        // Before anything is picked up or divided, so that the hide is on the corpse by the time either
        // happens and travels with the rest of the takings instead of needing a trip of its own.
        _hides = Skin(body, corpse);

        // In company, what came off it is divided by worth rather than grabbed. Solo — which is every fight
        // on this shard today, because nothing musters a squad yet — it all goes in the one pack.
        var squad = (bot as IBotSquadMember)?.Squad;

        if (squad != null)
        {
            _taken = BotSpoils.Share(squad, (IBotSquadMember)bot, corpse);

            BotQuarry.Release(_quarry);

            return BotDoing.Done($"{_quarry?.Name} split {_taken} ways with the squad");
        }

        Take(bot, body, corpse);

        // What this kind of creature actually carried, written down for everybody. A goat priced at nothing
        // stops being chosen; a lizardman priced at fifty starts being sought out.
        BotQuarry.Paid(_quarry?.GetType(), _coins);

        // The city's price on this kind's head, if it carries one, paid at the corpse like the corpse's own gold. See BotCity.Collect.
        var head = BotCity.Collect(_quarry?.GetType(), body);

        BotQuarry.Release(_quarry);

        // The spells are in the sentence because otherwise casting is invisible: a cast is reported through
        // BotDoing.Work, and Work is the one answer the decision layer deliberately never writes down.
        // Leather is said separately from the thing-count it is part of, because it is the one thing coming
        // off a corpse that somebody else is waiting on: a leather order on the needs board is filled or not
        // filled by this number, and "eleven things" does not say whether it was.
        var said = (_casts > 0, _hides > 0) switch
        {
            (true, true) =>
                $"{_taken} things and {_coins}gp off {_quarry?.Name}, {_hides} leather, {_casts} spells thrown",
            (true, false) => $"{_taken} things and {_coins}gp off {_quarry?.Name}, {_casts} spells thrown",
            (false, true) => $"{_taken} things and {_coins}gp off {_quarry?.Name}, {_hides} leather",
            (false, false) => $"{_taken} things and {_coins}gp off {_quarry?.Name}"
        };

        return BotDoing.Done(head > 0 ? $"{said}, {head}gp from the city for its head" : said);
    }

    /// <summary>
    /// Empties what is worth taking into the pack, and puts the goods straight onto the market.
    ///
    /// <para>
    /// <b>Everything goes to the population first, whatever it is.</b> There is no test here for whether a
    /// rusty blade is treasure or rubbish, and there does not need to be: a stall that nobody buys from in
    /// half an hour is taken to a shopkeeper by <see cref="BotPeddler"/>, which is the same road produced
    /// goods travel. One bot's junk is another's material, and the market is the only thing that can tell the
    /// difference — so it is asked, every time, before a counter is.
    /// </para>
    ///
    /// <para>
    /// Weight is the limit, not value. Loot is not bound, so it weighs, and a bot that empties a corpse into
    /// an already full pack is a bot that cannot walk home.
    /// </para>
    /// </summary>
    /// <summary>
    /// Take the hide off it and cut that down to leather, both onto the corpse, and answer how many pieces
    /// came out. Nothing, and no complaint, when the creature carries no hide or there is nothing to cut with.
    ///
    /// <para>
    /// <b>Leather had no way into this world at all.</b> A tailor's orders for anything made of it stood on
    /// the board unfillable at any price while the material walked around Felucca on the backs of everything
    /// the population was already killing — because carving is a thing a player does with a blade and no bot
    /// had been taught the gesture. The engine charges nothing for it: no skill roll, no roll to fail, no
    /// wear on the blade, and on Felucca it doubles what comes off. That is why this is folded into going
    /// through a corpse rather than made an undertaking a bot has to choose over hunting — there is no
    /// decision here worth a bot's time to make.
    /// </para>
    ///
    /// <para>
    /// <b>Two steps and not one, because the hide is not the thing anybody wants.</b> On this era carving
    /// yields hides, tailoring consumes leather, and scissors turn one into the other piece for piece — so a
    /// population that carved and stopped there would have filled the market with a commodity no crafter can
    /// use and left the leather orders exactly as unfillable as before. Cut here, while it is still on the
    /// corpse, because <see cref="Take"/> lists what it lifts the moment it lifts it: leather offered to the
    /// market is an order filled, hides offered to the market is a second problem.
    /// </para>
    ///
    /// <para>
    /// Carving somebody else's kill is a criminal act and the engine says so. This only ever runs on a kill
    /// already established as ours, and asks anyway rather than trusting that.
    /// </para>
    /// </summary>
    public static int Skin(Mobile body, Corpse corpse)
    {
        if (corpse.Carved || corpse.IsCriminalAction(body))
        {
            return 0;
        }

        var blade = Blade(body);

        if (blade == null)
        {
            return 0;
        }

        corpse.Carve(body, blade);

        var shears = body.Backpack?.FindItemByType<Scissors>();

        if (shears == null)
        {
            return 0;
        }

        var cut = 0;

        // A snapshot: cutting replaces the hides in the corpse, which mutates the list being read.
        List<Item> lying = [.. corpse.Items];

        for (var i = 0; i < lying.Count; i++)
        {
            if (lying[i] is not (BaseHides and IScissorable hide))
            {
                continue;
            }

            var was = lying[i].Amount;

            if (hide.Scissor(body, shears))
            {
                cut += was;
            }
        }

        return cut;
    }

    /// <summary>
    /// Something to carve with, in a hand or in the pack, or nothing.
    ///
    /// <para>
    /// The engine's own notion of what is bladed rather than a list of item names kept here to drift: a knife
    /// and a sword are precisely the two things that put up a <c>BladedItemTarget</c> when a player
    /// double-clicks them. A swordsman is holding one already for reasons of its own; everybody else is
    /// carrying the skinning knife birth hands out — see <c>BotOutfit.ToolsFor</c>.
    /// </para>
    /// </summary>
    private static Item Blade(Mobile body)
    {
        if (body.Weapon is Item held and (BaseKnife or BaseSword))
        {
            return held;
        }

        var pack = body.Backpack;

        if (pack == null)
        {
            return null;
        }

        List<Item> carried = pack.Items;

        for (var i = 0; i < carried.Count; i++)
        {
            var item = carried[i];

            if (item is BaseKnife or BaseSword && !item.Deleted)
            {
                return item;
            }
        }

        return null;
    }

    private void Take(IBotWilful bot, Mobile body, Corpse corpse)
    {
        var (taken, coins, made) = Rifle(bot, body, corpse);

        _taken += taken;
        _coins += coins;
        _made += made;
    }

    /// <summary>
    /// Goes through a corpse and says what came off it. The whole of going through a corpse, for anybody.
    ///
    /// <para>
    /// <b>Lifted out of the hunt because a kill is no longer the only way a corpse appears.</b> This used to
    /// be an instance method writing straight into a hunt's own tally, on the reasoning that the only bot
    /// standing over a body is the one whose undertaking killed it. That stopped being true the day
    /// self-defence became a reflex: a bot now kills whatever walks up and hits it, without ever having taken
    /// on a fight, and the body it leaves has nobody whose errand includes emptying it. A sage killed a harpy
    /// and walked away from it on 26.08.2026, which is what sent anybody looking.
    /// </para>
    ///
    /// <para>
    /// Static and returning its counts rather than writing them, so the same code answers for the hunt that
    /// planned the kill and for the scavenger that only survived one. Two copies of the weight rule would
    /// have been the more familiar mistake.
    /// </para>
    /// </summary>
    public static (int Taken, int Coins, int Made) Rifle(IBotWilful bot, Mobile body, Corpse corpse) =>
        Rifle(bot, body, corpse, item => corpse.CheckLoot(body, item));

    /// <summary>
    /// The same, out of any container the bot is allowed to go through — a chest in a camp, a crate, a barrel.
    ///
    /// <para>
    /// <b>One rule, not two.</b> What goes in a pack, what is held back as kit or as raw material, what is
    /// listed and at what price — all of it is decided here and nowhere else. A second copy written for
    /// chests would have agreed with this one for about a week: this file already carries the scars of the
    /// leather trade being strangled by a flat one-gold fallback that lived in only one of two places.
    /// </para>
    ///
    /// <para>
    /// The permission is the caller's, because it differs: a corpse asks the engine whether this bot may
    /// loot it at all, and a chest standing in an orc camp asks nothing of anybody.
    /// </para>
    /// </summary>
    public static (int Taken, int Coins, int Made) Rifle(
        IBotWilful bot, Mobile body, Container from, Func<Item, bool> may
    )
    {
        var taken = 0;
        var coins = 0;
        var made = 0;

        var pack = body.Backpack;

        if (pack == null)
        {
            return (0, 0, 0);
        }

        var ceiling = BotLadder.Ceiling(body) * FillFraction;

        // A snapshot: moving things out mutates the list being read.
        List<Item> lying = [.. from.Items];

        for (var i = 0; i < lying.Count; i++)
        {
            var item = lying[i];

            if (item == null || item.Deleted || !item.Movable || may?.Invoke(item) == false)
            {
                continue;
            }

            if (item is Gold coin)
            {
                coins += coin.Amount;

                pack.DropItem(coin);

                continue;
            }

            if (BotLadder.Load(body) >= ceiling)
            {
                // Full. What is left stays where it lies for whoever comes past, which is a better answer than
                // a hunter that cannot carry its own takings home.
                break;
            }

            pack.DropItem(item);

            taken++;

            // Told apart before it is listed, which takes it out of the world: a magic blade off a corpse used to go on the
            // market as "an unidentified longsword". Patrick's order of 16.09.2026; see BotTidy.Appraise.
            BotTidy.Appraise(body, item, from is Corpse ? "off a corpse" : "as it took it");

            // Its own ammunition stays in the quiver.
            //
            // <b>Four hits in ten put the arrow into the target, which means into this corpse</b> — so an
            // archer going through what it has just killed is very often picking up its own arrows, and
            // listing them for sale is an archer selling the means of its trade and then buying it back at a
            // counter. Everything else off a corpse is goods; this is kit.
            if (item.GetType() == bot.Bond?.Weapon?.Ammunition)
            {
                continue;
            }

            // Its own raw material stays in the pack, for exactly the reason its own arrows do: a cook that
            // lists the meat it is standing over has sold the one ingredient of the trade it carries a
            // skillet for, and will buy a supper back at a counter later. See BotOven.Spares, which holds
            // back a round's worth and no more, and only for somebody who can actually cook it.
            if (BotOven.Spares(body, item))
            {
                continue;
            }

            // The same rule, on the other material this island produces and no shopkeeper sells. See
            // BotFletching.Spares: a fletcher standing over a bird was listing the only feathers there are.
            if (BotFletching.Spares(body, item))
            {
                continue;
            }

            // Valued at whatever the shard reckons one is worth, and kept in the pack: since the auction went (01.10.2026, market
            // change, minimal edit here) loot is no longer put on a stall from the corpse — it rides in the pack to a want by a
            // carrier's walk (BotCourier), or to the bank box by the porter, and from there to a shop by the bank or a shopkeeper.
            //
            // <b>The fallback was a flat one gold, and a flat one gold is what strangled the leather
            // trade.</b> Worth answers with the market's own price the moment anybody has bid or bought —
            // but until then it hands back whatever the caller guessed, and every kind of loot in the world
            // was guessed at one. So a bear's four-and-twenty hides went out at 24gp when the same bear's
            // purse held fifty, and the tailor's floor, which multiplies up from the material, priced a pair
            // of leather shoes at twelve. Nothing was broken anywhere in that chain: it was simply valued at
            // nothing, so a crafter weighing leather against cloth rationally chose cloth every time, and in
            // eight hours not one leather piece was ever made.
            //
            // A shopkeeper's own offer is the honest floor, and the engine already knows it: a tanner pays
            // five for a leather, a furtrader two for a hide. Measured rather than declared, like everything
            // else here — no table to go stale when somebody edits a loot pack — and it only sets the
            // opening ask, after which the market moves the price the way it moves every other.
            var floor = BotShops.Buyer(body, item, out var offered) != null ? offered : 1;
            var worth = BotMarket.Worth(item.GetType(), Math.Max(1, floor));

            if (worth >= BotMarket.FloorFor(item.GetType()))
            {
                made += worth * (item.Amount > 0 ? item.Amount : 1);
            }
        }

        return (taken, coins, made);
    }

    /// <summary>
    /// The way to it turned out not to exist. Say so about <em>this creature</em> rather than about the
    /// ground, and give up.
    ///
    /// The ledger's caution is keyed by patch of ground, which is the wrong grain for this: a wraith on the
    /// far side of a river makes a bot wary of a whole field it could otherwise hunt perfectly well, while
    /// leaving the wraith itself as attractive as ever to the next bot that looks. What could not be reached
    /// is the wraith.
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        BotQuarry.Shun(_quarry);

        return false;
    }

    /// <summary>
    /// Whether this caster has thrown at the quarry all that one of its kind pays for. Never while the quarry is fighting the bot,
    /// never for a bot that does not cast, and never under <see cref="CastFloor"/> casts. The cap is what one of the kind pays
    /// (<c>BotQuarry.Pays</c>, the city's price on its head counted in) over what one cast costs this bot
    /// (<c>BotProvision.CastCost</c>), reckoned once a hunt. Five Harm scrolls at eleven gold on a cougar carrying nothing was the
    /// Mage's hour on build 386, -59gp a bot-hour.
    /// </summary>
    private bool Thrifty(Mobile body)
    {
        if (!CastsByGold || _quarry == null || body is not BotMobile { Class.Role: BotRole.Caster }
            || ReferenceEquals(_quarry.Combatant, body) || !BotStrike.Can(body))
        {
            return false;
        }

        if (_castCap < 0)
        {
            var spell = BotStrike.Best(body);

            if (spell < 0)
            {
                spell = BotStrike.Stock(body);
            }

            var cost = BotProvision.CastCost(body, spell);

            _castCap = Math.Max(CastFloor, (int)(BotQuarry.Pays(_quarry.GetType()) / Math.Max(1, cost)));
        }

        if (_casts < _castCap)
        {
            return false;
        }

        if (!_thrifted)
        {
            _thrifted = true;
            Thrifted++;
        }

        return true;
    }

    /// <summary>Over, however it ended. Whatever was reserved goes back.</summary>
    public override void Drop(IBotWilful bot)
    {
        BotQuarry.Release(_quarry);

        // What a fight a sweep found came to, credited to the square it was found in: the price of the next sweep there. See
        // BotSweepRecord (06.10.2026, build next).
        BotSweepRecord.Hunted(bot?.Self, _coins + _made);

        // <b>The bow comes back up the moment the fight is over, and forgetting this would have been silent.</b>
        // A captain that closes ends every fight holding a sword, and the next one would then open at one
        // tile — so the class that exists to shoot first would have shot first exactly once, on the day it
        // was born. Nothing would have logged it: the bot is armed, it is fighting, it is winning some of
        // them. It would simply never have been an archer again.
        if (bot?.Self is BotMobile { Class.Closes: true } closer)
        {
            closer.Draw(melee: false);
        }

    }

    /// <summary>
    /// Whether this bot should be opening the distance right now rather than standing to shoot.
    ///
    /// <para>
    /// Three conditions, and every one of them has to hold. It must be carrying something that shoots, or a
    /// blade would walk away from the fight it is supposed to be in. The quarry must be nearer than the
    /// weapon carries, or there is nothing to gain and a step outwards throws the shot away. And the bow must
    /// have long enough left on its own clock to move and be still again before the shot is due — which is
    /// the whole trick, and the engine is the only thing that knows it.
    /// </para>
    /// </summary>
    /// <summary>Times the kite was considered, and which of its three conditions turned it down.</summary>
    public static long Asked { get; private set; }

    public static long Handless { get; private set; }

    public static long Distant { get; private set; }

    public static long Rushed { get; private set; }

    public static long Kited { get; private set; }

    /// <summary>The longest the weapon's own clock was ever seen to have left, in milliseconds.</summary>
    public static long Longest { get; private set; }

    /// <summary>
    /// How often a bot answered being closed on by drawing its blade instead of giving ground.
    ///
    /// <para>
    /// Counted beside the kite rather than inside it, because the two are opposite answers to the identical
    /// fact and a single number covering both would say nothing about either. A shard where this rises and
    /// <see cref="Kited"/> does not has one captain in a lot of trouble; the other way round has a captain
    /// carrying a sword it never draws.
    /// </para>
    /// </summary>
    public static long Closed { get; private set; }

    /// <summary>
    /// Why the bow never once gave ground.
    ///
    /// <para>
    /// <b>Two nights of "kites: 0" and no way to tell which of three conditions was the one saying no.</b>
    /// Counted apart, with the longest clock ever seen beside them, because "the bow is never idle long
    /// enough" and "the bow is never in a hand" are different faults and were producing the same silence.
    /// </para>
    ///
    /// <para>
    /// <b>The first thing it found was that there was nothing wrong.</b> The kite fires — three hundred and
    /// eight times in one five-minute window, on a clock running out to five seconds against the seventeen
    /// hundred milliseconds it needs. What was being counted against it all along was a grep for the phrase
    /// this work walks under, and a walk's reason reaches the log <em>only when the journey fails</em>. Every
    /// successful kite is silent by construction, so the measure could report nothing but disasters and was
    /// read as though nought meant never. A metric that can only go up when something breaks is not a metric.
    /// </para>
    ///
    /// <para>
    /// The melee bucket is named for what it is rather than counted as a refusal: every bot in every fight is
    /// asked this question, and a swordsman having no bow is not the bow failing. Left unlabelled it read as
    /// a thousand archers with empty hands.
    /// </para>
    /// </summary>
    public static string Bows() =>
        Asked == 0
            ? "nobody has been offered a kite"
            : $"{Asked} asked: {Handless} were melee and have no kite to give, {Kited} gave ground, {Distant} were far enough off to simply shoot, {Rushed} had the shot too near, {Closed} drew steel instead — longest clock seen {Longest}ms against {StillMs + KiteSlackMs}ms needed; "
              + $"{Steady} of them had been standing still long enough for the engine to loose an arrow ({SteadyBows} of the bows' own asks), longest stillness seen {Stillest}ms against the {StillMs}ms it wants; "
              + $"{Outpaced} beats an archer stood and shot rather than give ground to a quarry that steps as fast as it runs, {Planted} fights an archer stopped backing off in after {PlantMs / 1000}s with the quarry still inside {TooClose} tiles; "
              + $"{Backed} back-off walks ordered, {Due} beats a crowded archer stood for an arrow that was due instead, {Halted} retreats ended for one, {Unpaid} fights an archer planted in after {RetreatMs / 1000}s without the quarry's health falling";

    /// <summary>Zeroes the kite's tally.</summary>
    public static void ForgetBows()
    {
        Asked = 0;
        Handless = 0;
        Distant = 0;
        Rushed = 0;
        Kited = 0;
        Longest = 0;
        Closed = 0;
        Stillest = 0;
        Steady = 0;
        SteadyBows = 0;
        Outpaced = 0;
        Planted = 0;
        Backed = 0;
        Due = 0;
        Halted = 0;
        Unpaid = 0;
        Rebased = 0;
        Dragged = 0;
        Unleashed = 0;
        StaleResumes = 0;
    }

    /// <summary>
    /// The longest a bow-carrying bot has been standing still at the moment it was asked, in milliseconds.
    ///
    /// <para>
    /// <b>The number the whole archer question turns on, and nothing had ever read it.</b> Pre-AOS,
    /// <c>BaseRanged.OnSwing</c> fires only when <c>Core.TickCount - attacker.LastMoveTime >= 1000</c>; if the
    /// shooter has moved inside that second the engine does not fire and does not wait either — it returns a
    /// quarter of a second and tries again. So an archer that is never still for a full second never looses an
    /// arrow, and its weapon clock never advances past 250ms, which is precisely what the kite's own tally was
    /// reporting: <b>longest clock seen 227ms against the 1,700ms it waits for</b>, over four thousand asks.
    /// </para>
    ///
    /// <para>
    /// Read here rather than reasoned about, because "the bot is standing" and "the engine thinks the bot is
    /// standing" are different claims and only the second one fires a bow.
    /// </para>
    /// </summary>
    public static long Stillest { get; private set; }

    /// <summary>Asks at which the bot had been still long enough for the engine to loose an arrow; the same, of bows only.</summary>
    public static long Steady { get; private set; }

    public static long SteadyBows { get; private set; }

    private bool Kiting(Mobile body, int reach)
    {
        Asked++;

        var still = Core.TickCount - body.LastMoveTime;

        if (still > Stillest)
        {
            Stillest = still;
        }

        if (still >= StillMs)
        {
            Steady++;
        }

        if (reach <= 1 || (body.Weapon?.MaxRange ?? 1) <= 1)
        {
            Handless++;

            return false;
        }

        // Counted again past the melee gate, because Steady above counts every bot in every fight and a swordsman is still
        // nearly always; Steady read 123 % of the archers' asks on 12.10.2026. This is the share the archer question turns on.
        if (still >= StillMs)
        {
            SteadyBows++;
        }

        // <b>Close enough to be worth backing away from, not merely close enough to shoot.</b> This read
        // reach - 1 and so fired at nine tiles for a bow. See KiteWithin.
        if (!body.InRange(_quarry.Location, KiteWithin))
        {
            Distant++;

            return false;
        }

        // Nothing to gain from walking away from what walks as fast (01.10.2026, build next). See GainsGround.
        if (!GainsGround(body))
        {
            Outpaced++;
            Stand(body);

            return false;
        }

        var left = body.NextCombatTime - Core.TickCount;

        if (left > Longest)
        {
            Longest = left;
        }

        if (left <= StillMs + KiteSlackMs)
        {
            Rushed++;

            return false;
        }

        Kited++;
        _kited++;

        return true;
    }

    /// <summary>
    /// A place to stand at most <paramref name="want"/> tiles from the quarry, on the far side of the bot from
    /// it, or nothing if no such place both holds a body and can see the quarry from where it stands.
    ///
    /// <para>
    /// Straight back along the line the creature came in on, and no cleverness beyond that: a bot that tries
    /// to be clever about where it retreats to is a bot computing a second pathfinder. If the ground behind it
    /// will not take it, it stands and fights, which is the honest fallback.
    /// </para>
    ///
    /// <para>
    /// <b>Sight is a condition of the place, and leaving it out is most of why archers never fired.</b>
    /// Retreat was written as a distance and nothing else, so a bow gave ground until something came between
    /// it and the quarry — and from there it could not shoot, because the engine refuses a swing without a
    /// line. The weapon clock then never advances, and <see cref="Kiting"/> waits on exactly that clock, so
    /// the kite dies with the shot it was supposed to be buying time for. The far point is tried first and
    /// the line walked inwards a tile at a time, so a bot gives up as much of its reach as the ground costs
    /// it and not one tile more.
    /// </para>
    /// </summary>
    private static Point3D Away(Mobile body, Mobile from, int want)
    {
        var map = body.Map;

        if (map == null)
        {
            return Point3D.Zero;
        }

        var dx = body.X - from.X;
        var dy = body.Y - from.Y;

        if (dx == 0 && dy == 0)
        {
            return Point3D.Zero;
        }

        var step = Math.Max(Math.Abs(dx), Math.Abs(dy));

        var mark = from.Location;

        mark.Z += Eye;

        for (var d = want; d >= 1; d--)
        {
            // Normalised to a unit direction, then thrown out to the distance being tried.
            var x = from.X + dx * d / step;
            var y = from.Y + dy * d / step;

            if (!BotStep.Settle(map, x, y, out var z))
            {
                continue;
            }

            if (map.LineOfSight(new Point3D(x, y, z + Eye), mark))
            {
                return new Point3D(x, y, z);
            }
        }

        return Point3D.Zero;
    }

    /// <summary>
    /// Whether walking away from the quarry opens the distance: not once this fight has planted the archer, and only from a
    /// creature whose step (<c>ActiveSpeed</c>, seconds a tile) is at least <see cref="OutpaceShare"/> times the bot's own run
    /// (<see cref="FootStepMs"/>, or <see cref="MountStepMs"/> mounted). See the note at the back-off in <c>Fighting</c>.
    /// </summary>
    private bool GainsGround(Mobile body)
    {
        if (_planted)
        {
            return false;
        }

        var step = _quarry?.ActiveSpeed ?? 0.0;

        // Already standing: a quarry as fast as the bot is not retreated from, so there is no retreat to pay for (review B).
        if (step > 0.0 && step * 1000.0 < (body.Mounted ? MountStepMs : FootStepMs) * OutpaceShare)
        {
            return false;
        }

        // A retreat must be paid for: RetreatMs without a new low in the quarry's health, measured from the fight's own start
        // (a resumed hunt's walk back is not a retreat), and the archer plants for the rest of the fight. See RetreatMs.
        if (RetreatMs > 0)
        {
            var since = _progressTick - _fightTick >= 0 ? _progressTick : _fightTick;

            if (Core.TickCount - since >= RetreatMs)
            {
                _planted = true;
                Unpaid++;

                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The archer stops where it is: a back-off or a kite still being walked is finished, or the walk would carry on after the
    /// fight has answered Work, and a bow fires only for a body a second still. Answers whether a retreat this hunt ordered was
    /// still being walked and is ended here — the honest count for <see cref="Halted"/>: the journey's bottom errand is the
    /// retreat's own point (an interruption stacked on top of it is still a retreat ended; an "after X" follow, a walk of
    /// somebody else's, or a retreat that already arrived is not).
    /// </summary>
    private bool Stand(Mobile body)
    {
        _backing = Point3D.Zero;

        var ended = false;

        if (body is BotMobile { Journey: { Active: true } journey })
        {
            ended = _retreatTo != Point3D.Zero
                    && journey.Bottom is { Interruption: false, Follow: null } bottom
                    && bottom.Where == _retreatTo;

            journey.Finish();
        }

        _retreatTo = Point3D.Zero;

        return ended;
    }

    /// <summary>How much slower than the archer a quarry must step for giving ground to be worth it. A quarter.</summary>
    public static double OutpaceShare { get; set; } = 1.25;

    /// <summary>
    /// A running bot's step afoot, in milliseconds, as measured rather than as the engine names it.
    ///
    /// <para>
    /// <b>The engine's 200 passed every quarry that steps at 0.25 s a tile, which is 92 % of the ones the archers failed
    /// against (12.10.2026, w4).</b> The bot is stepped by its beat — <c>BotBeat</c> schedules the next turn at now + 200 on a
    /// 100 ms clock — so its measured pace is 4.4 tiles a second (227 ms a tile, median of 1 054 foot walks), and a wolf, a
    /// bear, a bull or a horse at 0.25 s ("Medium" in npc-speeds) follows at 4.0: against those a retreat gains under half a
    /// tile a second and the stand that must follow it gives all of that back. The threshold reads 200 × 1.25 = 250 ≥ 250, so
    /// the archer was told it could leave them behind, and it ran 3–6 tiles ahead of them for 45 s. At 225 the threshold is
    /// 281: a 0.25 s quarry is stood against from the first beat, a 0.3 s one (Slow) is still kited. The archer's test only:
    /// the run for a town reads its own <c>BotHaven.FootStepMs</c> (it read this one until review B of 12.10.2026 found the
    /// move would refuse the town to every runner from a 0.2 s creature beyond <c>BotHaven.NearTiles</c>).
    /// </para>
    /// </summary>
    public static int FootStepMs { get; set; } = 225;

    /// <summary>
    /// The same mounted. Left at the engine's run delay: a mounted bot short of breath walks at 200, which the measured pace
    /// (297 mounted walks, the same 227) says is what fighting mounted archers do, and on those <see cref="StandsWhenDue"/>
    /// and <see cref="RetreatMs"/> govern; a mounted bot with breath does run at this.
    /// </summary>
    public static int MountStepMs { get; set; } = 100;

    /// <summary>
    /// Whether an archer whose arrow is due stands and shoots rather than back off, and ends a retreat still being walked.
    ///
    /// <para>
    /// The switch on the rule written at the back-off in <see cref="Fighting"/>: 85–90 % of "would not go down", ~42 an hour
    /// on 101–103 bots, were an archer walking away from a same-pace quarry on the beat the next arrow came due. Off, the
    /// archer backs off whenever the quarry is inside <see cref="TooClose"/>, as before 12.10.2026.
    /// </para>
    /// </summary>
    public static bool StandsWhenDue { get; set; } = true;

    /// <summary>
    /// How long a retreat may go without the quarry's health making a new low before the archer plants for the fight.
    ///
    /// <para>
    /// <b>The plant clock that was here could not accrue.</b> <see cref="PlantMs"/> counts unbroken beats with the quarry
    /// inside TooClose and is zeroed on every beat it is outside; a quarry that follows at the archer's own pace swings between
    /// three and six tiles, so three seconds never gathered, and the kite never fed it at all: 1 253 plants against 41 failures
    /// an hour on 12.10.2026. Measured instead from the last fall in the quarry's health — <c>_progressTick</c>, the verdict's
    /// own clock — because that is what a retreat is for: a retreat that has bought no arrow in ten seconds is a bot running.
    /// Ten is well under the 45 s the verdict allows and covers two misses at the 3.75 s reload of a rested archer (7.5 s);
    /// at the slowest reload, 6.3 s at stamina 25, one miss and the wait to the next shot (12.6 s) plants the archer — meant:
    /// an archer that short of breath cannot outrun anything, and the fight it then stands is the kind the measurement found
    /// won (planted fights kept a median 92 % of health). Asked only of a quarry the archer could retreat from; one it already
    /// stands against (<see cref="Outpaced"/>) is not counted. Nought turns it off. Counted in <see cref="Unpaid"/>.
    /// </para>
    /// </summary>
    public static int RetreatMs { get; set; } = 10000;

    /// <summary>How long an archer backs off with the quarry still inside <see cref="TooClose"/> before it stands for good.</summary>
    public static int PlantMs { get; set; } = 3000;

    /// <summary>Since when backing off has left the quarry inside TooClose; nought when it is not. See <see cref="PlantMs"/>.</summary>
    private long _crowdedTick;

    /// <summary>Whether this fight's archer has stopped giving ground. See <see cref="GainsGround"/>.</summary>
    private bool _planted;

    /// <summary>Beats an archer stood and shot rather than give ground to a quarry as fast as itself; fights it stopped backing off in.</summary>
    public static long Outpaced { get; private set; }

    public static long Planted { get; private set; }

    /// <summary>
    /// Back-off walks ordered; beats a crowded archer stood for an arrow that was due instead of backing off; retreats
    /// (kites and back-offs) still being walked that were ended for a due arrow; fights an archer planted in because
    /// <see cref="RetreatMs"/> passed without the quarry's health falling. All printed in <see cref="Bows"/>.
    /// </summary>
    public static long Backed { get; private set; }

    public static long Due { get; private set; }

    public static long Halted { get; private set; }

    public static long Unpaid { get; private set; }

    /// <summary>
    /// What the weapon in the bot's hand has to fire, by its own ammunition type: "131 arrows", "0 bolts", or "no bow". A
    /// crossbow holding arrows reads as empty, which it is.
    /// </summary>
    private static string Quiver(Mobile body)
    {
        if (body.Weapon is not BaseRanged bow)
        {
            return "no bow";
        }

        var count = body.Backpack?.GetAmount(bow.AmmoType) ?? 0;

        return bow.AmmoType == typeof(Bolt) ? $"{count} bolts" : $"{count} arrows";
    }

    /// <summary>Whether the quarry is out of the fight, however it got there.</summary>
    private bool Down() =>
        _quarry == null || _quarry.Deleted || !_quarry.Alive || _quarry.Map != _map;

    /// <summary>
    /// Whether this hunt is at its fight now: the quarry within weapon reach and still standing. A company's call leaves such a
    /// hunter to its fight (07.10.2026, work18). See <c>BotSquads.Fighting</c>.
    /// </summary>
    public bool Engaged => _leg == Leg.Fight && !Down();
}
