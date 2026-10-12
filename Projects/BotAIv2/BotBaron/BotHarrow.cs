using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Regions;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// Six bots taken to ground that has killed people, and kept there until it has been emptied or the
/// afternoon is gone.
///
/// <para>
/// <b>This is the shard's third reason for a company, and it is not a bigger patrol.</b> A muster forms
/// against one creature and ends when that creature is dead. A patrol — see <see cref="BotSweep"/> — is sent
/// to a square that reads dangerous <em>lately</em> and finishes when the reading falls, which is a
/// statement about frequency and can be satisfied by the trouble simply wandering off. A harrowing is
/// answerable to neither: it goes where people have actually died, it is finished by a count of corpses or
/// by a clock, and when it ends the square comes off the board altogether. Nothing else on this shard clears
/// anything — everything else knocks a number down and lets it climb back.
/// </para>
///
/// <para>
/// <b>Everything alive inside the box, and the exclusions are the interesting half.</b> Not "hostile", which
/// is what every other fight on the shard asks: hostility is a notoriety judgement and it lets a field of
/// harmless things stand between a company and the thing it came for. Bots are excluded structurally rather
/// than by a rule — they are players as far as the engine is concerned and this only ever looks at
/// creatures — and guarded ground is excluded outright, so a chase that leads into a town ends at the gate.
/// </para>
///
/// <para>
/// <b>It is worth what it hands to the five who came.</b> The Baron takes no share, so measured the ordinary
/// way — coin in his own pack — this work pays nothing and the ledger would have learned, correctly by its
/// own arithmetic, that leading companies into deadly ground is worthless. See <see cref="BotSquad.Won"/>:
/// the company keeps the figure, and this reads it.
/// </para>
/// </summary>
public sealed class BotHarrow : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHarrow));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "harrow";

    /// <summary>
    /// How many tiles across the ground the company walks.
    ///
    /// <para>
    /// Seventy-five, by order, and it is three times a peril square on purpose. The square is where the
    /// deaths were recorded and it is only twenty-four tiles across — the resolution at which "a place"
    /// means something to a map, not the resolution at which a wood empties. What actually killed people
    /// there lives in the wood around it, so the errand is the neighbourhood and the square is only its
    /// centre.
    /// </para>
    /// </summary>
    public static int Side { get; set; } = 75;

    /// <summary>How many bodies march, the Baron included. The floor: a square that has eaten a company asks
    /// for more. See <see cref="BotQuad.Levy"/>.</summary>
    public static int Company { get; set; } = 6;

    /// <summary>
    /// How many grandmasters a damned square asks for before anybody marches on it.
    ///
    /// <para>
    /// Fifteen, by Patrick's order on 02.09.2026: ground that has swallowed thirty may be gathered against
    /// "only from fifteen grandmasters and from four thousand of strength". It is a different kind of rule
    /// from the levy above — that one is about how many, this one is about who — and a square earns it by
    /// killing a company whole rather than by any amount of ordinary bad luck.
    /// </para>
    /// </summary>
    public static int Grandmasters { get; set; } = 15;

    /// <summary>
    /// The fighting power each of them must have, not the company's between them.
    ///
    /// <para>
    /// Four thousand a head, by Patrick's correction on 02.09.2026: "four thousand per person, that is,
    /// properly trained and equipped bots, and the mages must have their reagents." Power on this shard is
    /// health times what a body hits for — see <c>BotThreat.Power</c> — so four thousand is not a figure a
    /// bot reaches by being handed a sword: it is a bot that has grown and has been outfitted. Which is the
    /// point. Damned ground is ground the population is not yet good enough for, and it should say so by
    /// standing empty rather than by swallowing another company.
    /// </para>
    /// </summary>
    public static double Might { get; set; } = 4000.0;

    /// <summary>
    /// Reagents a caster must be carrying to count towards the company.
    ///
    /// <para>
    /// By the same correction. A mage with an empty pack casts nothing from its book — <c>BotStrike.Ready</c>
    /// refuses the spell — so on damned ground it is a body with a staff, and counting it is how a company
    /// of fifteen arrives as a company of nine. Anybody carrying a spellbook is asked; everybody else is not.
    /// </para>
    /// </summary>
    public static int Reagents { get; set; } = 10;

    /// <summary>The skill at which a bot counts as a grandmaster. The era's own ceiling.</summary>
    public static double GrandmasterAt { get; set; } = 100.0;

    /// <summary>
    /// Fewest worth setting out with once the muster has run its course.
    ///
    /// <para>
    /// <b>Three, and the number moved because the rule around it did.</b> It was the full six, and six was
    /// read as a gate: no six standing near, no harrowing, refused on the spot. That produced a Baron who
    /// never left town while the graveyard filled up — the volunteers a shard has at any one instant are
    /// whoever happens to be idle in that second, and asking for five of them at once is asking for a
    /// coincidence. The order is now a muster: he stands in the square and calls for <see cref="MusterMs"/>,
    /// and what this number does is decide whether what turned up is a company or an escort.
    /// </para>
    ///
    /// <para>
    /// Still separate from <see cref="Company"/>, which is what the squad will hold. One is the target, the
    /// other is the floor, and they are different statements even when they happen to agree.
    /// </para>
    /// </summary>
    /// <summary>
    /// <b>No longer the floor under a march, by Patrick's protocol of 05.09.2026.</b> A muster that has
    /// not raised its full company when the clock runs out is dropped and rested, not set out with. Kept
    /// because a floor is the kind of number this file may want again, and deleting it would take the
    /// argument above with it.
    /// </summary>
    public static int Least { get; set; } = 3;

    /// <summary>
    /// How long he stands in the square calling for volunteers before setting out with whoever came.
    ///
    /// <para>
    /// Five minutes, by order. It ends early the moment <see cref="Company"/> have gathered — a muster that
    /// waited out its clock with a full company would be five bots standing about for no reason.
    /// </para>
    /// </summary>
    public static int MusterMs { get; set; } = 300000;

    /// <summary>
    /// How long a Baron leaves the whole idea alone after a muster that came to nothing. Five minutes.
    ///
    /// <para>
    /// <b>Patrick's order of 05.09.2026, and what it is against is a Baron who never stops asking.</b> A
    /// muster levies bots — nobody volunteers, <see cref="Levy"/> takes them — so a call that fails and is
    /// posted again on the next beat is a standing tax on the population's working time. Three musters in
    /// four minutes called up 1837 bots and marched none of them.
    /// </para>
    ///
    /// <para>
    /// Kept against the Baron rather than against the square, because the order was "he leaves the idea and
    /// comes back to it in ten minutes". A rest per square would let him work through a list of dire ground
    /// one square at a time and never rest at all, which is the same tax under a longer name.
    /// </para>
    /// </summary>
    public static int RestMs { get; set; } = 600000;

    /// <summary>
    /// How long past the bell a muster is held while its company is fighting, and how long after the last blow the
    /// company is given to walk back into the square.
    ///
    /// <para>
    /// <b>A company fighting together has been raised; the bell was counting it as absent.</b> A member set upon on
    /// the way in puts the whole company on the creature (<c>BotSquads.Note</c>), and every station follows the fight
    /// out of the square. Gerda's muster at Britain's gate on 24.09.2026 (build 211) read 4 of 6 at 12:34:08, when
    /// Squad 1 took on a wraith off the graveyard, and 1 of 6 seven seconds later; the wraith was down at 12:34:21 and
    /// the tally back at 4 by 12:34:28. The same company took on a spectre four seconds before the bell and was failed
    /// at 3 of 6 while it fought.
    /// </para>
    ///
    /// <para>
    /// Bounded, so that a muster beside a graveyard cannot be held open for ever by a stream of the dead: two minutes
    /// past the bell at most, and half a minute after the fight to walk back in.
    /// </para>
    /// </summary>
    public static int HoldMs { get; set; } = 120000;

    /// <summary>How long after the company's last fight the bell still waits. See <see cref="HoldMs"/>.</summary>
    public static int RegroupMs { get; set; } = 30000;

    /// <summary>Bells held because the company was fighting, or had only just stopped. For the summary.</summary>
    public static long Held { get; private set; }

    /// <summary>Bots passed over by the levy for already answering to a company or a class. Counts looks.</summary>
    public static long Engaged { get; private set; }

    /// <summary>
    /// The square each harrow now standing is on, by quadrant key, with the errand holding it and when it last beat.
    ///
    /// <para>
    /// <b>One harrow to a square.</b> At the boot of 18:06 on 24.09.2026 five of The Blade — Aric, Emrys, Garrow, Selwyn,
    /// Cassia — took the harrow of the guild's own square at (1275, 1395) in the same second, and two of The Ash that of
    /// (1515, 1305): nine companies stood holding 54 bots, 24 of them tired and kept from their rest, and the island
    /// finished 92.4% of its work. Nothing said a square was already being harrowed.
    /// </para>
    /// </summary>
    private static readonly Dictionary<(int Map, int X, int Y), (BotHarrow Deed, long Beat)> _taken = [];

    /// <summary>How long a harrow's hold on its square lasts without a beat, so a holder gone to rest or deleted lets it go.</summary>
    public static int TakenMs { get; set; } = 60000;

    /// <summary>Harrows let go on their first beat because another already stood on the square, and offers not made for it.</summary>
    public static long Doubled { get; private set; }

    /// <summary>Whether a harrow other than <paramref name="except"/> stands on this square now.</summary>
    public static bool Taken(Map map, Point3D square, BotHarrow except = null) =>
        map != null && _taken.TryGetValue(BotQuad.Key(map, square), out var held) && !ReferenceEquals(held.Deed, except)
        && Core.TickCount - held.Beat < TakenMs;

    /// <summary>An offer not made because the square is already being harrowed. See <see cref="Taken"/>.</summary>
    public static void Declined() => Doubled++;

    /// <summary>How many clearings by fighters stand on the island now: harrows holding their square that are clearings. See <c>BotClearer.MostAtOnce</c>.</summary>
    public static int ClearingsStanding()
    {
        var now = Core.TickCount;
        var standing = 0;

        foreach (var held in _taken.Values)
        {
            if (held.Deed is { Clearing: true } && now - held.Beat < TakenMs)
            {
                standing++;
            }
        }

        return standing;
    }

    /// <summary>Musters given up on, after which the Baron stands off the whole idea for <see cref="RestMs"/>.</summary>
    public static long Rested { get; private set; }

    private static readonly Dictionary<Serial, long> _resting = new();

    /// <summary>Whether this Baron is standing off the idea of a harrowing altogether.</summary>
    public static bool Resting(Mobile body)
    {
        if (body == null || !_resting.TryGetValue(body.Serial, out var until))
        {
            return false;
        }

        // By subtraction against a stamp that was itself a real tick, never against a nought default.
        if (Core.TickCount - until < 0)
        {
            return true;
        }

        _resting.Remove(body.Serial);

        return false;
    }

    /// <summary>Stands this Baron off the idea for <see cref="RestMs"/>.</summary>
    public static void Rest(Mobile body)
    {
        if (body == null)
        {
            return;
        }

        _resting[body.Serial] = Core.TickCount + RestMs;
        Rested++;
    }

    /// <summary>
    /// Where the company forms up, or nought for the population's own home.
    ///
    /// <para>
    /// <b>It was the bank counter, and a bank counter is not a square.</b> <c>BotGround.Counter</c> is the
    /// only thing on this shard that knows where a town is at all, so it was the obvious anchor — and what it
    /// actually answers is "where is the nearest place to put money", which put a Baron in full plate
    /// standing in the queue at the bank while he called for a company. The two questions look alike and are
    /// not: one is about coin and one is about assembly.
    /// </para>
    ///
    /// <para>
    /// The population's home is the honest default. It is where every bot on the island was raised, it is
    /// what <c>bot-population.json</c> already calls the middle of things, and it needs no survey to be
    /// known. Configuration may name somewhere else — the drill field was moved the same way.
    /// </para>
    /// </summary>
    public static Point3D Square { get; set; }

    /// <summary>
    /// How near the muster point he has to be to count as standing in the square.
    ///
    /// Not called <c>Post</c>, which is the name of the method that picks corners of the box a few dozen
    /// lines down. Two things in one class that are both "a place to stand" and mean different places is the
    /// kind of name collision the compiler catches today and a reader trips over for ever.
    /// </summary>
    public static int Station { get; set; } = 2;

    /// <summary>
    /// How close a volunteer has to be standing to the muster point to count as having turned up.
    ///
    /// <para>
    /// By Patrick's order on 02.09.2026: the Baron gathers the party in the square, waits until everybody has
    /// come, and only then sets out for the quadrant. Until now the call ended when enough had <em>joined</em>
    /// — and joining is instant while walking across Britain is not, so the company set off as a list of
    /// names and reached the ground in single file, which is how six bots get killed one at a time by
    /// something six of them together could beat.
    /// </para>
    ///
    /// <para>
    /// <b>The five-minute call is no longer a backstop that marches short.</b> Patrick's protocol of
    /// 05.09.2026: the company is the company, and a call that cannot raise it is dropped and rested. See
    /// RestMs.
    /// </para>
    ///
    /// <para>
    /// <b>Widened from eight to twenty-four on Patrick's order of 05.09.2026, and eight was not a zone, it
    /// was a doorstep.</b> A company here can be twenty-one bodies — BotQuad.Levy raises the ask every time
    /// a company is lost — and twenty-one bots do not fit in a circle eight tiles across without standing
    /// on each other; the squad's own formation then places them where its stations are, which is further
    /// out than that. So the count of who had turned up kept losing people who were standing perfectly well
    /// at the muster: the recorded tally read "11 of 21 standing in it, 21 in the company" at 02:17, and
    /// the eight tiles were the whole of the difference. Twenty-four is three squad stations deep and still
    /// plainly one gathering rather than a district.
    /// </para>
    /// </summary>
    public static int Assembly { get; set; } = 24;

    /// <summary>Corpses that finish the errand.</summary>
    public static int Quota { get; set; } = 20;

    /// <summary>
    /// The longest one harrowing may last, when the quota is never reached.
    ///
    /// Forty minutes, by order — a third again as long as a patrol's half hour, which is right for an errand
    /// that has a count to fill rather than a reading to wait out.
    /// </summary>
    public static int CapMs { get; set; } = 1800000;

    /// <summary>
    /// How far around the muster point he calls people up.
    ///
    /// <para>
    /// <b>Forty tiles was a shout, and what is wanted is a summons.</b> At forty he was calling to whoever
    /// happened to be crossing the same street, and on 27.08.2026 he stood in the square for the full five
    /// minutes and nobody came at all. The population is spread over five hundred tiles by design; a call
    /// that only reaches the next block is a call to an empty street.
    /// </para>
    /// </summary>
    public static int Reach { get; set; } = 200;

    /// <summary>
    /// How often the island is swept for bodies while the call is open.
    ///
    /// Two seconds. The sweep is a spatial query over two hundred tiles and the errand is asked ten times a
    /// second; done every beat it would be two hundred sweeps for every one that could possibly find anybody
    /// new, which is the shape of cost this project has paid for twice in the movement budget alone.
    /// </summary>
    public static int SweepMs { get; set; } = 2000;

    /// <summary>
    /// The company he wants, by role: two who stand in the line, two who shoot, one who mends.
    ///
    /// <para>
    /// <b>A want, not a requirement, and the difference is what keeps this from being another gate that
    /// never opens.</b> The quota is filled first and by distance, so a Baron with three warriors and no
    /// healer within reach still marches with three warriors — the alternative is the rule that stopped him
    /// leaving town in the first place. What it buys is that the nearest five are not five of the same thing.
    /// </para>
    /// </summary>
    public static int Melee { get; set; } = 2;

    public static int Ranged { get; set; } = 2;

    public static int Medics { get; set; } = 1;

    /// <summary>
    /// How far the company looks for something to kill, from wherever the Baron is standing.
    ///
    /// Forty, by Patrick's order of 16.09.2026 that the Baron attacks any hostile thing: the box is seventy-five
    /// across and its corners are twenty-five out, so twenty tiles from a corner did not reach the middle of the
    /// ground he was sent to clear, and he walked past what lived there.
    /// </summary>
    public static int Sight { get; set; } = 40;

    /// <summary>
    /// How long the company spends walking to one corner of the box before trying the next.
    ///
    /// Longer than a patrol's, because the box is three times as wide and a corner is twenty-five tiles out
    /// rather than eight.
    /// </summary>
    public static int RoundMs { get; set; } = 90000;

    /// <summary>How many corners may turn out to be unwalkable before the ground is given up.</summary>
    public static int MaxBends { get; set; } = 4;

    /// <summary>
    /// What a harrowing is reckoned at per minute before experience corrects it.
    ///
    /// <para>
    /// A company of six working ground that has been killing people, and the figure it is measured against
    /// is what the five of them actually carried away. High, and it is meant to be: this is six bots' worth
    /// of hunting concentrated on the one place that has proved it holds something. Nothing else the Baron
    /// may take comes near it, which is what makes him walk the town only when there is nowhere to go.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 150.0;

    /// <summary>How long a harrowing is expected to take, walk and all.</summary>
    public static double WorkMinutes { get; set; } = 25.0;

    /// <summary>
    /// What a guild's own company for its own ground is reckoned at: a duty, not a trade (<see cref="Unpaid"/>).
    ///
    /// <para>
    /// <b>Patrick's answer of 26.09.2026 about halls raised beside a spawn: "they put it there, so let them defend their own
    /// ground — that is their duty", and "yes, they gather themselves and clear it themselves".</b> Reckoned by the Baron's
    /// prior, the guild's harrow was corrected by what harrows had paid to 38–41/min and lost to a restock at the spare price
    /// (179/min), to peddling (54) and to a hunt (47): The Lantern was offered the harrowing of its squares beside its hall
    /// some 150 times between 13:17 and 13:50 and took it three times. Above supplies and ordinary work, below a war's call
    /// (<c>BotRally.Prior</c>, 400) and the defence of the yard (700).
    /// </para>
    /// </summary>
    public static double DutyPrior { get; set; } = 300.0;

    /// <summary>Bots the levy passed over for want of their supplies. See <see cref="BotProvision"/>.</summary>
    public static long Unsupplied { get; private set; }

    /// <summary>Companies that formed and set out.</summary>
    public static long Marches { get; private set; }

    /// <summary>Musters that ran their full course and still could not raise <see cref="Least"/> bodies.</summary>
    public static long Undermanned { get; private set; }

    /// <summary>Musters called. Not marches: see <see cref="Undermanned"/> for the ones that came to nothing.</summary>
    public static long Musters { get; private set; }

    /// <summary>Squares finished by filling the quota, and squares finished by the clock. Never one number.</summary>
    public static long Emptied { get; private set; }

    public static long Timedout { get; private set; }

    public static long Killed { get; private set; }

    private readonly Map _map;

    private readonly Point3D _square;

    private readonly int _dead;

    private BotSquad _squad;

    private long _won;

    private long _began;

    private int _called;

    private int _kills;

    private bool _standing;

    /// <summary>What the company was last seen fighting, so that a new focus can be reported to the will as progress.</summary>
    private Mobile _fighting;

    private long _steppedTick;

    private int _round;

    private Point3D _post;

    private int _bends;

    private Mobile _quarry;

    /// <summary>Whether the company has been raised and is on its way. Never "is the squad null".</summary>
    private bool _marching;

    /// <summary>How many this square asks for. See <see cref="BotQuad.Levy"/> — nought until the muster sets it.</summary>
    private int _wanted;

    /// <summary>The levy, or the ordinary company before the muster has asked the map.</summary>
    private int Wanted => _wanted > 0 ? _wanted : Company;

    /// <summary>How many marched, so that a company lost whole can be reported as the size it was.</summary>
    private int _marched;

    /// <summary>How many of the called are standing in the square this moment. See <see cref="Assembly"/>.</summary>
    private int _here;

    /// <summary>
    /// Whether the call has been opened. A flag rather than "is the squad null", because the squad may be
    /// replaced underneath this errand and the call has still been running the whole time — which is the
    /// distinction the first version of this got wrong and paid for in every march it made.
    /// </summary>
    private bool _mustering;

    private long _musteredTick;

    /// <summary>Whether he has reached the muster point. The clock does not start until he has.</summary>
    private bool _atMuster;

    /// <summary>When the company was last seen fighting during the muster, if ever. See <see cref="HoldMs"/>.</summary>
    private long _foughtTick;

    private bool _fought;

    /// <summary>Whether this muster's bell has been held once already, so that <see cref="Held"/> counts musters.</summary>
    private bool _held;

    /// <summary>Whether the harrow came to one of its own ends (<see cref="Finish"/>) rather than being put down under it.</summary>
    private bool _ended;

    /// <summary>The last muster tally written down, so that only changes are said. See Advance.</summary>
    private int _said0 = -1;

    private int _said1 = -1;

    private Point3D _muster;

    /// <summary>When the island was last swept for bodies to call up. See <see cref="SweepMs"/>.</summary>
    private long _sweptTick;

    /// <summary>The guild whose own company this is, or null for the Baron's. See <see cref="BotReeve"/>.</summary>
    private readonly Guild _guild;

    /// <summary>The guild this company is raised by, or null for the Baron's.</summary>
    public Guild Guild => _guild;

    public BotHarrow(Map map, Point3D square, int dead, Guild guild = null)
    {
        _map = map;
        _square = square;
        _dead = dead;
        _guild = guild;
        _began = Core.TickCount;
    }

    /// <summary>
    /// <b>A clearing: any fighter's company for a square where bots lately died (02.10.2026, Patrick: "I don't see bots
    /// gathering in companies and going to the squares where others died, to clear them and protect other bots on the
    /// road").</b> The same harrowing — the muster, the levy, the march, the box walked until it is empty or the clock runs
    /// out, the ground's reputation set to nothing at the end — led by whoever <see cref="BotClearer"/> offered it to, which
    /// is any fighter and not only the Baron or a guild's head. What differs: the company is sized to the strength the
    /// square asks (<paramref name="asked"/>: the record of what has killed there, or what stands there now, whichever is
    /// more) at <see cref="ClearFit"/>, and marches as soon as that much is standing at the muster rather than waiting for a
    /// head count; the muster is where the leader stands when it is outside a town; and when the ground is cleared the
    /// closures on it — the quarantines and the boxes closed to bots that died there — are lifted with the reading.
    /// </summary>
    public BotHarrow(Map map, Point3D square, int dead, double asked, bool clearing, int half = 0) : this(map, square, dead)
    {
        _clearing = clearing;
        _asked = Math.Max(0.0, asked);
        _half = half;
    }

    /// <summary>
    /// How far each way of the square the company hunts, for a clearing of a quarantine: the quarantine's own half-width, so the
    /// whole box it closed is walked and can be found empty (<see cref="BotBarred.Lift"/>, 03.10.2026). Nought: <see cref="Side"/>'s.
    /// </summary>
    private readonly int _half;

    /// <summary>How far each way of the square the company hunts and the ground is reckoned. See <see cref="_half"/>.</summary>
    private int Edge => _half > 0 ? _half : Side / 2;

    /// <summary>Whether this is a clearing rather than the Baron's or a guild's harrow. See the third constructor.</summary>
    private readonly bool _clearing;

    /// <summary>
    /// The strength the square asks of the company, for a clearing. See <see cref="ClearFit"/>. Asked again at the bell, and raised
    /// if the ground asks more by then (review of clear12, F4).
    /// </summary>
    private double _asked;

    /// <summary>Whether this muster's ask was raised at the bell, counted once. See <see cref="ClearReasked"/>.</summary>
    private bool _reasked;

    public bool Clearing => _clearing;

    /// <summary>
    /// How many times the strength a square asks a clearing's company brings before it marches. One and a quarter: the
    /// fight's own margin is <c>BotThreat.Tolerance</c> the other way (a company fights what is under one and a half times
    /// itself), so a company at this much over what the square asks fights it with room, and a company at less waits for
    /// more or does not go.
    /// </summary>
    public static double ClearFit { get; set; } = 1.25;

    /// <summary>What a clearing is reckoned at per minute. The Baron's harrow's figure: it is the same work.</summary>
    public static double ClearPrior { get; set; } = 150.0;

    /// <summary>Clearings marched, emptied, run out of time, and short of the strength asked at the bell.</summary>
    public static long ClearMarches { get; private set; }

    public static long ClearEmptied { get; private set; }

    public static long ClearTimedout { get; private set; }

    public static long ClearUndermanned { get; private set; }

    public static long ClearKilled { get; private set; }

    /// <summary>Closures lifted with a clearing: quarantines on the ground, and boxes closed to single bots that died on it.</summary>
    public static long QuarantinesLifted { get; private set; }

    /// <summary>Clearings that marched into a standing quarantine (03.10.2026).</summary>
    public static long ClearClosed { get; private set; }

    /// <summary>Clearings' musters that called past the leader's first reckoning of heads, for the strength asked. See <see cref="Calling"/>.</summary>
    public static long ClearResized { get; private set; }

    /// <summary>Clearings that ended on their ground with nothing alive in it past <see cref="BotBarred.EmptyBelow"/> at its kind's price.</summary>
    public static long ClearWon { get; private set; }

    /// <summary>Quarantines lifted by clearings, of <see cref="QuarantinesLifted"/>.</summary>
    public static long ClearLifted { get; private set; }

    /// <summary>Clearings whose company withdrew from something it could not beat (<c>BotSquad.Overmatched</c>): failed.</summary>
    public static long ClearWithdrew { get; private set; }

    /// <summary>Whether this muster has been counted in <see cref="ClearResized"/>.</summary>
    private bool _resized;

    /// <summary>Clearings whose square asked more at the bell than when it was offered (review of clear12, F4).</summary>
    public static long ClearReasked { get; private set; }

    /// <summary>Clearings given up at the bell because the plague or a killer of 30000 had closed their square meanwhile.</summary>
    public static long ClearDoomed { get; private set; }

    /// <summary>Clearings' levies that stopped calling with the strength asked already in the company (review of clear12, F3).</summary>
    public static long ClearEnough { get; private set; }

    /// <summary>What the members able to fight bring, wherever they stand: the company a clearing is sized by.</summary>
    private static double CompanyStrength(BotSquad squad, out int heads)
    {
        var strength = 0.0;
        heads = 0;

        var members = squad?.Members;

        for (var i = 0; i < members?.Count; i++)
        {
            if (members[i] is IBotAlly { AbleToFight: true } && members[i].Self is { Deleted: false, Alive: true } self)
            {
                strength += BotThreat.Power(self);
                heads++;
            }
        }

        return strength;
    }

    /// <summary>The company's strength as the levy fills it, for a clearing. See <see cref="Take"/>.</summary>
    private double _levied;

    /// <summary>Whether this muster's levy has been counted in <see cref="ClearEnough"/>.</summary>
    private bool _enough;

    public static long BoxesLifted { get; private set; }

    public static string Describe() =>
        $"{Musters} musters called and {Called} bots called up ({Pressed} of them pressed to fall in), {Marches} of them marched, {Undermanned} could not raise the company asked for in {MusterMs / 60000} minutes of calling ({Rested} times the idea was then left alone for {RestMs / 60000} minutes), {Held} bells held while the company fought, {Engaged} looks at a bot already answering to a company or a class passed over, {Unsupplied} without their supplies, {Across} round the far side of something by road from the muster, {Doubled} harrows let go or not offered for a square another already stood on, {Emptied} grounds emptied and {Timedout} run out of time, {Killed} things killed on them, {FellBack} times the leader put the harrow down and the company fell back with him, {TookUp} of them taken up again; "
        + $"of these, clearings by any fighter: {ClearMarches} marched ({ClearClosed} into a standing quarantine, {ClearResized} musters called past the leader's first reckoning for the strength asked), {ClearEmptied} emptied their ground, {ClearTimedout} ran out of time on it, {ClearWon} left it with nothing past {BotBarred.EmptyBelow:F0}, {ClearUndermanned} came up short of ×{ClearFit:F2} the strength asked, {ClearWithdrew} withdrew from what they could not beat, {ClearReasked} asked more at the bell than at the offer, {ClearDoomed} given up at the bell for the plague, {ClearEnough} levies stopped with the strength asked in the company, {ClearKilled} things killed; lifted with the ground cleared: {QuarantinesLifted} quarantines ({ClearLifted} by clearings) and {BoxesLifted} boxes closed to bots that had died there";

    public override string Kind => Trade;

    /// <summary>
    /// Standing at the muster is standing on purpose. Until 16.09.2026 a call that waited its five minutes for a sixth
    /// body was abandoned by BotStall at four ("it had stopped getting anywhere"): the Baron stood where he was told to
    /// stand, the stage line did not change because nobody new arrived, and the watcher read a man doing his job as a
    /// man stuck. Once the company marches it walks, and the watcher's rule applies as before.
    /// </summary>
    public override bool Still => !_marching;

    /// <summary>Goes to the deadliest ground on purpose: that is what a harrowing is. See <see cref="BotDeed.Braves"/>.</summary>
    public override bool Braves => true;

    /// <summary>
    /// <b>The leader's flight put the whole company down, and it should only have put the leader down.</b> A harrow
    /// is not steadfast by default, so when the Baron fled at 16:58 on 16.09.2026 the will dropped the harrow to
    /// take the flight, <see cref="Drop"/> let the company go, six bots that had been winning were sent home, and
    /// he died alone with "nowhere to run to". Steadfast, the harrow is put down for the flight or the bandage and
    /// taken up again when he has half his health back (BotWill.ResumeHealth) inside ten minutes (AsideCapMs);
    /// meanwhile <see cref="Paused"/> turns the company to marching, which stations it round him wherever he runs,
    /// and anything that follows him into it is engaged by BotSquads.Note as it always was. Patrick's decision of
    /// 16.09.2026, evening.
    /// </summary>
    public override bool Steadfast => true;

    public override Map Map => _map;

    public override Point3D Where => _square;

    public override double Expects => _guild != null ? DutyPrior : _clearing ? ClearPrior : Prior;

    /// <summary>
    /// The company this harrow will raise, or has: the levy's size at this bot's strength a head until the company is
    /// standing, then the company. See <see cref="Expected"/>.
    ///
    /// <para>
    /// <b>A clearing brings the company it will not march without (03.10.2026).</b> It said the leader's strength times the square's
    /// levy of six — 7662 for a bot of the median 1277 — while it marches only with <see cref="ClearFit"/> times what the square
    /// asks (16875 for a square at the bottom of the record), so the auction's road veto (<c>BotDanger.Road</c>: a square on the
    /// way whose record asks past ×1.5 what the work brings) and its ground veto (<c>BotPeril.Overwhelms</c>) weighed a company
    /// of six against ground the company would be sized for.
    /// </para>
    /// </summary>
    public override double Brings(Mobile body) =>
        _clearing
            ? Math.Max(BotQuad.Strength(body), _asked * ClearFit)
            : Math.Max(BotQuad.Strength(body), Expected(_map, _square, BotThreat.Power(body), _guild));

    /// <summary>
    /// The strength a harrow of this square would march with, at <paramref name="head"/> a body: what the square levies
    /// (<see cref="BotQuad.Levy"/>), and for a guild's own company no more than the guild can field
    /// (<see cref="BotReeve.Fighters"/>). Asked by the proposers with the numbers the appraisal will use
    /// (<see cref="BotPeril.Overwhelms"/>), so an offer they make is not one the auction then refuses at every review.
    /// </summary>
    public static double Expected(Map map, Point3D square, double head, Guild guild = null)
    {
        var bodies = map == null ? Company : BotQuad.Levy(map, square, Company);

        if (guild != null)
        {
            bodies = Math.Max(Least, Math.Min(bodies, BotReeve.Fighters(guild)));
        }

        return head * bodies;
    }

    /// <summary>
    /// A guild's own ground is its duty and is not judged on what it pays; the Baron's harrow is a trade as it always was.
    /// See <see cref="DutyPrior"/>.
    /// </summary>
    public override bool Unpaid => _guild != null || _clearing;

    public override double Minutes => WorkMinutes;

    /// <summary>
    /// Nothing. The fights inside a harrowing are the shard's ordinary combat and are already credited where
    /// they happen; naming a skill here would be claiming the same gain twice.
    /// </summary>
    public override SkillName? Trains => null;

    public override int Outlay => 0;

    /// <summary>
    /// What the five of them have carried away since the company formed.
    ///
    /// See the note at the top: the Baron's own pack is empty at the end of every harrowing by design, and a
    /// measure that read his pack would teach the ledger that this work is worthless.
    /// </summary>
    public override int Made => _squad == null ? 0 : (int)Math.Max(0, _squad.Won - _won);

    public override bool Alongside => true;

    public override string Stage =>
        !_marching
            ? $"calling for volunteers to {(_clearing ? "clear" : "harrow")} ({_square.X}, {_square.Y}), where {_dead} have died: {_here} of {Wanted} gathered, {_called} called"
            : !_standing
                ? $"marching {_called} of us on ({_square.X}, {_square.Y}), where {_dead} have died"
                : $"{(_clearing ? "clearing" : "harrowing")} ({_square.X}, {_square.Y}) with {_called} of us, {_kills} of {Quota} down";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        if (bot is not IBotSquadMember member)
        {
            return BotDoing.Failed("not the sort of thing that leads companies");
        }

        // One harrow to a square: the second to its first beat lets it go, which is not a failure. See _taken.
        if (Taken(_map, _square, this))
        {
            Doubled++;

            return BotDoing.Done($"another harrow already stands on ({_square.X}, {_square.Y})");
        }

        _taken[BotQuad.Key(_map, _square)] = (this, Core.TickCount);

        return _marching ? Harrowing(member, body) : Calling(member, body);
    }

    /// <summary>
    /// Standing in the square and calling, for five minutes, and then going with whoever came.
    ///
    /// <para>
    /// <b>A muster, not a headcount, and the difference is the whole of why this class does anything at
    /// all.</b> The first version of this asked how many free bots were standing within forty tiles at the
    /// instant the offer was weighed, and refused outright if that was fewer than six. Read as arithmetic
    /// that is correct; read as behaviour it is a Baron who never leaves town, because "five idle bots in one
    /// place in one second" is a coincidence and not a state a working population is often in. Nobody was
    /// asked. Nobody was given the chance to finish what they were doing and come.
    /// </para>
    ///
    /// <para>
    /// So he goes to the square, and he calls, and the population walks past him — to the bank, to the
    /// shops, home from a hunt — and whoever is free when they pass falls in. Five minutes of that is worth
    /// more than any instant's census. It ends early on a full company, because five bots standing about
    /// waiting for a clock is exactly the waste this was meant to avoid.
    /// </para>
    /// </summary>
    private BotDoing Calling(IBotSquadMember member, Mobile body)
    {
        var squad = member.Squad ?? BotSquads.Form(member);

        if (squad == null)
        {
            return BotDoing.Failed("could not call a company together");
        }

        var now = Core.TickCount;

        // <b>Re-adopted every beat rather than remembered once.</b> A squad is an object, and the one this
        // errand started with can be thrown away underneath it — that is what happened for the whole of
        // 27.08.2026, and the symptom was a march that ended in the same second it began. Holding the
        // reference from the first beat is the bug even now that the cause is fixed: an errand that can only
        // work while nothing else touches the world is an errand waiting for the next thing that does.
        if (!ReferenceEquals(squad, _squad))
        {
            // A different company means a different tally of what has been divided, so the mark it is
            // measured from moves with it. Left behind, Made would read a new squad's takings against an old
            // squad's baseline.
            _squad = squad;
            _won = squad.Won;
        }

        // What this square asks for, which is the ordinary company until one has been lost here. The ladder
        // is the square's own and it is kept by the map, so it survives a night and a restart.
        _wanted = BotQuad.Levy(_map, _square, Company);

        // A guild's own company is at most the guild: a square that has eaten companies asks more than one guild has, and a
        // levy nobody can fill is a muster that fails every five minutes (BotReeve.Fighters).
        if (_guild != null)
        {
            _wanted = Math.Max(Least, Math.Min(_wanted, BotReeve.Fighters(_guild)));
        }

        // A clearing is sized by strength: as many bodies of the leader's strength as the square asks at ClearFit, never
        // fewer than Least nor more than a company holds. The levy fills to it; the march goes when the strength stands.
        if (_clearing)
        {
            // The square's ladder (BotQuad.Levied) grows by Reinforcement at every company lost on it and is not capped, so
            // on the worst squares it stands above a company; a lower bound above the upper one throws (Math.Clamp), and
            // the clearing would be given up at its first beat, every time, exactly where it is wanted most (review 389).
            var least = Math.Min(Math.Max(Least, _wanted), BotSquad.MaxSize);

            // <b>Re-reckoned every beat from the company as it stands (03.10.2026).</b> The heads were reckoned once, at the leader's
            // strength, and the levy stopped at them while the march waited on strength: on build 396 every clearing muster that was
            // called came up short at the bell with every member standing in it — 11 of 11 with 16221 of the 16875 asked, 6 of 6
            // with 8970 (a mage leader of 2800 or more had reckoned six heads), 9 of 9 with 11509 — and kept 23 bots off their work
            // for five minutes each for nothing. Now the company is what it brings so far, and as many more heads as the shortfall is at its own
            // average strength, up to a full company.
            var need = _asked * ClearFit;
            var company = CompanyStrength(squad, out var heads);
            var head = heads > 0 ? company / heads : Math.Max(1.0, BotThreat.Power(body));
            var more = company >= need ? 0 : (int)Math.Ceiling((need - company) / Math.Max(1.0, head));
            var first = (int)Math.Ceiling(Math.Min(need / Math.Max(1.0, BotThreat.Power(body)), BotSquad.MaxSize));

            _wanted = Math.Clamp(squad.Count + more, least, BotSquad.MaxSize);

            if (!_resized && _wanted > Math.Max(first, least))
            {
                _resized = true;
                ClearResized++;
            }
        }

        squad.Ceiling = _wanted;
        squad.Charged = true;

        if (!_mustering)
        {
            _mustering = true;

            _muster = Rally(body);

            Musters++;

            logger.Information(
                "{Name} is calling for volunteers at ({MX}, {MY}) to harrow ({X}, {Y}), where {Dead} have died",
                body.Name,
                _muster.X,
                _muster.Y,
                _square.X,
                _square.Y,
                _dead
            );
        }

        if (now - _sweptTick >= SweepMs)
        {
            _sweptTick = now;

            Levy(squad, body);
        }

        _called = squad.Count;
        _here = Gathered(squad);

        // <b>The five minutes begin when he is standing in the square, not when he thought of it.</b>
        // Patrick's protocol of 05.09.2026: declare, walk, and only then start counting. A clock started at
        // the declaration is spent on the road — the muster point can be most of a town away — so a call
        // that looked like five minutes of asking was however much of it was left when he arrived.
        if (!body.InRange(_muster, Station))
        {
            return BotDoing.Walk(_map, _muster, BotArrival.Within(Station), $"to the muster at ({_muster.X}, {_muster.Y})");
        }

        if (!_atMuster)
        {
            _atMuster = true;
            _musteredTick = now;

            logger.Information(
                "{Name} has reached the muster at ({MX}, {MY}) and is calling for {Wanted} to harrow ({X}, {Y}), where {Dead} have died",
                body.Name,
                _muster.X,
                _muster.Y,
                _wanted,
                _square.X,
                _square.Y,
                _dead
            );
        }

        // <b>Every change in the tally is written down, because a count read off a screen is not evidence.</b>
        // Patrick watched a muster read 1 of 21, then 6, then 24, then 1 again on 05.09.2026, and none of
        // those numbers could be got out of the log: Gathered counts company members inside Assembly tiles
        // of the point, squad.Ceiling is set to the same _wanted every beat, and BotSquads.Join refuses
        // anybody over it — so twenty-four of twenty-one cannot come from this pair. Which means the screen
        // is showing some other pair, and the way to find out which is to have this one on the record with
        // all three populations named apart: who is standing here, who is in the company at all, and what
        // was asked for. Said on change only, so a five-minute muster is a handful of lines.
        if (_here != _said0 || _called != _said1)
        {
            _said0 = _here;
            _said1 = _called;

            logger.Information(
                "{Name}'s muster at ({MX}, {MY}): {Here} of {Wanted} standing in it, {Called} in the company, ceiling {Ceiling}, {Left}s left",
                body.Name,
                _muster.X,
                _muster.Y,
                _here,
                _wanted,
                _called,
                squad.Ceiling,
                Math.Max(0, (MusterMs - (now - _musteredTick)) / 1000)
            );
        }

        if (squad.Focus is { Deleted: false, Alive: true })
        {
            _fought = true;
            _foughtTick = now;
        }

        // Held past the bell while the company fights and for a moment after, never beyond HoldMs. See HoldMs.
        var fighting = _fought && now - _foughtTick < RegroupMs && now - _musteredTick < MusterMs + HoldMs;

        // <b>A clearing marches on strength (02.10.2026).</b> The square asks so much of a company (what has killed there, or
        // what stands there); when that much at ClearFit is standing at the muster, with at least Least bodies, the clearing
        // goes — the head count above is the levy's ceiling, not the bell. At the bell, short of the strength, it does not go.
        if (_clearing)
        {
            var standing = Strong(squad);
            var enough = _here >= Least && standing >= _asked * ClearFit;

            // <b>The ask again at the bell (review of clear12, F4).</b> It was fixed at the offer, and the march goes five to seven
            // minutes later: a respawn or a wanderer stronger than what was counted is met, withdrawn from, and the members were
            // pressed for nothing. One cached look; a square the plague has closed meanwhile is given up and rested.
            if (enough)
            {
                var closed = BotBarred.Closing(_map, _square, out var closure);

                if (closed && closure.Dire)
                {
                    ClearDoomed++;
                    Rest(body);
                    BotClearer.RestSquare(_map, _square);
                    BotSquads.Leave(member);
                    _squad = null;

                    return BotDoing.Failed($"({_square.X}, {_square.Y}) was closed for the plague or a killer of {BotBarred.QuarantinePower:F0} while the company was called");
                }

                var again = BotClearer.Asks(_map, _square, closed ? closure : null);

                if (again > _asked)
                {
                    if (!_reasked)
                    {
                        _reasked = true;
                        ClearReasked++;
                    }

                    _asked = again;
                    enough = standing >= _asked * ClearFit;
                }
            }

            if (!enough && (now - _musteredTick < MusterMs || fighting))
            {
                if (!_held && now - _musteredTick >= MusterMs)
                {
                    _held = true;
                    Held++;
                }

                return BotDoing.Work($"calling at ({_muster.X}, {_muster.Y}): {_here} of {_wanted} gathered, {standing:F0} of the {_asked * ClearFit:F0} the ground asks");
            }

            if (!enough)
            {
                Undermanned++;
                ClearUndermanned++;

                logger.Information(
                    "{Name}'s muster at ({MX}, {MY}) to clear ({X}, {Y}) came up {Here} of {Wanted} and {Standing:F0} of the {Asked:F0} of strength the ground asks; not in the square: {Absent}",
                    body.Name,
                    _muster.X,
                    _muster.Y,
                    _square.X,
                    _square.Y,
                    _here,
                    _wanted,
                    standing,
                    _asked * ClearFit,
                    Absent(squad)
                );

                // The square rests too, not only the leader (review of clear12, F2): on 396 Petra, Kaelthas and Isembard took
                // (1695, 1365) one after another inside twenty minutes, each short at the bell, 23 bots pressed for nothing.
                Rest(body);
                BotClearer.RestSquare(_map, _square);
                BotSquads.Leave(member);
                _squad = null;

                return BotDoing.Failed(
                    $"only {_here} of us with {standing:F0} of strength stood at the muster for ({_square.X}, {_square.Y}), which asks {_asked * ClearFit:F0}, after"
                    + $" {MusterMs / 60000} minutes, of {_called} called"
                );
            }
        }

        // <b>The instant the company is whole, it marches.</b> No waiting out the clock with a full company,
        // which is what the early exit has always been for; the change is what happens when the clock runs
        // out instead. See below.
        if (!_clearing && _here < _wanted && (now - _musteredTick < MusterMs || fighting))
        {
            if (!_held && now - _musteredTick >= MusterMs)
            {
                _held = true;
                Held++;
            }

            // Waiting is not walking. A walk to the tile the bot is already standing on never shortens, so
            // the walker gave the journey up after about two minutes and the errand failed with "it got no
            // nearer than 1 tiles" — 3 musters, 1837 bots called up, 0 marched, at 01:50 on 05.09.2026. The
            // jam detector is safe on Work here: BotWill.LabourMs is fifteen minutes and this cannot outlive
            // MusterMs.
            return BotDoing.Work($"calling at ({_muster.X}, {_muster.Y}): {_here} of {_wanted} gathered");
        }

        // <b>Short at the bell means no march at all, and that is a change of rule made on Patrick's order.</b>
        // This used to set out with whoever came down to Least — three — on the reasoning that a Baron who
        // waits for a coincidence never leaves town. The order of 05.09.2026 is the other way about: the
        // company is the company, and a call that cannot raise it is dropped for RestMs rather than answered
        // with an escort. What made the old reasoning necessary was a muster that could not survive its own
        // clock; with the clock fixed, five real minutes of levying is a fair test of whether the bodies
        // exist, and marching six-strong work with three is how a harrowing becomes three more deaths on the
        // square that is already on the board for having killed people.
        if (!_clearing && _here < _wanted)
        {
            Undermanned++;

            logger.Information(
                "{Name}'s muster at ({MX}, {MY}) came up {Here} of {Wanted}; not in the square: {Absent}",
                body.Name,
                _muster.X,
                _muster.Y,
                _here,
                _wanted,
                Absent(squad)
            );

            Rest(body);

            // Let go rather than held. A Baron standing about with one volunteer is two bots not working, and
            // the ground is still on the board for the next review.
            BotSquads.Leave(member);

            _squad = null;

            return BotDoing.Failed(
                $"only {_here} of the {_wanted} were standing in the square for ({_square.X}, {_square.Y}) after"
                + $" {MusterMs / 60000} minutes, of {_called} called"
            );
        }

        // <b>Damned ground, and who may walk on it.</b> By order: a square that has swallowed thirty is
        // gathered against only by fifteen grandmasters and four thousand of strength between them. Checked
        // here rather than at the muster because it is a fact about who turned up, and the call stays open
        // while it is not met — a Baron who cannot raise the company he needs waits for it, and the ground is
        // still on the board either way.
        if (BotQuad.Damning(_map, _square) && !Fit(squad, out var ready, out var unarmed))
        {
            Unfit++;

            if (now - _musteredTick < MusterMs)
            {
                // Waiting, not walking — the same correction as the muster above, for the same reason.
                return body.InRange(_muster, Station)
                    ? BotDoing.Work($"calling for grandmasters at ({_muster.X}, {_muster.Y}): {ready} of {Grandmasters} fit")
                    : BotDoing.Walk(_map, _muster, BotArrival.Within(Station), $"calling for grandmasters at ({_muster.X}, {_muster.Y})");
            }

            Rest(body);

            BotSquads.Leave(member);

            _squad = null;

            return BotDoing.Failed(
                $"({_square.X}, {_square.Y}) is damned ground and {ready} of the {Grandmasters} answered fit for it,"
                + $" each needing a skill at {GrandmasterAt:F0} and {Might:F0} of strength"
                + (unarmed > 0 ? $"; {unarmed} more were casters with fewer than {Reagents} reagents" : "")
            );
        }

        _marching = true;
        _marched = _here;

        Marches++;

        if (_clearing)
        {
            ClearMarches++;

            if (BotBarred.Closing(_map, _square, out _))
            {
                ClearClosed++;
            }
        }

        // Its own finder, because the ordinary one refuses anything a single bot could handle — which is
        // correct for a muster and is the exact opposite of this errand. Set now rather than at the muster,
        // so that a company standing in a town square is not quietly hunting the town.
        squad.Quarry = Prey;

        // Stamped when the company actually sets out. The forty minutes are the harrowing's, and a clock that
        // started when the call opened would spend a eighth of them standing in a square.
        _began = now;

        // <b>Both numbers, because the rule between them is the one that changed.</b> The Baron now waits for
        // bodies standing in the square rather than for names on a list — see Assembly — and a line that
        // printed only one of the two could not show whether he waited or whether nobody was late. On
        // 02.09.2026 the first march after the change read "6 of them" and told nobody that all six had
        // spawned on the muster point a second earlier, which is a true sentence about an untested rule.
        logger.Information(
            "{Name} is marching {Count} of the {Called} called on ({X}, {Y}), where {Dead} have died, after {Waited:F1} minutes of calling",
            body.Name,
            _here,
            _called,
            _square.X,
            _square.Y,
            _dead,
            (now - _musteredTick) / 60000.0
        );

        return BotDoing.Walk(_map, _square, BotArrival.Within(Side / 3), $"marching on ({_square.X}, {_square.Y})");
    }

    private BotDoing Harrowing(IBotSquadMember member, Mobile body)
    {
        var squad = member.Squad;

        // A company that withdrew from what it could not beat: failed with the reason, not finished (03.10.2026, review of march10,
        // F2). See BotSquad.Overmatched.
        if (_squad is { Disbanded: true, WithdrewFrom: { } withdrewFrom })
        {
            if (_clearing)
            {
                ClearWithdrew++;
            }

            return BotDoing.Failed($"the company withdrew from {withdrewFrom.Name}, which it could not beat");
        }

        if (squad == null || !ReferenceEquals(squad, _squad))
        {
            return BotDoing.Done("the company broke up on the road");
        }

        _called = squad.Count;

        if (_called < 2)
        {
            // <b>Lost whole, and the map is told the size of what it took.</b> The next levy climbs by
            // BotQuad.Reinforcement and a loss of BotQuad.DireLoss damns the ground outright — Patrick's
            // order of 02.09.2026, that the crown keeps calling five more until the victory is won. Counted
            // from what marched rather than from what is standing, because what is standing is the point.
            if (_marched > 0)
            {
                BotQuad.LostCompany(_map, _square, _marched);
            }

            return Finish(squad, "there was nobody left to harrow with", cleared: false);
        }

        Count(squad);

        if (_kills >= Quota)
        {
            Emptied++;

            if (_clearing)
            {
                ClearEmptied++;
            }

            return Finish(squad, $"{_kills} of them are dead and ({_square.X}, {_square.Y}) is done", cleared: true);
        }

        var now = Core.TickCount;

        if (now - _began >= CapMs)
        {
            Timedout++;

            if (_clearing)
            {
                ClearTimedout++;
            }

            // Cleared even so, and that is the order rather than an oversight. Half an hour of six bots
            // walking a box seventy-five tiles across is the ground having been dealt with as thoroughly as
            // this shard knows how; leaving the dead on the count afterwards would send the same company
            // back to the same coordinates for ever, because the dead are the one number that never fades.
            return Finish(squad, $"{CapMs / 60000} minutes on ({_square.X}, {_square.Y}) was enough at {_kills} down", cleared: true);
        }

        // Two distances rather than one, and the gap is what keeps the company on the ground. Arriving is a
        // third of a side; leaving is the whole of it, because a Baron who steps out of the box after
        // something that hit him has not abandoned the errand. Judged on one number this flaps, and the
        // patrol paid for that lesson on 26.08.2026 — see BotSweep.
        var away = _standing ? Side : Side / 2;

        if (!body.InRange(_square, away))
        {
            _standing = false;

            return BotDoing.Walk(_map, _square, BotArrival.Within(Side / 3), $"marching on ({_square.X}, {_square.Y})");
        }

        if (!_standing)
        {
            _standing = true;
            _steppedTick = now;
            _round = 0;
            _post = Post(_round);

            logger.Information(
                "{Name}'s company is on ({X}, {Y}) and has begun walking it",
                body.Name,
                _square.X,
                _square.Y
            );
        }

        // <b>In a fight the Baron is one of the company, and until 16.09.2026 he was its only deserter.</b> This
        // errand answered a walk to the next corner on every beat, and the company's own stationing put him at the
        // ring round the creature on the same beat — two hands on one journey, and BotWill's is the later one, so
        // he set off for the corner while six bots he had levied fought without him. Patrick watched him "jump
        // between the monsters without seeing them". While the company is on something, the leader answers Work
        // and is stationed and pressed like everybody else; the corners resume when it is down.
        if (squad.Stance == BotSquadStance.Fighting && squad.Focus is { Deleted: false, Alive: true } fighting)
        {
            // Each new thing the company is on is a new stage of this errand, and the will's labour clock — which
            // fails a quarter hour of nothing but Work — is told so, or a Baron fighting from one creature straight
            // into the next for fifteen minutes would be failed for jamming while he was doing the one thing asked.
            if (!ReferenceEquals(fighting, _fighting))
            {
                _fighting = fighting;

                if (member is IBotWilful wilful && wilful.Resolve != null)
                {
                    wilful.Resolve.StirredTick = now;
                }
            }

            return BotDoing.Work($"fighting {fighting.Name} with the company on ({_square.X}, {_square.Y}), {_kills} of {Quota} down");
        }

        // It walks rather than standing in the middle, for the two reasons the patrol has: a box this wide is
        // not covered by a company parked at its centre, and an errand that only ever answers Work is
        // invisible to BotWill.LabourMs, which fails anything that has done nothing else for a quarter of an
        // hour. This errand's own cap is forty minutes, so the two clocks could not both be obeyed — and a
        // failure marks the ground with caution, which would teach the whole population to avoid the very
        // square the company was sent to empty.
        if (now - _steppedTick >= RoundMs || body.InRange(_post, 1))
        {
            _steppedTick = now;
            _bends = 0;
            _post = Post(++_round);
        }

        return BotDoing.Walk(
            _map,
            _post,
            BotArrival.Within(1),
            $"harrowing ({_square.X}, {_square.Y}), {_kills} of {Quota} down"
        );
    }

    /// <summary>
    /// Corpses, counted off the company's own focus.
    ///
    /// <para>
    /// <b>A kill is the thing the company was fighting ceasing to exist, and nothing else.</b> Counted per
    /// beat while in contact, one skirmish reads as hundreds — the patrol reported 1064 fights for a single
    /// half hour on 25.08.2026 and the number could not be acted on. Counted on the focus changing, a
    /// creature that simply walked away counts as dead. So the remembered focus is only ever credited when
    /// it is gone or down, and a focus that is replaced while still alive is replaced silently.
    /// </para>
    /// </summary>
    private void Count(BotSquad squad)
    {
        if (_quarry != null && (_quarry.Deleted || !_quarry.Alive))
        {
            _kills++;
            Killed++;

            if (_clearing)
            {
                ClearKilled++;
            }

            _quarry = null;
        }

        var focus = squad.Focus;

        if (focus is { Deleted: false, Alive: true } && !ReferenceEquals(focus, _quarry))
        {
            _quarry = focus;
        }
    }

    /// <summary>
    /// Where the company forms up: the named square, or the population's home when none is named.
    ///
    /// Not called <c>Where</c>: that name is already the undertaking's own — the place the work happens —
    /// and for the whole of the muster the two are different places.
    ///
    /// Settled on the ground rather than taken as written, because a coordinate out of a configuration file
    /// is two numbers and a guess at the third — the fault that put <c>(x, y, 0)</c> across the whole peril
    /// map and failed ten patrols in a night.
    /// </summary>
    private Point3D Rally(Mobile body)
    {
        // <b>A guild's own company forms at its own hall.</b> Patrick's answer of 26.09.2026: the guild gathers itself and
        // clears the spawn by its hall itself. The town's gate and the population's home are the Baron's, and they were the
        // guild's too until now: Brannoc of The Lantern called his guild's company for (1065, 1425) at the population's home,
        // (1440, 1470), 375 tiles off beyond the river, found no way there and failed at 13:33:56; Ysolt called hers at
        // Britain's gate (1369, 1534), gathered six of six in four minutes, marched on (1065, 1395) — beside her own hall at
        // 1082,1402 — and failed on the road at 13:40:52. The hall's door (BotFeud.MusterPoint) is where its war company
        // gathers too.
        if (_guild != null)
        {
            var door = BotFeud.MusterPoint(_guild, body);

            if (door != Point3D.Zero)
            {
                return BotStep.Settle(_map, door.X, door.Y, out var dz) ? new Point3D(door.X, door.Y, dz) : door;
            }
        }

        // <b>The edge of the town in the direction of the ground, before the town square.</b> Patrick's
        // order of 03.09.2026, and the reason is the march: calling the levy on Britain's square puts six
        // bots in the middle of a town they must then cross before they have gone anywhere. The gate on the
        // right side is the shortest honest place to meet, and more of the population passes it. Outside a
        // town Gate answers with nothing and the named square stands, exactly as it did.
        var gate = BotPopulation.Gate(_map, body.Location, _square);

        // <b>A clearing never musters inside closed ground (review of clear12, F1).</b> Its company would walk in one at a time and
        // stand there up to seven minutes, the first three fighting what twenty were sized for. The population's home below is
        // never closed (BotBarred.Homely).
        if (gate != Point3D.Zero && !(_clearing && BotBarred.Holds(_map, gate)))
        {
            return gate;
        }

        // <b>A clearing called on the road musters where its leader stands (02.10.2026).</b> The population's home is the
        // Baron's muster; a fighter of Trinsic who found a death square by the road would otherwise call its company at
        // Britain, a town away, and the levy reaches two hundred tiles from the muster.
        if (_clearing && !BotBarred.Holds(_map, body.Location) && BotStep.Settle(_map, body.X, body.Y, out var hz))
        {
            return new Point3D(body.X, body.Y, hz);
        }

        var named = Square != Point3D.Zero ? Square : BotPopulation.Where;

        if (BotStep.Settle(_map, named.X, named.Y, out var z))
        {
            return new Point3D(named.X, named.Y, z);
        }

        return named != Point3D.Zero ? named : body.Location;
    }

    /// <summary>
    /// Calls up the nearest bodies that can fight, whatever they were doing.
    ///
    /// <para>
    /// <b>Nobody is asked, and this is the one place on the shard where that is true.</b> Every other company
    /// here is made of bots that were free and came because they were free — the patrol says so at length and
    /// it is right to. A harrowing cannot be built that way and the evening of 27.08.2026 proved it: the
    /// Baron stood in the square for the full five minutes and not one bot was idle at the moment he looked.
    /// A population that is working well is a population with no volunteers in it.
    /// </para>
    ///
    /// <para>
    /// So it is a levy. What the bot was doing is set aside rather than thrown away — being in a company puts
    /// it on the <c>Bound</c> rung and its own errand is still underneath when the company ends — which is
    /// the same thing that happens to anybody who joins a muster, and is why this costs the population an
    /// interruption rather than a job.
    /// </para>
    ///
    /// <para>
    /// <b>The producers are exempt, by order, and it is the right exemption.</b> A smith, a miner and the
    /// Architect are the three trades whose work is a chain — ore to ingots to armour, on the board, for
    /// somebody else — and breaking one link idles everything downstream of it. They are also the three worst
    /// at the thing they would be called up to do.
    /// </para>
    /// </summary>
    private void Levy(BotSquad squad, Mobile body)
    {
        if (squad.Count >= Wanted)
        {
            return;
        }

        _muster = _muster != Point3D.Zero ? _muster : Rally(body);

        // The quota first, nearest of each, then anybody at all to fill the rest. Two passes over one
        // gathered list rather than four spatial sweeps: the sweep is the expensive half.
        _called0.Clear();

        if (_guild != null)
        {
            // <b>A guild's own company is its own members, wherever they are.</b> "They gather themselves" (Patrick,
            // 26.09.2026): not whoever happens to pass within Reach of the door, and not the members of another guild.
            var members = _guild.Members;

            for (var i = 0; i < members?.Count; i++)
            {
                if (members[i] is BotMobile other && other != body && other.Map == _map && Callable(other)
                    && Reaches(_map, other.Location, _muster))
                {
                    _called0.Add(other);
                }
            }
        }
        else
        {
            foreach (var mobile in _map.GetMobilesInRange<Mobile>(_muster, Reach))
            {
                if (mobile != body && mobile is BotMobile other && Callable(other) && Reaches(_map, other.Location, _muster))
                {
                    _called0.Add(other);
                }
            }
        }

        // A clearing is sized by strength, so it calls the strongest first (03.10.2026): fewer bodies pressed for the same strength,
        // as the sweep's call (BotSweep.Calling). Everybody else, the nearest.
        if (_clearing)
        {
            _called0.Sort((a, b) => BotThreat.Power(b).CompareTo(BotThreat.Power(a)));
        }
        else
        {
            _called0.Sort((a, b) => Apart(a, body).CompareTo(Apart(b, body)));
        }

        _levied = _clearing ? CompanyStrength(squad, out _) : 0.0;

        Take(squad, BotRole.Melee, Melee);
        Take(squad, BotRole.Ranged, Ranged);
        Take(squad, BotRole.Medic, Medics);
        Take(squad, null, Wanted);
    }

    /// <summary>
    /// <b>A clearing's levy stops once the strength asked is in the company and the least heads are (review of clear12, F3).</b>
    /// On its first beat the company is the leader alone, so the heads asked were the need over the leader's strength, and the
    /// strongest first filled them: a leader of 1277 asked fourteen, and the thirteen strongest after him bring some 53000 against
    /// 16875 — two or three times what is needed, pressed for half an hour.
    /// </summary>
    private bool Enough(BotSquad squad) =>
        _clearing && squad.Count >= Least && _levied >= _asked * ClearFit;

    /// <summary>Bots the levy passed over for standing round the far side of something by road from the muster.</summary>
    public static long Across { get; private set; }

    /// <summary>
    /// Whether a bot at <paramref name="from"/> can be expected to walk to the muster at <paramref name="to"/>: not further
    /// round by road than <c>BotPlot.MostBehind</c>, the far side of a river, whichever side of it the bot stands on. Where the
    /// road map cannot say, it can.
    ///
    /// <para>
    /// <b>The walker plans a hundred and fifty milliseconds, and a river's bridge can be a thousand steps off.</b> The Lantern's
    /// hall at 1082,1402 lies 1439 steps from home by road for 356 straight; its company called at the door at 14:05:57 on
    /// 26.09.2026, and Ysolt, 90 tiles east of it at (1171, 1382) on the near bank, "found no way round in 150ms" and failed
    /// the muster at 14:08:30 — as its war company's members had failed the same door at 13:12. A body that cannot come is
    /// not a body the muster waits five minutes for.
    /// </para>
    /// </summary>
    public static bool Reaches(Map map, Point3D from, Point3D to, bool count = true)
    {
        if (map == null || to == Point3D.Zero)
        {
            return true;
        }

        var there = BotRoads.Behind(map, from, to);
        var back = BotRoads.Behind(map, to, from);

        if (there < 0 || Math.Max(there, back) <= BotPlot.MostBehind)
        {
            return true;
        }

        if (count)
        {
            Across++;
        }

        return false;
    }

    /// <summary>
    /// How many bots within <see cref="Reach"/> of <paramref name="muster"/> the levy could call up this moment. The same
    /// question the muster asks when it stands, asked before it is offered: see <c>BotHarrower</c>.
    ///
    /// <para>
    /// <b>49 harrows in three hours of build 272 ended "only N of the M were standing in the square ... of K called", and in
    /// most of them K was the whole of what stood within call (29.09.2026).</b> Corwin's at 23:39: 3 of 9, 3 called, "every
    /// member is in the square". A muster is five minutes of the Baron standing still; a muster that cannot be filled from
    /// the ground round it is five minutes spent to learn a number this can say for nothing.
    /// </para>
    /// </summary>
    public static int Supply(Map map, Point3D muster, Mobile caller)
    {
        if (map == null || map == Map.Internal || muster == Point3D.Zero)
        {
            return 0;
        }

        var supply = 0;

        foreach (var mobile in map.GetMobilesInRange<Mobile>(muster, Reach))
        {
            // Not one the company's door refuses as hostile to the caller (BotSquads.Join; review of clear12, F2).
            if (mobile != caller && mobile is BotMobile other && Callable(other, count: false) && Reaches(map, other.Location, muster, count: false)
                && (caller == null || !BotRegard.Hostile(caller, other)))
            {
                supply++;
            }
        }

        return supply;
    }

    /// <summary>Whether the levy may call this bot up.</summary>
    private static bool Callable(BotMobile other, bool count = true)
    {
        if (other.Squad != null || other is not IBotAlly { AbleToFight: true })
        {
            return false;
        }

        if (other.Class is not { } klass || klass.Role == BotRole.Producer)
        {
            return false;
        }

        // <b>Not a bot that already answers to a company or a class.</b> Work that runs alongside a company goes on
        // under this one too, so a student in the captain's ranks, a captain drilling them or a fighter on its
        // way to a rally would be in two places at once, and stays in the other: Gerda's muster at (1603, 1493)
        // came up 3 of 6 at 13:56 on 24.09.2026, and the three missing were Faron "drilling 6" and two of his
        // students "taking my place in the ranks", 140 tiles off at the training field.
        if (other.Resolve?.Deed is { Alongside: true })
        {
            if (count)
            {
                Engaged++;
            }

            return false;
        }

        // <b>Nor a carrier with another bot's goods in its pack (06.10.2026, deliver16).</b> 35 deliveries were put down for a
        // harrowing on build 407; a harrowing outlasts BotWill.AsideCapMs and the walk is dropped. See BotSquads.Pressable.
        if (BotSquads.Carrying(other))
        {
            if (count)
            {
                BotSquads.Spare();
            }

            return false;
        }

        // Nor a hunter at its fight (07.10.2026, work18): the fight is seconds, the harrowing half an hour. See BotSquads.Pressable.
        if (BotSquads.Fighting(other))
        {
            if (count)
            {
                BotSquads.SpareFighter();
            }

            return false;
        }

        // Nor a thief at its turn in a crowd (07.10.2026, loops18). See BotSquads.Pressable.
        if (BotSquads.Thieving(other))
        {
            if (count)
            {
                BotSquads.SpareThief();
            }

            return false;
        }

        // <b>Nor one without its supplies.</b> Patrick's order of 26.09.2026, an entry threshold for everybody (BotProvision);
        // the shopper sends such a bot for them at the spare price, and the guild pays what it cannot.
        // Asked uncounted when the census of who is within call asks it (count: false): BotProvision counts doors, and the
        // clearing's proposer asks about every bot within two hundred tiles at every review (03.10.2026).
        if (count ? !BotProvision.Fit(other, out _) : BotProvision.Short(other))
        {
            if (count)
            {
                Unsupplied++;
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// Whether this company may walk on ground the map has damned: grandmasters enough, and strength enough.
    ///
    /// <para>
    /// A grandmaster is a bot with any skill at the era's own ceiling, which is what the word means on this
    /// shard and is not a number this file gets to invent. Strength is the population's own reckoning of
    /// fighting power — see <c>BotThreat.Power</c>, health times what it hits for — added up across whoever
    /// answered, because that is the thing the ground will be measured against.
    /// </para>
    /// </summary>
    /// <summary>
    /// How many bots alive on this shard could walk on damned ground, asked of the whole population.
    ///
    /// <para>
    /// <b>The test existed and was applied after the company had been raised, which is the wrong end of
    /// it.</b> A square at the bleakest reading may be walked only by <see cref="Grandmasters"/> of them —
    /// and on 05.09.2026 the Baron muster</see>ed twenty-one bots against (855, 1695) at -1.00, failed that
    /// test because a population four hours old has no grandmasters at all, went on calling because the
    /// clock had not run out, and had the company taken apart under him by its own beat for having nowhere
    /// to be. The recorded tally reads 2 of 21, then 21 of 21, then 2, then 21, once a second: nineteen bots
    /// pulled off their work and handed back every second for five minutes.
    /// </para>
    ///
    /// <para>
    /// So it is asked before anybody is called. The same rule <c>BotArmourer</c> states about the board — a
    /// want nobody can fill is worse than an empty one — said about a company nobody can raise. Counted over
    /// the population rather than over a squad, which is the only honest way to ask "could this island field
    /// one at all".
    /// </para>
    /// </summary>
    public static int Musterable()
    {
        var bots = BotPopulation.Bots;
        var ready = 0;

        for (var i = 0; i < bots.Count; i++)
        {
            var body = bots[i];

            if (body is not { Deleted: false, Alive: true })
            {
                continue;
            }

            if (!Master(body) || BotThreat.Power(body) < Might)
            {
                continue;
            }

            if (BotGrimoire.Book(body) != null && Herbs(body) < Reagents)
            {
                continue;
            }

            ready++;
        }

        return ready;
    }

    private static bool Fit(BotSquad squad, out int ready, out int unarmed)
    {
        ready = 0;
        unarmed = 0;

        if (squad == null)
        {
            return false;
        }

        var members = squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            var body = members[i]?.Self;

            if (body is not { Deleted: false, Alive: true })
            {
                continue;
            }

            // Each of the three, of the same bot. A company that averages the requirement is a company where
            // six carry the other nine, and the ground has already shown what it does to those nine.
            if (!Master(body) || BotThreat.Power(body) < Might)
            {
                continue;
            }

            if (BotGrimoire.Book(body) != null && Herbs(body) < Reagents)
            {
                unarmed++;

                continue;
            }

            ready++;
        }

        return ready >= Grandmasters;
    }

    /// <summary>How many of the company are actually standing in the square. See <see cref="Assembly"/>.</summary>
    private int Gathered(BotSquad squad)
    {
        if (squad == null || _muster == Point3D.Zero)
        {
            return 0;
        }

        var here = 0;
        var members = squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            var body = members[i]?.Self;

            if (body is { Deleted: false, Alive: true } && body.Map == _map && body.InRange(_muster, Assembly))
            {
                here++;
            }
        }

        return here;
    }

    /// <summary>
    /// Who of the company is not standing in the square, and what might be keeping each one: where it is and how far
    /// off, dead, on another map, below ground, further round by road than the muster, fighting, and what its walker
    /// holds.
    ///
    /// <para>
    /// <b>Written after three musters of Gerda's in forty minutes on 24.09.2026 (build 211) ended "only 5 of the 6",
    /// "3 of the 6" and "5 of the 6" — fifteen bots taken off their work for five minutes each and nobody
    /// marched.</b> Only the first had a fight in it; in the other two the missing members simply never came, and
    /// the tally named how many and never who. A member held by a fight of its own, one on the far bank of the river,
    /// one in a cellar and one whose walker has given up all read "not in the square", and each wants a different
    /// cure.
    /// </para>
    /// </summary>
    private string Absent(BotSquad squad)
    {
        var members = squad.Members;
        var say = ValueStringBuilder.Create(256);

        try
        {
            for (var i = 0; i < members.Count; i++)
            {
                var body = members[i]?.Self;

                if (body is { Deleted: false, Alive: true } && body.Map == _map && body.InRange(_muster, Assembly))
                {
                    continue;
                }

                if (say.Length > 0)
                {
                    say.Append("; ");
                }

                if (body is not { Deleted: false })
                {
                    say.Append("a member with no body");

                    continue;
                }

                var apart = Math.Max(Math.Abs(body.X - _muster.X), Math.Abs(body.Y - _muster.Y));

                say.Append($"{body.Name} {apart} tiles off at ({body.X}, {body.Y}, {body.Z})");

                if (!body.Alive)
                {
                    say.Append(", dead");
                }

                if (body.Map != _map)
                {
                    say.Append($", on {body.Map}");
                }

                if (BotDungeon.Under(body.Location))
                {
                    say.Append(", underground");
                }

                var round = BotRoads.Behind(_map, _muster, body.Location);

                if (round > 0)
                {
                    say.Append($", {round} further round by road than the muster");
                }

                if (body.Combatant is { Deleted: false } foe)
                {
                    say.Append($", fighting {foe.Name}");
                }

                if (body is BotMobile bot)
                {
                    say.Append($", {BotStall.Road(bot)}");
                }
            }

            return say.Length > 0 ? say.ToString() : "nobody, every member is in the square";
        }
        finally
        {
            say.Dispose();
        }
    }

    /// <summary>Whether any of this bot's skills stands at the era's ceiling.</summary>
    private static bool Master(Mobile body)
    {
        var skills = body?.Skills;

        if (skills == null)
        {
            return false;
        }

        for (var i = 0; i < skills.Length; i++)
        {
            if (skills[i].Base >= GrandmasterAt && !BotMobile.Granted(skills[i].SkillName))
            {
                return true;
            }
        }

        return false;
    }


    /// <summary>How many reagents of any kind are in the pack. One type check covers every herb in the era.</summary>
    private static int Herbs(Mobile body)
    {
        var pack = body?.Backpack;

        if (pack == null)
        {
            return 0;
        }

        var held = 0;

        foreach (var item in pack.Items)
        {
            if (item is BaseReagent { Deleted: false } herb)
            {
                held += herb.Amount;
            }
        }

        return held;
    }

    /// <summary>Musters turned away from damned ground for want of grandmasters. See <see cref="Fit"/>.</summary>
    public static long Unfit { get; private set; }

    /// <summary>Fills up to <paramref name="most"/> places from the gathered list, nearest first.</summary>
    private void Take(BotSquad squad, BotRole? role, int most)
    {
        var taken = 0;

        for (var i = 0; i < _called0.Count && taken < most && squad.Count < Wanted; i++)
        {
            if (Enough(squad))
            {
                if (!_enough)
                {
                    _enough = true;
                    ClearEnough++;
                }

                break;
            }

            var other = _called0[i];

            if (other.Squad != null || role != null && other.Class?.Role != role)
            {
                continue;
            }

            if (BotSquads.Join(squad, other))
            {
                taken++;
                Called++;

                if (_clearing && other is IBotAlly { AbleToFight: true })
                {
                    _levied += BotThreat.Power(other);
                }

                // <b>A levied bot falls in, here as in a delve (01.10.2026, build 380).</b> Joining put the name in the company and
                // left the work in the hands, the fault build 299 closed for delves and never for the harrowing: at 23:11 Cedric's
                // muster at (1284, 1534) read "1 of 6 standing in it, 6 in the company" for five minutes while Gunnloda foraged,
                // Brynhild brewed, Bardryn carried a pack to the bank and Maud and Oriel held nothing, all of them in Britain —
                // Patrick: «why the hell did Cedric gather a group of 6, and all 6 stand in the town and do not move». The enlistment is
                // pressed on the bot (BotWill.Press): the errand is put down, the bot walks to the muster and holds its station.
                if (BotWill.Press(other, new BotEnlist(squad, _map, _muster, true), $"called to {squad.Leader?.Self?.Name ?? "a leader"}'s harrowing"))
                {
                    Pressed++;
                }
            }
        }
    }

    /// <summary>Levied bots whose errand was put down for the enlistment. See <see cref="Take"/>.</summary>
    public static long Pressed { get; private set; }

    private static int Apart(Mobile a, Mobile b) =>
        Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    /// <summary>
    /// Bodies gathered by one sweep. Static and reused: the levy runs on the population's beat and a fresh
    /// list every two seconds is garbage for nothing.
    /// </summary>
    private static readonly System.Collections.Generic.List<BotMobile> _called0 = [];

    /// <summary>Bots called up, all told. What a harrowing costs the rest of the island.</summary>
    public static long Called { get; private set; }

    /// <summary>
    /// Everything alive inside the box that is not a bot, a shopkeeper, somebody's pet or standing on
    /// guarded ground.
    ///
    /// <para>
    /// <b>Nearest, not strongest, and that is the order as given.</b> Everything that moves, so there is
    /// nothing to rank: the company works outwards from wherever it stands and the box empties from the
    /// inside. Bots need no exclusion of their own — they are players to the engine and this only ever asks
    /// about creatures — which is a far better guarantee than a rule somebody has to remember to write.
    /// </para>
    ///
    /// <para>
    /// <b>The town is excluded on the creature rather than on the leader.</b> Asked of the Baron, the answer
    /// would be "he is outside a town, so anything goes" — and a thing standing three tiles inside the gate
    /// would be attacked by six bots in front of the guards. Asked of the creature, the gate is the line.
    /// </para>
    /// </summary>
    private BaseCreature Prey(Mobile leader)
    {
        var map = leader?.Map;

        if (map == null || map == Map.Internal || map != _map)
        {
            return null;
        }

        BaseCreature nearest = null;
        var closest = int.MaxValue;
        var edge = Edge;
        var able = _squad?.Able() ?? 0.0;

        foreach (var creature in map.GetMobilesInRange<BaseCreature>(leader.Location, Sight))
        {
            if (creature is not { Deleted: false, Alive: true } or BaseVendor)
            {
                continue;
            }

            if (creature.Controlled || creature.Summoned || creature.IsDeadBondedPet)
            {
                continue;
            }

            // Outside the ground the company was sent to. Without this the box is a suggestion: one thing
            // running north takes six bots with it and the square is never walked.
            if (Math.Abs(creature.X - _square.X) > edge || Math.Abs(creature.Y - _square.Y) > edge)
            {
                continue;
            }

            if (creature.Region?.IsPartOf<GuardedRegion>() == true)
            {
                continue;
            }

            // Nearest wins, so a farther one is not weighed at all (review of clear12: the tests below walk the company).
            var apart = Math.Max(Math.Abs(creature.X - leader.X), Math.Abs(creature.Y - leader.Y));

            if (apart >= closest)
            {
                continue;
            }

            // Nor one standing in a standing bar or in ground the plague or a killer of 30000 closed: the box walked may overlap
            // them, and the clearing was never sent there (review of clear12, F6).
            if (BotBarred.BarredOutright(map, creature.Location) || BotBarred.DireAt(map, creature.Location))
            {
                continue;
            }

            // The shard's own word for what a bot fights, rather than a second one kept here: not innocent, not
            // ours, and something the engine lets us strike. Patrick's order of 16.09.2026: any hostile target.
            if (!BotThreat.Hostile(leader, creature))
            {
                continue;
            }

            // Nor one a company lately found it could not hurt. See BotDelve.Prey.
            if (BotQuarry.Shunned(creature))
            {
                continue;
            }

            // <b>Nor one this company would withdraw from (03.10.2026).</b> The squad's hunt refuses what is beyond it (BotSquad.Hunt),
            // and with the nearest such thing offered every beat the company stood idle beside it and the rest of the box went
            // unworked. The next nearest is offered instead; a box with such a thing left in it is not lifted (BotBarred.Lift).
            if (_squad != null && (_squad.Beyond(creature) || BotSquad.Shuns(creature, able)))
            {
                continue;
            }

            closest = apart;
            nearest = creature;
        }

        return nearest;
    }

    /// <summary>
    /// The next place inside the box to walk to: the middle and the four corners, in turn, each on its own
    /// ground.
    ///
    /// A corner's height is looked up rather than carried sideways from the middle — a box this wide crosses
    /// hillsides, and a walk to a corner at the middle's height is a walk to nowhere. That fault put
    /// <c>(x, y, 0)</c> on the whole peril map and failed ten patrols in one night.
    /// </summary>
    private Point3D Post(int round)
    {
        var reach = Math.Max(2, Side / 3);

        var (dx, dy) = (round % 5) switch
        {
            0 => (0, 0),
            1 => (-reach, -reach),
            2 => (reach, -reach),
            3 => (reach, reach),
            _ => (-reach, reach)
        };

        var x = _square.X + dx;
        var y = _square.Y + dy;

        return BotStep.Settle(_map, x, y, out var z) ? new Point3D(x, y, z) : _square;
    }

    private BotDoing Finish(BotSquad squad, string why, bool cleared)
    {
        _ended = true;

        if (cleared)
        {
            // The whole box, not the cell at its middle. The company walked all of it; clearing one cell
            // would offer the Baron the next cell of the ground he has just spent the afternoon on.
            BotPeril.Cleared(_map, _square, Side / 2);

            // <b>And the standing reputation of the ground, which is the map the Baron is now sent by.</b>
            // The square he was sent to and the eight around it, because the box he walks is Side tiles
            // across and a quadrant is BotQuad.Side — so the neighbours are ground his company genuinely
            // covered, not ground being cleared on his behalf. Set to nothing rather than to safe: what a
            // company killed everything in is not safe, nobody has walked it since. See BotQuad.Cleared.
            var quad = BotQuad.Known(_map, _square);

            if (quad != null)
            {
                var around = BotQuad.Around(quad, madeIfNew: false);

                for (var i = 0; i < around.Count; i++)
                {
                    BotQuad.Cleared(around[i]);
                }
            }

            // And the city's bounty on this square, if it put one there, split among whoever is still standing in
            // the company. See BotCity.Claim.
            BotCity.Claim(_map, _square, squad.Members);

            // <b>And the closures on the ground, lifted with its reading (02.10.2026).</b> A quarantine round a killing field
            // (BotBarred) and the box closed to a bot that died there twice (BotBreaker) were written for ground nobody had
            // dealt with; a company has just walked all of it. Left standing they would keep the population off ground the
            // record now calls nothing, for up to two hours, and send the next company to the same coordinates.
            // Only a quarantine on the ground walked, only when empty, never the plague's (03.10.2026). See BotBarred.Lift.
            var lifted = BotBarred.Lift(_map, _square, Edge, $"cleared by {squad.Leader?.Self?.Name ?? "a company"}'s company");

            QuarantinesLifted += lifted;
            BoxesLifted += BotBreaker.LiftGround(_map, _square, Side / 2);

            if (_clearing)
            {
                ClearLifted += lifted;

                if (BotClearer.Strongest(_map, _square, Edge, out _, fresh: true) <= BotBarred.EmptyBelow)
                {
                    ClearWon++;
                }
            }
        }

        Release(squad);

        return BotDoing.Done($"{why} — {_called} of us, {Made}gp between them");
    }

    /// <summary>The strength of the company standing at the muster: the members within <see cref="Assembly"/> of it that can fight.</summary>
    private double Strong(BotSquad squad)
    {
        if (squad == null || _muster == Point3D.Zero)
        {
            return 0.0;
        }

        var strength = 0.0;
        var members = squad.Members;

        for (var i = 0; i < members.Count; i++)
        {
            var body = members[i]?.Self;

            if (body is { Deleted: false, Alive: true } && body.Map == _map && body.InRange(_muster, Assembly)
                && members[i] is IBotAlly { AbleToFight: true })
            {
                strength += BotThreat.Power(body);
            }
        }

        return strength;
    }

    /// <summary>
    /// The strength the levy could call up this moment within <see cref="Reach"/> of <paramref name="muster"/>, the caller's
    /// own included. The question a clearing's proposer asks before the muster is called: a square that asks more than
    /// everybody within call brings is a muster that fails at the bell.
    /// </summary>
    public static double SupplyStrength(Map map, Point3D muster, Mobile caller)
    {
        if (map == null || map == Map.Internal || muster == Point3D.Zero)
        {
            return 0.0;
        }

        var strength = caller != null ? BotThreat.Power(caller) : 0.0;

        _supplied.Clear();

        foreach (var mobile in map.GetMobilesInRange<Mobile>(muster, Reach))
        {
            if (mobile != caller && mobile is BotMobile other && Callable(other, count: false) && Reaches(map, other.Location, muster, count: false)
                && (caller == null || !BotRegard.Hostile(caller, other)))
            {
                _supplied.Add(BotThreat.Power(other));
            }
        }

        // The strongest that fit in one company with the caller (03.10.2026): a square asking more than a full company of them
        // brings is not one this muster can fill, however many stand within call.
        _supplied.Sort();

        for (int i = _supplied.Count - 1, taken = 0; i >= 0 && taken < BotSquad.MaxSize - 1; i--, taken++)
        {
            strength += _supplied[i];
        }

        _supplied.Clear();

        return strength;
    }

    private static readonly List<double> _supplied = [];

    /// <summary>The way to the next corner does not exist. Somewhere else inside the same box, or give up.</summary>
    public override bool Bend(IBotWilful bot)
    {
        if (!_standing || ++_bends > MaxBends)
        {
            BotPeril.Baulked(_map, _square);

            return false;
        }

        _steppedTick = Core.TickCount;
        _post = Post(++_round);

        return true;
    }

    /// <summary>Times the leader put the harrow down and the company fell back with him. For the summary.</summary>
    public static long FellBack { get; private set; }

    /// <summary>Times the harrow was taken up again after that. For the summary.</summary>
    public static long TookUp { get; private set; }

    /// <summary>The leader is running or binding his wounds: the company breaks off and forms on him. See <see cref="Steadfast"/>.</summary>
    public override void Paused(IBotWilful bot)
    {
        FellBack++;

        if (_squad == null)
        {
            return;
        }

        _squad.Disengage("the leader has put the harrow down");

        logger.Information(
            "{Name} put the harrow of ({X}, {Y}) down; the company of {Count} falls back with him",
            bot?.Self?.Name,
            _square.X,
            _square.Y,
            _squad.Count
        );
    }

    /// <summary>The leader is back on his feet: the clocks that measure progress start from now, and the company walks the square again.</summary>
    public override void Resumed(IBotWilful bot)
    {
        TookUp++;

        var now = Core.TickCount;

        _steppedTick = now;
        _sweptTick = now;

        if (_mustering)
        {
            _musteredTick = now;
        }

        logger.Information(
            "{Name} took the harrow of ({X}, {Y}) up again with {Count} in the company",
            bot?.Self?.Name,
            _square.X,
            _square.Y,
            _squad?.Count ?? 0
        );
    }

    /// <summary>
    /// Everything the charge switched on, switched off, whichever way the errand ended.
    ///
    /// Left set, a company whose Baron died would stand in a wood until the world was reloaded: the quiet
    /// clock is the only thing that disbands a squad nobody is fighting, and the charge is exactly what
    /// turns it off.
    /// </summary>
    public override void Drop(IBotWilful bot)
    {
        var key = BotQuad.Key(_map, _square);

        if (_taken.TryGetValue(key, out var held) && ReferenceEquals(held.Deed, this))
        {
            _taken.Remove(key);
        }

        // Put down on the march — the leader ran and did not take it up again, or died — with the ground not cleared: the
        // square asks for more next time. See BotQuad.DrivenOff.
        if (_marching && !_ended && _marched > 0)
        {
            BotQuad.DrivenOff(_map, _square, _marched);
        }

        Release(_squad);

        if (bot is IBotSquadMember member && member.Squad != null && ReferenceEquals(member.Squad, _squad))
        {
            BotSquads.Leave(member);
        }

        _squad = null;
        _marching = false;
        _mustering = false;
    }

    private void Release(BotSquad squad)
    {
        if (squad == null)
        {
            return;
        }

        squad.Charged = false;
        squad.Quarry = null;
        squad.Ceiling = BotSquad.MaxSize;
    }

    public static void Forget()
    {
        Marches = 0;
        Musters = 0;
        Rested = 0;
        _resting.Clear();
        Called = 0;
        Undermanned = 0;
        Held = 0;
        Engaged = 0;
        Unsupplied = 0;
        Across = 0;
        Doubled = 0;
        _taken.Clear();
        Emptied = 0;
        Timedout = 0;
        Killed = 0;
        FellBack = 0;
        TookUp = 0;
        ClearMarches = 0;
        ClearEmptied = 0;
        ClearTimedout = 0;
        ClearUndermanned = 0;
        ClearKilled = 0;
        QuarantinesLifted = 0;
        BoxesLifted = 0;
        ClearClosed = 0;
        ClearResized = 0;
        ClearWon = 0;
        ClearLifted = 0;
        ClearWithdrew = 0;
        ClearReasked = 0;
        ClearDoomed = 0;
        ClearEnough = 0;
    }
}
