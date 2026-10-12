using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Getting away from whatever is killing it. The one piece of work in this project whose whole product is
/// that the bot still exists afterwards.
///
/// <para>
/// <b>The rung it answers has had a proposer for a while and it was only half the answer.</b>
/// <c>Failing</c> is the rung for a bot whose health is going, and the only thing offering work on it was
/// <see cref="BotMedic"/> — mending, standing still, where it stands. That is right when a bot is simply
/// hurt and wrong in the one case the rung exists for: three creatures on one bot, the bot at a third of
/// its health, winding a bandage that takes several seconds while all three go on hitting it. Watched from
/// a client it reads as a bot that has decided to die politely, and it is the same defect the ladder's own
/// notes describe twice — <em>standing still is not an option at any number</em> — reappearing as the thing
/// the rung does instead of standing still.
/// </para>
///
/// <para>
/// <b>It walks away from the worst of it and no further than that.</b> There is no clever route and there
/// must not be: a bot computing an escape path is a bot spending the population's whole path-search budget
/// at the moment it can least afford to stand about. Straight back along the line the creature came in on,
/// recomputed only on arrival, and towards home when straight back would leave the ground the population
/// lives on.
/// </para>
/// </summary>
public sealed class BotBolt : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBolt));

    /// <summary>How long a flight may go without a step before it says why. See <see cref="Advance"/>.</summary>
    public static int StuckMs { get; set; } = 4000;

    /// <summary>Interruptions dropped from a fleeing bot's road; flights that went <see cref="StuckMs"/> without a step.</summary>
    public static long Shed { get; private set; }

    public static long Stuck { get; private set; }

    private Point3D _lastAt;

    private long _movedTick;

    private bool _saidStuck;

    private bool _saidShed;

    private int _beats;

    private int _walkBeats;

    private int _leastStam = int.MaxValue;

    /// <summary>Flights that ended in death; of them, the ones that went at a walk for more than half their beats.</summary>
    public static long Fell { get; private set; }

    public static long FellWalking { get; private set; }

    /// <summary>
    /// <b>Said when a bot dies running (30.09.2026, build 341).</b> Flights from casters were fatal two times in three on
    /// 337–340 whether early or late (`scratchpad/flightx.py`), and the fatal ones covered 34 to 69 tiles in 11 to 28 seconds —
    /// about a walk's pace, not a run's; Osric's stuck flight read "stamina 0 of 50 (walking)". How long the flight lasted,
    /// how far it got, how many of its beats were at a walk and the least stamina it had, from <c>BotMobile.OnDeath</c>.
    /// </summary>
    public void Died(Mobile body, Point3D fellAt)
    {
        if (body == null || !_started)
        {
            return;
        }

        Fell++;

        if (_walkBeats * 2 > _beats)
        {
            FellWalking++;
        }

        // A death on the run for a town, counted apart (02.10.2026): Patrick's order was the run; whether it saves anybody
        // is this number against Reached and RunnerGuardKills.
        if (ForTown)
        {
            FellOnWay++;
        }

        // <b>Poison and the cure, at the death (01.10.2026, build 388).</b> 67 % of 386's flight deaths were to giant serpents,
        // giant spiders and whipping vines, which poison; whether the runner was poisoned when it fell, and whether it had a
        // cure it never drank, was not written anywhere. An instrument.
        // As it died, not as the engine left it: Kill clears the poison before OnDeath. See BotMobile.PoisonAtDeath.
        var poison = (body as BotMobile)?.PoisonAtDeath ?? body.Poison;
        var cures = BotMend.Bottles(body, BotPotionKind.Cure);

        if (poison != null)
        {
            FellPoisoned++;

            if (cures > 0)
            {
                FellWithCure++;
            }
        }

        logger.Information(
            "{Name} died running from ({FromX}, {FromY}) at ({X}, {Y}), {Tiles} tiles in {Seconds}s: {Walked} of {Beats} beats at a walk, stamina at least {Least} of {StamMax}{Mounted}; poison {Poison}, {Cures} cure bottles in the pack",
            body.Name,
            _from.X,
            _from.Y,
            fellAt.X,
            fellAt.Y,
            Math.Max(Math.Abs(fellAt.X - _from.X), Math.Abs(fellAt.Y - _from.Y)),
            (Core.TickCount - _begun) / 1000,
            _walkBeats,
            _beats,
            _leastStam == int.MaxValue ? body.Stam : _leastStam,
            body.StamMax,
            body.Mounted ? ", mounted" : "",
            poison == null ? "none" : $"level {poison.Level}",
            cures
        );
    }

    /// <summary>The ledger's key.</summary>
    public const string Trade = "flee";

    /// <summary>
    /// What getting away is reckoned at per minute.
    ///
    /// <para>
    /// <b>Enormous, and it is not a thumb on the scale.</b> Everything on this shard is priced in
    /// gold-equivalent per minute so that wants can be compared, and what a death actually costs is the
    /// unbound half of the kit, the purse, the resurrection walk and the three minutes the ledger charges
    /// for a job that never finished. Against that, mending at thirty a minute is what it is worth and this
    /// is what this is worth; a number small enough to lose to a bandage would be a number that says a bot
    /// should stand and be killed tidily.
    /// </para>
    ///
    /// <para>
    /// It also has to survive its own measurement. The ledger blends this prior with what flight actually
    /// pays — which is nothing, for ever — so a place fled from a dozen times settles at roughly a seventh
    /// of what is written here. That floor is the number worth reading, not this one.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 2000.0;

    /// <summary>How long getting clear is expected to take.</summary>
    public static double WorkMinutes { get; set; } = 0.5;

    /// <summary>
    /// How far a bot looks for what it is running from, and therefore how far counts as away.
    ///
    /// Fourteen tiles: outside a bow's ten and a caster's eight with enough margin that the thing has to
    /// actually follow to get back into range. One number for both questions on purpose — "what is after me"
    /// and "am I clear of it" are the same question asked twice, and answering them with two numbers is how
    /// this project keeps producing a bot that is neither fleeing nor fighting.
    /// </summary>
    public static int Watch { get; set; } = 14;

    /// <summary>
    /// How far to head in one go. Beyond the watch, so arriving means being clear rather than being asked
    /// again; short enough that the ground is still ground the bot knows.
    /// </summary>
    public static int Bound { get; set; } = 18;

    /// <summary>
    /// What one look at the road to a retreat may cost. Nought turns the look off.
    ///
    /// <para>
    /// <b>47 flights in three hours of build 272 failed "no way through" to the place they picked (29.09.2026).</b> A
    /// retreat was a settled tile eighteen away on the far side of the bot from the threat, and a settled tile is a tile
    /// a body can stand on, not one it can get to: water, a fence, a cave wall between. Bend turned to the next direction
    /// only after the walker had refused the first, and a refusal is a plan and a failed leg. A search of eighteen tiles
    /// is a fraction of a millisecond when there is a way and this ceiling when there is not, so the way is asked for
    /// before the leg is walked, best direction first.
    /// </para>
    /// </summary>
    public static double VetMs { get; set; } = 6.0;

    /// <summary>Retreats refused by the look ahead and turned another way before anybody walked. See <see cref="VetMs"/>.</summary>
    public static long Vetoed { get; private set; }

    /// <summary>
    /// <b>A flight runs towards help (29.09.2026).</b> Twenty-two of the twenty-nine deaths in the first eighteen minutes
    /// of build 290 were bots on the run, dead between nought and twenty-seven seconds after taking flight, and the
    /// flight ran straight away from the threat into whatever lay that way — alone, with a troll a step behind. The
    /// eight ways are still all away from the threat; among those, the one that also leads towards the nearest allies
    /// within <see cref="HelpReach"/>, or towards home when none are near, is ranked first by this much of a full step
    /// per unit of alignment. A troll that follows a bot into three of its guildmates is a troll the company kills;
    /// the same troll following it into a wood is a death. Nought restores the plain "straight away".
    /// </summary>
    public static double HelpWeight { get; set; } = 0.75;

    /// <summary>How far a flight looks for allies to run towards. Sixty tiles: further than that, home is the help.</summary>
    public static int HelpReach { get; set; } = 60;

    /// <summary>Flights that ran towards allies, and towards home. See <see cref="HelpWeight"/>.</summary>
    public static long TowardAllies { get; private set; }

    public static long TowardHome { get; private set; }

    /// <summary>
    /// Where help lies from here: the nearest fighting ally beyond arm's reach within <see cref="HelpReach"/>, else home
    /// on the island. Returned as a unit-ish direction in (hx, hy), each −1, 0 or 1; false when there is none.
    /// </summary>
    private static bool Help(Map map, Mobile body, bool under, out int hx, out int hy)
    {
        hx = 0;
        hy = 0;

        if (HelpWeight <= 0.0)
        {
            return false;
        }

        Mobile nearest = null;
        var nearestAt = int.MaxValue;

        foreach (var mobile in map.GetMobilesInRange<Mobile>(body.Location, HelpReach))
        {
            if (mobile == body || mobile is not IBotAlly { AbleToFight: true } || !mobile.Alive)
            {
                continue;
            }

            var at = Math.Max(Math.Abs(mobile.X - body.X), Math.Abs(mobile.Y - body.Y));

            // Company already at the bot's side is not somewhere to run to.
            if (at <= 3 || at >= nearestAt)
            {
                continue;
            }

            nearest = mobile;
            nearestAt = at;
        }

        int tx, ty;

        if (nearest != null)
        {
            tx = nearest.X;
            ty = nearest.Y;
            TowardAllies++;
        }
        else if (!under)
        {
            // Where this bot lives, not the population's home (29.09.2026).
            var home = BotPopulation.HomeOf(body);

            if (home == Point3D.Zero || Math.Max(Math.Abs(home.X - body.X), Math.Abs(home.Y - body.Y)) <= 3)
            {
                return false;
            }

            tx = home.X;
            ty = home.Y;
            TowardHome++;
        }
        else
        {
            return false;
        }

        hx = Math.Sign(tx - body.X);
        hy = Math.Sign(ty - body.Y);

        return hx != 0 || hy != 0;
    }

    /// <summary>
    /// How long a flight may go on before it is given up as not working.
    ///
    /// <para>
    /// Half a minute. Something that keeps pace with a bot for half a minute is not going to be outrun, and
    /// a bot that runs for ever is a bot dragging a train of creatures across the whole population's ground.
    /// Giving up hands it back to the rung, which will offer mending — a bandage under fire is a poor answer
    /// and it is a better one than sprinting until dead.
    /// </para>
    /// </summary>
    public static int GiveUpMs { get; set; } = 30000;

    private readonly Map _map;

    private readonly Point3D _from;

    private Point3D _to;

    private long _begun;

    private bool _started;

    private int _legs;

    /// <summary>The least this flight runs before nothing-following may end it. See the second constructor.</summary>
    private readonly int _least;

    public BotBolt(Map map, Point3D from)
    {
        _map = map;
        _from = from;
    }

    /// <param name="leastMs">
    /// The least this flight runs before "nothing is following" may end it.
    ///
    /// <para>
    /// <b>A robber that struck out of hiding is not a threat anything can see.</b> The first mark ever given
    /// Patrick's choice, at 20:32:23 on 18.09.2026, answered "run" — and the flight ended in the same second,
    /// "nothing following", because <c>BotThreat.Strongest</c> looks for something in warmode standing near and the
    /// thief had just hidden again. Nessa Ashdown went straight back to fighting Ilsa, which is not running away by
    /// any reading. A flight out of a robbery therefore runs for a spell whatever it can see behind it.
    /// </para>
    /// </param>
    public BotBolt(Map map, Point3D from, int leastMs) : this(map, from)
    {
        _least = Math.Max(0, leastMs);
    }

    /// <summary>
    /// <b>A run for a town (02.10.2026, Patrick: "if a bot is beaten on the road and understands it cannot win the fight, it
    /// must run to town, because the guards are supposed to kill hostile creatures that fight within the town limits").</b>
    /// The flight heads for <paramref name="haven"/>, the guarded edge of <paramref name="town"/> that <see cref="BotHaven"/>
    /// judged a realistic escape, instead of eighteen-tile legs away from the threat; inside the town it calls the guards the
    /// way a player does (<see cref="GuardsEveryMs"/>) and does not swing at what follows it (<see cref="HoldsSwing"/>), since
    /// the swing is what makes the guards blind to it. See BotHaven for what the engine's guards actually do.
    /// </summary>
    public BotBolt(Map map, Point3D from, Point3D haven, string town, int tiles) : this(map, from)
    {
        _haven = haven;
        _town = town ?? "a town";
        _havenTiles = tiles;
    }

    private Point3D _haven;

    private string _town;

    private int _havenTiles;

    private bool _reached;

    private long _calledTick;

    /// <summary>The thing this run is watched against, so that a guard's kill of it can be counted.</summary>
    private Mobile _watched;

    private bool _guardKilled;

    /// <summary>Whether this flight is a run for a town. See the third constructor.</summary>
    public bool ForTown => _haven != Point3D.Zero;

    /// <summary>The town run for, for the log.</summary>
    public string Town => _town;

    /// <summary>
    /// How long a run for a town may go on with the thing still on the bot. Two and a half minutes: the run itself is under
    /// <see cref="BotHaven.MostSeconds"/>, and a creature the bot struck before it ran stays blind to the guards for the
    /// engine's two minutes after the last swing (<c>AggressorInfo.ExpireDelay</c>) — a bot standing in the town and not
    /// swinging is innocent to it again inside this clock, and its next blow is the criminal one the guards answer.
    /// </summary>
    public static int HavenMs { get; set; } = 150000;

    /// <summary>How often, standing in the town with the thing still on it, the runner calls for the guards. Three seconds.</summary>
    public static int GuardsEveryMs { get; set; } = 3000;

    /// <summary>
    /// Whether a bot on a run for a town, and a bot outmatched that has not struck what hits it, holds its swing. See
    /// <c>BotMobile.OnDamage</c>: the reflex squares the bot up to whatever hit it, and one swing puts the bot in the
    /// creature's aggressors for two minutes, during which nothing it does in town brings a guard.
    /// </summary>
    public static bool HoldsSwing { get; set; } = true;

    /// <summary>Runs for a town begun, and how they went: reached guarded ground, called the guards, saw a guard kill the pursuer, died on the way.</summary>
    public static long Runs { get; private set; }

    public static long Reached { get; private set; }

    public static long GuardsCalled { get; private set; }

    /// <summary>Calls made while the runner stood in the pursuer's aggressors — calls the guards cannot answer.</summary>
    public static long RunnerCallsBlind { get; private set; }

    public static long RunnerGuardKills { get; private set; }

    public static long FellOnWay { get; private set; }

    /// <summary>Runs that reached the town and still could not shake the thing inside <see cref="HavenMs"/>.</summary>
    public static long UnshakenInTown { get; private set; }

    /// <summary>Swings held by a bot outmatched that had not struck its attacker. See <see cref="HoldsSwing"/>.</summary>
    public static long SwingsHeld { get; set; }

    public override string Kind => Trade;

    /// <summary>Running away is never kept out of anywhere: it is how a bot leaves bad ground. See <see cref="BotDeed.Braves"/>.</summary>
    public override bool Braves => true;

    /// <summary>A flight put down for a bandage is taken up again and, if nothing follows, finishes at once. See <see cref="BotDeed.Resumes"/>.</summary>
    public override bool Resumes => true;

    /// <summary>
    /// A second flight adds nothing the first is not already doing.
    ///
    /// <para>
    /// The fugitive offers a fresh flight every review while the bot is still losing, and the fresh one was
    /// priced above the one in hand — "interrupted by flee at 327/min ... taken at 174/min" — so the flight
    /// in hand was dropped and the new one began from the same spot: Kestrel three times in a minute on
    /// 28.09.2026, 21 of the half hour's 42 flee drops. The flight in hand already re-aims at every arrival
    /// (<see cref="Advance"/>) and turns when refused a road (<see cref="Bend"/>); a replacement is the same
    /// flight with its clock reset and a drop on the record.
    /// </para>
    /// </summary>
    public override bool Repeats(BotDeed other) => other is BotBolt;

    /// <summary>
    /// What whoever offered this flight wants done when it is taken, not when it is offered: the count against the runs a bot
    /// is allowed, and the line that says it ran. See <c>BotFugitive.Propose</c> (06.10.2026).
    /// </summary>
    internal Action Began { get; init; }

    public override void Taken(IBotWilful bot)
    {
        base.Taken(bot);

        // A run for a town begun, counted where it is taken: the constructor counted every offer of one, and a flight in hand
        // is offered again on every beat.
        if (ForTown)
        {
            Runs++;
        }

        Began?.Invoke();
    }

    public override Map Map => _map;

    /// <summary>
    /// Where it was standing when it decided to run, never where it ends up.
    ///
    /// The ledger files outcomes by patch of ground and the fact worth filing is <em>this patch made me
    /// run</em>. Filing it under the safe place the bot reached would teach it that safety is dangerous.
    /// </summary>
    public override Point3D Where => _from;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    /// <summary>Nothing. Running teaches a bot nothing the engine will write on its sheet.</summary>
    public override SkillName? Trains => null;

    public override int Outlay => 0;

    /// <summary>
    /// Counted as coin, and it produces none.
    ///
    /// <para>
    /// A deliberate lie, and the alternative is worse. Work that pays in anything but money is discounted by
    /// how badly the bot needs money — and a bot that needs it badly enough discounts such work to nothing,
    /// which is a veto rather than a discount. Written honestly as zero, this would read "a bot with an empty
    /// purse may not run away", and the bots on this shard are born with an empty purse. What that factor is
    /// for is choosing between ways of earning, and there is no earning on this rung to choose between.
    /// </para>
    /// </summary>
    public override double Coin => 1.0;

    public override int Made => 0;

    public override string Stage =>
        ForTown
            ? _reached ? $"in {_town}, calling the guards" : $"running for {_town}"
            : _to == Point3D.Zero ? "getting away" : $"getting away to {_to}";

    /// <summary>How long after the last blow a flight is still a flight. See <see cref="Pursued"/>.</summary>
    public static int ClearCalmMs { get; set; } = 4000;

    /// <summary>How far off a creature that has the bot for its combatant keeps the flight going.</summary>
    public static int Pursuit { get; set; } = 24;

    /// <summary>Flights kept going past Watch for a blow still landing or a pursuer still coming.</summary>
    public static long Pursuits { get; private set; }

    private Server.Mobiles.BaseCreature _pursuer;

    private bool Pursued(IBotWilful bot, Mobile body, long now)
    {
        _pursuer = null;

        foreach (var creature in body.Map.GetMobilesInRange<Server.Mobiles.BaseCreature>(body.Location, Pursuit))
        {
            if (creature is { Deleted: false, Alive: true } && creature.Combatant == body)
            {
                _pursuer = creature;
                Pursuits++;

                return true;
            }
        }

        if (bot?.Resolve is { Struck: true } resolve && now - resolve.HurtTick < ClearCalmMs)
        {
            Pursuits++;

            return true;
        }

        return false;
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal || !body.Alive)
        {
            return BotDoing.Failed("no body");
        }

        var now = Core.TickCount;

        if (!_started)
        {
            _started = true;
            _begun = now;
            _lastAt = body.Location;
            _movedTick = now;
        }

        if (body.Location != _lastAt)
        {
            _lastAt = body.Location;
            _movedTick = now;
        }

        _beats++;

        if (body is BotMobile { Running: false })
        {
            _walkBeats++;
        }

        if (body.Stam < _leastStam)
        {
            _leastStam = body.Stam;
        }

        // <b>Nothing may sit on top of the road away (30.09.2026, build 340).</b> See BotJourney.Shed: a "fight" pushed
        // before the flight began followed its creature, so the walker took the bot to the thing it was running from and
        // stood it there, Arrived. Every beat, because a stable's "a horse" can be pushed mid-flight too.
        if (body is BotMobile { Journey: { } road })
        {
            var top = road.Current is { Interruption: true } over ? over.Reason : null;

            if (road.Shed() > 0)
            {
                Shed++;

                if (!_saidShed)
                {
                    _saidShed = true;
                    logger.Information(
                        "{Name} drops its {Top} to run: the walk away had been kept under it",
                        body.Name,
                        top ?? "detour"
                    );
                }
            }
        }

        // Said every beat, not once. Every blow that lands re-points the bot at whatever hit it — see
        // BotMobile.OnDamage — so a flight that dropped its combatant once would be swinging again a
        // fraction of a second later, and a bot that is swinging is a bot standing still.
        body.Combatant = null;
        body.Warmode = false;

        // <b>Bandaged on the run, as a player does.</b> A bandage winds on while its bearer moves — the engine only slips it
        // on a blow — and a bottle is swallowed in a step. Without this a hurt bot alternated two pieces of work: it stopped
        // to mend itself, something came near, it ran, the mend was dropped, it stopped to mend again — Doran Ashdown six
        // times in a minute and Gerda four in twenty seconds on build 209 (11:35–11:38, 24.09.2026), fifteen of the window's
        // 59 drops. See BotMend.Wind and BotMend.Draught.
        if (BotMend.Share(body) < BotMend.Hurt || body.Poisoned)
        {
            if (!BotMend.Winding(body) && BotMend.Wind(body, body))
            {
                Tended++;
            }

            if (BotMend.Draught(body) is { } bottle && BotMend.Swallow(body, bottle))
            {
                Tended++;
            }
        }

        var worst = BotThreat.Strongest(body, Watch);

        // <b>Clear means nothing is still at it (29.09.2026, build 335).</b> "Nothing strong within Watch" ended the flight
        // while a caster beyond fourteen tiles was still casting, or a creature was still coming: on 333, eight of the flights
        // that "finished" were followed by that bot's death within twenty seconds at other work — Petra "clear of it after
        // 1 legs" from a wraith, mending herself, dead five seconds later (`night-r5-flights.md`). Not clear while something
        // hit the bot inside ClearCalmMs, or while anything within Pursuit has the bot for its combatant.
        if (worst == null && Pursued(bot, body, now))
        {
            worst = _pursuer;
        }

        // The run for a town: its own clock, its own ending, and the guards. See the third constructor.
        if (ForTown)
        {
            return RunForTown(bot, body, worst, now);
        }

        if (worst == null && now - _begun >= _least)
        {
            // Remembered, so that the fight at the edge of Watch cannot flip it back into flight on the next beat. See
            // BotFugitive.CalmMs.
            BotFugitive.Cleared(body);

            return BotDoing.Done(_legs > 0 ? $"clear of it after {_legs} legs" : "nothing following");
        }

        if (now - _begun >= GiveUpMs)
        {
            // <b>"Could not shake it" is not an escape (01.10.2026, build 388).</b> It was finished rather than failed so the
            // patch would not be marked with caution; on 386, 154 flights ended so and counted among the 65 % "finished", with
            // 178 flights dead — the honest clean share was 39 %. A flight that ends with the thing still on the bot failed, and
            // the ground it ran from is where the thing that kept pace lives: the caution is right. The breaker never rests
            // running (Spared), and the fugitive stands on the third run (Winded) as before.
            if (worst == null)
            {
                return BotDoing.Done($"ran for {(now - _begun) / 1000}s");
            }

            Unshaken++;

            return BotDoing.Failed($"could not shake {worst.Name}");
        }

        // Recomputed on arrival rather than every beat, and that is the whole of the cost control here.
        // Aiming at a point that moves with the creature would buy a fresh path search on every step, per
        // fleeing bot, at exactly the moment the population has several of them.
        if (_to == Point3D.Zero || body.InRange(_to, 1))
        {
            _to = Retreat(_map, body, worst);
            _legs++;
        }

        if (_to == Point3D.Zero)
        {
            // Nowhere behind it holds a body. Standing and fighting is the honest fallback, and the rung will
            // offer a bandage in the same breath.
            // worst can be null when the thing that set the flight going is gone by the time the walk ends (10:10 on 07.10.2026, one NullReferenceException in BotBolt.Advance): the flight is still over.
            // <b>A cornered flight is a failed flight, and it is not offered again at once (07.10.2026, work18).</b> The flight's
            // promise was to get away and it did not; that stays a failure, counted apart from "could not shake". What follows is
            // the stand on the rung below, and BotFugitive.Cornered keeps a fresh flight from being offered for CorneredRestMs,
            // so the bot fights rather than starting another run into the same wall.
            EndedCornered++;
            BotFugitive.CorneredRun(body);

            return BotDoing.Failed($"cornered by {worst?.Name ?? "nothing it could name"}");
        }

        // The instrument for whatever else holds a fleeing bot where it stands: said once per flight, with the walker's
        // own last word on it.
        if (!_saidStuck && now - _movedTick >= StuckMs)
        {
            _saidStuck = true;
            Stuck++;

            var mobile = body as BotMobile;
            var queue = mobile?.Journey;

            logger.Information(
                "{Name} has not taken a step in {Seconds}s of running from {Foe} ({Tiles} tiles off) towards {To}: the walker last said {Walk}, the road's top is {Top} ({Errands} errands), stamina {Stam} of {StamMax} ({Gait}){Spell}{Paralyzed}{Frozen}",
                body.Name,
                (now - _movedTick) / 1000,
                worst?.Name ?? "nothing it can see",
                worst == null ? -1 : (int)body.GetDistanceToSqrt(worst),
                _to,
                mobile?.LastWalk.ToString() ?? "nothing",
                queue?.Current?.Reason ?? "nothing",
                queue?.Queued ?? 0,
                body.Stam,
                body.StamMax,
                mobile?.Running == true ? "running" : "walking",
                body.Spell != null ? ", mid-spell" : "",
                body.Paralyzed ? ", paralyzed" : "",
                body.Frozen ? ", frozen" : ""
            );
        }

        return BotDoing.Walk(_map, _to, BotArrival.Within(1), $"away from {worst.Name}");
    }

    /// <summary>
    /// The run for a town, beat by beat. Walks to the haven; inside the town, with the thing still on the bot, calls the
    /// guards every <see cref="GuardsEveryMs"/> as a player does — <c>DoSpeech</c> with the <em>guards</em> keyword, which
    /// is what the client sends and what <c>GuardedRegion.OnSpeech</c> answers — and walks on to the town's square so the
    /// chase crosses the town's own people, who call the guards on a criminal within eight tiles by themselves. Ends when
    /// nothing follows, or fails at <see cref="HavenMs"/> with the thing still on it. Whether the guards could answer the
    /// call is counted at the call (<see cref="RunnerCallsBlind"/>): a creature the bot struck in the last two minutes is not a
    /// criminal to the region for striking the bot back.
    /// </summary>
    private BotDoing RunForTown(IBotWilful bot, Mobile body, Mobile worst, long now)
    {
        var inTown = BotHaven.Guarded(_map, body.Location);

        if (inTown && !_reached)
        {
            _reached = true;
            Reached++;

            logger.Information(
                "{Name} reached {Town} at ({X}, {Y}) after {Seconds}s of running from ({FromX}, {FromY}), {Tiles} tiles; {Followed}",
                body.Name,
                _town,
                body.X,
                body.Y,
                (now - _begun) / 1000,
                _from.X,
                _from.Y,
                _havenTiles,
                worst == null ? "nothing follows" : $"{worst.Name} still on it"
            );

            // Whether the town saved it is asked a minute on, when something followed it in. See BotHaven.Reached.
            if (worst != null)
            {
                BotHaven.Reached(body, _town);
            }
        }

        // Whatever is on the bot now is the thing watched for a guard's kill.
        if (worst != null && !ReferenceEquals(worst, _watched))
        {
            _watched = worst;
        }

        if (!_guardKilled && _watched is { } watched && (watched.Deleted || !watched.Alive) && watched.LastKiller is Server.Mobiles.BaseGuard)
        {
            _guardKilled = true;
            RunnerGuardKills++;

            logger.Information("{Name}'s pursuer {Foe} was cut down by the guards of {Town}", body.Name, watched.Name, _town);
        }

        if (worst == null && now - _begun >= _least)
        {
            BotFugitive.Cleared(body);

            return BotDoing.Done(
                _reached
                    ? $"reached {_town}: {(_guardKilled ? "the guards cut it down" : "nothing follows")}"
                    : $"clear of it on the way to {_town}"
            );
        }

        if (now - _begun >= HavenMs)
        {
            if (worst == null)
            {
                return BotDoing.Done($"ran for {(now - _begun) / 1000}s towards {_town}");
            }

            Unshaken++;

            if (inTown)
            {
                UnshakenInTown++;
            }

            return BotDoing.Failed($"could not shake {worst.Name} {(inTown ? "in" : "on the way to")} {_town}");
        }

        if (inTown && worst != null && now - _calledTick >= GuardsEveryMs)
        {
            _calledTick = now;

            // Counted as the runner's only when said: a blow in town may have made the same bot call a moment ago, and the
            // two share one clock (BotHaven.CallEveryMs). What the call found is BotHaven's to count.
            var aggressed = BotHaven.Aggressed(body, worst);

            if (BotHaven.CallGuards(body, worst, $"on a run for {_town}"))
            {
                GuardsCalled++;

                if (aggressed)
                {
                    RunnerCallsBlind++;
                }
            }
        }

        // Deeper into the town while something still follows: the square, where the town's own people stand.
        var to = _haven;

        if (inTown && worst != null && body.InRange(_haven, 2))
        {
            var deeper = BotTowns.Of(body.Location)?.Square ?? _haven;

            if (deeper != Point3D.Zero && !body.InRange(deeper, 2))
            {
                to = deeper;
            }
            else
            {
                return BotDoing.Work($"calling the guards of {_town} on {worst.Name}");
            }
        }

        _to = to;

        if (!_saidStuck && now - _movedTick >= StuckMs)
        {
            _saidStuck = true;
            Stuck++;

            logger.Information(
                "{Name} has not taken a step in {Seconds}s of running for {Town} from {Foe} towards {To}: stamina {Stam} of {StamMax}{Paralyzed}{Frozen}",
                body.Name,
                (now - _movedTick) / 1000,
                _town,
                worst?.Name ?? "nothing it can see",
                to,
                body.Stam,
                body.StamMax,
                body.Paralyzed ? ", paralyzed" : "",
                body.Frozen ? ", frozen" : ""
            );
        }

        return BotDoing.Walk(_map, to, BotArrival.Within(2), $"for {_town} away from {worst?.Name ?? "it"}");
    }

    /// <summary>Places this flight has already been refused a road to, so that a bend never picks one of them again.</summary>
    private readonly List<Point3D> _refused = [];

    /// <summary>
    /// The way out turned out not to exist. Try the next way round the threat, then towards home, then give up.
    ///
    /// <para>
    /// <b>Underground the only other way was home, and home is across a wall.</b> Ilsa the Sage in the Orc Caves
    /// at 07:25 on 22.09.2026: the tile straight back from Woghuglat was off the island's ground, so Retreat
    /// answered with a leg towards home — west into the cave wall at (5288, 1340) — the road was refused, this
    /// bend offered the same leg, and the flight failed six times in two seconds until the breaker rested
    /// <em>running away</em>, which is the one thing a losing bot must never be rested from. The other seven
    /// directions were never asked. Now they are, worst first, and the ones already refused are kept out.
    /// </para>
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return false;
        }

        // A run for a town refused its road falls back to the plain flight: legs away from the thing. Counted.
        if (ForTown)
        {
            BotHaven.Refused();
            _haven = Point3D.Zero;
            _to = Point3D.Zero;
            Bent++;

            return true;
        }

        if (_to != Point3D.Zero)
        {
            _refused.Add(_to);
        }

        var worst = BotThreat.Strongest(body, Watch);
        var next = worst == null ? Point3D.Zero : Retreat(_map, body, worst, _refused);

        if (next == Point3D.Zero)
        {
            next = Homeward(_map, body);

            if (next == Point3D.Zero || _refused.Contains(next))
            {
                return false;
            }
        }

        _to = next;
        Bent++;

        return true;
    }

    /// <summary>Flights that were refused a road and turned another way instead of failing.</summary>
    public static long Bent { get; private set; }

    /// <summary>How far round a leg's landing something hostile makes the way a bad one. See <see cref="Retreat"/>.</summary>
    public static int Clearance { get; set; } = 8;

    /// <summary>Legs that went a lesser way because a better one landed beside something else hostile.</summary>
    public static long Skirted { get; private set; }

    /// <summary>Retreats passed over for landing on ground the reach map says the runner cannot reach, and for landing in a deadly patrol it did not stand in.</summary>
    public static long Unreached { get; private set; }

    public static long DeadlierLandings { get; private set; }

    /// <summary>Flights that ended with the thing still on the bot, failed rather than finished. See <see cref="Advance"/>.</summary>
    public static long Unshaken { get; private set; }

    /// <summary>Flights that failed for finding nowhere behind the bot to run to after they began (07.10.2026, work18).</summary>
    public static long EndedCornered { get; private set; }

    /// <summary>Deaths in flight of a bot that was poisoned, and of one that still carried a cure.</summary>
    public static long FellPoisoned { get; private set; }

    public static long FellWithCure { get; private set; }

    /// <summary>Whether a landing lies in a deadly patrol zone other than the one the runner stands in. Counted.</summary>
    private static bool Deadlier(Map map, Point3D landing, int standing)
    {
        if (BotZones.LevelAt(map, landing.X, landing.Y) != BotZoneLevel.Deadly)
        {
            return false;
        }

        var zone = BotZones.ZoneAt(map, landing.X, landing.Y)?.Id ?? 0;

        if (zone == 0 || zone == standing)
        {
            return false;
        }

        DeadlierLandings++;

        return true;
    }

    /// <summary>
    /// Whether a landing lies in the reach of a creature the runner has lately seen and cannot beat (03.10.2026, sight13): of
    /// build 396's 142 deaths in flight, 71 were to a kind other than the one the bot ran from. Counted.
    /// </summary>
    private static bool Sighted(Mobile body, Point3D landing)
    {
        if (!BotSight.Covers(body, landing))
        {
            return false;
        }

        SightedLandings++;

        return true;
    }

    /// <summary>Landings passed over on the first pass for lying in the reach of a creature the runner had seen. See <see cref="Sighted"/>.</summary>
    public static long SightedLandings { get; private set; }

    /// <summary>Whether something hostile and awake, other than what the bot is running from, stands within Clearance of a landing.</summary>
    private static bool Occupied(Map map, Mobile body, Point3D landing, Mobile from)
    {
        foreach (var creature in map.GetMobilesInRange<Server.Mobiles.BaseCreature>(landing, Clearance))
        {
            if (creature is { Deleted: false, Alive: true } && !ReferenceEquals(creature, from) && BotThreat.Menacing(body, creature))
            {
                return true;
            }
        }

        return false;
    }

    public static string Describe() =>
        $"{Fell} flights ended in death ({FellWalking} of them mostly at a walk; {FellPoisoned} poisoned when they fell, {FellWithCure} of those with a cure bottle unopened), {Unshaken} flights failed for not shaking what followed, {EndedCornered} failed cornered after they began ({BotFugitive.CorneredRested} flights not offered within {BotFugitive.CorneredRestMs / 1000}s of a bot's last cornered one), {Unreached} retreats passed over for landing on ground the reach map says the runner cannot reach and {DeadlierLandings} on the first pass for landing in a deadly patrol it did not stand in ({SightedLandings} in the reach of a creature it had seen and could not beat), {Shed} fleeing bots' detours dropped from the road away, {Stuck} flights {StuckMs / 1000}s without a step, {Pursuits} beats a flight went on past Watch for a blow still landing or a pursuer still coming, {Bent} flights turned another way after a refused road, {Skirted} legs taken a lesser way for something else hostile at the better one's end (within {Clearance}), {Vetoed} retreats refused by the look ahead ({VetMs:F0} ms) before anybody walked, {Tended} bandages and bottles taken on the run, "
        + $"{TowardAllies} retreats bent towards allies and {TowardHome} towards home (×{HelpWeight:F2}); "
        + $"runs for a town: {Runs} begun, {Reached} reached guarded ground, {GuardsCalled} calls for the guards ({RunnerCallsBlind} of them while the runner stood in its pursuer's aggressors, which the guards answer only for a hostile creature), {RunnerGuardKills} runners' pursuers cut down by the guards, {FellOnWay} died on the way, {UnshakenInTown} could not shake it in the town inside {HavenMs / 1000}s, {SwingsHeld} swings held by a bot outmatched that had not struck its attacker ({(HoldsSwing ? "on" : "off")}); {BotHaven.Describe()}";

    /// <summary>Bandages wound and bottles swallowed by bots on the run. See <see cref="Advance"/>.</summary>
    public static long Tended { get; private set; }

    /// <summary>
    /// Somewhere roughly <see cref="Bound"/> tiles from the threat, on the far side of the bot from it, or
    /// nothing at all when there is nowhere.
    ///
    /// <para>
    /// <b>Public and static so that the proposer can ask it before offering flight, and that is the whole
    /// point of it being here.</b> A bot with nowhere to run failed this undertaking the instant it took it
    /// on, and was offered it again in the same beat — Cedric failed to flee a harpy eighty-one times in
    /// thirty-six seconds on 26.08.2026, which is the shard's oldest defect shape: a piece of work that is
    /// taken up and refused, over and over, because the refusal is invisible to whatever offers it. The
    /// answer is not to price flight lower — being cornered is not a reason to run <em>less</em> keenly — it
    /// is for nobody to offer running to somebody who cannot run.
    /// </para>
    ///
    /// Falls back to heading home when straight back would put the bot off the ground the population lives
    /// on — which is not a nicety. The far edge of the roam is where the terrain that strands bots is, and a
    /// flight that ends in being carried back by the rescue has traded one emergency for another.
    /// </summary>
    public static Point3D Retreat(Map map, Mobile body, Mobile from) => Retreat(map, body, from, null);

    /// <summary>The eight ways to run, in no order; ordered by how far each takes the bot from the threat.</summary>
    private static readonly (int X, int Y)[] _ways =
    [
        (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1), (1, -1)
    ];

    /// <summary>
    /// As above, skipping any place in <paramref name="not"/>, and trying every direction rather than the one
    /// straight back: straight back first, then the two beside it, and so round to straight at the threat, which
    /// is never taken. Underground the island's ground is not asked for — the cave is the ground — and a way
    /// out of the dungeon's box is refused, because there is none on foot.
    /// </summary>
    public static Point3D Retreat(Map map, Mobile body, Mobile from, List<Point3D> not)
    {
        if (map == null || map == Map.Internal || body == null || from == null)
        {
            return Point3D.Zero;
        }

        var dx = body.X - from.X;
        var dy = body.Y - from.Y;
        var step = Math.Max(Math.Abs(dx), Math.Abs(dy));

        if (step > 0)
        {
            var under = BotDungeon.Under(body.Location);
            var helped = Help(map, body, under, out var hx, out var hy);

            // Each way scored by how far it carries the bot from the threat; the one straight at it scores
            // lowest and is never picked, because a score below nought is a step towards it. The ways that
            // hold ground are gathered best first, and the first one a search can reach is the retreat.
            Span<(int Score, Point3D Back)> ways = stackalloc (int, Point3D)[_ways.Length];
            var found = 0;

            for (var i = 0; i < _ways.Length; i++)
            {
                var (wx, wy) = _ways[i];
                var score = wx * dx + wy * dy;

                if (score <= 0)
                {
                    continue;
                }

                var x = body.X + wx * Bound;
                var y = body.Y + wy * Bound;

                if (!BotStep.Settle(map, x, y, out var z))
                {
                    continue;
                }

                var back = new Point3D(x, y, z);

                if (not != null && not.Contains(back))
                {
                    continue;
                }

                // Barred ground is no refuge: a bot running from something picks the tile directly away
                // from it, and on 11.09.2026 that put one straight at a walled pocket it could not enter —
                // the flight failed with "no way through" and the thing it was running from was still
                // behind it. See BotBarred; falling through leads home, which is always reachable.
                // A bot on the road between two towns is outside the population's ground already; it runs where it can.
                if (under ? !BotDungeon.Under(back) : !BotPopulation.Within(map, back) && BotPopulation.Within(map, body.Location) || BotBarred.Barred(map, back))
                {
                    continue;
                }

                // <b>No retreat onto ground the reach map says the runner cannot reach (01.10.2026, build 388; plan v2, point 6).</b>
                // A settled tile on a roof or across the water was a leg the look ahead vetoed after a search, or a refused road
                // and a bend; now it is one array read.
                if (BotReachMap.RefusesTile(map, body.Location, back))
                {
                    Unreached++;

                    continue;
                }

                // Ranked by how squarely the way leads away from the threat, plus how squarely it leads towards
                // help (HelpWeight); a way towards the threat was thrown out above and cannot come back through
                // the bonus. Kept in order, highest first: a handful of entries, so an insertion is the sort.
                var rank = (int)(1000.0 * (score / (double)step + (helped ? HelpWeight * (wx * hx + wy * hy) : 0.0)));
                var at = found;

                while (at > 0 && ways[at - 1].Score < rank)
                {
                    ways[at] = ways[at - 1];
                    at--;
                }

                ways[at] = (rank, back);
                found++;
            }

            // <b>Not into the arms of the next one (30.09.2026, build 343).</b> The ways were ranked by the one threat alone, so a
            // flight ran straight at whatever else stood behind the bot: of twelve fatal flights on 342, five died to a different
            // creature met on the way or at the leg's end — Kargan ran from a troll and a gazer killed it fifty tiles on, Mardred
            // from an ogre to (814, 1834) where a giant spider stood (`night-r19-flights.md`). The first pass passes over a way
            // whose landing has something hostile and awake within Clearance of it; the second takes the best as before, since
            // a way with company is still better than none.
            var skirting = false;

            // <b>And not into the next deadly zone (01.10.2026, build 388).</b> 178 flights on 386 ended in death, 67 % of them
            // to serpents, spiders and vines met on the way; a leg's landing inside a deadly patrol the runner does not
            // already stand in is passed over on the first pass, as a landing with company is. See Deadlier.
            var standing = BotZones.ZoneAt(map, body.X, body.Y)?.Id ?? 0;

            for (var pass = 0; pass < 2; pass++)
            {
                for (var i = 0; i < found; i++)
                {
                    var back = ways[i].Back;

                    if (pass == 0 && (Occupied(map, body, back, from) || Deadlier(map, back, standing) || Sighted(body, back)))
                    {
                        skirting = true;

                        continue;
                    }

                    if (VetMs > 0.0 && !BotPath.CanReach(map, body.Location, back, BotArrival.Within(1), VetMs))
                    {
                        Vetoed++;

                        continue;
                    }

                    if (pass == 0 && skirting)
                    {
                        Skirted++;
                    }

                    return back;
                }
            }

            // Nothing underground worth a leg: there is no walking home from a cave, so no leg towards it either.
            if (under)
            {
                return Point3D.Zero;
            }
        }

        var home = Homeward(map, body);

        return not != null && not.Contains(home) ? Point3D.Zero : home;
    }

    /// <summary>A leg of the way home, or home itself when it is nearer than a leg.</summary>
    private static Point3D Homeward(Map map, Mobile body)
    {
        // Where this bot lives, not the population's home (29.09.2026).
        var home = BotPopulation.HomeOf(body);
        var dx = home.X - body.X;
        var dy = home.Y - body.Y;
        var step = Math.Max(Math.Abs(dx), Math.Abs(dy));

        if (step <= 0)
        {
            return Point3D.Zero;
        }

        if (step <= Bound)
        {
            return BotStep.Settle(map, home.X, home.Y, out var atHome)
                ? new Point3D(home.X, home.Y, atHome)
                : Point3D.Zero;
        }

        var x = body.X + dx * Bound / step;
        var y = body.Y + dy * Bound / step;

        return BotStep.Settle(map, x, y, out var z) ? new Point3D(x, y, z) : Point3D.Zero;
    }
}
