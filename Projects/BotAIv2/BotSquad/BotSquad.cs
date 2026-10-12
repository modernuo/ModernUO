using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Targeting;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>What a squad is doing. Three states, and none of them is "stand and wait".</summary>
public enum BotSquadStance
{
    /// <summary>Going somewhere together, holding formation on the leader.</summary>
    Marching,

    /// <summary>Ground with nothing on it. Spread into small knots and cover it. See <see cref="BotScatter"/>.</summary>
    Scouting,

    /// <summary>Something is being dealt with. The formation anchors on whoever is in contact with it.</summary>
    Fighting
}

/// <summary>
/// A standing company of bots: a leader, a few followers, a formation and a share-out.
///
/// <para>
/// <b>One kind of group, not two.</b> The first version had a <em>warband</em> — spontaneous, formed the
/// moment a bot met something it could not take alone — and separately a <em>band</em> raised once a minute
/// by the system, exactly five strong, a medic compulsory, aimed at a square of the map. Both were called
/// "the group", including in the documentation, and the two answered different questions with different
/// lifetimes. This is one thing: it forms, it lives, it walks, it scouts, it fights, it splits what it took,
/// and it goes on.
/// </para>
///
/// <para>
/// <b>No state here may be "stand and wait", and that is a rule paid for in bodies.</b> The first version's
/// mustering and loot-settling states both opened by setting <c>Warmode = false</c> and standing still — and
/// a rally point was almost always inside assembly range, so "go and assemble" degenerated into "stand and
/// look at the enemy". A lich strikes from eight tiles and never closes, so not one rung of the survival
/// ladder ever fired: six bots waited politely in a ring while it killed them one at a time. Every state
/// below is a state in which the bot is going somewhere.
/// </para>
///
/// <para>
/// <b>The collective mind is arithmetic, not messages.</b> Stations, scouting patches and shares are all
/// derived by every member from the same facts in the same order. What passes between bots is only what is
/// genuinely an event: <em>I am being attacked</em>, and <em>get off this tile</em>.
/// </para>
/// </summary>
public sealed partial class BotSquad
{
    /// <summary>Stations not written because member and station stood on different sides of a dungeon's edge. See Station.</summary>
    public static long Sundered { get; private set; }

    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSquad));

    /// <summary>
    /// How long the squad keeps at a target that is not dying.
    ///
    /// <para>
    /// <b>Measured in damage, never in time or distance.</b> The first version's engaged state had no exit at
    /// all beyond victory or the death of everybody — so anything that could not be killed held its whole
    /// company for ever, and assisting outranks every personal errand. Twelve bots out of twenty were
    /// permanently "assisting": not idle, bound. The economy worked fine; there was nobody left to take part
    /// in it.
    /// </para>
    ///
    /// <para>
    /// And the measure is the <b>lowest health seen this fight</b>, not the current health, or a creature
    /// regenerating between blows reads as progress.
    /// </para>
    ///
    /// <para>
    /// <b>Ninety seconds was a number, not a measurement, and it read as a crowd standing still.</b> Nothing
    /// a bot carries takes anywhere near that long to come round again: the slowest weapon in any kit is the
    /// crossbow, and the engine gives it <c>15000/((Stam+100) × 18)</c> — four seconds at full stamina, five
    /// and a half at half — while the longest spell in the book has a base cast of four. So ninety seconds is
    /// something like twenty chances to land a blow, times every member, all of them coming to nothing while
    /// the whole company stands on the Bound rung with its own trades set aside. Watched from a client it is
    /// simply a knot of bots doing nothing for a minute.
    /// </para>
    ///
    /// <para>
    /// Reckoned from the engine's own slowest cycle instead — see <see cref="SlowestBlowMs"/> — so that the
    /// question being asked is the honest one: has anybody managed to hurt it in the time it takes the
    /// slowest thing here to swing, three times over. For a company of five that is upwards of twenty blows,
    /// and any one of them landing resets the clock, because the measure is the lowest health seen and not
    /// the current one.
    /// </para>
    /// </summary>
    public static int NoProgressMs => SlowestBlowMs * Blows;

    /// <summary>
    /// The longest a single attack can take to come round again, taken from the engine rather than chosen.
    ///
    /// <para>
    /// A crossbow is the slowest thing in any bot's kit at <c>OldSpeed</c> 18, which the pre-AOS formula
    /// <c>15000/((Stam+100) × 18)</c> turns into four seconds at full stamina and five and a half at half;
    /// the deepest circle in the grimoire has a four-second base cast. Four is the common case, because a bot
    /// that has been standing in a fight failing to hurt anything is not the one that is out of breath. Bots
    /// carry no heavy crossbow — if one ever enters a kit, this is the number that has to move.
    /// </para>
    ///
    /// <para>
    /// <b>Six, read off the engine's own formula this time (01.10.2026, build next).</b> The one above is not the engine's:
    /// <c>BaseWeapon.GetDelay</c> on this era is <c>floor(80000 / ((Stam + 100) × speed) − 2)</c> quarter-second ticks, which at
    /// the bots' sixty stamina is 6.25 s for a crossbow, 4.5 for a bow and 3.5 for a longsword, and more as the stamina goes.
    /// Three "blows" of four seconds gave a bow two shots and a crossbow one, and on build 386 companies broke off "its
    /// health has not moved" 434 times against 883 "it is down" — 82 of them with one of two able to strike, 56 with two of
    /// two — mostly from ettins, trolls, harpies and ogres, each break-off shunning the creature for the population. At six the
    /// window is eighteen seconds: three swings of the slowest weapon, as the number below says it means.
    /// </para>
    /// </summary>
    public static int SlowestBlowMs { get; set; } = 6000;

    /// <summary>
    /// How many fruitless swings of the slowest weapon a company will sit through before it accepts that this
    /// is not a fight.
    ///
    /// <para>
    /// <b>Three, and the walk from ninety down to here was worth writing out.</b> Ninety seconds was a
    /// number nobody had measured. Fifteen came from the arithmetic and Patrick's answer on watching it was
    /// that it still reads as a crowd standing about, so it went to two cycles — eight seconds — which held
    /// for three windows and then showed its price: on the fourth, companies broke off seventeen times
    /// against six kills, with the kill rate itself down by a third. A window that ends fights the squad was
    /// winning is worse than one that looks slow, because the second costs patience and the first costs the
    /// fight.
    /// </para>
    ///
    /// <para>
    /// Twelve seconds is the middle nobody had tried: shorter than the fifteen he judged too long, and long
    /// enough that a company of five gets some thirty swings at the thing, any one of which landing resets
    /// the clock. If it reads slow again from the client, this is the one number to move — and the thing to
    /// watch when moving it is the kill rate, not the break-off count, because only the first says whether a
    /// change cost anything.
    /// </para>
    /// </summary>
    public static int Blows { get; set; } = 3;

    /// <summary>
    /// The hard ceiling on one fight. Something healing faster than it is being hit is not a fight, it is a
    /// way of life.
    /// </summary>
    public static int FightCapMs { get; set; } = 240000;

    /// <summary>
    /// Most a squad may hold. Checked in one place only — see <see cref="BotSquads.Join"/>.
    ///
    /// <para>
    /// <b>Twenty, from five (01.10.2026, Patrick's night order: «Companies may gather up to 20, only friendly bots,
    /// never hostile ones»).</b> A ceiling, not a size: every caller asks for what its fight needs — <see cref="Usual"/> when the
    /// fight asks no more, and beyond it only while the company's strength is short of <see cref="CallOdds"/> times what it is
    /// to meet (<see cref="BotSquads.Enough"/>). Measured before it: a band against a whipping vine, an ettin and a giant spider
    /// at once was five strong however strong the three were.
    /// </para>
    /// </summary>
    public static int MaxSize { get; set; } = 20;

    /// <summary>
    /// The company a muster, a prowl, a sweep or a scouting party calls when its fight asks no more (the old five). Also what the
    /// proposers reckon a company raised for ground to hold (<c>BotQuad.Together</c>, <c>BotKept</c>, <c>BotProwl.Brings</c>), so
    /// raising the ceiling did not quietly move which ground is offered.
    /// </summary>
    public static int Usual { get; set; } = 5;

    /// <summary>
    /// How many times what a company is to meet its summed strength must be before it stops calling past <see cref="Usual"/>. Two:
    /// the proving ground's two to one (<c>BotProving.PartyMultiple</c>), said in strength rather than in heads.
    /// </summary>
    public static double CallOdds { get; set; } = 2.0;

    /// <summary>
    /// How long a squad may go with nothing to fight before it is let go.
    ///
    /// <para>
    /// <b>A squad with no end is a squad that eats the population.</b> Being in one puts a bot on the
    /// <c>Bound</c> rung, where its own wants are set aside — which is right while there is a fight on and
    /// ruinous the moment there is not: five bots that came together for one ogre would otherwise sweep
    /// empty ground for the rest of the session, mining nothing, crafting nothing and buying nothing, and
    /// the first version has the measurement for what that does — twelve of twenty bots permanently
    /// "assisting", an economy in perfect health with nobody left in it.
    /// </para>
    ///
    /// <para>
    /// Two minutes, which is long enough to look for a second fight where the first one was — that is what
    /// the scouting stance is for — and short enough that a quiet company is back at work before anybody
    /// notices it was gone.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <b>Raised to five minutes on 24.08.2026, by order and against the reasoning above.</b> Two minutes was
    /// chosen to protect the economy from bots bound to a company with nothing to fight — a real risk, and the
    /// first version's bill for it is quoted above. What that number could not know is how much ground a
    /// company covers once it is sweeping properly: two minutes is barely one leg of a sweep, so a squad was
    /// disbanded before it had finished looking at the first patch, and every fight had to start by forming a
    /// company from scratch. A company that lasts long enough to work a graveyard through, then a field, then
    /// the next field, is the thing being asked for — and the quiet clock still tears itself up the moment a
    /// fight starts, so a working company never ages at all.
    /// </remarks>
    /// <summary>
    /// How long a company that is not fighting and cannot find anything to fight stays together.
    ///
    /// <para>
    /// <b>Five minutes was written when a standing company was a company between fights, and it turned out
    /// to be a company that had stopped working.</b> A member is Bound and a Bound bot takes no work of its
    /// own — so every second a quiet squad exists is a second four or five bots are contributing nothing,
    /// and the only thing the clock was buying was the chance that something would wander past. Now that a
    /// standing company goes looking (see <see cref="Hunt"/>), the clock means what it should: nothing worth
    /// a company has been found anywhere near, so give everybody their own afternoon back.
    /// </para>
    ///
    /// <para>
    /// <b>Eight seconds, cut from forty-five on 25.08.2026 after watching it.</b> Forty-five was already six
    /// times better than what it replaced and it was still far too long to look at: a company that has just
    /// killed the last thing in the field stands there for the best part of a minute, four or five bots
    /// deep, and from outside there is no way to tell that from a company that has broken. The sweep for a
    /// new quarry runs every <see cref="HuntEveryMs"/>, so eight seconds is still three honest attempts to
    /// find something before the company gives up on the field — and the clock is torn up the instant
    /// anything is engaged or anybody is hit, so a fight in progress can never age it.
    /// </para>
    /// </summary>
    public static int IdleCapMs { get; set; } = 8000;

    /// <summary>How often a company with no focus sweeps for one. A spatial search on a once-a-second beat.</summary>
    public static int HuntEveryMs { get; set; } = 3000;

    /// <summary>
    /// How far from the focus a member is told to set about it.
    ///
    /// Wide, because a bow carries ten tiles and a spell eight, and a member pointed at the fight from
    /// where it is standing shoots from there. Being pointed at something out of reach costs nothing: the
    /// engine swings when the range is right and not before.
    /// </summary>
    public static int PressReach { get; set; } = 12;

    /// <summary>
    /// How far the anchor may drift before everybody is re-stationed.
    ///
    /// Not every tick: working out a station ends in a path search, and a squad that redraws five of them
    /// every beat is five searches per beat for as long as it is walking. Two tiles of drift is a formation
    /// that visibly holds and costs a fraction of that.
    /// </summary>
    public const int ReformDistance = 2;

    /// <summary>
    /// Least time between two rounds of stationing. Each round is a path search per member who has to move,
    /// and a fight re-stations on two triggers rather than one — the enemy walking, and anybody who stopped
    /// short of it. Something over a second keeps a company of five inside the movement budget the whole
    /// population shares.
    /// </summary>
    public static int RestationMs { get; set; } = 1500;

    /// <summary>
    /// How long the focus may go unhurt before the squad tries standing somewhere else.
    ///
    /// <para>
    /// One slow blow's worth, so a fight that is going nowhere gets a different arrangement tried for each of
    /// the chances <see cref="Blows"/> allows before it is given up on — and a fight that is merely slow,
    /// where blows land every few seconds, never turns at all.
    /// </para>
    ///
    /// <para>
    /// <b>Derived rather than written down, because written down it was already wrong.</b> The line above
    /// used to say "a sixth of NoProgressMs" beside a hard fifteen thousand, which was true only while
    /// NoProgressMs happened to be ninety — and the moment that number came down to where the engine says it
    /// belongs, the two would have met and the squad would have given up at the very instant it first tried
    /// moving. A rule stated in a comment is a rule waiting to be broken by the next edit.
    /// </para>
    /// </summary>
    public static int RotateAfterMs => Math.Max(RestationMs, SlowestBlowMs);

    private readonly List<IBotSquadMember> _members = [];

    private long _stationedTick;

    private int _focusLowest;

    private long _focusProgressTick;

    private long _focusSinceTick;

    /// <summary>Members that have had a blow on the present focus, so the first is counted once. See <see cref="Answer"/>.</summary>
    private readonly HashSet<Serial> _answered = [];

    /// <summary>What the leader was doing when the company took on its present focus, which is how the answers are filed.</summary>
    private string _engagedBy;

    /// <summary>
    /// The present focus's strength when the company took it on: a creature that is down is off the map, and its strength with it.
    /// The odds a kill is recorded at (03.10.2026). See <see cref="BotCompanyRecord"/>.
    /// </summary>
    private double _focusPower;

    /// <summary>
    /// Each kind this company has taken on: when it last held one (taken on, or let go), what the company brought when it took
    /// it on, and how many it was. A member's death to such a kind is the company's own fight's (03.10.2026). See <see cref="Fell"/>.
    /// </summary>
    private readonly Dictionary<Type, (long Tick, double Able, int Members)> _fought = [];

    /// <summary>The last creature this company was let at on the company record, and the last it stood to; each counted once.</summary>
    private Mobile _letAt;

    private Mobile _stoodTo;

    private Point3D _stationedAt;

    private BotSquadStance _stationedStance;

    private int _stationedCount;

    /// <summary>Where the focus was standing the last time it was alive, so its corpse can be found.</summary>
    private Point3D _fell;

    private long _quietTick;

    private long _huntedTick;

    private bool _quiet;

    public BotSquad(int id, IBotSquadMember leader)
    {
        Id = id;
        Leader = leader;
        Stance = BotSquadStance.Marching;

        // Seeded from a real tick rather than left at zero: these counters can start enormous and wrap, and a
        // zero here would hold the first stationing off until the clock caught up with it.
        _stationedTick = Core.TickCount;
    }

    public int Id { get; }

    /// <summary>
    /// Whoever called the squad together, whatever class they are. On their death the strongest survivor
    /// inherits.
    ///
    /// The formation is built from here even when the leader is the last person who ought to be in front: an
    /// archer leading means the blades form up ahead of <em>the archer</em> and wait to be led, which is the
    /// intended behaviour rather than a defect to be corrected.
    /// </summary>
    public IBotSquadMember Leader { get; private set; }

    public IReadOnlyList<IBotSquadMember> Members => _members;

    /// <summary>
    /// The living member farthest behind the leader beyond <paramref name="spread"/> tiles, with how far, or nothing.
    /// A leader on the march asks this before every step (BotDelve.Marching, 29.09.2026): the members are stationed
    /// on the leader, so a leader that stops is a company that closes up.
    /// </summary>
    public IBotSquadMember Straggler(int spread, out int distance)
    {
        distance = 0;
        var lead = Leader?.Self;

        if (lead == null)
        {
            return null;
        }

        IBotSquadMember worst = null;

        for (var i = 0; i < _members.Count; i++)
        {
            var member = _members[i];

            if (ReferenceEquals(member, Leader) || member?.Self is not { Deleted: false, Alive: true } self || self.Map != lead.Map)
            {
                continue;
            }

            var at = Math.Max(Math.Abs(self.X - lead.X), Math.Abs(self.Y - lead.Y));

            if (at > spread && at > distance)
            {
                distance = at;
                worst = member;
            }
        }

        return worst;
    }

    /// <summary>
    /// <b>A straggler that is walking is coming (29.09.2026, build 322; Patrick: "full mutual aid — it is a company").</b>
    /// The march left a member the moment it was <c>MarchLost</c> (sixty) tiles back, walking or not: on 322 Thalia gathered
    /// five at the captain in six seconds and, a minute into the march, left Godfrey, Gunnloda and Aerith "61 tiles back
    /// after 15s / 0s / 1s", every one of them "journey moving". A member within <see cref="MarchFar"/> whose walk is moving
    /// is waited for as one within sixty is; the hold's own clock still ends the wait.
    /// </summary>
    public static bool Coming(IBotSquadMember straggler, int behind) =>
        behind <= MarchFar && straggler?.Journey is { Moving: true } && straggler.Self is { Deleted: false, Alive: true };

    /// <summary>Whether members are left without stations on the march, the close-up being the formation. See <see cref="Station"/>.</summary>
    public static bool MarchUnstationed { get; set; } = true;

    /// <summary>How far back a walking member is still waited for on the march. See <see cref="Coming"/>.</summary>
    public static int MarchFar { get; set; } = 200;

    /// <summary>
    /// <b>Whether the company is on the road to somewhere (30.09.2026, build 338).</b> Set by whatever marches it — the delve
    /// from the muster to the mouth, the sweep from the captain to its square — and put down when it arrives or ends.
    ///
    /// <para>
    /// Two things change while it is set. A leader that stands still is a company holding for a straggler or standing to a
    /// fight, not a company arrived on empty ground, so it stays in <see cref="BotSquadStance.Marching"/> and nobody is
    /// scattered to a scouting patch: on 336 Durgan held ninety seconds for Dagna at 01:06–01:07 and she was "journey active,
    /// not moving towards (1088, 995, 0) (sweep)" twenty-four tiles back — sent there by the company's own scatter, then
    /// left behind for standing there. And the company hunts only what stands by the road (<see cref="MarchHuntReach"/>),
    /// not what the muster's finder sees at a hundred and twenty tiles: every sweep that came apart on 336–337 had engaged
    /// something while its captain walked on, its members stationed round a wraith or a plague spawn sixty to a hundred and
    /// fifteen tiles back ("we never got near it — nearest 11 tiles off"). A member set upon is still answered at any
    /// distance: that is <see cref="BotSquads.Note"/>, and the leader stands for it.
    /// </para>
    /// </summary>
    public bool OnTheMarch { get; set; }

    /// <summary>
    /// <b>A marching leader keeps to the pace of the one behind it (30.09.2026, build 340).</b> A member closing up follows the
    /// leader's body at the leader's own pace, so one that fell six tiles back stayed six tiles back: Durgan and Morgrim of
    /// company 13 on 339 "could not get one tile closer to … (closing up on Liriel, company 13)" 117 times in nine minutes,
    /// every three seconds, twelve to nineteen plans each, as Liriel ran from Britain towards (1956, 828) — 2130 of the walker's
    /// 12846 searches that were cut short that far went to two companies' close-ups (`night-r15-march.md`). The straggler hold
    /// begins only past <c>BotSweep.MarchSpread</c> (8) and the close-up at <c>BotEnlist.CloseUp</c> (6), so a member between
    /// them is never waited for and never catches up. While a member whose work is the enlistment stands further than CloseUp
    /// from a marching leader, the leader's turns come at twice its walking pace (<c>BotBeat</c>): a mounted leader at a footman's
    /// walk, a leader afoot at half of one, and the member running at its own pace closes.
    /// </summary>
    /// <summary>
    /// <b>Behind is past the close-up and a margin, not at it (30.09.2026, build 377).</b> Patrick that night: «they used to
    /// run, and now they step one tile a tick — that is clearly a problem». A member closing up stops within
    /// <c>BotEnlist.CloseUp</c> (6) of its leader and then keeps the leader's pace, and this counted a member behind at more than
    /// that same six — so the tail of a company walking in column, six to eight tiles back, was behind on nearly every turn and
    /// the leader went at half a walk for the whole road: Faunalyn's seven from Britain towards the Orc Caves at 22:45 made
    /// about 1.2 tiles a second with every member within eight tiles of her, and "turns a marching leader was slowed" read 352
    /// in eight minutes. A member counts as behind only past CloseUp and this many tiles more.
    /// </summary>
    public static int LagBeyond { get; set; } = 6;

    /// <summary>A slowed leader's turns come at this many times its walking step: one, a walk, while its members run.</summary>
    public static int PacedSteps { get; set; } = 1;

    public bool Lagging()
    {
        if (!OnTheMarch || Leader?.Self is not { Deleted: false, Alive: true } lead)
        {
            return false;
        }

        for (var i = 0; i < _members.Count; i++)
        {
            var member = _members[i];

            if (ReferenceEquals(member, Leader) || member.Self is not { Deleted: false, Alive: true } body || body.Map != lead.Map)
            {
                continue;
            }

            if (Math.Max(Math.Abs(body.X - lead.X), Math.Abs(body.Y - lead.Y)) <= BotEnlist.CloseUp + LagBeyond)
            {
                continue;
            }

            // Only a member about the company's business: one running or binding a wound is not being waited for here.
            // Counted, with what it held instead: build 340 slowed a leader three times in twenty minutes while two companies'
            // members "could not get one tile closer" 153 times (`night-r17-check340.md`).
            if ((member as IBotWilful)?.Resolve?.Deed is not BotEnlist)
            {
                UnpacedFor[(member as IBotWilful)?.Resolve?.Deed?.Kind ?? "nothing"] =
                    UnpacedFor.TryGetValue((member as IBotWilful)?.Resolve?.Deed?.Kind ?? "nothing", out var had) ? had + 1 : 1;

                continue;
            }

            Paced++;

            return true;
        }

        return false;
    }

    /// <summary>Members behind a marching leader not waited for, by the work they held instead of the enlistment. See <see cref="Lagging"/>.</summary>
    public static readonly Dictionary<string, long> UnpacedFor = [];

    /// <summary>The instrument above, for the Companies line.</summary>
    public static string Unpaced()
    {
        if (UnpacedFor.Count == 0)
        {
            return "none";
        }

        var say = ValueStringBuilder.Create(128);

        try
        {
            foreach (var (kind, n) in UnpacedFor)
            {
                if (say.Length > 0)
                {
                    say.Append(", ");
                }

                say.Append($"{kind} {n}");
            }

            return say.ToString();
        }
        finally
        {
            say.Dispose();
        }
    }

    /// <summary>Turns a marching leader was slowed to for a member behind it. See <see cref="Lagging"/>.</summary>
    public static long Paced { get; private set; }

    /// <summary>Looks for a fight of its own a delve's company skipped on the road to the mouth. See <see cref="Hunt"/>.</summary>
    public static long RoadSpared { get; private set; }

    /// <summary>How far round its leader a marching company looks for something to fight of its own accord. See <see cref="OnTheMarch"/>.</summary>
    public static int MarchHuntReach { get; set; } = 16;

    /// <summary>Beats a member being struck in melee was left on what struck it rather than turned to the company's enemy. See <see cref="Press"/>.</summary>
    public static long Defended { get; private set; }

    /// <summary>Stations not handed to a member that was running or binding a wound of its own. See <see cref="Station"/>.</summary>
    public static long Excused { get; private set; }

    /// <summary>Whether this body is one of the company's own.</summary>
    public bool Has(Mobile who)
    {
        if (who == null)
        {
            return false;
        }

        for (var i = 0; i < _members.Count; i++)
        {
            if (ReferenceEquals(_members[i].Self, who))
            {
                return true;
            }
        }

        return false;
    }

    public int Count => _members.Count;

    /// <summary>
    /// Gold-equivalent this company has divided among its members since it formed.
    ///
    /// <para>
    /// <b>What a company is worth, kept where the company is, because some work is worth exactly what it
    /// hands to other people.</b> An errand is judged on takings per minute and takings are read off the
    /// bot that did it — which is right for every trade on the shard and wrong for one: a leader who stands
    /// out of the share-out produces the whole of a corpse for five other bots and measures at nought.
    /// Left that way the ledger would learn, correctly by its own arithmetic and disastrously in fact, that
    /// leading companies pays nothing. Written by <see cref="BotSpoils"/> at the one moment the number is
    /// known, and read by whatever gave the charge.
    /// </para>
    /// </summary>
    public long Won { get; set; }

    public BotSquadStance Stance { get; private set; }

    /// <summary>
    /// Whether this company has been taken apart. A dead company is not a company anybody may join.
    ///
    /// <para>
    /// <b>Nothing marked one, and a dissolved company is still a live object with a live leader.</b>
    /// BotSquads.Dissolve clears the Squad of everybody in it and drops it out of the list the squad timer
    /// walks — so it is never thought about again, and therefore can never disband again. Join looked at the
    /// count, the ceiling and the map, all of which a dead company still answers perfectly well, and
    /// attached new members to it. They sit on the Bound rung with the auction switched off, in a company
    /// that no longer exists and so can never let them go.
    /// </para>
    ///
    /// <para>
    /// The log says it in two lines one second apart on 03.09.2026: "Squad 20 is no more: it went a while
    /// with nothing to fight and nobody holding it together" at 17:41:35, and "Doran 2 fell in with company
    /// 20, now 4 strong" at 17:41:35. Doran 2 was still standing there with its errand reading "nothing"
    /// forty-five minutes later, its own barren clock reading nought the whole time because a Bound bot is
    /// never asked what it wants to do.
    /// </para>
    /// </summary>
    public bool Disbanded { get; private set; }

    /// <summary>Marks this company dead. Called only by <c>BotSquads.Dissolve</c>, which owns the list.</summary>
    internal void Bury() => Disbanded = true;

    /// <summary>
    /// Whether this company is out on a standing charge and is therefore not disbanded for being quiet.
    ///
    /// <para>
    /// Set by whatever gave the charge, and it takes on the duty of ending the company along with it — a
    /// flag that switches off the only clock that ever disbands an unfought squad is a licence to stand in a
    /// field for ever if nobody takes that duty seriously. See <see cref="BotSweep"/>.
    /// </para>
    /// </summary>
    public bool Charged { get; set; }

    /// <summary>
    /// Whether this is a guild's war company, charged by <c>BotFeud</c> and let go by it and by nothing else.
    ///
    /// <para>
    /// <b>A war company's charge is not held by its leader's errand.</b> The rule below that takes the charge
    /// off a company whose leader holds nothing <c>Alongside</c> is right for a hunt — the errand that raised
    /// it owns it — and wrong here: a member that died in the ranks comes back with no errand at all (a
    /// Bound bot is offered nothing), and when the strongest survivor is one of those and inherits the
    /// lead, the charge came off and a company of nine dissolved nine seconds after its enemy fell (Squad
    /// 31, 04:08:48 on 14.09.2026, the third formation of a war that should have had two). The feud knows
    /// when the war is over; the leader's pocket does not.
    /// </para>
    /// </summary>
    public bool Warring { get; set; }

    /// <summary>
    /// Most this particular company may hold, which is <see cref="MaxSize"/> unless whatever called it
    /// together asked for something else.
    ///
    /// <para>
    /// <b>Per company rather than a second global number, because the global one is a statement about
    /// crowding and this is a statement about one errand.</b> Five is what a muster is worth: more bots on
    /// one ogre is more bots not working. A harrowing was ordered at six, and raising <see cref="MaxSize"/>
    /// to get it would have quietly widened every muster on the shard — the shape of change this project
    /// keeps paying for, where one number is moved and something nobody was looking at moves with it.
    /// </para>
    /// </summary>
    public int Ceiling { get; set; } = MaxSize;

    /// <summary>
    /// What this company looks for when it has nothing to fight, or null for the ordinary rule.
    ///
    /// <para>
    /// <b>A hook rather than a flag, because the answer belongs to whoever gave the charge.</b> The ordinary
    /// rule — see <see cref="Hunt"/> — deliberately refuses anything one bot could handle alone, and it is
    /// right to: a company of five falling on a rat is four bots doing nothing. A harrowing is the case where
    /// that is precisely the point, so the errand supplies its own finder and this file learns nothing about
    /// what a Baron is.
    /// </para>
    /// </summary>
    public Func<Mobile, BaseCreature> Quarry { get; set; }

    /// <summary>What the squad is dealing with, or null.</summary>
    public Mobile Focus { get; private set; }

    /// <summary>
    /// Whoever the formation is built around: the leader ordinarily, and whoever is in contact when there is
    /// a fight.
    ///
    /// That second case is the whole of "if one of us is attacked, go to them". It is not a rescue mechanism —
    /// there is no rescue mechanism. The anchor moves onto the member who was hit, every station is derived
    /// from the anchor, and so everybody is already walking towards the trouble before anything has been
    /// decided.
    /// </summary>
    public IBotSquadMember Contact { get; private set; }

    public Map Map => Leader?.Self?.Map;

    public Point3D Anchor
    {
        get
        {
            // <b>In a fight the formation is built round the creature, not round one of ours.</b> Anchoring on
            // the member in contact is the right answer to "who do we gather on" and the wrong answer to
            // "where do we stand", and it was answering both: a healer who called a company anchored it on
            // himself, stood still because a healer's own station is behind himself, and so nobody was ever
            // re-stationed and nobody ever closed. What it was reaching for survives untouched — the member
            // under attack is where the fight is, because the focus is whatever is attacking him.
            // Forming up short of the enemy, the company is built round the rally (06.10.2026, company16). See BeginMuster.
            if (Mustering)
            {
                return _rally;
            }

            if (Stance == BotSquadStance.Fighting && Focus is { Deleted: false })
            {
                return Focus.Location;
            }

            // Contact can be pruned out from under this — it is a member like any other and members die — so
            // the leader is always the fallback. An anchor of zero would put the whole formation in the corner
            // of the map.
            var on = Contact?.Self is { Deleted: false } && Stance == BotSquadStance.Fighting ? Contact : Leader;

            return on?.Self?.Location ?? Point3D.Zero;
        }
    }

    /// <summary>
    /// How many times the squad has had to send somebody to its station again during this fight.
    ///
    /// Read by <see cref="BotFormation"/> to turn the ring of tiles round the enemy, so that a blade whose
    /// place is behind a fence tries a different one instead of the same one until the fight times out.
    /// </summary>
    public int Attempt { get; private set; }

    /// <summary>
    /// The line to the threat, as a unit offset. Towards the focus when there is one, otherwise wherever the
    /// leader is facing.
    /// </summary>
    public (int X, int Y) Axis
    {
        get
        {
            // Forming up short of the enemy the line faces it: the blades' rank is towards the thing (company16).
            if (Mustering && Focus is { Deleted: false })
            {
                var towards = Unit(_rally, Focus.Location);

                if (towards != (0, 0))
                {
                    return towards;
                }
            }

            // From the creature towards us, now that the ranks are measured from the creature: the axis has to
            // point back down the line the squad is standing on, or the blades are stationed on the far side
            // of the thing and walk round it to get there.
            if (Stance == BotSquadStance.Fighting && Focus is { Deleted: false })
            {
                var side = (Contact?.Self is { Deleted: false } ? Contact : Leader)?.Self;

                var offset = side == null ? (0, 0) : Unit(Focus.Location, side.Location);

                if (offset != (0, 0))
                {
                    return offset;
                }
            }
            else if (Focus is { Deleted: false })
            {
                var offset = Unit(Anchor, Focus.Location);

                if (offset != (0, 0))
                {
                    return offset;
                }
            }

            var leader = Leader?.Self;

            return leader == null ? (0, -1) : Facing(leader.Direction);
        }
    }

    internal void Attach(IBotSquadMember member)
    {
        if (member != null && !_members.Contains(member))
        {
            _members.Add(member);
            Roster++;
        }
    }

    internal bool Detach(IBotSquadMember member)
    {
        if (!_members.Remove(member))
        {
            return false;
        }

        Roster++;

        // <b>And a member that left the roster dead is kept apart, for raising where it fell (02.10.2026).</b> See BotSquadRaise.
        if (member?.Self is BotMobile { Deleted: false } dead && (!dead.Alive || dead.Fallen))
        {
            KeepFallen(dead);

            // And remembered for its corpse: ground this company holds is ground it may walk back onto (build next). See BotUndertaker.Held.
            BotUndertaker.FellIn(dead, this);
        }

        // <b>A member that died has already left the roster by the time the company beats again (01.10.2026, build 382).</b>
        // BotMobile.OnDeath calls BotSquads.Leave, so Watch — which closes a dying member's wait on the beat it reads it dead —
        // never read one: the Companies line of 381 said "0 died helped and 0 unhelped" over eighteen deaths "doing enlist" in
        // its first quarter hour, and the instrument Patrick's point 3 is judged by was blind to the one outcome it is for.
        // Closed here, the one door out; a member leaving alive is simply struck off the wait. Dead by the engine's word or by
        // the bot's own (BotMobile.Fallen is set in OnDeath before the squad is told), whichever the engine sets first.
        if (member?.Self is { } body && _dying.Remove(body.Serial, out var fell) && (!body.Alive || body is BotMobile { Fallen: true }))
        {
            if (fell.Helped)
            {
                DiedHelped++;
            }
            else
            {
                DiedUnhelped++;
            }
        }

        return true;
    }

    /// <summary>
    /// Bumped whenever somebody joins or leaves, so the formation can keep each member's file for as long as the roster stands
    /// (01.10.2026: a company of twenty would otherwise sort its ranks twenty times a round). See <c>BotFormation.StationFor</c>.
    /// </summary>
    public int Roster { get; private set; }

    internal void Promote(IBotSquadMember member) => Leader = member;

    /// <summary>Whether this bot belongs here.</summary>
    public bool Holds(IBotSquadMember member) => _members.Contains(member);

    /// <summary>
    /// Deals with this. Resets the damage bookkeeping, because a new target is a new fight.
    ///
    /// <para>
    /// <b>The one door into a fight (06.10.2026, company16).</b> A creature the company pulled back from inside the shun is refused
    /// (<see cref="Bleeding"/>, counted). A fight the company <paramref name="chosen"/> — its finder's, a band's return to its quarry —
    /// with less than <see cref="MusterShare"/> of its strength within <see cref="FieldReach"/> of the enemy musters short of it first
    /// (<see cref="BeginMuster"/>) and engages when the members have come up; a fight the company was set upon in, or that a member is
    /// being killed in, is engaged now, whoever is there. A muster under way when a different enemy is taken on ends with it.
    /// </para>
    /// </summary>
    public void Engage(Mobile focus, IBotSquadMember contact, bool chosen = false)
    {
        if (focus is not { Deleted: false, Alive: true } || Has(focus))
        {
            return;
        }

        if (ReferenceEquals(Focus, focus))
        {
            // Already on it. Only the point of contact may have changed, which it does whenever somebody else
            // gets hit — and that is worth honouring, because it is where the formation should be built from.
            Contact = contact ?? Contact;

            return;
        }

        if (Bleeding(focus))
        {
            BledRefused++;

            return;
        }

        if (Mustering)
        {
            // Another enemy while forming up: the muster is over, and the fight below is the one the company is in.
            MusterStruck++;
            Mustering = false;
        }

        if (chosen && WantsMuster(focus))
        {
            BeginMuster(focus, contact);

            return;
        }

        if (chosen && Musters)
        {
            MusterUnneeded++;
        }

        Begin(focus, contact);
    }

    /// <summary>Engages <paramref name="focus"/> now: the body of <see cref="Engage"/>, and where a muster ends. See <see cref="EndMuster"/>.</summary>
    private void Begin(Mobile focus, IBotSquadMember contact)
    {
        Tally();

        // A switch from an enemy still standing, counted with how long the company had held it: the flicker of 30.09 (57 % of
        // 1324 engagements were such switches, 517 of them inside four seconds) is read off these two numbers. Not the enemy a
        // muster was formed up against: that is this fight beginning, not a switch.
        if (Focus is { Deleted: false, Alive: true } && !ReferenceEquals(Focus, focus))
        {
            Switched++;

            if (Core.TickCount - _focusSinceTick < FocusHoldMs)
            {
                SwitchedSoon++;
            }
        }

        Focus = focus;
        Contact = contact ?? Leader;
        Stance = BotSquadStance.Fighting;

        // The stopwatch: a company turning on what one of its own is crying about is that cry answered. See BotReflex.
        BotReflex.CompanyOn(focus);

        // And the members are turned on it now, not at the company's next beat. See BotSquads.Hurry.
        BotSquads.Hurry(this);
        Attempt = 0;
        _focusLowest = focus.Hits;
        _focusProgressTick = Core.TickCount;
        _focusSinceTick = Core.TickCount;
        _answered.Clear();
        _engagedBy = (Leader as IBotWilful)?.Resolve?.Deed?.Kind ?? "nothing";
        _focusPower = BotThreat.Power(focus);
        Fought(focus, Able());
        _ableTick = Core.TickCount;
        _closedTick = Core.TickCount;
        _nearestSeen = int.MaxValue;
        Engaged++;

        // With what the company weighs it at and what the company brings (03.10.2026): the odds of every fight taken on, which
        // the withdrawal is judged by and nothing printed before. See Overmatched.
        logger.Information(
            "Squad {Id} of {Count} is dealing with {What} ({Power:F0}), reckoned {Reckoned:F0} against the {Ours:F0} the company brings",
            Id,
            Count,
            focus.Name,
            BotThreat.Power(focus),
            Reckon(focus),
            Able()
        );
    }

    /// <summary>Done with the fight, whatever the reason. Back to walking.</summary>
    /// <summary>
    /// A member's walker found no road to <paramref name="foe"/> (BotWalk.Unreached). With none of the company within
    /// <see cref="PressReach"/> of it the company lets the fight go as hopeless, which shuns the creature, and answers true;
    /// with somebody at it the fight goes on without that member, and it answers false (06.10.2026, work15).
    /// </summary>
    public bool Unreachable(Mobile foe)
    {
        if (foe == null || !ReferenceEquals(Focus, foe))
        {
            return Focus == null;
        }

        for (var i = 0; i < _members.Count; i++)
        {
            if (_members[i]?.Self is { Deleted: false, Alive: true } self && self.Map == foe.Map && self.InRange(foe.Location, PressReach))
            {
                return false;
            }
        }

        UnreachedLet++;
        Disengage("no road any of us drew reached it", true);

        return true;
    }

    /// <summary>Fights let go because no member of the company could reach the thing. See <see cref="Unreachable"/>.</summary>
    public static long UnreachedLet { get; private set; }

    public void Disengage(string why, bool hopeless = false)
    {
        if (Focus == null)
        {
            return;
        }

        // <b>The numbers that would have told the whole story in one line and were not there.</b> "Its health
        // has not moved" is true of a company that cannot hurt the thing and equally true of one that never
        // got near it, and those want opposite fixes. How many of us were standing in reach of it separates
        // them at a glance — and the denominator has to be there, or none is indistinguishable from all.
        //
        // <b>Said only where it answers something, which is the correction to its first evening.</b> Printed
        // on every break-off it read "0 of 3 in reach, nearest -1" over three clean kills in a row: a dead
        // focus is a deleted mobile off on the internal map, so the count had nobody to count and the honest
        // answer to a question nobody was asking looked exactly like the failure it was written to detect.
        if (hopeless)
        {
            var reach = InReach(out var nearest, out var blind, out var refused, out var unsteady);

            // <b>"In reach" meant a distance, and a distance is not the question the engine answers.</b>
            // Every swing goes through <c>InLOS</c>, so a blade on the tile that touches a wraith through a
            // crypt wall counted as in reach, in the fight, and doing nothing — and this line said so in the
            // most reassuring words available. The two ways a bot standing on the right tile still cannot
            // hurt anything are counted apart, exactly as BotSlay counts them, because a broken line is
            // cured by another tile and a refusal is not.
            logger.Information(
                "Squad {Id} broke off from {What}: {Why} — {Reach} of {Count} able to strike ({Blind} with no line to it, {Refused} refused by the engine, {Unsteady} moved too recently to shoot), nearest {Nearest} tiles off, {Tries} tries at standing right",
                Id,
                Focus.Name,
                why,
                reach,
                Count,
                blind,
                refused,
                unsteady,
                nearest,
                Attempt
            );
        }
        else
        {
            logger.Information("Squad {Id} broke off from {What}: {Why}", Id, Focus.Name, why);
        }

        // Nobody could hurt it, whatever the reason. Left alone long enough that the population stops
        // rediscovering it: the same wraith was called a company against every two minutes for an evening,
        // thirty-five times, because breaking off taught nothing to anybody.
        if (hopeless)
        {
            BotQuarry.Shun(Focus, BotQuarry.HopelessMs);

            // Kept for whoever raised the fight: a band whose own quarry was given up on has failed, and says why (01.10.2026).
            Broken = Focus;
            BrokenWhy = why;
        }

        Tally();

        // When the company last held this kind: a death to the next of a field within BotCompanyRecord.FightMs is still this fight's.
        if (Focus != null && _fought.TryGetValue(Focus.GetType(), out var fought))
        {
            _fought[Focus.GetType()] = (Core.TickCount, fought.Able, fought.Members);
        }

        Focus = null;
        Contact = null;
        Attempt = 0;
        Mustering = false;
        Stance = BotSquadStance.Marching;
    }

    /// <summary>A kind taken on, with what the company brought to it. See <see cref="_fought"/>.</summary>
    private void Fought(Mobile focus, double able)
    {
        if (focus is not BaseCreature)
        {
            return;
        }

        if (_fought.Count >= 64)
        {
            _fought.Clear();
        }

        _fought[focus.GetType()] = (Core.TickCount, able, Count);
    }

    /// <summary>
    /// <b>One of this company fell to <paramref name="killer"/> (03.10.2026).</b> Asked by <c>BotKept.Fell</c> with the company the
    /// bot fell in. A wild creature of a kind the company was fighting, or had fought within <see cref="BotCompanyRecord.FightMs"/>,
    /// goes to the companies' record at the odds the company took that kind on at over the killer's strength; any other wild killer
    /// is counted as a death to a kind the company was not fighting, and left to the island's record.
    /// </summary>
    internal void Fell(Mobile killer)
    {
        // Whatever killed it: the second inside a minute pulls the company back (06.10.2026, company16). See Lost.
        Lost(killer);

        if (killer is not BaseCreature { Controlled: false, Summoned: false } creature)
        {
            return;
        }

        var kind = creature.GetType();

        if (_fought.TryGetValue(kind, out var fought) && fought.Members >= 2
            && (Focus?.GetType() == kind || Core.TickCount - fought.Tick < BotCompanyRecord.FightMs))
        {
            BotCompanyRecord.Fell(kind, fought.Able / Math.Max(1.0, BotThreat.Power(creature)));
        }
        else if (Count >= 2)
        {
            BotCompanyRecord.FellElsewhere();
        }
    }

    /// <summary>
    /// What stops this member landing a blow on the focus from exactly where it stands, if anything.
    ///
    /// <para>
    /// <b>One rule, asked from the three places that were each answering it their own way.</b> The press set
    /// a combatant on anybody inside twelve tiles, the break-off line counted anybody inside its own ring,
    /// and the stationing asked neither — so a company could report itself fully engaged while not one arrow
    /// left it. Every one of the four refusals below is the engine's, taken from the engine, and each has a
    /// different cure: distance wants a walk, a broken line wants another tile, a refusal wants nothing at
    /// all, and stillness wants to be left alone.
    /// </para>
    /// </summary>
    private enum BotBlow
    {
        /// <summary>Not near enough for its own rank's ring.</summary>
        Far,

        /// <summary>Near enough, and no line to it. See BotFormation.Sighted — the engine drops the swing silently.</summary>
        Blind,

        /// <summary>Near enough and in plain sight, and the engine refuses the blow anyway.</summary>
        Refused,

        /// <summary>
        /// Near enough, in sight, holding a bow — and it has moved too recently to loose an arrow.
        ///
        /// <para>
        /// <b>This is the one the company never knew about, and it is the answer to a whole evening.</b>
        /// <c>BaseRanged.OnSwing</c> refuses outright unless the archer has stood still for a second on this
        /// era, and the formation re-stations everybody every <see cref="RestationMs"/> whenever the thing
        /// being fought drifts two tiles — which a creature in a melee does constantly. So the shooters were
        /// walked back and forth over one tile for the whole fight and never once fired, while every measure
        /// this file had said they were in the fight. The lone hunter has known the number since 25.08.2026
        /// and says so in as many words: "a kite that does not know this number is a bot that moves for ever
        /// and never fires". <c>BotSlay.StillMs</c> is that number, and it is read from there rather than
        /// copied, because two copies of an engine constant is how they come apart.
        /// </para>
        /// </summary>
        Unsteady,

        /// <summary>Swinging.</summary>
        Able
    }

    /// <summary>
    /// How far this member can actually hurt the focus from — which is not the same question as where the
    /// formation means it to stand, and answering the first with the second put the archers on a treadmill.
    ///
    /// <para>
    /// <b>A ring is a place to walk to; a reach is what the engine will let a blow cross.</b> The shooters'
    /// ring is five tiles and a bow carries ten, so an archer that was hitting the thing perfectly well from
    /// seven was judged out of the fight, marched two tiles back in, and lost its shot to the second of
    /// stillness <c>BaseRanged.OnSwing</c> demands — and then the creature drifted and it happened again.
    /// Measured on the first window that could see it: 96 beats of "moved too recently to fire" in sixteen
    /// minutes, against six of every other refusal put together.
    /// </para>
    ///
    /// <para>
    /// The ring stays the floor, because a blade's reach is one tile and its ring is one tile, and nothing
    /// should be able to make a rank <em>narrower</em> than the formation drew it. Casters are given the
    /// spell range rather than the stick in their hands, which is the thing they are actually fighting with;
    /// asked of the role rather than of the pack, because <c>BotStrike.Can</c> walks the whole backpack and
    /// this is asked three times a second for every member of every company.
    /// </para>
    /// </summary>
    private static int Reach(IBotSquadMember member, Mobile body)
    {
        var role = BotFormation.RoleOf(member);
        var arm = body.Weapon?.MaxRange ?? 1;

        if (role is BotRole.Caster or BotRole.Medic)
        {
            arm = Math.Max(arm, BotStrike.Range);
        }

        return Math.Max(BotFormation.PressRingFor(role), arm);
    }

    /// <summary>Beats a member with no line to the focus was set on the enemy nearest the leader instead. See <see cref="Press"/>.</summary>
    public static long NearLeader { get; private set; }

    /// <summary>The hostile creature nearest the leader, within PressReach of it, that this member can see; null when none.</summary>
    private Mobile NearestToLeader(Mobile body)
    {
        var lead = Leader?.Self;

        if (lead is not { Deleted: false, Alive: true } || lead.Map != body.Map)
        {
            return null;
        }

        Mobile best = null;
        var bestRange = double.MaxValue;

        foreach (var creature in lead.Map.GetMobilesInRange<BaseCreature>(lead.Location, PressReach))
        {
            if (creature is not { Deleted: false, Alive: true } || !BotThreat.Menacing(body, creature) || !body.InLOS(creature)
                || !body.CanBeHarmful(creature, false) || body.IsHarmfulCriminal(creature))
            {
                continue;
            }

            var range = lead.GetDistanceToSqrt(creature);

            if (range < bestRange)
            {
                bestRange = range;
                best = creature;
            }
        }

        return best;
    }

    /// <summary>The verdict above, with how far off this member is on the way out.</summary>
    private BotBlow Strike(IBotSquadMember member, out int away)
    {
        away = int.MaxValue;

        var focus = Focus;
        var body = member?.Self;

        if (body is not { Deleted: false, Alive: true } || focus is not { Deleted: false } || body.Map != focus.Map)
        {
            return BotBlow.Far;
        }

        away = Math.Max(
            Math.Abs(body.Location.X - focus.Location.X),
            Math.Abs(body.Location.Y - focus.Location.Y)
        );

        if (away > Reach(member, body))
        {
            return BotBlow.Far;
        }

        if (!body.InLOS(focus))
        {
            return BotBlow.Blind;
        }

        // <b>And not a blow the engine would call a crime.</b> CanBeHarmful answers whether harm is possible here, not whether
        // it is lawful: a company's focus is whatever it is fighting, and a guild's war company fights the enemy guild, who
        // are enemies to its members and innocents to anybody else in its ranks. On 24.09.2026 (07:04–07:09) four bots of
        // guilds not in the war fell in with The Crown's company and each was a criminal half a minute later; the guards in
        // Britain killed the fighters of the war there. No company of this shard is raised to commit a crime — hunts, delves,
        // harrowings, wars — so the member for whom this blow would be one is left out of it, as the refused are.
        if (!body.CanBeHarmful(focus, false) || body.IsHarmfulCriminal(focus))
        {
            return BotBlow.Refused;
        }

        if (body.Weapon is BaseRanged && Core.TickCount - body.LastMoveTime < BotSlay.StillMs)
        {
            return BotBlow.Unsteady;
        }

        return BotBlow.Able;
    }

    /// <summary>
    /// How many members could actually land a blow on the focus, how far off the nearest is, and — of the
    /// ones near enough — how many are stopped by a broken line, by the engine's refusal, and by having
    /// moved too recently to shoot.
    /// </summary>
    private int InReach(out int nearest, out int blind, out int refused, out int unsteady)
    {
        nearest = int.MaxValue;
        blind = 0;
        refused = 0;
        unsteady = 0;

        var reach = 0;

        for (var i = 0; i < _members.Count; i++)
        {
            var verdict = Strike(_members[i], out var away);

            if (away < nearest)
            {
                nearest = away;
            }

            switch (verdict)
            {
                case BotBlow.Blind:
                    blind++;

                    break;

                case BotBlow.Refused:
                    refused++;

                    break;

                case BotBlow.Unsteady:
                    unsteady++;

                    break;

                case BotBlow.Able:
                    reach++;

                    break;
            }
        }

        if (nearest == int.MaxValue)
        {
            nearest = -1;
        }

        return reach;
    }

    /// <summary>
    /// One beat of the squad's own life. Called from the squad tick, not from a bot's.
    ///
    /// Returns false when the squad should cease to exist — too few left, or nobody to lead it.
    /// </summary>
    /// <summary>
    /// One beat of the company's own thinking. Null while it lives; otherwise why it is over.
    ///
    /// <para>
    /// <b>A reason rather than a false, because the three ways a company ends are three different things
    /// and read as one.</b> Every disbanding on this shard was logged "there is nothing left of it" — the
    /// caller's own words for a bare <c>false</c> — so a company that lost its last member, a company of one
    /// that nobody had charged, and a company whose leader died with nobody fit to inherit were the same
    /// line. Five of them in seventeen minutes on 27.08.2026 and no way to tell which had happened.
    /// </para>
    /// </summary>
    /// <summary>Whether this company has been asked to answer between its beats. Owned by <see cref="BotSquads.Hurry"/>.</summary>
    internal bool Due;

    /// <summary>
    /// <b>The company's answer to an event, between its beats (06.10.2026, react16).</b> A company thinks once a second
    /// (<c>BotSquads.BeatMs</c>), and everything that turns its members on an enemy or a medic on a patient hung on that
    /// beat: an enemy taken on at a blow (<c>BotSquads.Note</c>) put nobody's hand on it until the beat came round. This is
    /// the part of <see cref="Update"/> that answers and nothing that judges: in a fight the stations and the press (each
    /// member's combatant, its arms, the medics' support, the casters); out of one, the medics. No clock that counts beats
    /// is touched — the giving up, the withdrawing, the quiet and the retargeting stay on the beat — and the stations keep
    /// their own throttle (<c>RestationMs</c>).
    /// </summary>
    internal void AnswerNow()
    {
        Due = false;

        if (Disbanded || _members.Count == 0)
        {
            return;
        }

        if (Stance == BotSquadStance.Fighting && !Mustering)
        {
            Station();
            Press();
        }
        else if (Mustering)
        {
            Station();
            Tend();
        }
        else
        {
            Tend();
        }
    }

    internal string Update()
    {
        Prune();
        Release();

        // <b>A company of one is nothing, unless somebody is standing in a square calling for the second.</b>
        // Every company on this shard is born at one — <c>BotSquads.Form</c> makes a squad out of its leader
        // and members join afterwards — and for a muster that gap is a single beat, so this never mattered.
        // A harrowing calls for five minutes, and for those five minutes it is a company of one: dissolved on
        // the next squad beat, re-formed on the next Baron beat, dissolved again. 720 companies formed and
        // 720 dissolved in eight hours on 27.08.2026, and every march that came out of it reported "the
        // company broke up on the road" within the second, because the squad the errand was holding had been
        // thrown away and replaced while it was still calling.
        //
        // The charge is exactly the right thing to ask, and it is already the answer to the same question one
        // line below in Quiet(): whoever set it owns ending the company. Nought members is still nothing —
        // there is nobody left to own anything.
        if (_members.Count == 0)
        {
            return "the last of them is gone";
        }

        // <b>The fallen first, and a company at a body is not given up on for being small (02.10.2026).</b> In a company of two the
        // one left standing is the only hand there is, and the company it would be raised back into is this one. See BotSquadRaise.
        Raise();

        // <b>A charge is held by an undertaking, and when the undertaking is gone nobody is holding it.</b>
        // This is the fourth time on this shard that a company has outlived whoever raised it: BotScout,
        // BotSweep and BotHarrow each learned to put the flag down, and BotProwl was written without a
        // matching release at all — so a company raised for a walk to a hunting ground stood for ever,
        // because a charged company does not age (see Quiet) and a leader is never let go of (see Release).
        // Ninety-four of the hundred and thirty-six stall reports of 04.09.2026 were that, some nineteen
        // minutes long, every one of them ending in the words "in a company".
        //
        // Rather than a fifth copy of the same two lines, the invariant is asserted here where it can be
        // seen: whoever charged it did so from inside a running errand, and that errand is <c>Alongside</c>
        // by construction — it is the only kind a Bound bot may hold. A leader holding anything else, or
        // nothing, is a leader who is not charging this company with anything.
        if (Charged && !Warring && Leader?.Self is BotMobile head && head.Resolve?.Deed is not { Alongside: true })
        {
            Charged = false;
            Unowned++;
        }

        if (_members.Count < 2 && !Charged && !RaiseUnderway)
        {
            return "one bot was left in it and nobody had charged it with anything";
        }

        if (Leader?.Self is not { Deleted: false, Alive: true } || !_members.Contains(Leader))
        {
            if (!Inherit())
            {
                return "it lost its leader and nobody in it could take over";
            }
        }

        // Forming up short of the enemy, the muster's own clocks run and the fight's do not (06.10.2026, company16). See MusterBeat.
        if (Mustering)
        {
            MusterBeat();
        }

        if (Stance == BotSquadStance.Fighting && !Mustering)
        {
            Judge();
        }

        // <b>A company does not stay in a fight it cannot win (03.10.2026, build next).</b> See Overmatched.
        var withdrew = Overmatched();

        if (withdrew != null)
        {
            return withdrew;
        }

        // The strongest thing attacking the company is its enemy, held against flicker (01.10.2026). See Retarget.
        Retarget();

        Settle();
        Station();
        Watch();

        // Where everybody stands is one half of a fight and it was the only half there was. Nothing anywhere
        // told a member to swing: the formation walked five bots into a ring around an ogre and left them
        // watching it, and the only ones that ever fought back were the one or two it happened to hit, by
        // way of their own reflex. A company that has agreed on a target attacks the target.
        // Forming up, nobody is pressed: the members stand to their places round the rally and the medics mend (company16).
        if (Stance == BotSquadStance.Fighting && !Mustering)
        {
            Press();
        }
        else if (Mustering)
        {
            Tend();
        }
        else
        {
            Tend();

            // A company that pulled back chooses no fight while it holds on its leader (company16). See Bleed.
            if (!Recovering)
            {
                Hunt();
            }
        }

        return Quiet() ? null : "it went a while with nothing to fight and nobody holding it together";
    }

    /// <summary>
    /// A company with nothing to fight goes and finds something.
    ///
    /// <para>
    /// <b>Five bots stood in a graveyard for ninety seconds and this is the line that was missing.</b> A
    /// squad member is <c>Bound</c>, and a Bound bot's own auction is skipped on purpose — the company owns
    /// where it stands and what it hits, so letting each member shop for work would be five bots walking
    /// five different ways. The consequence nobody had followed through is that a company with no focus is
    /// five bots with <em>no source of work at all</em>: they hold formation, they scatter prettily when the
    /// leader stops, and not one of them will ever start a fight. Every muster on this shard ended that way
    /// — the quarry died, the focus cleared, and the company stood about until the quiet clock disbanded it
    /// five minutes later. On 25.08.2026 that was Squad 1 at 20:22:06, five bots, ninety seconds and
    /// counting, while the miners around them worked.
    /// </para>
    ///
    /// <para>
    /// Asked of the same finder a muster is called by, so a company hunts exactly what a company is for:
    /// things big enough to be worth more than one bot. It is deliberately not <c>BotQuarry.Nearest</c> —
    /// a squad of five falling on a rat is five bots doing one bot's work, which is the crowding this
    /// project spends most of its arithmetic avoiding.
    /// </para>
    /// </summary>
    private void Hunt()
    {
        var leader = Leader?.Self;

        if (leader is not { Deleted: false, Alive: true } || leader.Map != Map)
        {
            return;
        }

        // Throttled: this is a spatial sweep and the squad beats once a second. A company that has just
        // failed to find anything will not find anything a tick later either.
        var now = Core.TickCount;

        if (now - _huntedTick < HuntEveryMs)
        {
            return;
        }

        _huntedTick = now;

        // The charge's own finder when it brought one, and a company is never given both. On the march, only what stands by
        // the road (build 338): see OnTheMarch.
        // <b>A delve's company on the road to its mouth starts no fights of its own (30.09.2026, build 360).</b> The one delve of
        // 359 that marched — Durgan's eight for the Orc Caves — took on twelve fights by the road in eleven minutes, lost its
        // leader to one of them and came apart before the mouth (`night-r25-check359.md`); the delve is what it was raised for.
        // What sets upon a member is still answered (BotSquads.Note); nothing is sought.
        if (OnTheMarch && (Leader as IBotWilful)?.Resolve?.Deed is BotDelve)
        {
            RoadSpared++;

            return;
        }

        // The finder is asked at the choice odds (company16), so a creature between ChooseOdds and the withdrawal's odds is passed
        // over inside it and the next one down is offered, rather than the strongest being refused here and nothing taken.
        var quarry = Quarry != null ? Quarry(leader) : BotQuarry.Company(leader, OnTheMarch ? MarchHuntReach : BotMuster.Reach, ChoiceOdds);

        if (quarry == null)
        {
            return;
        }

        // Nor one the company would not choose: the finder counts every ally standing near the leader, the company only its own
        // members (03.10.2026). Nor one a company no stronger lately withdrew or pulled back from. See Overmatched, Bleed.
        if (Beyond(quarry) || Shuns(quarry, Able()) || Bleeding(quarry))
        {
            ShyOf(quarry);

            return;
        }

        // Chosen, not set upon: a company with its strength spread out forms up short of it first (company16). See Engage.
        Engage(quarry, Leader, true);
    }

    /// <summary>
    /// <b>The company's enemy is the strongest thing attacking it; the leader's call breaks a tie; and it is held (01.10.2026,
    /// Patrick's night order, point 3: «support in killing the strongest of the attacking mobs»).</b>
    ///
    /// <para>
    /// <b>Measured before it, over six sessions of 30.09 (builds 359–378):</b> 1324 times a company took on an enemy, and 760 of
    /// them (57 %) were a switch away from an enemy still standing — 517 inside four seconds of the last, 375 straight back to the
    /// one before (A, B, A). Squad 9 at 22:53–22:54 on 378 went ettin, whipping vine, ettin, whipping vine … some twenty times in a
    /// minute: a blow on the leader turned the company to the ettin beside it (build 359's rule), a blow on a member beside the
    /// vine turned it back to the strongest thing there (half as strong again). Neither enemy died; the band that raised the
    /// company ended "the company broke off a whipping vine" at 0 coin in 0.3 minutes; and 25 of 49 bands that ended so on 30.09
    /// had simply been turned. The first-blow instrument read "band … a mean of 16–25 s, 0 inside 2 s": a company that changes
    /// its enemy every two seconds never lands a blow on any of them. Over the whole of 30.09: 6103 enemies taken on, 3224 of them
    /// switches from one still standing (2157 inside four seconds, 1807 back to the one before), 1411 down. It is the war of
    /// 17:33 on 24.09.2026 again — The Blade's Squad 26 on Merrick Ashdown, Torvin, Merrick, Vance, Torvin, Alden, Wynn inside
    /// six seconds — which the Steady rule of that day cured for blows near the enemy and not for blows on the leader.
    /// </para>
    ///
    /// <para>
    /// Every beat the company gathers what is attacking it — every living thing in a member's aggression lists, within
    /// <see cref="BotSquads.Reach"/> of that member and of <see cref="AnswerReach"/> of the leader, whose own combatant is one of
    /// ours (a friend's stray blow and anything the company gave up on excepted) — and names the strongest (<c>BotThreat.Power</c>).
    /// Within <see cref="TieShare"/> of it the leader's own combatant wins. The company's present enemy is kept unless the new one
    /// is <see cref="BotSquads.Switch"/> times as strong (only <see cref="TieShare"/> as strong when the present one is not attacking
    /// anybody of ours) and the present one has been held <see cref="FocusHoldMs"/>. One walk of the members' aggression lists a
    /// beat, no spatial query: a company of twenty costs forty short lists.
    /// </para>
    /// </summary>
    private void Retarget()
    {
        Gather();

        if (_attackers.Count == 0)
        {
            return;
        }

        var best = Strongest(out var power, out var called);

        if (best == null || ReferenceEquals(best, Focus) || Bleeding(best))
        {
            return;
        }

        var focus = Focus;

        if (focus is { Deleted: false, Alive: true } && focus.Map == Map)
        {
            var held = BotThreat.Power(focus);
            var need = _attackers.Contains(focus) ? BotSquads.Switch : TieShare;

            // A caster attacking the company takes the enemy off a thing that is not one at the tie, not at Switch (company16):
            // casters killed 60 % of 429 company deaths on 405–409 while the company stood on something else. See CastersFirst.
            if (CastersFirst && Caster(best) && !Caster(focus))
            {
                need = TieShare;
            }

            if (power < held * need)
            {
                return;
            }

            if (Core.TickCount - _focusSinceTick < FocusHoldMs)
            {
                HeldFor++;

                return;
            }

            ToStronger++;
        }
        else
        {
            ToStrongest++;
        }

        if (called)
        {
            LeaderCalls++;
        }

        Engage(best, best.Combatant as IBotSquadMember ?? Leader);
    }

    /// <summary>
    /// The strongest thing attacking the company now, the leader's call breaking a tie; the thing that just struck when nothing
    /// else is and it stands within <see cref="AnswerReach"/> of the leader; else null. For <see cref="BotSquads.Note"/>, when a
    /// company with no enemy is set upon.
    /// </summary>
    internal Mobile StrongestAttacking(Mobile attacker)
    {
        Gather();

        if (Strongest(out _, out _) is { } best)
        {
            return best;
        }

        // Nothing gathered: the thing that struck, if it stands where the company can answer it (see AnswerReach).
        return attacker is { Deleted: false, Alive: true } && Leader?.Self is { Deleted: false } lead && attacker.Map == lead.Map
               && lead.InRange(attacker.Location, AnswerReach)
            ? attacker
            : null;
    }

    /// <summary>Whether this body is one of this company, by its own record of its squad: no walk of the roster.</summary>
    private bool Ours(Mobile body) => body is IBotSquadMember { Squad: { } squad } && ReferenceEquals(squad, this);

    /// <summary>What is attacking the company, gathered once a beat by <see cref="Gather"/>.</summary>
    private readonly List<Mobile> _attackers = [];

    /// <param name="all">
    /// Shunned attackers too (03.10.2026): a creature a company gave up on is still killing whoever it is on, and the withdrawal
    /// must see it (see <see cref="Overmatched"/>); the choosing of an enemy passes it over, as before.
    /// </param>
    private void Gather(bool all = false)
    {
        _attackers.Clear();

        var map = Map;
        var lead = Leader?.Self;

        if (map == null || map == Map.Internal || lead is not { Deleted: false })
        {
            return;
        }

        for (var i = 0; i < _members.Count; i++)
        {
            var body = _members[i].Self;

            if (body is not { Deleted: false, Alive: true } || body.Map != map)
            {
                continue;
            }

            // Both lists: what struck this member first is in its aggressors, and what it struck first — the band's quarry, the
            // company's own find — is in its aggressed list, the engine never adding a defender that strikes back as an
            // aggressor (Mobile.AggressiveAction). Either way the test is the same: is it fighting one of ours now.
            var aggressors = body.Aggressors;

            for (var j = 0; j < aggressors.Count; j++)
            {
                Consider(body, lead, map, aggressors[j].Attacker, all);
            }

            var aggressed = body.Aggressed;

            for (var j = 0; j < aggressed.Count; j++)
            {
                Consider(body, lead, map, aggressed[j].Defender, all);
            }
        }
    }

    /// <summary>One entry of a member's aggression lists, gathered if it is attacking the company. See <see cref="Gather"/>.</summary>
    private void Consider(Mobile body, Mobile lead, Map map, Mobile a, bool all = false)
    {
        if (a is not { Deleted: false, Alive: true } || a.Map != map || !Ours(a.Combatant) || Ours(a) || _attackers.Contains(a))
        {
            return;
        }

        // Near the member it is on, and near enough the leader that the company answering it is still one company: a levied
        // member set upon two hundred tiles off on its way to the muster fights for itself (BotMobile.OnDamage), and the medics'
        // reach is where they stand. A friend's stray blow is not an attack (see BotSquads.Note), nor is a creature the company
        // has already given up on as hopeless.
        if (!body.InRange(a.Location, BotSquads.Reach) || !lead.InRange(a.Location, AnswerReach)
            || a is BotMobile && !BotRegard.AtWar(body, a) || !all && BotQuarry.Shunned(a))
        {
            return;
        }

        _attackers.Add(a);
    }

    /// <summary>The strongest of <see cref="_attackers"/>; the leader's own combatant when it is within <see cref="TieShare"/> of it.</summary>
    private Mobile Strongest(out double power, out bool called)
    {
        Mobile best = null;
        power = 0.0;
        called = false;

        for (var i = 0; i < _attackers.Count; i++)
        {
            var p = BotThreat.Power(_attackers[i]);

            if (p > power)
            {
                power = p;
                best = _attackers[i];
            }
        }

        if (best != null && Leader?.Self?.Combatant is { } call && !ReferenceEquals(call, best) && _attackers.Contains(call))
        {
            var p = BotThreat.Power(call);

            if (p >= power * TieShare)
            {
                power = p;
                best = call;
                called = true;
            }
        }

        // <b>A caster first, inside the tie (06.10.2026, company16).</b> Casters (AI_Mage) killed 253 of 423 company deaths on
        // 405–409, most of them while the company's enemy was something else; the strongest caster attacking the company within
        // TieShare of the strongest thing is the enemy. Beyond the tie the stronger thing still is: a dragon outranks a reaper.
        if (CastersFirst && best != null && !Caster(best))
        {
            Mobile caster = null;
            var strongest = 0.0;

            for (var i = 0; i < _attackers.Count; i++)
            {
                if (!Caster(_attackers[i]))
                {
                    continue;
                }

                var p = BotThreat.Power(_attackers[i]);

                if (p > strongest)
                {
                    strongest = p;
                    caster = _attackers[i];
                }
            }

            if (caster != null && strongest >= power * TieShare)
            {
                power = strongest;
                best = caster;
                called = false;
                CasterTaken++;
            }
        }

        return best;
    }

    /// <summary>
    /// For a member with no line to the company's enemy: the strongest other thing attacking the company that it can see and
    /// lawfully strike, within its reach. Null when there is none.
    /// </summary>
    private Mobile Seen(Mobile body)
    {
        Mobile best = null;
        var power = 0.0;

        for (var i = 0; i < _attackers.Count; i++)
        {
            var a = _attackers[i];

            if (ReferenceEquals(a, Focus) || a is not { Deleted: false, Alive: true } || a.Map != body.Map
                || !body.InRange(a.Location, PressReach))
            {
                continue;
            }

            var p = BotThreat.Power(a);

            if (p <= power || !body.InLOS(a) || !body.CanBeHarmful(a, false) || body.IsHarmfulCriminal(a))
            {
                continue;
            }

            power = p;
            best = a;
        }

        return best;
    }

    // ---- Withdrawing from what the company cannot beat (03.10.2026, build next) ----------------------------------------------

    /// <summary>
    /// Whether a company withdraws from a creature it cannot beat. Off, a company fights whatever attacks it, as before.
    ///
    /// <para>
    /// <b>Patrick, 03.10.2026: the deaths must come down while the other figures do not fall.</b> Measured on build 395
    /// (03:20–04:19, 210 bots): 245 deaths, 138 of them at a company's work — 67 in companies a prowl raised, 40 in bands, 23 in
    /// the ten sweeps of the hour. The company chose its quarry against <c>BotThreat.Tolerance</c> times what it brought
    /// (<c>BotQuarry.Company</c>), and nothing asked the same question of what attacked it: <see cref="Retarget"/> and
    /// <c>BotSquads.Note</c> take on the strongest attacker whatever it is. Company 47, nine bots raised by Beatrix for a prowl
    /// to (1847, 1162), took on a wandering plague beast of 20262 at 03:18:11, broke off 68 seconds later "its health has not
    /// moved — 7 of 8 able to strike" and stood where it was — the beast shunned, so nothing in the company looked at it again —
    /// while it and its spawn killed Aldric, Haldir, Kaelthas Silverleaf, Hallgerd, Kirsten, Beatrix and Verity one at a time,
    /// its strength growing from 20262 to 34562 as it ate them. Of the 138, 59 fell with the company fighting nothing at all
    /// (broken off, or on the march) and 41 to something other than the company's enemy.
    /// </para>
    ///
    /// <para>
    /// <b>Never from a caster: Patrick, 30.09.2026, "fight to the end".</b> A mage follows and casts at the back of whoever runs
    /// (<c>BotFugitive</c>), so a company set upon by one fights it as before; it only does not choose one it cannot beat, and
    /// nobody walks over to join a fight it cannot win against anything else (<see cref="Beyond"/>). Replayed on the same hour
    /// (each company's strength from its members' powers in the log, each kind's price from the log's deaths and kills, kinds read
    /// by type as the engine prices them — the reviewer's correction), the rule as written withdraws from 22 of 1005 engagements —
    /// the plague beast, giant serpents, whipping vines, an ettin or a troll on a weak company — that brought 109gp of 23,008 and
    /// cost 16 deaths while the company stood to them; on live 396 (04:29–05:18) from 7 of 770, all whipping vines, 0gp.
    /// </para>
    /// </summary>
    public static bool Withdraws { get; set; } = true;

    /// <summary>
    /// How many times what the company brings a creature may be reckoned at before the company withdraws from it.
    /// <c>BotThreat.Tolerance</c>'s figure, one and a half: the yardstick the company chose its quarry by
    /// (<c>BotQuarry.Company</c>) and a lone bot stands or walks on by (<c>BotThreat.Decide</c>). A dial of its own because it
    /// is the one to move if the withdrawals cost the hunt its income.
    /// </summary>
    public static double WithdrawOdds { get; set; } = 1.5;

    /// <summary>
    /// How long after its company withdrew a member set upon by what it withdrew from is offered flight at any health
    /// (<c>BotFugitive</c>). A minute: the run and the walk off, and not the rest of the evening.
    /// </summary>
    public static int WithdrawRunMs { get; set; } = 60000;

    /// <summary>Companies that withdrew; their members within the thing's reach, and out of it.</summary>
    public static long Withdrawals { get; private set; }

    public static long WithdrewNear { get; private set; }

    public static long WithdrewFar { get; private set; }

    /// <summary>
    /// Creatures a company would have taken on, by its finder or a blow on a member, that it would withdraw from: counted once a
    /// company a creature, not once a look — the finder offers the same one every <see cref="HuntEveryMs"/> (review of march10, F7).
    /// </summary>
    public static long Shied { get; private set; }

    /// <summary>The creature this company last passed over for being beyond it. See <see cref="ShyOf"/>.</summary>
    private Mobile _shiedFrom;

    /// <summary>Counts a creature passed over for being beyond this company, once. For <see cref="Hunt"/> and <c>BotSquads.Note</c>.</summary>
    internal void ShyOf(Mobile foe)
    {
        if (!ReferenceEquals(_shiedFrom, foe))
        {
            _shiedFrom = foe;
            Shied++;
        }
    }

    /// <summary>
    /// How long a creature a company withdrew from is left alone by companies, and by bots joining one, no stronger than that
    /// company was (review of march10, F4). Two minutes, the plain shun's (<c>BotQuarry.ShunMs</c>): the population at large is
    /// not told to leave it — a stronger company, a strong hunter and the Baron's harrow may still take it, and every weaker
    /// fight is already kept off it by <see cref="Beyond"/>.
    /// </summary>
    public static int WithdrawShunMs { get; set; } = 120000;

    /// <summary>What each withdrawn-from creature is left alone until, the strength that withdrew, and when it last did.</summary>
    private static readonly Dictionary<Serial, (long Until, double Strength, long Last)> _shunned = [];

    /// <summary>Withdrawals from a creature some company had already withdrawn from within <c>BotQuarry.HopelessMs</c>: a loop shows here (F5).</summary>
    public static long WithdrewAgain { get; private set; }

    /// <summary>
    /// Whether a company, or a bot weighing whether to join one, bringing <paramref name="strength"/> passes this creature over:
    /// a company no stronger withdrew from it within <see cref="WithdrawShunMs"/>.
    /// </summary>
    public static bool Shuns(Mobile creature, double strength)
    {
        if (creature == null || !_shunned.TryGetValue(creature.Serial, out var shun))
        {
            return false;
        }

        return Core.TickCount - shun.Until < 0 && strength <= shun.Strength;
    }

    /// <summary>Withdrawals by what the leader was doing: sweep, prowl, band, harrow…</summary>
    public static readonly Dictionary<string, long> WithdrewBy = [];

    /// <summary>The creature this company withdrew from, once it has; null while it has not. Read by the errand that raised it.</summary>
    public Mobile WithdrewFrom { get; private set; }

    /// <summary>Who withdrew from what, and when, for the flight's own rung. See <see cref="Withdrawing"/>.</summary>
    private static readonly Dictionary<Serial, (Mobile From, long Tick)> _withdrawn = [];

    /// <summary>Whether this is a caster, which a company fights to the end rather than withdraw from. See <see cref="Withdraws"/>.</summary>
    public static bool Caster(Mobile foe) => foe is BaseCreature { AI: AIType.AI_Mage };

    /// <summary>
    /// What a creature brings to a fight now, as a company weighs it: its strength at its kind's price in our lives
    /// (<c>BotQuarry.Priced</c>, the same figure the company chose its quarry by) at the share of its health it has left, never
    /// under a tenth — a nearly dead thing is finished, not fled. Nought for anything that is not a wild creature (a bot of
    /// another guild is a war's business, a pet its owner's).
    ///
    /// <para>
    /// <b>At the kind's company price (03.10.2026, build next; Patrick: "if they kill it, let them gather — the higher the level,
    /// the higher the chance to kill").</b> What companies as strong as this one have beaten in their own fights, never above the
    /// island's price (<see cref="BotCompanyRecord"/>): a company of thirteen at twice a Reaper's strength was reckoned at 47,000
    /// on the island's ×4.8 while companies of ten and more had put Reapers down 20 times in 23 on 396.
    /// </para>
    /// </summary>
    public static double Reckon(Mobile foe) =>
        foe is BaseCreature { Deleted: false, Alive: true, Controlled: false } creature
            ? BotCompanyRecord.Priced(creature) * Math.Clamp(creature.HitsMax > 0 ? creature.Hits / (double)creature.HitsMax : 1.0, 0.1, 1.0)
            : 0.0;

    /// <summary>The same at the island's price (<c>BotQuarry.Priced</c>), the reckoning before the company record: for its counters.</summary>
    public static double ReckonAlone(Mobile foe) =>
        foe is BaseCreature { Deleted: false, Alive: true, Controlled: false } creature
            ? BotQuarry.Priced(creature) * Math.Clamp(creature.HitsMax > 0 ? creature.Hits / (double)creature.HitsMax : 1.0, 0.1, 1.0)
            : 0.0;

    /// <summary>
    /// What the company brings to a fight: the summed strength (<c>BotThreat.Power</c>, at full health, as everywhere a side is
    /// weighed) of its members able to fight, on the leader's facet — the company's own sum (<c>BotQuad.Strength</c>), which a
    /// band or a prowl was raised to. Members still walking in from the muster are counted: a band whose callers are on their way
    /// is not a band of one.
    /// </summary>
    public double Able()
    {
        var lead = Leader?.Self;

        if (lead is not { Deleted: false, Alive: true })
        {
            return 0.0;
        }

        var sum = 0.0;

        for (var i = 0; i < _members.Count; i++)
        {
            if (_members[i] is IBotAlly { AbleToFight: true } && _members[i].Self is { Deleted: false, Alive: true } self
                && self.Map == lead.Map)
            {
                sum += BotThreat.Power(self);
            }
        }

        return sum;
    }

    /// <summary>
    /// Whether this company could not beat <paramref name="foe"/>: its reckoning past <see cref="WithdrawOdds"/> times what the
    /// company brings, with <paramref name="extra"/> added (a bot weighing whether to fall in). The company does not choose such
    /// a creature, nobody walks over to join a fight with one, and — unless it is a caster — the company withdraws from it.
    /// Never for a war company, a company underground or a delve's: the war, the dungeon and the delve judge their own fights.
    ///
    /// <para>
    /// <b>At the choice odds, not the withdrawal's (06.10.2026, company16).</b> Every caller of this is a choice — the finder, a blow
    /// on a member of a company with no enemy, a creature seen coming, a band's call, a cry's company, a prowl's — and the band
    /// between <see cref="ChooseOdds"/> and <see cref="WithdrawOdds"/> was the deadliest on 405–409 (0.29 deaths a fight, 27 % of
    /// the in-fight deaths in 4 % of the fights). A caller judging defence rather than choice — the protection of a member, the
    /// enlistment into a fight already on — passes <paramref name="odds"/> = <see cref="WithdrawOdds"/>. Creatures refused between
    /// the two gates are counted (<see cref="ChoiceRefused"/>), once a company a creature.
    /// </para>
    /// </summary>
    public bool Beyond(Mobile foe, double extra = 0.0, double odds = 0.0)
    {
        if (!Withdraws || Warring || foe == null || Leader?.Self is not { Deleted: false } lead || BotDungeon.Under(lead.Location)
            || (Leader as IBotWilful)?.Resolve?.Deed is BotDelve)
        {
            return false;
        }

        var threat = Reckon(foe);
        var ours = Able() + extra;
        var bound = ours * WithdrawOdds;

        if (threat > 0.0 && threat > bound)
        {
            return true;
        }

        var at = odds > 0.0 ? Math.Min(odds, WithdrawOdds) : ChoiceOdds;

        if (threat > 0.0 && threat > ours * at)
        {
            if (!ReferenceEquals(_choiceRefused, foe))
            {
                _choiceRefused = foe;
                ChoiceRefused++;
            }

            return true;
        }

        // Let at it on the company record where the island's price would have put it beyond the company (03.10.2026): counted once a
        // company a creature. See BotCompanyRecord.
        if (threat > 0.0 && !ReferenceEquals(_letAt, foe) && ReckonAlone(foe) > bound)
        {
            _letAt = foe;
            BotCompanyRecord.Let(foe);
        }

        return false;
    }

    /// <summary>
    /// <see cref="Beyond"/> asked before there is a company: whether a company led by <paramref name="lead"/> and bringing
    /// <paramref name="able"/> would withdraw from <paramref name="foe"/> on sight. The muster asks it before offering a band, so
    /// the offer and the call ask one question (06.10.2026, work15). Not underground, where Beyond never withdraws.
    /// </summary>
    public static bool BeyondFor(Mobile lead, Mobile foe, double able)
    {
        if (!Withdraws || foe == null || lead == null || BotDungeon.Under(lead.Location))
        {
            return false;
        }

        // At the choice odds (company16): the muster is choosing a quarry for a company that does not exist yet.
        var threat = Reckon(foe);

        return threat > 0.0 && threat > able * ChoiceOdds;
    }

    /// <summary>
    /// <b>The company's enemy, or the strongest thing attacking it, reckoned past what the company can beat: the company
    /// withdraws.</b> Every beat, over the company's present enemy and everything in its members' aggression lists, shunned ones
    /// included (<see cref="Gather"/>, no spatial query); casters are fought (<see cref="Withdraws"/>). The reason the company
    /// ends, or null while it stays.
    /// </summary>
    private string Overmatched()
    {
        var map = Map;

        if (!Withdraws || Warring || map == null || map == Map.Internal || Leader?.Self is not { Deleted: false, Alive: true } lead
            || BotDungeon.Under(lead.Location) || (Leader as IBotWilful)?.Resolve?.Deed is BotDelve)
        {
            return null;
        }

        // Shunned ones too: company 47 broke off the plague beast "hopeless", the beast was shunned, and nothing in the company
        // looked at it again while it killed seven of them.
        Gather(all: true);

        Mobile worst = null;
        var threat = 0.0;

        // And the same at the island's price, for the withdrawals the company record spares (03.10.2026). See BotCompanyRecord.
        Mobile worstAlone = null;
        var alone = 0.0;

        if (Focus is { Deleted: false, Alive: true } focus && focus.Map == map && !Caster(focus))
        {
            threat = Reckon(focus);
            worst = threat > 0.0 ? focus : null;
            alone = ReckonAlone(focus);
            worstAlone = alone > 0.0 ? focus : null;
        }

        for (var i = 0; i < _attackers.Count; i++)
        {
            if (Caster(_attackers[i]))
            {
                continue;
            }

            var reckoned = Reckon(_attackers[i]);

            if (reckoned > threat)
            {
                threat = reckoned;
                worst = _attackers[i];
            }

            var island = ReckonAlone(_attackers[i]);

            if (island > alone)
            {
                alone = island;
                worstAlone = _attackers[i];
            }
        }

        if (worst == null)
        {
            return null;
        }

        var ours = Able();

        // A company with nobody able to fight left weighs nothing; its members' own flights decide (BotFugitive).
        if (ours <= 0.0 || threat <= ours * WithdrawOdds)
        {
            // Stood to on the company record where the island's price would have withdrawn: counted once a company a creature.
            if (ours > 0.0 && alone > ours * WithdrawOdds && worstAlone != null && !ReferenceEquals(_stoodTo, worstAlone))
            {
                _stoodTo = worstAlone;
                BotCompanyRecord.Stand(worstAlone);
            }

            // The instrument for the next build (company16): would the withdrawal have fired on the strength within FieldReach of
            // the thing rather than on the map's sum? Counted once a company a creature; the gate itself is not moved tonight.
            if (ours > 0.0 && !Mustering && !ReferenceEquals(_fieldOvermatched, worst) && threat > Afield(worst.Location, FieldReach) * WithdrawOdds)
            {
                _fieldOvermatched = worst;
                FieldOvermatched++;
            }

            return null;
        }

        return Withdraw(worst, threat, ours);
    }

    /// <summary>
    /// <b>How a company withdraws.</b> The creature is left alone for <see cref="WithdrawShunMs"/> by companies no stronger and by
    /// bots joining them (<see cref="Shuns"/>: the finder, a blow on a member, the enlistment), every member is written down as
    /// having withdrawn from it (<see cref="Withdrawing"/>), and the company ends, which the caller does with the reason returned.
    ///
    /// <para>
    /// <b>Nobody is pressed to run, and that is deliberate.</b> A member's own work is already put down beneath its enlistment
    /// (<c>BotSquads.Enrol</c>), so a flight pressed on it would throw the enlistment away; on its next beat the enlistment ends
    /// "the company broke up" and the work beneath is taken up again, which walks the bot to its own business. A member the
    /// thing is hitting is on the Hunted rung, where <c>BotFugitive</c> now offers it the flight at once, whatever its health —
    /// for a guarded town where <c>BotHaven</c> judges it realistic, away otherwise — and the auction stacks the enlistment over
    /// the work beneath (<c>BotWill.StackAid</c>), so nothing is dropped.
    /// </para>
    /// </summary>
    internal string Withdraw(Mobile from, double threat, double ours)
    {
        var map = Map;
        var now = Core.TickCount;

        Withdrawals++;
        WithdrewFrom = from;

        var by = (Leader as IBotWilful)?.Resolve?.Deed?.Kind ?? "nothing";

        WithdrewBy[by] = WithdrewBy.TryGetValue(by, out var had) ? had + 1 : 1;

        // Left alone by the no stronger, for a while; not shunned for everybody (review of march10, F4). See Shuns.
        if (_shunned.TryGetValue(from.Serial, out var before) && now - before.Last < BotQuarry.HopelessMs)
        {
            WithdrewAgain++;
        }

        if (_shunned.Count >= 1024)
        {
            _shunned.Clear();
        }

        _shunned[from.Serial] = (now + WithdrawShunMs, ours, now);

        if (ReferenceEquals(Focus, from))
        {
            Disengage("we withdraw from what we cannot beat");
        }

        if (_withdrawn.Count >= 1024)
        {
            _withdrawn.Clear();
        }

        int near = 0, far = 0;

        for (var i = 0; i < _members.Count; i++)
        {
            if (_members[i].Self is not { Deleted: false, Alive: true } body || body.Map != map)
            {
                continue;
            }

            _withdrawn[body.Serial] = (from, now);

            if (body.InRange(from.Location, BotBolt.Watch) || ReferenceEquals(from.Combatant, body))
            {
                near++;
            }
            else
            {
                far++;
            }
        }

        WithdrewNear += near;
        WithdrewFar += far;

        logger.Information(
            "Squad {Id} of {Count} withdraws from {What}: reckoned {Threat:F0} against the {Ours:F0} the company brings, {Ratio:F1} times, past ×{Odds:F1} — {Near} within its reach left to run when it strikes, {Far} out of it; leader {Leader} was {By}",
            Id,
            Count,
            from.Name,
            threat,
            ours,
            threat / Math.Max(1.0, ours),
            WithdrawOdds,
            near,
            far,
            Leader?.Self?.Name ?? "nobody",
            by
        );

        return $"it withdrew from {from.Name}, reckoned {threat:F0} against its {ours:F0}";
    }

    /// <summary>
    /// Whether this bot's company withdrew lately (<see cref="WithdrawRunMs"/>) from something still within its reach — within
    /// <c>BotBolt.Watch</c> or set on the bot. Asked by <c>BotFugitive</c>, which offers such a bot the flight at the first blow:
    /// the company already judged the thing beyond it.
    /// </summary>
    public static bool Withdrawing(Mobile body)
    {
        if (body == null || !_withdrawn.TryGetValue(body.Serial, out var left))
        {
            return false;
        }

        if (Core.TickCount - left.Tick >= WithdrawRunMs || left.From is not { Deleted: false, Alive: true } from || from.Map != body.Map)
        {
            _withdrawn.Remove(body.Serial);

            return false;
        }

        return body.InRange(from.Location, BotBolt.Watch) || ReferenceEquals(from.Combatant, body);
    }

    /// <summary>The withdrawals' clause of the Companies line.</summary>
    public static string Withdrawn()
    {
        var say = ValueStringBuilder.Create(128);

        try
        {
            foreach (var (kind, n) in WithdrewBy)
            {
                if (say.Length > 0)
                {
                    say.Append(", ");
                }

                say.Append($"{kind} {n}");
            }

            return $"{Withdrawals} companies withdrew from something reckoned past ×{WithdrawOdds:F1} what they brought (by the leader's errand: {(say.Length > 0 ? say.ToString() : "none")}; {WithdrewNear} members within its reach, {WithdrewFar} out of it; {WithdrewAgain} from a creature a company had withdrawn from within {BotQuarry.HopelessMs / 60000} minutes){(Withdraws ? "" : " (off)")}, {Shied} creatures a company passed over for being beyond it (once a company each)";
        }
        finally
        {
            say.Dispose();
        }
    }

    /// <summary>Within this share of the strongest attacker's strength, the leader's own combatant is taken instead. See <see cref="Retarget"/>.</summary>
    public static double TieShare { get; set; } = 0.8;

    /// <summary>
    /// How long a company holds a new enemy before a stronger attacker may take it off it — one slow blow's worth, so the
    /// company lands something on whatever it turns to. The enemy dying, or being given up, lets go at once. See <see cref="Retarget"/>.
    /// </summary>
    public static int FocusHoldMs { get; set; } = 4000;

    /// <summary>
    /// How far from the leader something attacking a member may stand for the whole company to answer it. Forty, the company
    /// finder's own reach: past it the member is far from its company and answers for itself.
    /// </summary>
    public static int AnswerReach { get; set; } = 40;

    /// <summary>Times a company took on the strongest thing attacking it, having no enemy standing. See <see cref="Retarget"/>.</summary>
    public static long ToStrongest { get; private set; }

    /// <summary>Times a company left its enemy for a stronger attacker. See <see cref="Retarget"/>.</summary>
    public static long ToStronger { get; private set; }

    /// <summary>Of those, the times the leader's own combatant was taken inside the tie. See <see cref="Retarget"/>.</summary>
    public static long LeaderCalls { get; private set; }

    /// <summary>Beats a stronger attacker waited because the company had held its enemy under <see cref="FocusHoldMs"/>.</summary>
    public static long HeldFor { get; private set; }

    /// <summary>Enemies taken on, switches away from one still standing, and of those the ones inside <see cref="FocusHoldMs"/>.</summary>
    public static long Engaged { get; private set; }

    public static long Switched { get; private set; }

    public static long SwitchedSoon { get; private set; }

    /// <summary>Enemies of a company that went down while it held them. See <see cref="Judge"/>.</summary>
    public static long Downed { get; private set; }

    /// <summary>What this company has put down since it formed, for whoever raised it. See <c>BotBand</c>.</summary>
    public int Kills { get; private set; }

    /// <summary>The last enemy this company gave up on as hopeless, and why. See <c>BotBand</c>.</summary>
    public Mobile Broken { get; private set; }

    public string BrokenWhy { get; private set; }

    /// <summary>
    /// <b>How long one of ours waited for help (01.10.2026).</b> Each beat of a fight, every member under <see cref="DyingShare"/>
    /// is written down with the tick it fell under; the first heal aimed at it or bandage begun on it by another member
    /// (<see cref="Helped"/>) closes the wait. Nothing measured it before: "30 heals landed by their medics" said how often, never
    /// on whom or how soon.
    /// </summary>
    private void Watch()
    {
        _dyingNow.Clear();

        // The fight is over (not merely turned to another enemy): whoever was still waiting is written off the wait.
        if (Stance != BotSquadStance.Fighting)
        {
            if (_dying.Count > 0)
            {
                Ended();
            }

            return;
        }

        var now = Core.TickCount;

        for (var i = 0; i < _members.Count; i++)
        {
            var body = _members[i].Self;

            if (body is not { Deleted: false })
            {
                continue;
            }

            if (!body.Alive)
            {
                if (_dying.Remove(body.Serial, out var fell))
                {
                    if (fell.Helped)
                    {
                        DiedHelped++;
                    }
                    else
                    {
                        DiedUnhelped++;
                    }
                }

                continue;
            }

            var share = BotMend.Share(body);

            if (share < DyingShare)
            {
                _dyingNow.Add(body);

                if (!_dying.ContainsKey(body.Serial))
                {
                    _dying[body.Serial] = (now, false);
                    FellUnder++;
                }
            }
            else if (share >= DyingShare + Recovered && _dying.Remove(body.Serial, out var was) && !was.Helped)
            {
                RoseUnhelped++;
            }
        }
    }

    /// <summary>One of ours was helped: a heal aimed at it or a bandage begun on it. The first one closes its wait. See <see cref="Watch"/>.</summary>
    private void Helped(Mobile patient)
    {
        if (patient == null || !_dying.TryGetValue(patient.Serial, out var fell) || fell.Helped)
        {
            return;
        }

        _dying[patient.Serial] = (fell.Since, true);

        var ms = Core.TickCount - fell.Since;

        HelpedDying++;
        HelpedMs += ms;

        if (ms <= QuickHelpMs)
        {
            HelpedQuick++;
        }
    }

    /// <summary>The fight is over: whoever was still waiting for help is written off the wait. See <see cref="Watch"/>.</summary>
    private void Ended()
    {
        foreach (var (_, fell) in _dying)
        {
            if (!fell.Helped)
            {
                FightEndedFirst++;
            }
        }

        _dying.Clear();
        _dyingNow.Clear();
        _binders.Clear();
    }

    /// <summary>Members under <see cref="DyingShare"/> by when they fell under and whether help has come.</summary>
    private readonly Dictionary<Serial, (long Since, bool Helped)> _dying = [];

    /// <summary>Members under <see cref="DyingShare"/> this beat, for the binders. See <see cref="Bind"/>.</summary>
    private readonly List<Mobile> _dyingNow = [];

    /// <summary>Back above <see cref="DyingShare"/> by this much, a member is out of danger.</summary>
    public static double Recovered { get; set; } = 0.15;

    /// <summary>Help inside this is quick: two squad beats and a cast.</summary>
    public static int QuickHelpMs { get; set; } = 3000;

    /// <summary>
    /// Members that fell under <see cref="DyingShare"/> in a company's fight; of them, those helped (and the time it took), those
    /// that died helped and unhelped, those that rose again unhelped, and those still waiting when the fight ended.
    /// </summary>
    public static long FellUnder { get; private set; }

    public static long HelpedDying { get; private set; }

    public static long HelpedMs { get; private set; }

    public static long HelpedQuick { get; private set; }

    public static long DiedHelped { get; private set; }

    public static long DiedUnhelped { get; private set; }

    public static long RoseUnhelped { get; private set; }

    public static long FightEndedFirst { get; private set; }

    /// <summary>Heals aimed at a member under <see cref="DyingShare"/>; heals aimed by mages rather than medics; bandages begun by one member on another.</summary>
    public static long MendedDying { get; private set; }

    public static long MagesMended { get; private set; }

    public static long Bandaged { get; private set; }

    /// <summary>The support instrument, for the Companies line.</summary>
    public static string Supported() =>
        $"{FellUnder} members fell under {DyingShare:P0} health in a company's fight: {HelpedDying} helped by another (a heal aimed or a bandage begun) in a mean of {(HelpedDying == 0 ? 0.0 : HelpedMs / 1000.0 / HelpedDying):F1}s ({HelpedQuick} inside {QuickHelpMs / 1000}s), {DiedHelped} died helped and {DiedUnhelped} unhelped, {RoseUnhelped} rose again unhelped, {FightEndedFirst} still waiting when the fight ended; {MendedDying} heals aimed at the dying, {MagesMended} by mages, {Bandaged} bandages wound by one member on another ({BindWalked} walks by a member with cloth to one of ours dying beyond its reach, {BindExcused} places not handed to a member on such a walk or winding a bandage), {Shielded} beats a caster threw at what was killing one of ours";

    /// <summary>
    /// <b>A company's medics mend between fights too (01.10.2026).</b> Press asks them only in a fight, so a company that had
    /// just killed its enemy went on to the next with its members where the last one left them. Off the march, where a cast
    /// would stop the column.
    /// </summary>
    private void Tend()
    {
        if (OnTheMarch)
        {
            return;
        }

        for (var i = 0; i < _members.Count; i++)
        {
            if (BotFormation.RoleOf(_members[i]) == BotRole.Medic && _members[i].Self is { Deleted: false, Alive: true } body
                && body is not BotMobile { Resolve.Deed: BotBolt or BotSalve } && !Raising(body))
            {
                if (body.Target is { } cursor && (cursor.Flags & TargetFlags.Harmful) == 0)
                {
                    var at = Worst(body, 1.0) ?? body;

                    if (BotMend.Aim(body, at))
                    {
                        Mended++;
                        Tended++;
                    }

                    continue;
                }

                Mend(body, MendAbove);
            }
        }
    }

    /// <summary>Heals aimed by a company's medics between fights. See <see cref="Tend"/>.</summary>
    public static long Tended { get; private set; }

    /// <summary>Everybody near enough sets about the focus. Said every beat; the setter is a no-op unchanged.</summary>
    private void Press()
    {
        var focus = Focus;

        if (focus is not { Deleted: false, Alive: true })
        {
            return;
        }

        _wound.Clear();

        for (var i = 0; i < _members.Count; i++)
        {
            var body = _members[i].Self;

            if (body is not { Deleted: false, Alive: true } || body.Map != focus.Map)
            {
                continue;
            }

            // A member at the body of one of ours is the body's until it is up or given up (02.10.2026). See BotSquadRaise.
            if (Raising(body))
            {
                continue;
            }

            // <b>The dying first, wherever the member stands (01.10.2026; Patrick: «support for the dying with healing and magic»).</b>
            // A company's medics and mages heal or cure the one of ours in the worst state before they throw anything, and a
            // member with cloth beside one of ours under half its health binds it. Asked before the member's distance from the
            // enemy, because the one dying is often not where the enemy is. See Support and Bind.
            if (Support(_members[i], body))
            {
                continue;
            }

            // <b>A member running is not put back into the fight (30.09.2026, build 338).</b> Since the enlistment yields to the
            // survival rungs (BotWill.Aid) a member of a company can take flight again; the flight clears its combatant every
            // beat (BotBolt.Advance), and setting it back here every second would square the runner up to the thing it is
            // leaving.
            if (body is BotMobile { Resolve.Deed: BotBolt })
            {
                continue;
            }

            // <b>And a member within reach of what is killing one of ours turns on that, whatever its price and wherever the
            // company's enemy stands (06.10.2026, company16).</b> 181 of 429 company deaths on 405–409 were to something other than
            // the company's enemy, 133 of them to a thing priced under it — a giant spider at 545 killing a bot of 7615 while the
            // company pressed a whipping vine. The casters already throw at it (Shield); the blades and shooters near it now do too.
            // See Peel.
            if (Peel(body) is { } killing)
            {
                BotArms.Check(body, _members[i].Class);
                BotArms.Suit(body, killing, BotFormation.PressRingFor(BotFormation.RoleOf(_members[i])));
                body.Warmode = true;

                if (!ReferenceEquals(body.Combatant, killing))
                {
                    body.Combatant = killing;
                }

                if (BotFormation.RoleOf(_members[i]) is BotRole.Medic or BotRole.Caster)
                {
                    Conjure(body, killing);
                }

                continue;
            }

            if (!body.InRange(focus.Location, PressReach))
            {
                continue;
            }

            // <b>And a member being struck in melee answers what is striking it (build 338).</b> The reflex sets the combatant
            // to whatever lands a blow (BotMobile.OnDamage) and this set it back to the company's enemy on every beat, so a
            // member fought by something beside it swung at nothing: on 336 Thalion and Norbert were hit by a ratman and an
            // ogre at (1021, 1395) while the company's focus went Peghed, gazer, Peghed, gazer, Ckukchik, gazer, ogre … twelve
            // times in thirty seconds, and both died holding their enlistment. A living hostile within two tiles that has this member
            // for its own combatant keeps it; everybody else is turned to the focus as before. A medic so held has already mended
            // above (Support).
            if (Defending(body, focus))
            {
                Defended++;
                body.Warmode = true;

                continue;
            }

            // A member spared to a cry near is left on the thing until that ends (06.10.2026, react18). See BotSquadSpare.
            if (Sparing(body))
            {
                body.Warmode = true;

                continue;
            }

            // Counted here and nowhere else, because this is the beat on which a bot either fights or does
            // not. Everyone in reach is still put in warmode and still given the combatant whatever the
            // verdict: the engine retries the swing every tick, so the moment the line opens or the bot has
            // stood still long enough the blow lands without waiting for anything of ours.
            var blow = Strike(_members[i], out _);

            switch (blow)
            {
                case BotBlow.Blind:
                    Blinded++;

                    // <b>A member that cannot see the company's enemy fights the strongest of the others on us that it can see,
                    // else the enemy nearest the leader that it can (build 355; 01.10.2026).</b> Build 355 took the hostile nearest
                    // the leader; with the company's enemy now the strongest thing on the company (see Retarget), the next thing
                    // worth a blow is the next strongest on it. Turned back to the enemy on the first beat the line is open.
                    if ((Seen(body) ?? NearestToLeader(body)) is { } near)
                    {
                        NearLeader++;
                        body.Warmode = true;

                        if (!ReferenceEquals(body.Combatant, near))
                        {
                            body.Combatant = near;
                        }

                        continue;
                    }

                    break;

                case BotBlow.Refused:
                    Refused++;

                    continue;

                case BotBlow.Unsteady:
                    Unsteadied++;

                    break;
            }

            // Nothing goes into a company's fight with its fists but the brawler. A squad member's own
            // undertaking is set aside while it is Bound, so the check that lives in BotSlay never runs for
            // any of them — this is the only place it can be asked of a bot fighting as part of a company.
            BotArms.Check(body, _members[i].Class);

            // And a captain fighting in its own company draws the same steel it would draw hunting alone.
            // The ring rather than the reach: a bot pressing a focus from the far edge of PressReach is
            // still shooting, and only the ones actually standing on it are in a blade fight.
            BotArms.Suit(body, focus, BotFormation.PressRingFor(BotFormation.RoleOf(_members[i])));

            body.Warmode = true;

            if (!ReferenceEquals(body.Combatant, focus))
            {
                body.Combatant = focus;
            }

            if (blow == BotBlow.Able)
            {
                Answer(_members[i], body);
            }

            // Only the ranks that stand off. A blow disturbs a cast whenever the caster is a player and every
            // bot is one, so a blade casting from the contact ring is a bot doing neither thing: its spell is
            // broken by the first swing against it and its own swing is spent waiting for a spell. The
            // formation already draws that line — casters and medics are stationed at seven tiles and blades
            // at one — so the rank is the answer and no new rule is needed. A warrior-mage is filed under
            // Melee on purpose (see BotRole) and holds the line; hunting alone it casts, through BotSlay.
            // A medic or a mage that found nobody to mend this beat (Support) throws at the enemy — or, while one of ours is dying,
            // at what is killing it (Shield).
            if (BotFormation.RoleOf(_members[i]) is BotRole.Medic or BotRole.Caster)
            {
                Conjure(body, Shield(body) ?? focus);
            }
        }
    }

    /// <summary>
    /// <b>The company holds up the one of it that is dying (01.10.2026, Patrick's night order, point 3: «support for the dying
    /// with healing and magic»).</b> Returns whether the member spent this beat on it — a heal aimed or begun, or a bandage begun — so
    /// it is not also sent at the enemy.
    ///
    /// <para>
    /// Three hands, in the order the engine lets them work. A cursor already up is answered by what it is for: a harmful one is
    /// left to <see cref="Conjure"/>, which throws it at the enemy, and any other goes to the worst of ours in reach — build 355's
    /// medic aimed whatever cursor it held at its patient, so a magic arrow begun on a quiet beat could be thrown at a hurt friend.
    /// A medic mends the worst of ours under <see cref="MendAbove"/>; a mage the worst under <see cref="DyingShare"/> — the
    /// poisoned reckoned <see cref="PoisonWeight"/> worse, so a cure is cast before a scratch is healed (<see cref="Worst"/>). And
    /// a member with cloth and the skill for it, standing beside one of ours under DyingShare, binds it (<see cref="Bind"/>): a
    /// bandage survives a blow, where a cast does not.
    /// </para>
    /// </summary>
    private bool Support(IBotSquadMember member, Mobile body)
    {
        // Not a member in flight — a cast begun holds the caster where it stands (Spell.BlocksMovement), and its flight is what it
        // is doing; Press skips it for the same reason — nor one mending itself, whose own cursor is its own (BotSalve).
        if (body is BotMobile { Resolve.Deed: BotBolt or BotSalve })
        {
            return false;
        }

        var role = BotFormation.RoleOf(member);

        if (role is BotRole.Medic or BotRole.Caster)
        {
            if (body.Target is { } cursor)
            {
                if ((cursor.Flags & TargetFlags.Harmful) != 0)
                {
                    return false;
                }

                var at = Worst(body, 1.0) ?? body;

                if (BotMend.Aim(body, at))
                {
                    Landed(role, at);
                }

                return true;
            }

            // Mid-cast: whatever it is, it is on its way, and a medic's is a heal more often than not.
            if (body.Spell != null)
            {
                return role == BotRole.Medic;
            }

            if (Mend(body, role == BotRole.Medic ? MendAbove : DyingShare))
            {
                return true;
            }
        }

        return Bind(member, body);
    }

    /// <summary>
    /// A member with cloth and <see cref="BindSkill"/> in Healing, standing within the engine's bandage reach of one of ours under
    /// <see cref="DyingShare"/>, winds a bandage on it — one binder a patient a beat. A member binding itself is its own business
    /// (<c>BotSalve</c>). Returns whether a bandage was begun — or, since the Fable review of 01.10.2026, whether the member set
    /// out to one dying beyond its reach (<see cref="Approach"/>).
    /// </summary>
    private bool Bind(IBotSquadMember member, Mobile body)
    {
        if (_dyingNow.Count == 0 || BotMend.Winding(body) || body.Skills[SkillName.Healing].Value < BindSkill)
        {
            return false;
        }

        for (var i = 0; i < _dyingNow.Count; i++)
        {
            var patient = _dyingNow[i];

            if (ReferenceEquals(patient, body) || patient is not { Deleted: false, Alive: true } || patient.Map != body.Map
                || !body.InRange(patient.Location, BotMend.Touch) || _wound.Contains(patient.Serial)
                || BotMend.Abetting(body, patient) != null)
            {
                continue;
            }

            if (!BotMend.Wind(body, patient))
            {
                return false;
            }

            _wound.Add(patient.Serial);
            _binders.Remove(body.Serial);
            Bandaged++;
            Helped(patient);

            return true;
        }

        return Approach(member, body);
    }

    /// <summary>
    /// <b>A binder goes to the one dying when nobody stands beside it (01.10.2026, Fable review; Patrick's night order, point 3:
    /// «support for the dying with healing and magic»).</b> <see cref="Bind"/> asked only who already stood within the bandage's
    /// reach of the dying member, and in a company the ones with cloth and the skill — fifteen of the twenty-four warriors, every
    /// healer, standing behind the line or two places round the ring — mostly did not: on build 384 (00:42–11:03) 785 members
    /// fell under half their health in a company's fight, 192 were helped and 353 died unhelped, while the mages' heals waited
    /// on reagents they could not pay for (6626 beats). A member not itself dying, not the leader, with cloth and
    /// <see cref="BindSkill"/>, within <see cref="BindReach"/> of a dying member nobody is already going to, walks to it — one
    /// binder a patient, the claim good for <see cref="BindClaimMs"/> — and binds on arrival; its place in the line waits
    /// (<see cref="Station"/> passes it over) and so does the enemy (<see cref="Press"/> does not send it). The walk follows the
    /// patient, so one in flight is followed, not chased to a tile it has left. Returns whether the beat was spent on it.
    /// </summary>
    private bool Approach(IBotSquadMember member, Mobile body)
    {
        if (!BindWalks || ReferenceEquals(member, Leader) || member.Journey is not { } journey || BotMend.Share(body) < DyingShare)
        {
            return false;
        }

        var now = Core.TickCount;

        // Already on its way to one: the beat is spent while the claim is good, the patient still needs it and the walk goes on.
        // A walk the walker gave up keeps the claim until it lapses, so the same place is not searched for every beat.
        if (_binders.TryGetValue(body.Serial, out var going))
        {
            if (now - going.Since < BindClaimMs)
            {
                return going.Patient is { Deleted: false, Alive: true } still && still.Map == body.Map
                       && BotMend.Share(still) < DyingShare && journey.Active;
            }

            _binders.Remove(body.Serial);
        }

        Mobile best = null;
        var nearest = BindReach + 1;

        for (var i = 0; i < _dyingNow.Count; i++)
        {
            var patient = _dyingNow[i];

            if (ReferenceEquals(patient, body) || patient is not { Deleted: false, Alive: true } || patient.Map != body.Map
                || _wound.Contains(patient.Serial) || Claimed(patient, now))
            {
                continue;
            }

            var away = Math.Max(Math.Abs(body.X - patient.X), Math.Abs(body.Y - patient.Y));

            if (away >= nearest || BotMend.Abetting(body, patient) != null)
            {
                continue;
            }

            nearest = away;
            best = patient;
        }

        // The pack is walked only for a member that has somebody to go to: not three times a second for every member.
        if (best == null || body.Backpack?.FindItemByType<Bandage>() == null)
        {
            return false;
        }

        _binders[body.Serial] = (best, now);
        journey.Rebase(Map, best, BotArrival.Within(BotMend.Touch), "bind");
        BindWalked++;

        return true;
    }

    /// <summary>Whether a binder is already on its way to this patient. See <see cref="Approach"/>.</summary>
    private bool Claimed(Mobile patient, long now)
    {
        foreach (var (_, claim) in _binders)
        {
            if (ReferenceEquals(claim.Patient, patient) && now - claim.Since < BindClaimMs)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether this member is on its way to bind one of ours, so its place in the line is not handed out over the walk — nor,
    /// since 01.10.2026 (build next), the enlistment's own walk to the fight (<c>BotEnlist.Holding</c>). See <see cref="Approach"/>.
    /// </summary>
    internal bool Binding(Mobile body) =>
        _binders.TryGetValue(body.Serial, out var claim) && Core.TickCount - claim.Since < BindClaimMs;

    /// <summary>Binders by their serial: whom they are walking to and since when. See <see cref="Approach"/>.</summary>
    private readonly Dictionary<Serial, (Mobile Patient, long Since)> _binders = [];

    /// <summary>Whether a member with cloth walks to one of ours dying beyond its reach. A switch. See <see cref="Approach"/>.</summary>
    public static bool BindWalks { get; set; } = true;

    /// <summary>
    /// How far a binder goes to one of ours dying. Ten tiles: the width of a company's ring and its back ranks. Fourteen since
    /// 01.10.2026 (build next): a member under half its health is often one that has run, and ten left it to the mages' reagents.
    /// </summary>
    public static int BindReach { get; set; } = 14;

    /// <summary>How long a binder's claim on a patient holds, walk and all. Eight seconds.</summary>
    public static int BindClaimMs { get; set; } = 8000;

    /// <summary>Walks begun by a member with cloth to one of ours dying beyond its reach; places not handed to a member on such a walk or winding a bandage.</summary>
    public static long BindWalked { get; private set; }

    public static long BindExcused { get; private set; }

    /// <summary>
    /// <b>Magic for the dying, when there is no heal to give (01.10.2026).</b> While one of ours is under <see cref="DyingShare"/>, a
    /// caster that has nobody to mend throws at the strongest thing attacking that member rather than at the company's enemy — the
    /// other half of «with healing and magic». Null when nobody is dying, or what is killing them is the enemy already or out of its
    /// sight or reach.
    /// </summary>
    private Mobile Shield(Mobile body)
    {
        if (_dyingNow.Count == 0)
        {
            return null;
        }

        Mobile best = null;
        var power = 0.0;

        for (var i = 0; i < _attackers.Count; i++)
        {
            var a = _attackers[i];

            if (ReferenceEquals(a, Focus) || a is not { Deleted: false, Alive: true } || a.Map != body.Map
                || a.Combatant is not Mobile victim || !_dyingNow.Contains(victim) || !body.InRange(a.Location, BotStrike.Range))
            {
                continue;
            }

            var p = BotThreat.Power(a);

            if (p <= power || !body.InLOS(a) || !body.CanBeHarmful(a, false) || body.IsHarmfulCriminal(a))
            {
                continue;
            }

            power = p;
            best = a;
        }

        if (best != null)
        {
            Shielded++;
        }

        return best;
    }

    /// <summary>Beats a caster threw at what was killing one of ours rather than at the company's enemy. See <see cref="Shield"/>.</summary>
    public static long Shielded { get; private set; }

    /// <summary>Patients a bandage was begun on this beat, so two binders do not both wind one. See <see cref="Bind"/>.</summary>
    private readonly HashSet<Serial> _wound = [];

    /// <summary>A heal aimed: counted, and against the one it was aimed at if that one was dying. See <see cref="Support"/>.</summary>
    private void Landed(BotRole role, Mobile at)
    {
        Mended++;

        if (role == BotRole.Caster)
        {
            MagesMended++;
        }

        if (BotMend.Share(at) < DyingShare)
        {
            MendedDying++;
        }

        Helped(at);
    }

    /// <summary>
    /// The least Healing a member needs before it binds another in a fight. Thirty: the engine's bandage heals from the first
    /// point of Healing, and a novice's slip costs a bandage, not a life.
    ///
    /// <para>
    /// <b>Twenty, because thirty was the whole population but the healers (01.10.2026, build next).</b> After the wipe of 11:32
    /// the warriors, archers and warrior-archers carried 22 to 29 Healing and the traders 25 to 30: 18 of 86 bots cleared thirty,
    /// ten of them healers stationed seven tiles behind the line. So 773 members fell under half their health in a company's
    /// fight on build 386 and 101 were helped, 88 walks to bind in all. The engine's bandage at 22 to 29 lands one time in three
    /// (<c>BandageContext</c>: (Healing + 10) %) for fourteen to twenty points, which is a third of a warrior's life, and every
    /// bandage wound trains the skill that this gate asks for. Below twenty the slip is the likelier outcome and the cloth is
    /// better left for the one who can use it.
    /// </para>
    /// </summary>
    public static double BindSkill { get; set; } = 20.0;

    /// <summary>
    /// Under this share of its health a member is dying: a mage mends it before it throws, one beside it with cloth binds it, and
    /// the instrument times how long it waited for the first help (<see cref="Watch"/>). A half.
    /// </summary>
    public static double DyingShare { get; set; } = 0.5;

    /// <summary>How much worse than its health a poisoned member is reckoned, for who is mended first. See <see cref="Worst"/>.</summary>
    public static double PoisonWeight { get; set; } = 0.3;

    /// <summary>
    /// <b>How quickly the company answers its leader (30.09.2026, build 359; Patrick: "the company's responsiveness").</b> The first
    /// beat on which each member other than the leader stands able to land a blow on the company's enemy — near enough for its
    /// rank, in sight, lawful and, for a bow, steady (<see cref="Strike"/>) — counted once per member per enemy, with the time
    /// since the company took the enemy on, and filed under what the leader was doing then (a delve, a sweep, a harrowing…).
    /// "Able" rather than "hit": the engine swings on its own clock from the moment the combatant is set, and a miss is not
    /// slowness. Nothing measured this before: the squad beats once a second and nothing said how many of those beats went by
    /// between a company engaging and its members striking.
    /// </summary>
    private void Answer(IBotSquadMember member, Mobile body)
    {
        if (ReferenceEquals(member, Leader) || !_answered.Add(body.Serial))
        {
            return;
        }

        var answers = Answers(_engagedBy);
        var ms = Core.TickCount - _focusSinceTick;

        answers.Count++;
        answers.SumMs += ms;

        if (ms <= QuickMs)
        {
            answers.Quick++;
        }
        else if (ms > SlowMs)
        {
            answers.Slow++;
        }
    }

    /// <summary>At the end of a fight, or when it is replaced: every member but the leader still standing that never had a blow on the enemy.</summary>
    private void Tally()
    {
        if (Focus == null || _engagedBy == null)
        {
            return;
        }

        var answers = Answers(_engagedBy);

        answers.Fights++;

        for (var i = 0; i < _members.Count; i++)
        {
            if (!ReferenceEquals(_members[i], Leader) && _members[i]?.Self is { Deleted: false, Alive: true } body
                && !_answered.Contains(body.Serial))
            {
                answers.Never++;

                // By rank (01.10.2026, Fable review): a ring has eight tiles and a company of twelve has nine blades, so a blade
                // that never had a blow may be the ring's arithmetic; a shooter or a caster that never had one is a defect.
                switch (BotFormation.RoleOf(_members[i]))
                {
                    case BotRole.Ranged:
                        answers.NeverRanged++;

                        break;

                    case BotRole.Caster or BotRole.Medic:
                        answers.NeverBack++;

                        break;

                    default:
                        answers.NeverBlade++;

                        break;
                }
            }
        }
    }

    private static Answering Answers(string kind)
    {
        if (!_answers.TryGetValue(kind, out var answers))
        {
            _answers[kind] = answers = new Answering();
        }

        return answers;
    }

    /// <summary>Within this of the company engaging, a first blow is quick: two squad beats.</summary>
    public static int QuickMs { get; set; } = 2000;

    /// <summary>Past this, slow.</summary>
    public static int SlowMs { get; set; } = 10000;

    private sealed class Answering
    {
        public long Fights;

        public long Count;

        public long SumMs;

        public long Quick;

        public long Slow;

        public long Never;

        /// <summary>Of Never, by rank: blades, shooters, and the back ranks (casters and medics). See <c>Tally</c>.</summary>
        public long NeverBlade;

        public long NeverRanged;

        public long NeverBack;
    }

    private static readonly Dictionary<string, Answering> _answers = [];

    /// <summary>The instrument above, for the Companies line.</summary>
    public static string Responsiveness()
    {
        if (_answers.Count == 0)
        {
            return "no company has engaged anything yet";
        }

        var say = ValueStringBuilder.Create(256);

        try
        {
            foreach (var (kind, a) in _answers)
            {
                if (say.Length > 0)
                {
                    say.Append(", ");
                }

                var mean = a.Count == 0 ? 0.0 : a.SumMs / 1000.0 / a.Count;

                say.Append($"{kind} {a.Count} over {a.Fights} fights (a mean of {mean:F1}s; {a.Quick} inside {QuickMs / 1000}s, {a.Slow} past {SlowMs / 1000}s; {a.Never} members never had one: {a.NeverBlade} blades, {a.NeverRanged} shooters, {a.NeverBack} of the back ranks)");
            }

            return say.ToString();
        }
        finally
        {
            say.Dispose();
        }
    }

    /// <summary>
    /// Whether this member is being struck in melee by something other than the company's enemy: a living hostile within two
    /// tiles that has it for its own combatant, and that it has for its own. See <see cref="Press"/> (build 338).
    /// </summary>
    private bool Defending(Mobile body, Mobile focus) =>
        body.Combatant is Mobile { Deleted: false, Alive: true } striker && !ReferenceEquals(striker, focus)
        && ReferenceEquals(striker.Combatant, body) && striker.Map == body.Map && body.InRange(striker.Location, DefendReach)
        && !(striker is IBotSquadMember && Has(striker));

    /// <summary>
    /// How far off a thing that has a member for its combatant, and that the member has for its own, may stand for the press to
    /// leave the member on it rather than turn it back to the company's enemy.
    ///
    /// <para>
    /// <b>Two tiles made every member passive to anything not in its face (06.10.2026, react17).</b> The reflex at the blow and
    /// the member's own look at what is coming (<c>BotMobile.Menace</c>) square it up to what it can beat, and the press turned
    /// it back on the next beat unless the thing stood within two tiles: an archer or a caster eight tiles off had the member
    /// for as long as it liked. On build 405 (02:05–05:35) 703 of 843 first blows that landed before anything was decided fell
    /// on a member whose company had been told, and 95 of 196 bots that crossed the failing line with nothing decided were
    /// members. Eight: what a bot notices at. The company keeps its enemy; the member stops being passive.
    /// </para>
    /// </summary>
    public static int DefendReach { get; set; } = 8;

    /// <summary>
    /// A medic in a company mends whoever in it is worst hurt. Returns whether it is busy doing so.
    ///
    /// <para>
    /// <b>The same hole as the casting, found in the same hour, and worse.</b> <c>BotSurgeon</c> — the only
    /// thing on this shard that offers "go and patch somebody up" — is a proposer on the <c>Free</c> rung,
    /// and a squad member is <c>Bound</c>, where <c>BotWill</c> skips the auction entirely. So a healer that
    /// joined a company stopped healing: not slowly, not badly — at all, for as long as the company lasted.
    /// Five of the fifty-four bots on this shard are healers and every muster is written on the assumption
    /// that one of them is behind the line.
    /// </para>
    ///
    /// <para>
    /// <b>Mending outranks throwing, and that ordering is the whole of the rule.</b> Given a spellbook and
    /// an enemy, the attack ladder would happily spend a medic's whole pool on magic arrows while the blade
    /// in front of it bled out — which is a strictly worse use of the same mana. So the medic is asked for a
    /// patient first and only reaches for the attack ladder when the company is whole.
    /// </para>
    ///
    /// <para>
    /// Cloth is not attempted here: this is the two-second spell, which is the thing a medic behind a line is for. Since
    /// 01.10.2026 cloth is <see cref="Bind"/>'s, for whoever stands beside one of ours at death's door with the skill for it. A
    /// medic with no book and no mana falls through to cloth, then to the attack ladder and then to its stick.
    /// </para>
    /// </summary>
    private bool Mend(Mobile body, double below)
    {
        // The click is Support's, which answers a cursor by what it is for (01.10.2026). What is left here is the choice.
        // <b>Worked out again rather than remembered, and with five healers on this shard that is the
        // difference between a rule and a race.</b> A field on the company holding "who is being healed"
        // is one field for however many medics are in it: the second one to begin a cast overwrites it, and
        // the first one's click then lands on the second one's patient. Recomputing costs a walk of the
        // members and cannot be wrong — if the worst-hurt has changed since the cast began, the new
        // worst-hurt is the right place to put the heal anyway.
        if (body.Target != null || body.Spell != null)
        {
            return true;
        }

        var patient = Worst(body, below);

        if (patient == null)
        {
            return false;
        }

        var spell = BotMend.Spell(body, patient);

        if (spell < 0)
        {
            return false;
        }

        if (!BotMend.Begin(body, spell))
        {
            return false;
        }

        // The stopwatch: the cast begun is the medic's decision; the heal lands when the cast is done. See BotReflex.Casting.
        BotReflex.Casting(body, patient);

        return true;
    }

    /// <summary>
    /// The member of this company in the worst state within the medic's own reach, itself included, under
    /// <paramref name="below"/> of its health — the poisoned reckoned <see cref="PoisonWeight"/> worse than their wounds (01.10.2026:
    /// «heal, or cure of poison, whoever is in the worst state»), so a cure is cast on a green bot at nine tenths before a scratch
    /// is healed on a clean one, and a bot at a third outranks both.
    ///
    /// <para>
    /// Itself included for the reason <c>BotSurgeon</c> gives: below its own failing mark a bot is on a
    /// higher rung and looking after itself already, and above it nothing else in the world would offer.
    /// </para>
    /// </summary>
    /// <summary>Whether a company with no enemy nurses a patient of ours who is not a member. A switch. See <see cref="Nurse"/>.</summary>
    public static bool Nurses { get; set; } = true;

    /// <summary>How long a guest is a patient of the company's medics after being brought to their notice.</summary>
    public static int GuestMs { get; set; } = 30000;

    /// <summary>Guests brought to a company's medics, and heals its medics aimed at one.</summary>
    public static long Guests { get; private set; }

    public static long GuestHeals { get; private set; }

    /// <summary>A patient of ours who is not a member, brought to the medics' notice. See <see cref="Nurse"/>.</summary>
    public Mobile Guest { get; private set; }

    private long _guestTick;

    /// <summary>
    /// <b>A company's medic answers a wounded bot of ours who is not one of its members, when the company has no enemy
    /// (06.10.2026, react17; Patrick: a healer helps and heals every non-hostile, non-criminal bot).</b> A member is bound,
    /// so the surgeon's auction never asks it; its medics mend by the company's own hand (<see cref="Worst"/>), which looked
    /// at the roster only. On build 405 11031 bots near a patient were passed over as "in a company". Told by the blow that
    /// made the patient (<c>BotSurgeon.Call</c>), never by a look of its own: the guest is one more name the roster's walk
    /// weighs, for <see cref="GuestMs"/>, and the company is asked to answer now.
    /// </summary>
    internal void Nurse(Mobile patient)
    {
        if (!Nurses || patient is not { Deleted: false, Alive: true } || Focus is { Deleted: false, Alive: true } || OnTheMarch)
        {
            return;
        }

        Guest = patient;
        _guestTick = Core.TickCount;
        Guests++;

        BotSquads.Hurry(this);
    }

    private Mobile Worst(Mobile medic, double below)
    {
        Mobile worst = null;
        var lowest = below;

        // The guest is weighed as a member is, while its time stands and the company has no enemy. See Nurse.
        var guest = Nurses && Guest is { Deleted: false, Alive: true } g && Core.TickCount - _guestTick < GuestMs
                    && Focus is not { Deleted: false, Alive: true } && !Has(g)
            ? g
            : null;

        for (var i = -1; i < _members.Count; i++)
        {
            var body = i < 0 ? guest : _members[i].Self;

            if (body is not { Deleted: false, Alive: true } || body.Map != medic.Map || body.HitsMax <= 0)
            {
                continue;
            }

            var share = body.Hits / (double)body.HitsMax - (body.Poisoned ? PoisonWeight : 0.0);

            if (share >= lowest)
            {
                continue;
            }

            if (!medic.InRange(body.Location, BotSurgeon.Reach) || !medic.InLOS(body))
            {
                continue;
            }

            // Not a member it would be a crime to help — the question BotMend asks of every lone healer, which a company's
            // medic never did: a member who has gone grey in the fight greys whoever bandages it. See BotMend.Abetting.
            if (BotMend.Abetting(medic, body) != null)
            {
                continue;
            }

            lowest = share;
            worst = body;
        }

        if (worst != null && ReferenceEquals(worst, guest))
        {
            GuestHeals++;
        }

        return worst;
    }

    /// <summary>
    /// How hurt somebody has to be before a company's medic spends a cast on them.
    ///
    /// <para>
    /// Four fifths, which is the engine's own opinion of "genuinely hurt" put in one number: healing a bot
    /// that has taken one scratch is the training dummy with a friend in it, and that is the shape the whole
    /// ledger exists to refuse. See BotSurgeon, which says the same thing on the other side of the rung.
    /// </para>
    /// </summary>
    public static double MendAbove { get; set; } = 0.8;

    /// <summary>
    /// A caster in a company casts, and until 04.09.2026 it did not.
    ///
    /// <para>
    /// <b>The same shape as the archers, found in the same hour and for the same reason.</b> <c>BotStrike</c>
    /// was written because "a caster's book filled up, its Inscribe climbed, its reagents were bought and
    /// spent — on writing; in a fight it walked up and hit things with a stick". It is called from exactly
    /// two places, <c>BotSlay</c> and the Baron's harrowing, and both of them are a bot fighting <em>alone</em>.
    /// A member of a company is Bound, its own undertaking is set aside, and the only thing anything did for
    /// it was set a combatant — which is the engine's word for "swing whatever is in your hands". So every
    /// mage, every healer and every warrior-mage that joined a company went straight back to hitting things
    /// with a stick, which is the one thing its whole build is arranged to avoid.
    /// </para>
    ///
    /// <para>
    /// <b>Melee goes on underneath, deliberately</b> — the engine swings on its own timer whatever else the
    /// mobile is doing, so a caster loses nothing by also being in a fight, and a caster out of mana is
    /// simply a bot with a stick again. That is BotSlay's own reasoning and it is repeated rather than
    /// referenced only because the two live on opposite sides of the rung.
    /// </para>
    ///
    /// <para>
    /// <b>Stateless, unlike the hunt's version, and that is a decision.</b> BotSlay keeps three fields to
    /// throttle itself; here the engine's own two are enough — a cast in flight sets <c>Spell</c>, a cast
    /// waiting for its target sets <c>Target</c> — and <c>Begin</c> refuses politely during recovery. A
    /// company beats once a second against a recovery of <see cref="BotStrike.CastMs"/>, so the wasted ask
    /// is at most one a cycle, and the alternative is a per-member record on an object that is rebuilt every
    /// world load.
    /// </para>
    /// </summary>
    private static void Conjure(Mobile body, Mobile focus)
    {
        if (!BotStrike.Can(body))
        {
            return;
        }

        // The click a bot has no client to make. Only ever our own cursor: a Bound bot's own undertaking is
        // set aside, so nothing else of ours is putting one up.
        if (body.Target != null)
        {
            BotStrike.Aim(body, focus);

            return;
        }

        // Mid-cast. The engine holds the delay, and the formation has already been told to leave a member
        // that can strike where it stands.
        if (body.Spell != null)
        {
            return;
        }

        var spell = BotStrike.Best(body);

        if (spell < 0)
        {
            // It holds a book or a scroll and cannot pay for anything in it this instant. Counted, because
            // "the casters are casting" and "the casters are out of mana" look identical from every other
            // number on this shard, and the cure for the second is reagents rather than arithmetic.
            Dry++;

            return;
        }

        if (BotStrike.Begin(body, spell))
        {
            Conjured++;
        }
    }

    /// <summary>
    /// Whether this company still has a reason to exist. See <see cref="IdleCapMs"/>.
    ///
    /// The clock starts when the fighting stops and is torn up when it starts again, so a squad working
    /// through a graveyard one resident at a time never ages, and one standing in an empty field does.
    /// </summary>
    private bool Quiet()
    {
        // <b>A company with a standing charge does not age.</b> The quiet clock is the right rule for a
        // muster — five bots who gathered against one troll and killed it have no further reason to be a
        // company, and holding them together would be fifteen bots' worth of shard doing four bots' worth of
        // work. It is exactly the wrong rule for a patrol, whose entire value is being in a dangerous place
        // *before* anything happens there: measured by this clock, the better a patrol is working the sooner
        // it is disbanded for having nothing to fight. Whoever sets this owns ending the company; see
        // BotSweep, which fences itself with two clocks of its own for that reason.
        if (Charged)
        {
            _quiet = false;

            return true;
        }

        // Nor one at the body of one of its own (02.10.2026). See BotSquadRaise. Nor one holding on its leader after a pull-back
        // (06.10.2026, company16): see Bleed.
        if (Stance == BotSquadStance.Fighting || RaiseUnderway || Recovering)
        {
            _quiet = false;

            return true;
        }

        if (!_quiet)
        {
            _quiet = true;
            _quietTick = Core.TickCount;

            return true;
        }

        return Core.TickCount - _quietTick < IdleCapMs;
    }

    /// <summary>
    /// How long a company may go without a fight before it stops holding members who are doing nothing for it.
    ///
    /// <para>
    /// Half a minute, which is well short of the four the stall watch waits and well past any gap between two
    /// undertakings, so a bot is let go before anybody reports it standing still and not for merely being
    /// between jobs.
    /// </para>
    /// </summary>
    public static int RestCapMs { get; set; } = 30000;

    /// <summary>Members let go for doing nothing for a company that was doing nothing. For the summary.</summary>
    public static long Released { get; private set; }

    /// <summary>Beats on which a member stood near enough to fight and had no line to the thing. For the summary.</summary>
    public static long Blinded { get; private set; }

    /// <summary>Beats on which the engine itself refused the blow. Counted apart: a different cure.</summary>
    public static long Refused { get; private set; }

    /// <summary>Beats on which a shooter was in place and had moved too recently to fire. See BotBlow.Unsteady.</summary>
    public static long Unsteadied { get; private set; }

    /// <summary>Spells a company's back ranks actually got off. Nought before 04.09.2026: see Conjure.</summary>
    public static long Conjured { get; private set; }

    /// <summary>Heals a company's medics landed on their own. Nought before 04.09.2026: see Mend.</summary>
    public static long Mended { get; private set; }

    /// <summary>Charges taken back because the errand holding them had ended. See Update.</summary>
    public static long Unowned { get; private set; }

    /// <summary>Beats a caster in a company held a book it could not pay a single spell out of.</summary>
    public static long Dry { get; private set; }

    /// <summary>Fights given up because not one member could land a blow from anywhere it could reach.</summary>
    public static long Blindfights { get; private set; }

    public static void Forget()
    {
        Released = 0;
        Blinded = 0;
        Refused = 0;
        Unsteadied = 0;
        Conjured = 0;
        Mended = 0;
        Unowned = 0;
        Dry = 0;
        Blindfights = 0;
        Defended = 0;
        Excused = 0;
        _answers.Clear();
        ToStrongest = 0;
        ToStronger = 0;
        LeaderCalls = 0;
        HeldFor = 0;
        Engaged = 0;
        Switched = 0;
        SwitchedSoon = 0;
        Downed = 0;
        Closing = 0;
        FellUnder = 0;
        HelpedDying = 0;
        HelpedMs = 0;
        HelpedQuick = 0;
        DiedHelped = 0;
        DiedUnhelped = 0;
        RoseUnhelped = 0;
        FightEndedFirst = 0;
        MendedDying = 0;
        MagesMended = 0;
        Bandaged = 0;
        Tended = 0;
        Shielded = 0;
        BindWalked = 0;
        BindExcused = 0;
        Withdrawals = 0;
        WithdrewNear = 0;
        WithdrewFar = 0;
        Shied = 0;
        WithdrewAgain = 0;
        WithdrewBy.Clear();
        _withdrawn.Clear();
        _shunned.Clear();
        ForgetField();
    }

    /// <summary>
    /// Lets go of anybody who is neither doing something of its own nor going anywhere for the company.
    ///
    /// <para>
    /// <b>An uncharged company already dies of this in eight seconds — see <see cref="IdleCapMs"/> — and a
    /// charged one never does, by design and rightly: whoever set the charge owns ending it.</b> But the
    /// charge was also holding everybody else. A bot in a company sits on the Bound rung, and
    /// <c>BotWill</c> skips the auction entirely for Bound — so a member of a captain's party that is walking
    /// somewhere five hundred tiles away, and has no station to walk to and nothing of its own in hand, is
    /// offered nothing at all. Not even the way home: <c>BotHomer</c> is a Free-rung errand.
    /// </para>
    ///
    /// <para>
    /// Patrick's decision of 03.09.2026, put as "release the bound from an idle company" — the other way
    /// round would have been to exempt the way home from the rung, which fixes the symptom and leaves the
    /// bot bound to something that has no use for it.
    /// </para>
    ///
    /// <para>
    /// Three guards, and each of them is what keeps this from breaking a working company. A company in a
    /// fight lets nobody go, and the clock is torn up the moment it engages. A member that has moved in the
    /// last two seconds is walking to a station and is doing exactly what the company asked. A member
    /// holding an undertaking of its own is not idle whatever it looks like — an <c>Alongside</c> errand is
    /// allowed to run while Bound. And the leader is never let go of, because a company without one is a
    /// separate rule two screens up.
    /// </para>
    /// </summary>
    private void Release()
    {
        // <b>A company on a charge keeps its members, idle or not.</b> A delve party is a charged company
        // three hundred tiles from the island with no road home but the delve's own carrying; on 13.09.2026
        // Squad 23 let Isolde and Faron go "for doing nothing" in the Orc Caves, the delve ended without
        // them, and the two stood underground being offered the island's work - 858 stakes refused in ten
        // minutes, with the rescue refusing to carry anybody who was "delving". Whoever set the charge owns
        // the roster until it is released.
        if (Charged)
        {
            _resting = false;

            return;
        }

        if (Stance == BotSquadStance.Fighting || Recovering)
        {
            _resting = false;

            return;
        }

        if (!_resting)
        {
            _resting = true;
            _restTick = Core.TickCount;

            return;
        }

        if (Core.TickCount - _restTick < RestCapMs)
        {
            return;
        }

        for (var i = _members.Count - 1; i >= 0; i--)
        {
            var member = _members[i];

            if (ReferenceEquals(member, Leader) || member.Self is not BotMobile bot)
            {
                continue;
            }

            // <b>The comment two screens up said <c>Alongside</c> and the code said "any errand at all", and
            // the difference is the longest freeze on this shard.</b> A Bound bot's own auction is skipped
            // (<c>BotWill.cs</c>), and an errand that is not <c>Alongside</c> is set aside rather than
            // advanced — so a member holding one is doing nothing, can do nothing, and cannot be offered
            // anything, and this line was reading exactly that state as "busy, leave it alone". It sat there
            // until <c>AsideCapMs</c> gave the errand up: ten minutes. The log of 03.09.2026 has twenty-four
            // of them in a day, eleven of them <c>unload</c>, and forty-four of sixty-five stall reports
            // carrying the words "in a company".
            //
            // A parked errand is not a claim on the bot. It is the reason to let go of it: released, the bot
            // is Free on the next beat and the errand it was holding advances instead of expiring.
            if (bot.Journey is { Moving: true } || bot.Resolve?.Deed is { Alongside: true } || Raising(bot))
            {
                continue;
            }

            _members.RemoveAt(i);
            Roster++;
            member.Squad = null;
            Released++;

            logger.Information(
                "Squad {Id} let {Name} go: it had nothing of its own and nowhere of ours to walk to",
                Id,
                bot.Name
            );
        }
    }

    /// <summary>When this company last had a fight to be in. See <see cref="Release"/>.</summary>
    private long _restTick;

    private bool _resting;

    /// <summary>Members who are gone, dead, or somewhere else entirely.</summary>
    private void Prune()
    {
        for (var i = _members.Count - 1; i >= 0; i--)
        {
            var body = _members[i].Self;

            if (body is { Deleted: false } && body.Map == Map && body.Map != Map.Internal)
            {
                continue;
            }

            _members.RemoveAt(i);
            Roster++;
        }
    }

    /// <summary>
    /// The leader has fallen. The strongest survivor takes over — power by the same measure everything else
    /// uses, so the answer is the same one the squad would give about an enemy.
    /// </summary>
    private bool Inherit()
    {
        IBotSquadMember heir = null;
        var best = 0.0;
        var heirHolds = false;

        for (var i = 0; i < _members.Count; i++)
        {
            var candidate = _members[i];

            if (candidate.Self is not { Deleted: false, Alive: true })
            {
                continue;
            }

            // A survivor still holding the company's own errand outranks a stronger one holding nothing:
            // the charge is read off the leader's errand (see Update), and a member back from the dead
            // holds none. See Warring for the war company, which does not depend on this.
            var holds = candidate.Self is BotMobile mobile && mobile.Resolve?.Deed is { Alongside: true };
            var power = BotThreat.Power(candidate.Self);

            if (heir != null && (heirHolds && !holds || heirHolds == holds && power <= best))
            {
                continue;
            }

            heir = candidate;
            best = power;
            heirHolds = holds;
        }

        if (heir == null)
        {
            return false;
        }

        Leader = heir;

        logger.Information("Squad {Id} lost its leader; {Name} has it now", Id, heir.Self.Name);

        return true;
    }

    /// <summary>Whether the fight is still a fight. See <see cref="NoProgressMs"/>.</summary>
    private void Judge()
    {
        var focus = Focus;

        if (focus is not { Deleted: false, Alive: true })
        {
            // Before the focus is let go, because letting it go loses the only reference to what was killed
            // and therefore to whose corpse this is. A company that kills something and walks away from the
            // purse has done the one piece of work on this shard that brings new gold into the world, and
            // then declined the gold.
            Spoils(focus);

            Kills++;
            Downed++;

            // A share of the kill to each member standing, and the leader and each member a little closer (07.10.2026, traits17).
            BotBiography.CompanyKill(this);

            // At the odds the company took it on at: the companies' record of what they beat (03.10.2026). See BotCompanyRecord.
            if (focus is BaseCreature && _fought.TryGetValue(focus.GetType(), out var fought) && fought.Members >= 2
                && _focusPower > 0.0)
            {
                BotCompanyRecord.Killed(focus.GetType(), fought.Able / _focusPower);
            }

            Disengage("it is down");

            return;
        }

        // Kept while it is alive: a corpse lies where the thing fell, and by the time anybody notices it is
        // dead the mobile itself may already be off the map.
        _fell = focus.Location;

        if (focus.Hits < _focusLowest)
        {
            _focusLowest = focus.Hits;
            _focusProgressTick = Core.TickCount;
        }

        if (Core.TickCount - _focusSinceTick >= FightCapMs)
        {
            Disengage("four minutes is long enough", true);

            return;
        }

        // <b>Twelve seconds is the patience a fight deserves; a fight nobody is in deserves none of it.</b>
        // "Its health has not moved" is the right clock for a company that is swinging and missing, and the
        // wrong one for a company that is not swinging at all — and until the line was asked of the engine
        // rather than of the distance, those two were the same reading. A company that cannot land a blow
        // from anywhere it has been able to reach has tested its hypothesis and failed it; sitting out the
        // rest of the window costs every member of it the difference, on top of the quiet clock afterwards.
        //
        // The clock is torn up by anybody at all becoming able to strike, so a company working its way round
        // an obstacle is never cut off — only one that has been standing blind throughout.
        // <b>And the clock must not start before it can mean anything.</b> Its first evening cut two fights
        // off at four seconds with the break-off line reading "0 of 5 able to strike (0 with no line to it,
        // 0 refused), nearest 14 tiles off" — nobody was blind, nobody was refused, nobody had arrived. A
        // company still walking to a fight is not a company failing at one, and "able to strike" is false of
        // both. Only the members standing on their own ring can say anything about whether the arrangement
        // works, so with none of them there yet the clock is torn up along with the rest.
        var able = InReach(out var nearest, out var blind, out var refused, out var unsteady);
        var arrived = able + blind + refused + unsteady;

        // <b>A company still closing is getting there (01.10.2026).</b> "We never got near it" was said of whatever nobody had
        // reached in CloseMs from the moment of engaging, and a company finder reaches a hundred and twenty tiles: on 30.09 it
        // was 101 of the 522 fights that ended (326 went down), the nearest member anywhere from 2 to 1769 tiles off when the
        // clock ran out, a median near 70. The clock is torn up whenever the nearest member stands nearer than any beat before;
        // only a company that has stopped closing for CloseMs gives the thing up. FightCapMs still bounds the whole.
        if (arrived == 0 && nearest >= 0 && nearest < _nearestSeen)
        {
            if (_nearestSeen != int.MaxValue)
            {
                Closing++;
            }

            _nearestSeen = nearest;
            _closedTick = Core.TickCount;
        }

        // <b>Both of the clocks below judge a fight, and neither of them may run before there is one.</b>
        // A company engages whatever the finder hands it, and the finder reaches forty tiles; the walk to a
        // spectre twenty tiles off is a fair part of a minute. Started at the moment of engaging, "its
        // health has not moved" was being said of a creature nobody had reached — Squad 63 said it twice in
        // one window at seventeen and twenty tiles, and every one of those break-offs shuns the creature and
        // then hands it straight back through the defender's path, which is where the forty-one rebuffs in
        // the same window came from. The outer bound stays: a company that can never close gives up on the
        // clock below rather than standing there for the whole FightCapMs.
        if (arrived == 0)
        {
            _focusProgressTick = Core.TickCount;
            _ableTick = Core.TickCount;

            if (Core.TickCount - _closedTick >= CloseMs)
            {
                Disengage("we never got near it", true);
            }

            return;
        }

        _closedTick = Core.TickCount;

        if (able > 0 || unsteady > 0 || blind + refused == 0)
        {
            _ableTick = Core.TickCount;
        }
        else if (Core.TickCount - _ableTick >= BlindMs)
        {
            Blindfights++;

            Disengage(
                blind > refused
                    ? "we are standing on it and there is no line to it"
                    : "we are standing on it and the engine refuses every blow",
                true
            );

            return;
        }

        if (Core.TickCount - _focusProgressTick >= NoProgressMs)
        {
            Disengage("its health has not moved", true);
        }
    }

    /// <summary>
    /// How long a company may stand round something none of it can hit before it gives the thing up.
    ///
    /// <para>
    /// Four seconds: long enough for the ring to be turned once and walked to — the formation re-stations at
    /// <see cref="RestationMs"/> and a blade covers a couple of tiles in that time — and short enough that a
    /// creature inside a crypt wall costs a company one third of what it used to.
    /// </para>
    /// </summary>
    public static int BlindMs { get; set; } = 4000;

    /// <summary>When somebody in this company was last able to land a blow on the focus. See <see cref="BlindMs"/>.</summary>
    private long _ableTick;

    /// <summary>
    /// How long a company may spend walking at something without one member of it arriving, before it gives
    /// the thing up.
    ///
    /// <para>
    /// Half a minute, which is a generous crossing of the forty tiles the company finder reaches, and a
    /// sixth of the outright cap on a fight. What it replaces is the wrong sentence rather than a missing
    /// one: before this, a company that never arrived broke off after twelve seconds saying the target's
    /// health had not moved — true, unhelpful, and it taught the whole population to shun a creature nobody
    /// had touched.
    /// </para>
    /// </summary>
    public static int CloseMs { get; set; } = 30000;

    /// <summary>When anybody in this company was last within reach of the focus at all, or came nearer to it than ever. See <see cref="CloseMs"/>.</summary>
    private long _closedTick;

    /// <summary>The nearest any member has stood to the present focus while nobody had reached it. See <see cref="Judge"/>.</summary>
    private int _nearestSeen = int.MaxValue;

    /// <summary>Beats the "never got near it" clock was torn up because the company was still closing. See <see cref="Judge"/>.</summary>
    public static long Closing { get; private set; }

    /// <summary>
    /// What the company killed, divided among whoever is standing there for it.
    ///
    /// <para>
    /// The share-out itself is <see cref="BotSpoils"/>'s and has been ready the whole time — it was written
    /// for the hunter's own corpse-rifling, which hands it a corpse when it finds one. A squad kill has
    /// nobody rifling anything: no member holds an undertaking about this creature, so the corpse was
    /// nobody's business. This is the one line that makes it somebody's.
    /// </para>
    ///
    /// <para>
    /// What it carried is priced for the whole population on the way past, exactly as a lone kill is, or the
    /// only creatures anybody ever learns the worth of are the small ones a bot can take by itself — which
    /// is precisely backwards.
    /// </para>
    /// </summary>
    private void Spoils(Mobile fallen)
    {
        var collector = Contact ?? Leader;
        var map = Map;

        if (fallen == null || collector?.Self is not { Deleted: false, Alive: true } || map == null)
        {
            return;
        }

        var corpse = BotQuarry.Remains(map, _fell, fallen);

        BotQuarry.Release(fallen);

        if (corpse == null)
        {
            return;
        }

        var coin = 0;
        var lying = corpse.Items;

        for (var i = 0; i < lying.Count; i++)
        {
            if (lying[i] is Gold gold)
            {
                coin += gold.Amount;
            }
        }

        BotSpoils.Share(this, collector, corpse);

        BotQuarry.Paid(fallen.GetType(), coin);
    }

    /// <summary>
    /// Works out which of the three states the squad is in.
    ///
    /// Scouting is entered from the one situation the first version described exactly: arrived somewhere and
    /// met nobody. Eight bots sent to the same coordinate stand in a heap, see the same eight tiles as each
    /// other, and the corner of the ground where the spawner is quietly refilling is watched by nobody — for
    /// the five to ten minutes a spawn timer takes.
    /// </summary>
    private void Settle()
    {
        if (Focus is { Deleted: false, Alive: true })
        {
            Stance = BotSquadStance.Fighting;

            return;
        }

        var leader = Leader?.Self;

        // On the road a leader that stands is holding the company together, not scouting empty ground. See OnTheMarch.
        // Nor is a company that pulled back scattered into knots: it holds on its leader (company16). See Bleed.
        Stance = leader != null && Leader.Journey?.Active != true && !OnTheMarch && !Recovering
            ? BotSquadStance.Scouting
            : BotSquadStance.Marching;
    }

    /// <summary>
    /// Puts everybody where they belong, when where they belong has changed.
    ///
    /// Guarded on three things rather than run every beat, because each station ends in a path search: the
    /// anchor drifting, the state changing, and the roster changing. Between those, everybody is already
    /// walking to the right place and there is nothing to say.
    /// </summary>
    private static readonly Dictionary<Serial, long> _stationRefused = [];

    /// <summary>How long a member whose place in the line could not be walked to is left without one.</summary>
    public static int StationRestMs { get; set; } = 15000;

    /// <summary>Places not handed out to a member resting from a refused one. See <see cref="StationRestMs"/>.</summary>
    public static long StationRested { get; private set; }

    /// <summary>How far from the anchor a member may stand and still be handed a place in a fight. Beyond it the enlistment walks it in. See <see cref="Station"/> (build 388).</summary>
    public static int StationReach { get; set; } = 120;

    /// <summary>Places withheld, in a fight, from a member further than <see cref="StationReach"/> from the anchor.</summary>
    public static long StationFar { get; private set; }

    /// <summary>Members set resting because the search for their last place came back short of it. See <see cref="Station"/>.</summary>
    public static long StationShorted { get; private set; }

    /// <summary>The walker could not take this member to its place in the line. From <c>BotWalk.Drop</c>.</summary>
    public static void StationRefused(Mobile member)
    {
        if (member == null)
        {
            return;
        }

        if (_stationRefused.Count > 512)
        {
            _stationRefused.Clear();
        }

        _stationRefused[member.Serial] = Core.TickCount;
    }

    private void Station()
    {
        var map = Map;

        if (map == null)
        {
            return;
        }

        var anchor = Anchor;

        var moved = Math.Max(Math.Abs(anchor.X - _stationedAt.X), Math.Abs(anchor.Y - _stationedAt.Y));

        var all = moved >= ReformDistance || _stationedStance != Stance || _stationedCount != _members.Count;

        // Each round of this ends in a path search per member, and the fighting stance now re-stations
        // whoever has stopped short as well as whoever the anchor has walked away from — so it is worth
        // saying how often at most, once, here.
        if (Core.TickCount - _stationedTick < RestationMs)
        {
            return;
        }

        if (all)
        {
            _stationedAt = anchor;
            _stationedStance = Stance;
            _stationedCount = _members.Count;
        }

        var sent = false;

        for (var i = 0; i < _members.Count; i++)
        {
            var member = _members[i];
            var journey = member.Journey;

            if (journey == null)
            {
                continue;
            }

            // <b>The half of stationing that was missing: somebody who has stopped and is not there yet.</b>
            // Stations were worked out only when the anchor moved, so a member whose walk to its place was
            // dropped — a blocked tile, a plan that got no closer — simply stood where it gave up for the rest
            // of the fight, and nothing ever asked it again. That is a bot in a fight doing nothing, which
            // reads in the log exactly like a bot in a fight.
            if (!all && !Stranded(member, journey))
            {
                continue;
            }

            // The leader is not stationed on itself. It goes where it was going, and everybody else is
            // arranged around wherever that turns out to be.
            if (ReferenceEquals(member, Leader) && Stance != BotSquadStance.Fighting)
            {
                continue;
            }

            // <b>Nor a member running or binding a wound of its own (30.09.2026, build 338).</b> A station is written into the
            // same slot as the bot's own walk, so a flight away from the thing was turned back to a place beside it every time
            // the anchor moved two tiles, and the flight's next beat sent it away again. Since the enlistment yields to the
            // survival rungs (BotWill.Aid) this is every member of a company that is losing; it is stationed again the beat
            // the flight or the bandage is done.
            if (member.Self is BotMobile { Resolve.Deed: BotBolt or BotSalve })
            {
                Excused++;

                continue;
            }

            // Nor one at the body of one of ours, raising it, in any stance (02.10.2026). See BotSquadRaise.
            if (member.Self is { } raiser && Raising(raiser))
            {
                Excused++;

                continue;
            }

            // <b>Nor one walking to bind one of ours, or winding the bandage (01.10.2026, Fable review).</b> The walk to the
            // dying member is written into the same slot (Approach), and the engine's bandage asks the binder to be beside its
            // patient when it is done — a place in the line handed out meanwhile would walk it off both.
            if (Stance == BotSquadStance.Fighting && member.Self is { } binder && (Binding(binder) || BotMend.Winding(binder)))
            {
                BindExcused++;

                continue;
            }

            // <b>A member that can already hit the thing is standing in a good place, and the arithmetic has
            // nothing better to offer it.</b> The wholesale re-station fires whenever the focus drifts two
            // tiles, which a creature in a melee does every second or so — and every one of those walks
            // resets a shooter's stillness clock, which <c>BaseRanged.OnSwing</c> requires a full second of
            // on this era. So the archers were marched back and forth across one tile for the length of the
            // fight and never fired once, while the company reported them in the fight the whole time:
            // Squad 7 broke off from three creatures in a row on 04.09.2026 with "2 of 3 able to strike,
            // 0 blind, 0 refused, nearest 5 tiles off" and the target's health exactly where it started.
            //
            // Unsteady counts as a good place too, and that is the point rather than an oversight: a bot
            // that has just moved wants the one thing a re-station cannot give it, which is to be left where
            // it is. Anybody the ring has to fix — too far, or with no line — is untouched by this and is
            // moved as before.
            if (Stance == BotSquadStance.Fighting && Strike(member, out _) is BotBlow.Able or BotBlow.Unsteady)
            {
                continue;
            }

            // Nor one standing to something that is striking it in melee: walked to its place round the company's enemy, it
            // would be struck in the back all the way there (build 338). See Press.
            if (Stance == BotSquadStance.Fighting && member.Self is { Deleted: false, Alive: true } struck && Focus is { } enemy
                && Defending(struck, enemy))
            {
                continue;
            }

            // <b>On the march a far member is not stationed (29.09.2026, build 313).</b> The enlistment's close-up walks it
            // to the leader (BotEnlist.Holding); a station handed out every second and a half sent it back sixty tiles
            // the other way, and Edith on 312 went to the station. Near, it is stationed as ever.
            if (Stance == BotSquadStance.Marching && !ReferenceEquals(member, Leader) && member.Self is { } far
                && !far.InRange(anchor, PressReach))
            {
                continue;
            }

            // <b>Nor a near one (29.09.2026, build 322).</b> On 322 three of Thalia's five were left sixty-one tiles back a
            // minute after gathering at the captain, each "journey moving towards (2447, 734) (station)": a station handed
            // out on the squad's cadence outlived the captain's pace and led them where the company had been. On the march
            // the enlistment's close-up on the leader's body (BotEnlist.Holding, 312) is the formation — build 314's note.
            if (Stance == BotSquadStance.Marching && !ReferenceEquals(member, Leader) && MarchUnstationed)
            {
                continue;
            }

            // <b>Nor, in a fight, a member further from the anchor than a station search can reach (01.10.2026, build 388).</b> A
            // station search is held to BotWalk.StationCeilingMs (15 ms) and a place five hundred tiles off is never found in
            // it: on 387 between 19:43 and 19:53 Falk, Freya and Ingram — levied at (1400, 2200), their leader fighting on at
            // (1540, 1640) — were handed a place every second and a half, 131 partial station searches in half an hour, and
            // the journeys to a place in the line read 92.95 % against 99 % on 386. The enlistment's own walk to the company's
            // fight (BotEnlist.Holding, with the walker's full clock) brings such a member in; it is stationed when it is near.
            if (Stance == BotSquadStance.Fighting && !ReferenceEquals(member, Leader) && member.Self is { } distant
                && !distant.InRange(anchor, StationReach))
            {
                StationFar++;

                continue;
            }

            // <b>A member whose last place could not be walked to rests from places for a while (30.09.2026, build 337).</b>
            // The company re-issues every place each second and a half; a member at the foot of a mountain round whose
            // captain every place lies on rock was handed a fresh one each time and refused each time — 1127 of the
            // walker's failures on 336 were stations, 480 of them at (1040, 1360) west of Britain, Lorandil alone 322
            // (the journeys to a place in the line fell from 100 % on 334 to 84 %). It stands where it is until StationRestMs
            // has passed; a blade that can already strike was never re-stationed anyway.
            if (member.Self is { } resting && _stationRefused.TryGetValue(resting.Serial, out var refusedAt)
                && Core.TickCount - refusedAt < StationRestMs)
            {
                StationRested++;

                continue;
            }

            // <b>A place the last search came back short of is refused as surely as a dropped one (30.09.2026, build 347).</b>
            // The rest above was written into the walker's drop only, and a place on rock is seldom dropped: the plan comes
            // back partial, the member walks to its end, and the company hands out a fresh place a second and a half later —
            // 274 partial searches on 346 in one fight at (1030–1059, 1300–1339) west of Britain by Maud, Lorandil, Oriel and
            // Brokk, the mountain of 336, and the journeys to a place in the line read 83 %. It rests as a refused one does.
            if (member.Self is { } shorted && member.Journey is { Partial: true, Reason: "station" or "sweep" })
            {
                StationRefused(shorted);
                StationShorted++;

                continue;
            }

            var where = Stance == BotSquadStance.Scouting
                ? BotScatter.PatchFor(this, member)
                : BotFormation.StationFor(this, member);

            if (where == Point3D.Zero)
            {
                continue;
            }

            // <b>A station on the other side of a dungeon's edge is not a place this member can walk to, and it
            // was asked for every beat.</b> A company that went down leaves the odd member on the island — one
            // the delve did not carry, one carried back up — and its station in the caves was written into its
            // journey each beat, refused each beat ("has dropped (5304, 1312, 0) because there is no way from
            // here (station)" twelve times running for Hale Ashdown at 08:18 on 22.09.2026, standing by The
            // Ash's hall), until twelve refusals read as a pocket and the rescue carried it twenty-four tiles
            // "home". See BotAppraisal's Undergroundish for the same edge in the auction.
            // Since 29.09.2026 a station across a cave mouth is a walk through the mouth (BotGates): only ground no gate
            // joins is passed over.
            if (member.Self is { } self && !BotGates.Joined(map, self.Location, where))
            {
                Sundered++;

                continue;
            }

            // <b>A fighting station is a tile, and it was being delivered with a tile of slack.</b> The eight
            // places round a creature are worked out one to a blade precisely so that no two of them collide;
            // <c>BotArrival.Beside</c> then says "anywhere within one tile of that will do", and one tile of
            // slack on a ring one tile across means two blades can both be satisfied standing on the same
            // ground. Watched from a client it is unmistakable: the second man stands behind the first,
            // swinging at his back. Marching and sweeping keep the slack, where a tile either way is genuinely
            // nothing and exactness would cost a search per member per beat.
            var precision = Stance == BotSquadStance.Fighting ? BotArrival.Exactly : BotArrival.Beside;

            // Rebase, never Begin. A station is what the bot fundamentally is doing; whatever is happening to
            // it right now sits on top, and it has to survive the formation shifting — which is precisely the
            // moment a fight starts. Beginning here would cancel the fight in order to tell the bot where to
            // stand for it.
            journey.Rebase(map, where, precision, Stance == BotSquadStance.Scouting ? "sweep" : "station");
            sent = true;
        }

        if (!all && !sent)
        {
            return;
        }

        _stationedTick = Core.TickCount;

        // <b>The ring turns when the fight is not working, and tying it to anything else left it never
        // turning at all.</b> It used to turn only on the rounds that chased a stopped member up — and those
        // rounds cannot happen while the creature is moving, because a moving focus drifts two tiles every
        // beat, which re-stations everybody wholesale before anyone is ever judged to have stopped. Three
        // stalemates in a row said so in the same words: <c>nearest 2 tiles off, 0 tries at standing right</c>
        // — five blades a step short of a wraith, each sent back to the identical blocked tile every second
        // and a half for ninety seconds, and the counter honestly reporting that nothing else was ever tried.
        //
        // The right trigger was already being measured for another purpose: health that will not move. If the
        // thing is not being hurt, where everybody is standing is a hypothesis that has been tested and
        // failed, so try the next arrangement. When blows are landing, nothing turns.
        if (Stance == BotSquadStance.Fighting && Core.TickCount - _focusProgressTick >= RotateAfterMs)
        {
            Attempt++;
        }
    }

    /// <summary>
    /// A member that has stopped walking and is not near enough to be part of the fight.
    ///
    /// <para>
    /// Judged against its own rank's distance rather than one number for everybody, or an archer holding its
    /// station at five tiles counts as stranded and is sent to it again every couple of seconds for the whole
    /// fight. Only bots that have stopped are asked: one that is still walking is already dealing with it.
    /// </para>
    /// </summary>
    private bool Stranded(IBotSquadMember member, BotJourney journey)
    {
        // Forming up short of the enemy, a member at its place round the rally is where it was sent (company16): "far from the
        // enemy" is the point, not a stranding.
        if (Stance != BotSquadStance.Fighting || Mustering || Focus is not { Deleted: false, Alive: true })
        {
            return false;
        }

        // <b>Not merely "has stopped": also "is still trying and getting nowhere".</b> The first evening of
        // this rule produced a company that stood two tiles off a wraith for ninety seconds with the counter
        // reading nought tries at standing right — because every member's walk was still live, replanning at
        // a place it could not finish, so not one of them ever counted as having stopped. A journey that has
        // run out of plans that get closer already knows it is beaten; this is the one caller that can do
        // anything about it.
        if (journey.Active && !journey.Hopeless)
        {
            return false;
        }

        var body = member.Self;

        if (body is not { Deleted: false, Alive: true } || body.Map != Focus.Map)
        {
            return false;
        }

        // <b>Near enough and unable to swing is the same problem as too far away, and only one of the two
        // was ever asked about.</b> A member with no line to the focus is standing where the engine will not
        // let it fight from — see BotFormation.Sighted — so it wants another tile exactly as a member who
        // stopped short does. Without this, the one arrangement the formation has already proved useless is
        // the one it holds: the station is satisfied, so nobody re-stations it, for the whole fight.
        //
        // A shooter that has merely moved too recently is <em>not</em> stranded: the cure for that is to
        // stand still, and sending it anywhere is the disease. See BotBlow.Unsteady.
        return Strike(member, out _) is BotBlow.Far or BotBlow.Blind;
    }

    /// <summary>A unit step from one point towards another, on the eight compass lines.</summary>
    private static (int X, int Y) Unit(Point3D from, Point3D to) =>
        (Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y));

    /// <summary>Which way a mobile is looking, as a unit offset.</summary>
    private static (int X, int Y) Facing(Direction direction) =>
        (direction & Direction.Mask) switch
        {
            Direction.North => (0, -1),
            Direction.Right => (1, -1),
            Direction.East => (1, 0),
            Direction.Down => (1, 1),
            Direction.South => (0, 1),
            Direction.Left => (-1, 1),
            Direction.West => (-1, 0),
            _ => (-1, -1)
        };

    public override string ToString() =>
        $"squad {Id}: {Count} under {Leader?.Self?.Name ?? "nobody"}, {Stance}{(Focus != null ? $" vs {Focus.Name}" : "")}";
}
