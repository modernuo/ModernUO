using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// The decision. Reads the ladder, holds what it has taken on, and runs an auction when it is free to want
/// something.
///
/// <para>
/// <b>It does not know what work exists.</b> It knows proposers — see <see cref="IBotProposer"/> — and they
/// belong to the subsystems that own the work. That is the structural answer to the first version's brain:
/// <c>ChooseGoal</c> was 1209 lines inside a file of 7985 which referenced 57 other modules, so every
/// change to any behaviour was a change to that file, and nobody could touch mining without touching
/// trade. Adding a kind of work here is a new folder and one line of registration; nothing in this file
/// changes.
/// </para>
///
/// <para>
/// <b>It does not re-decide every tick.</b> A decision is reviewed on a clock and switched only against a
/// margin, with what is already being done given a bonus for being underway. In the first version a bot in
/// state <em>Trade</em> was seen walking a graveyard: it was trading honestly, one tick at a time — two
/// steps towards town, a skeleton noticed ten tiles away, back to hunting, town noticed again. Any
/// intention longer than a second was impossible in principle, and the only reason it did not look broken
/// is that a bot walking in circles looks busy.
/// </para>
///
/// <para>
/// <b>Every decision is recorded in words and numbers.</b> The first version's brain took 85 of the 135
/// plans its own slow tier offered and nothing anywhere said so; the tier spent the night learning from
/// noise. A choice nobody can read is a choice nobody can correct.
/// </para>
/// </summary>
public static class BotWill
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWill));

    /// <summary>
    /// Whether deciding is switched on, from <c>bots.will.enabled</c>. Off, bots keep whatever the rest of
    /// the population's machinery gives them — squads still form up, movement still walks, combat still
    /// answers — and nothing chooses anything. That is the diagnostic: half the first version's
    /// investigations were the question "is this the brain, or is the brain covering for something else".
    /// </summary>
    public static bool Deciding { get; internal set; }

    /// <summary>
    /// How often a bot with something on looks up to see whether anything better has appeared.
    ///
    /// Fifteen seconds. The number that matters is that it is not "every tick": a bot that reconsiders four
    /// times a second cannot cross a continent, and one that reconsiders every quarter minute can.
    /// </summary>
    public static int ReviewMs { get; set; } = 15000;

    /// <summary>
    /// How soon a bot with nothing on looks again.
    ///
    /// Shorter than the review, because the two are different questions: a busy bot is being asked whether to
    /// change its mind, and an idle one is being asked whether there is anything to do at all. It is not the
    /// beat itself, though — sweeping every proposer twice a second for a population of a hundred and fifty is
    /// the first version's cost model, and two seconds of standing about is invisible.
    /// </summary>
    public static int IdleMs { get; set; } = 2000;

    /// <summary>
    /// How long a fresh undertaking is safe from being swapped out whatever the numbers say.
    ///
    /// The floor under the margin, and it is there for a specific pathology: two pieces of work whose scores
    /// keep crossing produce a bot that alternates between them and finishes neither. Half a minute is
    /// enough to get somewhere and find out.
    ///
    /// <para>
    /// <b>A floor now rather than the whole rule, because as the whole rule it was half a minute against a
    /// six-minute job.</b> See <see cref="Dwell"/>.
    /// </para>
    /// </summary>
    public static int DwellMs { get; set; } = 30000;

    /// <summary>
    /// The most a fresh undertaking may be safe for, however long it reckons itself.
    ///
    /// <para>
    /// Two minutes, and the number is chosen against <c>BotStall.PatienceMs</c> rather than against any
    /// work: the stall detector calls a bot stuck at four minutes, so nothing here may be protected long
    /// enough to hide from it. The cap is not tidiness — without one, an eight-minute errand that has gone
    /// wrong is immune for eight minutes, which is the frozen-work family this project has paid for more
    /// than once.
    /// </para>
    /// </summary>
    public static int DwellCapMs { get; set; } = 120000;

    /// <summary>
    /// How long this particular undertaking is safe from being swapped out: its own reckoning, floored by
    /// <see cref="DwellMs"/> and capped by <see cref="DwellCapMs"/>.
    ///
    /// <para>
    /// <b>The smith could not finish a single thing, and this is why.</b> Work is reconsidered every
    /// <see cref="ReviewMs"/> against what it has produced so far, and a craft that yields nothing until the
    /// item is done reads as nought a minute for as long as it takes. Mining and foraging are spared because
    /// they yield continuously; forging is not. On 04.09.2026 Calla dropped "beating out Cutlass (2 attempts,
    /// 0 made)" at exactly 0.5 minutes — the instant the flat dwell expired — and over that session the smith
    /// took five errands, dropped four and finished one, against 29 sewn and 37 brewed. The ledger then
    /// marked the trade down for failures it had itself caused: the base fell 55 → 37 → 28 across three
    /// readings, so forging lost every later auction as well.
    /// </para>
    ///
    /// <para>
    /// Two numbers on one shelf, and the cure is the usual one — stop having the second number.
    /// <see cref="BotDeed.Minutes"/> is already on the deed and already means "how long this takes".
    /// </para>
    /// </summary>
    private static int Dwell(BotDeed deed) =>
        Math.Clamp((int)Math.Round((deed?.Minutes ?? 0.0) * 60000.0), DwellMs, DwellCapMs);

    /// <summary>
    /// How long an undertaking may sit set aside — the bot dead, overloaded, dying, in a squad — before its
    /// reason is presumed stale and it is dropped.
    ///
    /// Ten minutes. An undertaking is not lost the moment something interrupts it, which is the whole point
    /// of holding one; but the market it was walking to closes, the vein it wanted is worked out, and the
    /// bot that comes back to it after an hour is acting on a fact from an hour ago.
    /// </summary>
    public static int AsideCapMs { get; set; } = 600000;

    /// <summary>
    /// Whether work put down for something that keeps it (<see cref="BotDeed.Keeps"/>: a company's call) is measured on
    /// <see cref="AsideCapMs"/> from the moment the company let the bot go, rather than from the moment the work was put
    /// down (12.10.2026, w2).
    ///
    /// <para>
    /// <b>The company's time was not the bot's.</b> The ten minutes above were written for the bot's own interruptions — a
    /// flight, a bandage, a rescue — and on 09–11.10 they were the whole reason for the largest drop on the shard: of the work
    /// put down for an enlistment, 95 % was taken up again when the company lasted under ten minutes and 2 of 1 578 pieces
    /// when it lasted longer (L2, 11.10, 10.2 h; L1, 38.7 h: 6 of 4 836), every one of the rest dropped "not taken up again
    /// after enlist: it was put down for N minutes" — 1 351 of 4 332 drops in L2, 3 472 of 13 672 in L1. The work was not
    /// stale: taken up again after a short company it finished 90 % of the time, and the 116 bots that took the same kind up
    /// from scratch within two minutes of the drop finished it 77 of 83 times. So a kept pause is not timed while the
    /// company holds the bot; its clock starts when the company breaks up, and it is the same ten minutes: the bot that
    /// comes back hurt has that long to be mended and go back. A switch, so it can be taken off a running shard; off, a
    /// company's call is an interruption like any other.
    /// </para>
    /// </summary>
    public static bool Keep { get; set; } = true;

    /// <summary>
    /// The longest work is kept for a company all told, from the put-down to the taking up, whatever the company's length;
    /// past it a kept pause lapses as any other would. See <see cref="Keep"/>.
    ///
    /// <para>
    /// An hour. The long companies of 09–11.10 ran 30–41 minutes (1 273 of 42 156 enlistments past thirty minutes, 32 past
    /// forty-five, 16 past sixty); the one at 386 minutes was Naevys, "too tired to go on" in a company that never broke up,
    /// and its mine of six hours before is a fact from six hours before. A ceiling, not a budget: counted apart
    /// (<see cref="KeptLapsed"/>) so that the day it fires more than a few times an hour is the day it is wrong.
    /// </para>
    /// </summary>
    public static int KeptCapMs { get; set; } = 3600000;

    /// <summary>
    /// How many beats an undertaking may hold one unchanged walk order without ever beating its own best
    /// distance to the place, before it is given up.
    ///
    /// <para>
    /// Six hundred, which is two minutes at a turn every two hundred milliseconds, and deliberately three
    /// times what <c>BotDig</c> and <c>BotProwl</c> allow themselves. Those two measure a walk to a known
    /// seam or a known square; this measures every walk on the shard, including a trip across Britain that
    /// has to go round a wall, and the counter only moves on a beat where the bot failed to beat its own
    /// record — so an honest walk resets it constantly and can never meet this at all. It sits well inside
    /// the stall watch's four minutes on purpose: the errand should end itself and teach its own ledger,
    /// rather than be cancelled from outside with nothing learned.
    /// </para>
    /// </summary>
    public static int TrekLimit { get; set; } = 600;

    /// <summary>Undertakings given up for a walk that stopped closing. For the summary.</summary>
    public static long Trudges { get; private set; }

    /// <summary>
    /// How long an undertaking may answer "working" without ever answering anything else before it is taken
    /// away as jammed.
    ///
    /// <para>
    /// A quarter of an hour, which is far above any honest stretch of standing still — the longest work in
    /// the project reckons itself at eight minutes and breaks that up with walking, and every step, finish or
    /// failure pushes the clock forward. Nothing that is really working can reach this.
    /// </para>
    ///
    /// <para>
    /// It exists because <c>Work</c> is the one answer nothing judges, which makes it the one an undertaking
    /// can hide behind. See the case that handles it for the two afternoons this cost.
    /// </para>
    ///
    /// <para>
    /// <b>Except work that stands still on purpose and ends on a clock of its own</b> (<see cref="BotDeed.Still"/>), which
    /// is the one kind that honestly answers "working" for longer. A sentence is an hour in a cell (<c>BotOutlaw.JailMs</c>),
    /// and on build 101b Faron Ashdown's, caught by Alden Ashdown at 06:14, was ended at 06:30 "nothing has come of this in
    /// 15 minutes" — the same ending the debugger's hand gave Nessa Ashdown's twice before build 101b, after which a bot in a
    /// jail nobody may travel out of takes a walk home and fails it every half-minute. The clock that set the work ends it.
    /// </para>
    /// </summary>
    public static int LabourMs { get; set; } = 900000;

    /// <summary>Beats of work that stands still on purpose left to its own clock past <see cref="LabourMs"/>.</summary>
    public static long OwnClock { get; private set; }

    /// <summary>
    /// How much better a new want must be than what is already underway, on top of
    /// <see cref="BotAppraisal.Inertia"/>. Together they are what makes behaviour look deliberate.
    /// </summary>
    public static double SwitchMargin { get; set; } = 1.25;

    /// <summary>
    /// How many times its own reckoning a piece of steadfast work is held against better-paid offers. See
    /// <see cref="BotDeed.Steadfast"/> and <see cref="Holding"/>.
    ///
    /// <para>
    /// <b>A third way between the two this file has already tried.</b> Reconsidering every tick was the first
    /// version's bot that could not cross a field; a flat half minute, then a flat two, was a bot that crossed
    /// the field and would not finish what it found on the far side. Kinny and Georgeff's answer is a bold
    /// agent that reconsiders on events — its way blocked, somebody in trouble, the guild calling — and
    /// otherwise carries its plan out. One and a half times the reckoning covers the ordinary overrun of a walk
    /// that had to go round a wall and stops short of protecting a trip that has plainly gone wrong.
    /// </para>
    ///
    /// <para>
    /// Nought switches the hold off and puts the auction back exactly as it was before 14.09.2026, which is
    /// why it is a dial: the change can be measured in both directions on a running shard.
    /// </para>
    /// </summary>
    public static double CommitStretch { get; set; } = 1.5;

    /// <summary>
    /// The longest any work is held, however long it reckons itself. Eight minutes: twice the stall watch, which
    /// runs on its own and does not care about holds, so nothing stuck can hide behind one.
    /// </summary>
    public static int CommitCapMs { get; set; } = 480000;

    /// <summary>
    /// How much of <see cref="TrekLimit"/> a walk may spend without getting any nearer before its hold is
    /// lifted. A third, forty seconds: a walk that has stopped closing is the one event every trade shares.
    /// </summary>
    public static double TroubleShare { get; set; } = 0.33;

    /// <summary>Whether steadfast work put down for something that would not wait is taken up again when that ends.</summary>
    public static bool Resume { get; set; } = true;

    /// <summary>
    /// The share of its health below which a bot does not go back to what it put down. A half: a miner that
    /// comes out of a fight at a third of its health and walks straight back to the rock where it was hit is
    /// the loop that ends in a corpse.
    /// </summary>
    public static double ResumeHealth { get; set; } = 0.5;

    /// <summary>How soon taking up a kind of work again, after dropping it for something else, counts as coming back to it.</summary>
    public static int ReturnMs { get; set; } = 600000;

    /// <summary>Work displaced by something that would not wait.</summary>
    public static long Jumped { get; private set; }

    /// <summary>Work displaced by a call from outside the bot's own business. See <see cref="BotDeed.Summons"/>.</summary>
    public static long Summoned { get; private set; }

    /// <summary>Work displaced by a better price, once any hold it had was over.</summary>
    public static long Outbid { get; private set; }

    /// <summary>
    /// Better offers refused because steadfast work was still inside its hold. Counted only where the old rule
    /// would have switched — the decisions the hold actually changed, not every review it sat through.
    /// </summary>
    public static long Held { get; private set; }

    /// <summary>Holds lifted at the moment they would have refused, because the work was in trouble.</summary>
    public static long Troubled { get; private set; }

    /// <summary>Steadfast work put down for something that would not wait.</summary>
    public static long Paused { get; private set; }

    /// <summary>Of those, the ones put down because the bot was pressed into something (<see cref="Press"/>) rather than outbid in an auction.</summary>
    public static long PressPaused { get; private set; }

    /// <summary>Work put down and taken up again.</summary>
    public static long Resumed { get; private set; }

    /// <summary>Work put down and not taken up again — the bot died, was hurt, or was away too long.</summary>
    public static long Unresumed { get; private set; }

    /// <summary>Work left put down a while longer because the bot came back too hurt to take it up. See <see cref="Unpause"/>.</summary>
    public static long HeldHurt { get; private set; }

    /// <summary>Work put down for a company's call and kept for as long as it lasted (<see cref="Keep"/>). Of <see cref="Paused"/>.</summary>
    public static long KeptForCompany { get; private set; }

    /// <summary>
    /// Of those, the ones taken up again having been put down longer than <see cref="AsideCapMs"/> — what the rule changed:
    /// every one of these was a drop before 12.10.2026. Of <see cref="Resumed"/>.
    /// </summary>
    public static long KeptPastCap { get; private set; }

    /// <summary>Kept work not taken up again for having been put down longer than <see cref="KeptCapMs"/> all told. Of <see cref="Unresumed"/>.</summary>
    public static long KeptLapsed { get; private set; }

    /// <summary>
    /// Kept work not taken up again within <see cref="AsideCapMs"/> of the bot first being free to take it up (it was sent to be
    /// mended, and may have been levied into a second company meanwhile — those minutes count). Of <see cref="Unresumed"/>.
    /// </summary>
    public static long KeptLate { get; private set; }

    /// <summary>Kept work refused by its own word (<see cref="BotDeed.Stale"/>) or for the bot being on another map. Of <see cref="Unresumed"/>.</summary>
    public static long KeptStale { get; private set; }

    /// <summary>Kinds of work taken up again within <see cref="ReturnMs"/> of being dropped for something else.</summary>
    public static long Returned { get; private set; }

    /// <summary>What happened to one kind of work, for the census and the debugger. Written on a decision, read every five minutes.</summary>
    private sealed class Tally
    {
        public long Taken;

        public long Finished;

        public long Failed;

        public long Dropped;

        public long Died;

        public long Jumped;

        public long Outbid;

        public long Held;

        public long Paused;

        public long Resumed;

        public long Returned;

        /// <summary>Endings kept out of the share for being no work at all. See <see cref="BotDeed.KeptOut"/>.</summary>
        public long KeptOut;

        /// <summary>Taken by winning an auction, not pressed. See <see cref="Auctioned"/>.</summary>
        public long Auctioned;
    }

    /// <summary>
    /// Whether work of this kind has been taken by winning an auction this session — the only way a claim steers anything.
    /// Work that is only ever pressed on a bot (a tournament's duel, the patrol, a temptation, a sentence) claims a number
    /// no auction reads.
    /// </summary>
    public static bool Auctioned(string kind) => kind != null && _tallies.TryGetValue(kind, out var tally) && tally.Auctioned > 0;

    private static readonly Dictionary<string, Tally> _tallies = new(StringComparer.OrdinalIgnoreCase);

    private static Tally TallyOf(string kind)
    {
        kind ??= "?";

        if (!_tallies.TryGetValue(kind, out var tally))
        {
            tally = new Tally();
            _tallies[kind] = tally;
        }

        return tally;
    }

    /// <summary>How often the population's decisions are summarised in the log.</summary>
    public static int CensusMs { get; set; } = 300000;

    /// <summary>
    /// Whether every commitment is logged as it is taken.
    ///
    /// On by default, because "why is this bot doing that" is the question this project exists to be able to
    /// answer, and a commitment happens at most every <see cref="DwellMs"/> per bot. Off when the log
    /// matters more than the answer: the first version left a 37 MB log for one session.
    /// </summary>
    public static bool Chatty { get; set; } = true;

    private static readonly List<IBotProposer> _proposers = [];

    /// <summary>
    /// Scratch space for one auction. Static and reused, like the planner's path buffer: decisions are made
    /// one bot at a time on the population's beat, and a fresh list per bot per fifteen seconds is garbage
    /// for nothing.
    /// </summary>
    private static readonly List<BotDeed> _offers = [];

    private static readonly Dictionary<string, int> _busy = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Rungs already complained about, so a missing proposer is said once and not per bot per beat.</summary>
    private static readonly HashSet<BotStanding> _mute = [];

    private static long _censusTick;

    /// <summary>
    /// Whether the census clock has been seeded from a real tick. Not "is the stamp zero": on some hosts the
    /// tick count is the machine's uptime counter passed through, so it starts enormous and can wrap
    /// negative. See <c>dev-docs/tick-counts.md</c> in the fork.
    /// </summary>
    private static bool _censused;

    public static long Taken { get; private set; }

    public static long Finished { get; private set; }

    public static long Failed { get; private set; }

    public static long Dropped { get; private set; }

    public static long Deaths { get; private set; }

    /// <summary>Pieces of work put down that died with their bot, counted with their trades and not in the headline. See Settle.</summary>
    public static long EchoDeaths { get; private set; }

    /// <summary>
    /// <b>A fight the bot did not choose is not work it took on (30.09.2026, build 370, Patrick's answer that evening).</b>
    /// «do it, it must hit back in a fight»: a bot set upon hits back — <see cref="BotRescue"/> with <c>Own</c> — and
    /// that ending is kept out of the finished share. On 363, 36 of rescue's ~40 endings short of finished were such
    /// defences ("Wraog was winning", "too many of them around an ogre", "drew it 108 tiles past where it was found and was
    /// let go"). Counted here, printed beside the headline, still tallied with their trade (rescue) and never rested by the
    /// breaker: the bot must always fight back.
    /// </summary>
    public static long DefenceFinished { get; private set; }

    public static long DefenceFailed { get; private set; }

    public static long DefenceDropped { get; private set; }

    public static long DefenceDeaths { get; private set; }

    /// <summary>
    /// How many times a bot looked for work and found nothing at all worth doing.
    ///
    /// <b>The number to watch, and it is about the world rather than the bots.</b> If work is valued by what
    /// it actually yields, then a bot with nothing worth doing is a shard with nothing left on it that this
    /// bot can profit from. The first version could not distinguish that from a broken motive: thirty-eight
    /// of fifty-one bots ended up patrolling with their drive frozen, which read as a bug in the drive and
    /// was not one.
    /// </summary>
    public static long Barren { get; private set; }

    /// <summary>
    /// Offers not made because the bot's class is sworn to other work. A named nought: see
    /// <see cref="Sworn"/>.
    /// </summary>
    public static long Unsworn { get; private set; }

    /// <summary>
    /// Times a full pack put the errand that needs no step ahead of better-paid work that does - that is,
    /// times the ordering actually overturned a price rather than merely agreeing with one.
    ///
    /// <b>Its own bucket, because a rank is worked out for every offer a heavy bot is made and means nothing
    /// until it changes an answer.</b> Counting every occasion a standing errand won would fold in all the
    /// ones where it was simply the best thing going, and the number worth watching is how often the shard
    /// is buying its way out of a standstill at a price it can name.
    /// </summary>
    public static long Grounded { get; private set; }

    /// <summary>
    /// Times that ordering took work out of a bot's hands mid-errand rather than filling an empty one.
    ///
    /// Separate from <see cref="Grounded"/> and not a subset of it: one is a gate in the auction, the other
    /// a gate on the switching margin, and they fire on different bots. This at nought while the other
    /// climbs means heavy bots are being caught only once they are already idle - which is exactly the half
    /// that was never broken.
    /// </summary>
    public static long Dislodged { get; private set; }

    /// <summary>
    /// Times better-paid work was refused because what the bot held had already spent money on itself.
    ///
    /// The number to read beside it is <c>BotSupplier.Trimmed</c>: this one climbing while that one stays
    /// at nought means couriers are being saved from the auction rather than never being overloaded in the
    /// first place, and the first repair is the one that has stopped working.
    /// </summary>
    public static long Kept { get; private set; }

    /// <summary>
    /// Times a member of a company put its enlistment down to run or to bind its own wounds. See <see cref="Aid"/>.
    /// </summary>
    public static long Aided { get; private set; }

    /// <summary>Beats a company member's own work was not advanced while it was raising one of ours. See <see cref="Advance"/>.</summary>
    public static long RaiseHeld { get; private set; }

    /// <summary>
    /// <b>A member of a company still runs and still binds its own wounds (30.09.2026, build 338).</b>
    ///
    /// <para>
    /// The enlistment is <c>Committed</c> since build 309 so that no errand bids a member away from its company, and its own
    /// note said "a rescue or a flight still jumps it". A flight never did: only <c>Pressing</c> work gets past a committed
    /// deed, and neither <see cref="BotBolt"/> nor <see cref="BotSalve"/> is pressing. So from 309 on a member of a company,
    /// hit and losing, was offered flight and a bandage on the rungs above and kept both out — "better offers turned down
    /// because the work in hand had already been paid for". Every one of the seven deaths "doing enlist" logged since 333
    /// began writing flights down (Amberle, Harald, Caelynn, Norbert, and Durgan's Helja, Thalion and Norbert on the march
    /// to the Orc Caves) had "runs … at N of M health" said for it in the forty seconds before and no flight taken; of 36
    /// deaths doing a band, whose deed is not committed, 24 had taken flight first.
    /// </para>
    ///
    /// <para>
    /// On the two survival rungs only, and only these two answers: the enlistment is put down and taken up again after
    /// (<see cref="BotEnlist.Resumes"/>); the company holds the member's place — it stops for a member running (the hold),
    /// and hands it no station while it runs or mends (<c>BotSquad.Station</c>). An ordinary offer still cannot bid it away.
    /// </para>
    /// </summary>
    public static bool Aid(BotDeed held, BotDeed best, BotStanding rung) =>
        held is BotEnlist && rung is BotStanding.Failing or BotStanding.Hunted && best is BotBolt or BotSalve;

    /// <summary>
    /// Offers of paperwork left for the next choice rather than taking a bot off the work in hand. Counts reviews, not
    /// orders: the same order is offered again at every review until the work ends. See <see cref="BotDeed.Paperwork"/>.
    /// </summary>
    public static long Filed { get; private set; }

    /// <summary>
    /// Whether a guess in hand holds against another guess of the same kind. See <see cref="BotDeed.Guess"/>. A switch,
    /// so the rule can be taken off a running shard and put back, which is how the hold was measured on 14.09.2026.
    /// </summary>
    public static bool GuessHolds { get; set; } = true;

    /// <summary>
    /// Offers of another guess of the same kind turned down for the one in hand. Counted past the margin, so each is a
    /// decision the rule changed and not a review it sat through. See <see cref="BotDeed.Guess"/>.
    /// </summary>
    public static long SecondGuesses { get; private set; }

    /// <summary>
    /// A thinking bot's choice, as the auction it was handed to weighed it: offered, refused outright, outscored, or on top.
    ///
    /// <para>
    /// <b>"0 taken up, 173 outbid" said nothing about why.</b> From 20:40 on 16.09.2026 not one of the five crafters' minds
    /// had a choice started, against five to sixty-four a session earlier that day, and the minds' own log could only say
    /// what the bot was holding when a choice went stale — 431 times Tailor while it mined, 107 times Tailor while it sewed
    /// by the shard's own arithmetic. Whether a choice lost on price, was refused, or came out on top and was held off by the
    /// work in hand are three different defects, and only this place sees which. Recognised by its kind, "mind-…", because
    /// the minds' assembly is built on this one and not the other way round.
    /// </para>
    /// </summary>
    public static long MindOffers { get; private set; }

    public static long MindRefused { get; private set; }

    public static long MindOutscored { get; private set; }

    /// <summary>A mind's choice was the best offer; whether it was then started is the mind's own count of taken up.</summary>
    public static long MindTopped { get; private set; }

    private static readonly Dictionary<string, long> _mindBeatenBy = [];

    /// <summary>
    /// The names of the proposers that have offered each kind of work, as the auctions have met them: "sew" offered as
    /// "Tailor", "mine" as "Miner".
    ///
    /// <para>
    /// <b>Two vocabularies for one trade, and the second one was the one an order is written in.</b> A mind chooses out of
    /// the proposers' names, so its guild charter reads "make Tailor" — and <c>BotCharter.Worth</c> compared that word with
    /// the deed's kind, "sew", which it never equals. So no member's work was worth more for being the trade its maker had
    /// named, and every charter boost came from the muster point: on build 99's first quarter of an hour all 378 of them,
    /// with "0 to gather, 7 to make, 8 to muster" given. The same mismatch <c>BotMindDeed</c> already names for the mind's own history ("the menu offers Scribe; the deed … calls
    /// itself inscribe"). Learned from the auctions rather than kept as a table, so a new proposer needs no entry here.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, List<string>> _offeredAs = new(StringComparer.OrdinalIgnoreCase);

    private static void Named(string kind, string name)
    {
        if (kind == null || name == null)
        {
            return;
        }

        if (!_offeredAs.TryGetValue(kind, out var names))
        {
            names = [];
            _offeredAs[kind] = names;
        }

        for (var i = 0; i < names.Count; i++)
        {
            if (string.Equals(names[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        names.Add(name);
    }

    /// <summary>Whether work of this kind has been offered by a proposer of this name. See <see cref="BotCharter"/>.</summary>
    public static bool OfferedAs(string kind, string name)
    {
        if (kind == null || name == null || !_offeredAs.TryGetValue(kind, out var names))
        {
            return false;
        }

        for (var i = 0; i < names.Count; i++)
        {
            if (string.Equals(names[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>One weighing of a mind's choice in this many is written out whole, the first always. Nought for none.</summary>
    public static int MindSampleEvery { get; set; } = 25;

    private static string Factors(BotWeigh w) =>
        $"estimate {w.Estimate:F0}, near {w.Nearness:F2}, new {w.Novelty:F2}, room {w.Room:F2}, safe {w.Caution:F2}, purse {w.Purse:F2}, standing {w.Stopped:F2}, revel {w.Revel:F2}, ground {w.Ground:F2}, charter {w.Charter:F2}, calling {w.Calling:F2}";

    /// <summary>What a mind's choice met in the auction, for the minds' line.</summary>
    public static string MindsMet()
    {
        if (MindOffers == 0)
        {
            return "no mind's choice has been weighed";
        }

        // Read every five minutes, so a sorted copy costs nothing worth saving.
        List<(string Kind, long Times)> beaten = [];

        foreach (var (kind, n) in _mindBeatenBy)
        {
            beaten.Add((kind, n));
        }

        beaten.Sort((a, b) => b.Times.CompareTo(a.Times));

        var by = "";

        for (var i = 0; i < beaten.Count && i < 3; i++)
        {
            by += (i == 0 ? " (by " : ", ") + $"{beaten[i].Kind} {beaten[i].Times}";
        }

        if (by.Length > 0)
        {
            by += ")";
        }

        return $"their choices weighed {MindOffers} times: {MindTopped} on top, {MindOutscored} outscored{by}, {MindRefused} refused outright";
    }

    /// <summary>
    /// Whether a prowl put down for the fighting it went looking for is counted finished rather than dropped. See <c>Commit</c>.
    ///
    /// <para>
    /// A switch because it changes what the finished share means rather than what any bot does: off, a prowl that met a
    /// band is dropped exactly as it was before 15.09.2026. The census prints the share with prowls and without them either
    /// way, so the builds on both sides of this stay comparable.
    /// </para>
    /// </summary>
    public static bool MetCounts { get; set; } = true;

    /// <summary>Prowls counted finished because the fighting they went for found them first. See <see cref="MetCounts"/>.</summary>
    public static long Met { get; private set; }

    /// <summary>Walks home counted finished because work found the bot on the way. See <see cref="MetCounts"/> and <c>Commit</c>.</summary>
    public static long MetHome { get; private set; }

    /// <summary>Prowls and strolls counted finished because a real offer outbid them. See the note at <c>metFiller</c>.</summary>
    public static long MetFiller { get; private set; }

    /// <summary>
    /// Whether the work that displaces a prowl is the fighting the prowl went looking for: a band, a company taking the bot
    /// in, a hunt, or a rescue against something within <see cref="BotMobile.NoticeRange"/> of the bot — and not selling,
    /// buying, cooking or anything else that merely paid better.
    /// </summary>
    private static bool Meets(IBotWilful bot, BotDeed next)
    {
        if (next?.Kind is not { } kind)
        {
            return false;
        }

        if (kind.InsensitiveEquals(BotBand.Trade) || kind.InsensitiveEquals(BotEnlist.Trade) || kind.InsensitiveEquals(BotSlay.Trade))
        {
            return true;
        }

        return kind.InsensitiveEquals(BotRescue.Trade)
               && next.Foe is { Deleted: false } foe
               && bot?.Self is { } body
               && body.InRange(foe.Location, BotMobile.NoticeRange);
    }

    /// <summary>
    /// Whether this proposer may be put to this bot at all.
    ///
    /// <para>
    /// <b>Only the free rung is a choice.</b> Mending, flight and the answer to being hit live below the
    /// auction and fire whether or not anything is deciding — a list that could switch those off would be a
    /// class that has sworn not to survive. So the gate is asked of the free rung and nowhere else, and
    /// every class but one leaves the list empty and passes in a single comparison.
    /// </para>
    /// </summary>
    private static bool Sworn(IBotWilful bot, BotStanding rung, IBotProposer proposer)
    {
        if (rung != BotStanding.Free)
        {
            return true;
        }

        var only = (bot?.Self as BotMobile)?.Class?.Sworn;

        if (only is not { Length: > 0 })
        {
            return true;
        }

        for (var i = 0; i < only.Length; i++)
        {
            if (string.Equals(only[i], proposer.Name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Takes a proposer. Called by the subsystem that owns the work, from its own module's start.
    ///
    /// Registration is not reset when the world reloads: a proposer is code, not state.
    /// </summary>
    public static void Offer(IBotProposer proposer)
    {
        if (proposer == null)
        {
            return;
        }

        for (var i = 0; i < _proposers.Count; i++)
        {
            if (!string.Equals(_proposers[i].Name, proposer.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            logger.Error(
                "Two proposers are both called {Name} ({First} and {Second}); the second is ignored",
                proposer.Name,
                _proposers[i].GetType().Name,
                proposer.GetType().Name
            );

            return;
        }

        _proposers.Add(proposer);

        logger.Information(
            "Proposer {Name} offers work on the {Rung} rung",
            proposer.Name,
            proposer.Rung
        );
    }

    /// <summary>Proposers registered, whatever rung they serve.</summary>
    public static IReadOnlyList<IBotProposer> Proposers => _proposers;

    /// <summary>
    /// One decision. Called on the bot's own beat, and it is the only entry point the bot needs.
    ///
    /// <para>
    /// It does at most three things: settles anything that has ended, advances what is held, and — when the
    /// review is due — holds an auction. Advancing is cheap and happens every beat, because an undertaking
    /// has to be able to notice that it has arrived; choosing is not, and does not.
    /// </para>
    /// </summary>
    /// <summary>Loop time spent choosing and advancing work, in milliseconds, and the turns it was spent over.</summary>
    public static double SpentMs { get; private set; }

    public static long Decisions { get; private set; }

    /// <summary>One turn's worth of deciding, timed by the caller. See BotMobile.Beat.</summary>
    public static void Spent(long ticks)
    {
        TurnMs = ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        SpentMs += TurnMs;
        Decisions++;
    }

    /// <summary>
    /// <b>What the last turn's deciding cost, and where (01.10.2026, build 382).</b> The slow-tick line named the costliest turn's
    /// bot and the kind of work it held — "Quillathe's at 235ms on mine", 80 such ticks in 35 minutes on build 381, every one
    /// "in the bots' turns" — and nothing said whether that was the work's own Advance, the auction, or which of the fifty-odd
    /// proposers asked in it. The ground sweep, the one cost the log itemises, was not it: no "Swept" line stood beside those
    /// turns. Read by BotBeat straight after the turn, before the next bot overwrites them.
    /// </summary>
    public static double TurnMs { get; private set; }

    /// <summary>The auction's share of <see cref="TurnMs"/>; nought when none ran.</summary>
    public static double TurnAuctionMs { get; private set; }

    /// <summary>The costliest proposer asked on the last turn, and what it cost; null when no auction ran.</summary>
    public static string TurnProposer { get; private set; }

    public static double TurnProposerMs { get; private set; }

    /// <summary>Clears the last turn's figures. Called by BotBeat before each turn, so a turn that decides nothing reads nought.</summary>
    public static void BeginTurn()
    {
        TurnMs = 0.0;
        TurnAuctionMs = 0.0;
        TurnProposer = null;
        TurnProposerMs = 0.0;
    }

    /// <summary>What one proposer, or one kind of work's Advance, has cost the loop in all. See <see cref="Costliest"/>.</summary>
    private sealed class Cost
    {
        public double Ms;

        public double WorstMs;

        public long Times;

        public string WorstBot;
    }

    private static readonly Dictionary<string, Cost> _proposerCosts = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, Cost> _advanceCosts = new(StringComparer.OrdinalIgnoreCase);

    private static void Charge(Dictionary<string, Cost> book, string name, double ms, string bot)
    {
        name ??= "?";

        if (!book.TryGetValue(name, out var cost))
        {
            cost = new Cost();
            book[name] = cost;
        }

        cost.Ms += ms;
        cost.Times++;

        if (ms > cost.WorstMs)
        {
            cost.WorstMs = ms;
            cost.WorstBot = bot;
        }
    }

    /// <summary>The five costliest entries of a book by total, for the census. Cold path: read every five minutes.</summary>
    private static string Costliest(Dictionary<string, Cost> book)
    {
        if (book.Count == 0)
        {
            return "nothing yet";
        }

        List<KeyValuePair<string, Cost>> rows = new(book);

        rows.Sort(static (a, b) => b.Value.Ms.CompareTo(a.Value.Ms));

        using var say = ValueStringBuilder.Create(256);
        var total = 0.0;
        var times = 0L;

        for (var i = 0; i < rows.Count; i++)
        {
            total += rows[i].Value.Ms;
            times += rows[i].Value.Times;
        }

        say.Append($"{total:F0}ms over {times}: ");

        for (var i = 0; i < rows.Count && i < 5; i++)
        {
            if (i > 0)
            {
                say.Append(", ");
            }

            var cost = rows[i].Value;

            say.Append($"{rows[i].Key} {cost.Ms:F0}ms/{cost.Times} (worst {cost.WorstMs:F0}ms, {cost.WorstBot ?? "?"})");
        }

        return say.ToString();
    }

    public static void Decide(IBotWilful bot)
    {
        if (!Deciding)
        {
            return;
        }

        var resolve = bot?.Resolve;
        var body = bot?.Self;

        if (resolve == null || body == null || body.Deleted)
        {
            return;
        }

        // <b>Some classes are not run by this layer at all, and the King's Rangers are the first of them.</b>
        // Everything here is an auction: proposers offer, the bot weighs, the best offer wins and displaces
        // what it was doing. That is exactly right for a population of tradesmen and exactly wrong for a
        // standing patrol under orders — every skirmish outbid the sweep and threw it away, every ending left
        // the company with nothing in hand, and no amount of adjusting prices or sworn lists fixed it,
        // because the problem was being in the auction at all. They have their own keeper: see BotRangers.
        if ((body as BotMobile)?.Class is { Bidding: false })
        {
            return;
        }

        var now = Core.TickCount;

        Census(now);

        var minutes = resolve.Urges.Since(now);
        var standing = BotLadder.Standing(bot);

        resolve.Standing = standing;

        if (standing == BotStanding.Dead)
        {
            Settle(bot, BotEnding.Died);

            return;
        }

        // Anything above the bot's own business. Set aside rather than abandoned — the road is still
        // underneath, exactly as it is in the journey — unless something on this rung has work of its own.
        if (standing < BotStanding.Busy)
        {
            Aside(bot, resolve, standing, now, minutes);

            return;
        }

        resolve.Aside = false;

        if (resolve.Deed != null)
        {
            resolve.Urges.Held(minutes);

            Advance(bot, resolve);
        }
        else
        {
            resolve.Urges.Idle(minutes);
        }

        // Hands empty, before the auction fills them: the one moment an itch that waited for the work in hand can be
        // scratched without dropping anything. See BotSkulk.WaitMs.
        if (resolve.Deed == null && bot is BotMobile { Tired: false } free)
        {
            BotSkulk.Urge(free, now);
        }

        // Free to shop, on a clock. The dwell is a floor under this, and the two mean different things — one
        // is how often it looks, the other how soon it may change its mind.
        //
        // <b>Not a tired bot: it finishes what it holds and takes on nothing new,</b> and BotRest takes it out of the
        // world the first time it is free. Skipping the review as well as the idle auction is the point — a better
        // offer taken at the last minute is how a player who is logging off ends up out all night.
        if ((resolve.Due || now - resolve.ReviewedTick >= (resolve.Deed == null ? IdleMs : ReviewMs))
            && bot is not BotMobile { Tired: true })
        {
            Auction(bot, resolve, BotStanding.Free, now);
        }
    }

    /// <summary>
    /// The walk an undertaking asked for has ended one way or another. Called from the tick with whatever
    /// <see cref="BotWalk.Advance"/> returned.
    ///
    /// <para>
    /// Only the two endings matter here, and both are the journey saying the place cannot be worked towards:
    /// <see cref="BotWalkResult.Refused"/> is proof there is no way, <see cref="BotWalkResult.GaveUp"/> is a
    /// bot admitting it is getting nowhere. Arriving is not reported, because the undertaking asks the
    /// journey itself — it is the one that knows what arriving means for its own stage.
    /// </para>
    /// </summary>
    /// <summary>Creatures deferred because the road to them was refused. See <see cref="Note"/>.</summary>
    public static long Unreachable { get; private set; }

    /// <summary>
    /// Endings that were not allowed to price the ground, because the bot never got there.
    ///
    /// Its own number, because a gate without one either breaks a denominator or hides its own mistakes: if
    /// this ever grows to swallow the ordinary failures, the shard would stop learning anything about what
    /// work is worth, and the only way to see that would be this counter beside the others.
    /// </summary>
    public static long Unpriced { get; private set; }

    /// <summary>
    /// Unreached endings that were not written into <see cref="BotRefused"/>, because what could not be
    /// reached was somebody rather than somewhere. See <see cref="Note"/>.
    /// </summary>
    public static long Unblamed { get; private set; }

    /// <summary>
    /// Places written into <see cref="BotRefused"/> when a proven refusal of the road sent the work somewhere else rather than
    /// ending it. See <see cref="Note"/>.
    /// </summary>
    public static long Rerouted { get; private set; }

    /// <summary>
    /// Refusals of a road the work in hand had not sent - a company's station, nearly always - left uncharged to that work.
    /// See <see cref="Note"/>.
    /// </summary>
    public static long Foreign { get; private set; }

    /// <summary>
    /// Raised whenever a piece of work is finished, for anything outside this assembly that is counting.
    ///
    /// Only on <see cref="BotEnding.Done"/>: a revel that counted attempts would be won by whoever failed
    /// fastest, which is the same defect as judging a trade on takings it never had time to earn.
    /// </summary>
    public static Action<Mobile, string> Completed { get; set; }

    /// <summary>Raised whenever a piece of work is taken up: the bot, the work, and the reckoning that won it.</summary>
    public static Action<Mobile, BotDeed, string> Started { get; set; }

    /// <summary>Raised whenever a piece of work ends, whichever of the four ways: the bot, the work, the ending, the reason, the minutes, the rate and the coin.</summary>
    public static Action<Mobile, BotDeed, BotEnding, string, double, double, int> Ended { get; set; }

    /// <summary>Every kind of work and how it has ended, for the page. Game loop only.</summary>
    public static IEnumerable<(string Kind, long Taken, long Finished, long Failed, long Dropped, long Died)> Tallies()
    {
        foreach (var (kind, t) in _tallies)
        {
            yield return (kind, t.Taken, t.Finished, t.Failed, t.Dropped, t.Died);
        }
    }

    public static void Note(IBotWilful bot, BotWalkResult result)
    {
        if (!Deciding || result is not (BotWalkResult.Refused or BotWalkResult.GaveUp or BotWalkResult.Stalled))
        {
            return;
        }

        var resolve = bot?.Resolve;
        var deed = resolve?.Deed;

        if (deed == null)
        {
            return;
        }

        // Kept before it is cleared: it is the only record of where the walk was actually headed, and the
        // log line below is the whole reason anybody will know which place was refused.
        var refused = resolve.Sent;

        // <b>A road this work never sent is not this work's road.</b> Sent is written only when the Will sends a walk for
        // the work in hand, and a company writes its stations into the same journey (BotJourney.Bottom, build 41) - so a
        // refused station arrived here as the refusal of work that had asked for no walk at all, and was failed "no way
        // through to what it was following, which is gone". That sentence came in the same second as the same bot's
        // "has dropped ... (station)" 60 times in 61 on build 45 and 9 in 9 on build 51, 2 to 61 a session all night; in
        // the Orc Caves at 06:58 on 15.09.2026 Nessa Ashdown lost her own bandaging seven times in fourteen seconds that
        // way, until the breaker rested her mending. The company deals with its own stations; the work in hand goes on.
        if (refused.Follow == null && refused.Where == Point3D.Zero)
        {
            Foreign++;

            return;
        }

        // The refusal as an event, with where the walk was headed, before the road is cleared.
        if (bot.Self is { } roadless)
        {
            var journey = (roadless as BotMobile)?.Journey;

            BotEvents.PathFailed(
                roadless,
                deed.Kind,
                refused.Note ?? journey?.Reason,
                result.ToString(),
                roadless.Location,
                refused.Follow?.Location ?? refused.Where,
                0.0,
                0,
                journey?.PlansSinceCloser ?? 0,
                refused.Follow != null,
                false
            );
        }

        resolve.Sent = default;

        var bent = false;

        try
        {
            bent = deed.Bend(bot);
        }
        catch (Exception e)
        {
            logger.Error(e, "Undertaking {Kind} threw when told its way was blocked; it is given up", deed.Kind);
        }

        if (bent)
        {
            // Trouble is an event, and an event is what lifts a hold. See Holding — and BotDeed.BendIsTrouble for the
            // work whose bends are routine, which keeps its hold.
            if (deed.BendIsTrouble)
            {
                resolve.Bent = true;
            }

            // <b>And the place written down, which until now only an errand that ended unreached ever did.</b> A bend
            // takes the work somewhere else and so never reaches Settle, the one caller of BotRefused.Refuse; on build
            // 50, between 06:04 and 06:25 on 15.09.2026, miners were turned away 53 times and sent to another seam,
            // 47 of them on the ridge west of Britain, and (1132, 1516, 41), (1092, 1552, 34) and (1092, 1572, 44)
            // each turned four different miners away, because nobody had written the first refusal anywhere a
            // chooser could read. Only the two proven refusals of a road (W3) and only a place: somebody is not
            // somewhere.
            // Not a red's, which walks round the towns and so proves nothing about a place. See BotOutlaw.KeptRound.
            if (result is BotWalkResult.Refused or BotWalkResult.GaveUp && refused.Follow == null && refused.Where != Point3D.Zero
                && !BotOutlaw.KeptRound(bot.Self))
            {
                BotRefused.Refuse(deed.Map, refused.Where);
                Rerouted++;
            }

            if (Chatty)
            {
                // <b>The same care as the sentence below, and it was missing here for a fortnight.</b> A
                // walk that was following something now dead has a null Follow and a Where that was never
                // set, and this arm printed the two of them as the bare coordinate (0, 0, 0). Underground
                // it is not a rare case but the ordinary one — a party's quarry dies while somebody is
                // still walking to it — and thirty of these went into the log on the evening of 11.09.2026
                // reading as though a delve were walking to the origin of the map.
                logger.Information(
                    "{Name} could not get to {Where} and is trying elsewhere for {Deed}",
                    bot.Self?.Name,
                    refused.Follow != null
                        ? refused.Follow.Name
                        : refused.Where == Point3D.Zero
                            ? "whatever it was following, which is gone"
                            : refused.Where.ToString(),
                    deed
                );
            }

            return;
        }

        // <b>The one exit from an undertaking that said nothing about itself.</b> Every other failure carries
        // a sentence — the shelf was bare, the odds were hopeless, nothing of it was in sight — and this one,
        // which is simply "the way there does not exist and this work has no second place to try", printed
        // the bare words "failed at mine" and stopped. Thirty-five of them in twelve minutes on the morning of
        // 25.08.2026, across unload, mine and hunt, and the only way to find out what any of them meant was
        // to go and read a BotWalk line somewhere above it and match up the timestamps by eye.
        //
        // The destination is right here and costs nothing to say. Named after the creature where there is
        // one, because "no way through to a skeleton" is a fact about a fight and "no way through to
        // (1424, 1468, 9)" is a fact about the ground, and they are chased down differently.
        // <b>A follow whose target has gone prints a place that never existed.</b> The two arms below are
        // "there is a creature" and "there is a spot", and an errand that was following something which has
        // since been deleted is neither: Follow has gone null, Where was never set for it, and the sentence
        // came out as "no way through to (0, 0, 0)". Nought is not a coordinate, it is the absence of one —
        // and a message that prints it sends the next reader hunting for a coordinate bug that is not there.
        // It cost half an hour of exactly that on 26.08.2026.
        // <b>And where the somebody was standing, because 9,507 quarrels ended "no way through to Orin"
        // on 13.09.2026 and not one of them said where Orin was.</b> Inside a hall, on a roof, across the
        // graveyard wall and in the next town are four different defects, and the name alone tells them
        // apart from nothing. The region and the house are the two facts that decide which it is.
        // <b>And which of the three endings it was, because they are three different facts.</b> Refused is
        // the pathfinder's proof that there is no way; GaveUp is a chase that drew twelve plans and never got
        // nearer - a target walking away as fast as the bot walks after it; Stalled is a bot that could not
        // take a step. On 13.09.2026 every one of them printed "no way through", and the road into the
        // graveyard was looked for by hand before the pathfinder was asked directly and answered in 0.1ms
        // that it was thirty-six tiles long. The words were the defect.
        var could = result switch
        {
            BotWalkResult.GaveUp => "could not get nearer to",
            BotWalkResult.Stalled => "could not take a step towards",
            _ => "no way through to"
        };

        var where = refused.Follow != null
            ? $"{could} {refused.Follow.Name} at {refused.Follow.Location}"
              + (refused.Follow.Region?.Name is { Length: > 0 } region ? $" in {region}" : "")
              + (Server.Multis.BaseHouse.FindHouseAt(refused.Follow) != null ? " inside a house" : "")
            : refused.Where == Point3D.Zero
                ? $"{could} what it was following, which is gone"
                : $"{could} {refused.Where}";

        // <b>And the creature is deferred, because no way through is no way through for anybody.</b> The same
        // ruling BotSlay already makes about a quarry it cannot see from the tile beside it: "as true for the
        // next bot along as for this one, which is precisely what the shared list means". A refused road was
        // not making it. On the night of 02-03.09.2026, in two hours and three quarters, 88 of the 830
        // hunts that ended did so on "no way through to" — a rat, a crow, a cat: creatures inside buildings
        // in Britain that every passing bot walked to in turn and none could reach. The mark lapses in two
        // minutes, so this defers them rather than writing them off, and a creature that wanders back out is
        // hunted again.
        //
        // Creatures only. A mend that cannot reach the bot it was going to bandage is a fact about the road
        // and about a friend, and the quarry list is not where either belongs — and the compiler settles that
        // for us, since a bot is a PlayerMobile and can never be a BaseCreature.
        if (refused.Follow is BaseCreature)
        {
            BotQuarry.Shun(refused.Follow);
            Unreachable++;
        }
        else if (refused.Follow != null)
        {
            // The same note about one of ours, kept on its own list because a patient is not quarry. See
            // BotMend.Beyond: 209 of the 230 refused roads of that night were a healer walking to somebody
            // it could not reach, and the picker had no way to know it had just tried.
            BotMend.Beyond(refused.Follow);
            Unreachable++;
        }

        // <b>And the ground is blamed only when the ground was what was walked to.</b> A peddle is an errand
        // to a person and a hunt is an errand to a creature; both of them keep walking while they are being
        // walked to, and both already have a record of their own two arms above this one - BotQuarry.Shun
        // and BotMend.Beyond. Aric, standing at (1323, 1372) with eighteen ribs to sell, failed to reach six
        // different buyers in ninety seconds on 08.09.2026, every one of them standing in Britain's market;
        // written as ground, that is the population teaching itself that its own market square cannot be
        // reached, and BotGround.Nearest would then refuse every bot on the island its bank.
        //
        // The deed's Where is the place a buyer happened to be standing when the errand was written, which is
        // a fact about a minute rather than about a road. Counted, because a gate without a bucket hides its
        // own mistakes.
        // <b>And never when the bot never moved.</b> A hundred attempts at stepping with the bot on the same
        // tile is a fact about that tile and about that bot — a mount out of steps, a pack too heavy, a
        // doorway full of people — and it is true of every destination on the island at once, so writing it
        // against this one is writing it against the wrong thing entirely.
        var ground = refused.Follow == null && result != BotWalkResult.Stalled;

        if (!ground)
        {
            Unblamed++;
        }

        // <b>A far road refused is a fact about where the bot stands, and it was only ever written about where it was
        // going.</b> Fenna of The Crown, north-west of the river at (1086, 1420) on 24.09.2026, took four supply runs to
        // four shopkeepers in Britain in one minute (02:12:29–02:13:20), each refused "no way round it was found in
        // 150ms" and each a different name, so neither the shopkeeper's note nor the breaker saw one reason twice. The
        // footing note is the one that fits (BotAppraisal.BecalmedMs): long walks from about here are not offered for
        // ten minutes, and near work is, which is what moves the bot. Not underground, where refusals are ordinary, and
        // not a chase given up: a creature that walked away says nothing about the ground the bot is standing on.
        var toward = refused.Follow?.Location ?? refused.Where;

        if ((result == BotWalkResult.Refused || result == BotWalkResult.GaveUp && refused.Follow == null)
            && toward != Point3D.Zero && bot.Self is { } walker && !BotDungeon.Under(walker.Location))
        {
            var off = Math.Max(Math.Abs(walker.X - toward.X), Math.Abs(walker.Y - toward.Y));

            BotAppraisal.Becalm(resolve, walker.Location, off, off);
        }

        Settle(bot, BotEnding.Failed, where, unreached: true, ground);
    }

    /// <summary>
    /// Something hit this bot. Called from <c>OnDamage</c>, beside <see cref="BotThreat.Decide"/>.
    ///
    /// <b>Told rather than observed</b>, and that is the whole of it: a caster strikes from eight tiles and
    /// never closes, so a bot that learns it is under attack by looking for something adjacent never learns
    /// it at all.
    /// </summary>
    public static void Hurt(IBotWilful bot)
    {
        var resolve = bot?.Resolve;

        if (resolve != null)
        {
            resolve.HurtTick = Core.TickCount;
            resolve.Struck = true;
        }
    }

    /// <summary>
    /// A creature that has taken this bot as its combatant is coming for it and has not struck yet. Called from
    /// <c>OnMovement</c>; the rung it puts the bot on is the one a blow does. See <see cref="BotLadder.MenacedMs"/>.
    /// </summary>
    public static void Menaced(IBotWilful bot)
    {
        var resolve = bot?.Resolve;

        if (resolve != null)
        {
            resolve.MenaceTick = Core.TickCount;
            resolve.Menaced = true;
        }
    }

    /// <summary>Bots told to look up at once because somebody near needed them. See <see cref="Alert"/>.</summary>
    public static long Alerts { get; private set; }

    /// <summary>
    /// <b>Something has happened near this bot that it may be the answer to: it looks up now, not at its next review
    /// (06.10.2026, react14).</b> The review clock (<see cref="ReviewMs"/>, fifteen seconds for a bot with work in hand) is what
    /// lets a bot cross a continent, and it was also the only moment a healer ten tiles from a wounded friend was asked whether
    /// it would mend it. This marks the auction due and asks for the decision between turns (<c>BotMobile.Hurry</c>); what the
    /// bot then does is the auction's own answer, with every hold that protects the work in hand still standing.
    /// </summary>
    public static void Alert(IBotWilful bot)
    {
        var resolve = bot?.Resolve;

        if (resolve == null)
        {
            return;
        }

        resolve.Due = true;
        Alerts++;

        (bot.Self as BotMobile)?.Hurry();
    }

    /// <summary>The bot died. Called from <c>OnDeath</c>: whatever it was doing ended, and badly.</summary>
    public static void Died(IBotWilful bot) => Settle(bot, BotEnding.Died);

    /// <summary>
    /// This bot is going away for good. Called when it is deleted, and it exists for one reason: the census
    /// counts what is being worked on, and a bot deleted mid-undertaking would leave its work in that count
    /// for ever. A total that is never released does not look wrong — it looks like a busier population.
    /// </summary>
    public static void Forget(IBotWilful bot)
    {
        // What the breaker held against this bot goes with it, whether or not it was holding any work. See BotBreaker.
        BotBreaker.Forget(bot?.Self);
        BotTreadmill.Forget(bot?.Self);
        BotYield.Forget(bot?.Self);
        BotWalk.ForgetChase(bot?.Self);

        var resolve = bot?.Resolve;
        var deed = resolve?.Deed;

        if (deed == null)
        {
            return;
        }

        Count(deed.Kind, -1);

        resolve.Deed = null;
        resolve.Sent = default;
        resolve.Paused = null;
        resolve.Under = null;
    }

    /// <summary>
    /// Above the bot's own business: hold what is held, and let the rung offer something if it can.
    /// </summary>
    private static void Aside(
        IBotWilful bot,
        BotResolve resolve,
        BotStanding standing,
        long now,
        double minutes
    )
    {
        // Not idle: something is happening to it. Boredom is for bots with nothing to do, not for bots in
        // trouble.
        resolve.Urges.Held(minutes);

        // A squad is not a rung with work on it — the squad *is* the work, and it rebases the journey itself.
        // Joining one will be an undertaking when something proposes it.
        if (standing != BotStanding.Bound && Auction(bot, resolve, standing, now))
        {
            return;
        }

        if (resolve.Deed == null)
        {
            // <b>Nothing in hand and something hitting it.</b> That is not a bot too busy being attacked to
            // go shopping — it is a bot standing in a fight with no plan at all, which is the state this rung
            // was written to prevent rather than to produce. The rung lasts eight seconds past the last blow,
            // so a bot that has just given a fight up spends them motionless in the middle of it, and
            // ordinary work is what gets it out: a walk to a mine is a walk away.
            if (standing == BotStanding.Hunted)
            {
                Auction(bot, resolve, BotStanding.Free, now);
            }

            return;
        }

        // <b>Work this rung handed out is not work set aside — it is what the bot is supposed to be doing at
        // this instant, and it was the one thing that never happened.</b> An undertaking is only ever
        // advanced from the Busy path, so a rung could offer a bot its own rescue and then decline to carry
        // it out on the grounds that a bot in trouble is not busy: the medic proposed a bandage, the bandage
        // became the bot's undertaking, and a bot at a third of its health then stood perfectly still holding
        // a plan to heal itself until something killed it. Every rung above Busy had the same hole.
        //
        // Hunted is included whatever handed the work out, because that rung has never been about the work —
        // its own note says it means "no business shopping for work right now" — and suspending a fight
        // because a fight is happening left the whole of BotSlay unreachable for as long as anything was
        // hitting the bot: no giving ground, no casting, no flight, no corpse gone through.
        // <b>And a stand is carried on under the failing line (06.10.2026, react14).</b> It was taken on the rung above, so
        // "work this rung handed out" did not cover it and the fight a bot had decided to see through was parked the moment its
        // health fell far enough to need it most. A flight or a bandage offered on this rung still takes it. See BotDefender.Stand.
        if (standing == BotStanding.Hunted
            || resolve.Took <= standing
            || standing == BotStanding.Failing && resolve.Deed is BotRescue { ToTheEnd: true }
            || standing == BotStanding.Bound && resolve.Deed.Alongside)
        {
            resolve.Aside = false;

            Advance(bot, resolve);

            return;
        }

        // <b>Here, and not before the branch above, and getting that wrong cost an afternoon of my own
        // making.</b> The labour clock must not run on an undertaking nobody is turning the handle of —
        // AsideCapMs is the clock for waiting, and two clocks on one deed is the project's oldest mistake.
        // But the branch above <em>does</em> advance the deed on elevated rungs, so resetting at the top of
        // the aside path reset it on every beat of a bot that was working perfectly normally while something
        // hit it. Godric held a hunt for seventeen minutes with the backstop watching and rejuvenated four
        // times a second. Only the two genuine parkings below reset it.
        if (!resolve.Aside)
        {
            resolve.Aside = true;
            resolve.AsideTick = now;
            resolve.StirredTick = now;

            return;
        }

        if (now - resolve.AsideTick < AsideCapMs)
        {
            resolve.StirredTick = now;

            return;
        }

        logger.Information(
            "{Name} gave up {Deed}: set aside {Minutes:F0} minutes while {Standing}",
            bot.Self?.Name,
            resolve.Deed,
            (now - resolve.AsideTick) / 60000.0,
            standing
        );

        Settle(bot, BotEnding.Dropped);
    }

    /// <summary>Asks what is held what to do now, and does exactly that and nothing else.</summary>
    private static void Advance(IBotWilful bot, BotResolve resolve)
    {
        // <b>A member of a company at the body of one of its fallen is the company's until the body is up or given up (02.10.2026).</b>
        // The company walks it there and it raises from where it stands; its own work, the leader's errand included, would write
        // another walk into the same slot on this beat. Forty seconds at most a body. See BotSquadRaise.
        if (bot is IBotSquadMember { Squad: { } company } && bot.Self is { } raiser && company.Raising(raiser))
        {
            RaiseHeld++;

            return;
        }

        var deed = resolve.Deed;
        BotDoing doing;
        var began = System.Diagnostics.Stopwatch.GetTimestamp();

        try
        {
            doing = deed.Advance(bot);
        }
        catch (Exception e)
        {
            // Loud and contained, as everywhere in this assembly: one subsystem's mistake must not take the
            // bot's beat with it, and it must not be quiet either.
            logger.Error(e, "Undertaking {Kind} threw while being advanced; it is given up", deed.Kind);

            Settle(bot, BotEnding.Failed);

            return;
        }
        finally
        {
            // What each kind of work costs to advance, kept apart from what its proposer costs to offer. See TurnMs.
            Charge(
                _advanceCosts,
                deed.Kind,
                (System.Diagnostics.Stopwatch.GetTimestamp() - began) * 1000.0 / System.Diagnostics.Stopwatch.Frequency,
                bot.Self?.Name
            );
        }

        switch (doing.Kind)
        {
            case BotDoingKind.Walk:
                if (doing.Map == null)
                {
                    logger.Error("Undertaking {Kind} asked for a walk to nowhere; it is given up", deed.Kind);

                    Settle(bot, BotEnding.Failed);

                    return;
                }

                var journey = bot.Journey;

                if (journey == null)
                {
                    // A wilful bot without a journey cannot be sent anywhere. A programming mistake rather
                    // than a situation, and it is said by name instead of throwing inside the beat.
                    logger.Error(
                        "{Name} has no journey, so {Kind} cannot be walked; it is given up",
                        bot.Self?.Name,
                        deed.Kind
                    );

                    Settle(bot, BotEnding.Failed);

                    return;
                }

                // Only when it is somewhere new. Rebasing throws the plan away, and an undertaking that says
                // "walk to the forge" on every beat would otherwise buy a search on every beat and never
                // arrive from further off than a few tiles.
                //
                // <b>Or when the road no longer carries it, and until 14.09.2026 nothing asked.</b> The bottom of the
                // road has two owners: this, and a company stationing its members, which rebases the same slot. A
                // company that stationed a bot and then let it go left its "sweep" there, one tile off and so arrived
                // for ever, while the bot's own walk - unchanged, so never sent again - was gone. Kestrel and Lorcan
                // stood four minutes each on the roof by Britain's stables that way at 22:15, and a bot parked within
                // eight tiles of a stablemaster buys a horse inside two seconds, which is why it looked like the horse.
                // An interruption at the bottom is somebody's fight and is left to end; an empty road is sent again.
                var carried = journey.Bottom;
                var lost = carried == null
                           || !carried.Interruption
                           && !(carried.Map == doing.Map
                                && ReferenceEquals(carried.Follow, doing.Follow)
                                && (doing.Follow != null || carried.Where == doing.Where)
                                && carried.Arrival.Tiles == doing.Arrival.Tiles);

                if (!doing.Matches(resolve.Sent) || lost)
                {
                    if (doing.Matches(resolve.Sent))
                    {
                        Resent++;
                    }

                    // Rebase, never Begin. Begin clears the queue, which would wipe the fight the bot is in
                    // the middle of; the bottom of the queue is where the bot belongs and the top is what is
                    // happening to it.
                    if (doing.Follow != null)
                    {
                        journey.Rebase(doing.Map, doing.Follow, doing.Arrival, doing.Note ?? deed.Kind, doing.KeepsUp);
                    }
                    else
                    {
                        journey.Rebase(doing.Map, doing.Where, doing.Arrival, doing.Note ?? deed.Kind);
                    }

                    resolve.Sent = doing;
                    resolve.Nearest = int.MaxValue;
                    resolve.Trudged = 0;
                    resolve.Started = -1;

                    // <b>Inside the rebase, not beside it, and the difference is a bot standing still for
                    // twenty minutes.</b> The labour clock was reset on every walk answer, on the reasoning
                    // that a step is a sign of life. A step is; the same step is not. An undertaking that
                    // asks for one unchanged destination beat after beat has not moved through its chain at
                    // all, and it kept the clock young for ever — Perri held a rescue for twenty-one minutes
                    // that way with the backstop watching and never firing.
                    //
                    // Here it fires only when the order actually changed, which is the same test the rebase
                    // itself uses, and that is precisely the definition of the undertaking getting somewhere:
                    // a new place to be, a new stage, a new leg. The longest honest journey on this shard is
                    // a couple of minutes, so nothing that is really walking can age out.
                    resolve.StirredTick = Core.TickCount;

                    return;
                }

                // <b>The same order, beat after beat, with the bot no nearer than it was — and until now
                // nothing anywhere counted that.</b> Two errands out of a dozen wrote this rule for
                // themselves — <c>BotDig</c> and <c>BotProwl</c>, both after a night of it — and the ones
                // that did not were left to the stall watch, which is four minutes away and cures the
                // symptom by cancelling the work. On 04.09.2026 the whole of what was left of standing
                // still was that: Kelda four minutes into "taking 1 Katana to Aislinn", Merrick into
                // "taking 10 Leather to Abra", Rowan into "taking 10 Raw Ribs to Iman", none of them a
                // company, none of them a fight, all of them a walk to a shopkeeper that had stopped
                // closing. Written here so it is one rule rather than a twelfth copy.
                //
                // <b>Only a fixed place, never a follow.</b> Work that walks after something moving points
                // at the target's own tile every beat, so "no nearer than it was" is true of it while it is
                // winning — that is this project's oldest false alarm and it is fenced out by construction.
                if (doing.Follow == null && bot.Self is { Deleted: false } walker && walker.Map == doing.Map)
                {
                    // A walk the work itself is holding where it stands — a leader for its column (BotJourney.Hold, 01.10.2026,
                    // build next) — is not a walk that stopped closing: nothing is counted against it until the hold lapses.
                    if (journey.Held)
                    {
                        return;
                    }

                    // By the road the bot is walking: along the chart's route while it holds one to this place
                    // (BotJourney.RoadLeft, BotChart), since the way round a river leads away from the place for a
                    // thousand steps; straight otherwise, as before.
                    var away = journey.RoadLeft(walker.Location, doing.Where);

                    // A route drawn since is a new measure: the best so far starts again with it.
                    if (resolve.RouteStamp != journey.RouteStamp)
                    {
                        resolve.RouteStamp = journey.RouteStamp;
                        resolve.Nearest = int.MaxValue;
                        resolve.Trudged = 0;
                    }

                    if (resolve.Started < 0)
                    {
                        resolve.Started = away;
                    }

                    if (away < resolve.Nearest)
                    {
                        resolve.Nearest = away;
                        resolve.Trudged = 0;
                    }
                    else if (++resolve.Trudged >= TrekLimit)
                    {
                        Trudges++;

                        // <b>Said differently from BotProwl's own give-up on purpose.</b> That one reads
                        // "got no nearer than N tiles to (x, y)" and this one used to read the same sentence
                        // with one extra word, so a grep for either found both and a count of one was a count
                        // of two trades' worth of quite different problems. This is the watchdog above every
                        // undertaking — the walk stopped closing, whatever the work was — and the trade's own
                        // give-up is a decision that trade made about its own ground.
                        //
                        // <b>And a walk that closed nothing at all is a fact about where the bot stands, written
                        // before the ending because the ending hands the resolve to the next piece of work.</b>
                        // See BotAppraisal.BecalmedMs for who reads it and for Fenna, who wrote it.
                        BotAppraisal.Becalm(resolve, walker.Location, resolve.Started, resolve.Nearest);

                        Settle(
                            bot,
                            BotEnding.Failed,
                            $"the walk stopped closing {away} tiles short of ({doing.Where.X}, {doing.Where.Y}) after {TrekLimit} beats, having set out {resolve.Started} off and got no nearer than {resolve.Nearest}"
                        );
                    }
                }

                return;

            case BotDoingKind.Work:
                // Nothing at all, and that is the answer. A bot at a counter, at a forge or at a vein is
                // standing still on purpose, its journey has no plan, and nothing judges it for progress.
                //
                // <b>Except that an undertaking which never answers anything else is invisible and
                // immortal, and two of them were found in one afternoon.</b> Three tailors held a craft the
                // engine had quietly stopped serving — CraftItem.Craft takes an action lock and returns in
                // silence when it is already held — and stood at their benches for two hours; Merrick held a
                // rescue for fifty-seven minutes without dying, failing, or writing a single line. Both read
                // as "working" in every summary, because this is the answer that means exactly that.
                //
                // So the licence stands and gets an outer edge. Any other answer — a step, a finish, a
                // failure — is a sign of life and pushes the clock forward; only an unbroken run of this one
                // ages. LabourMs is set far above any honest stretch of standing still, so nothing that is
                // really working ever meets it, and a bot the world has stopped talking to is back in the
                // population within the quarter hour instead of at the end of the day.
                //
                // <b>Work that rides beside a company and has stopped walking leaves the road to the company.</b> Sent is
                // the last walk this work asked for, and Note tells a refusal of the work's own road from a refusal of a
                // company's station by it; a rally that walked "to the war company" and fell in kept that walk in Sent,
                // so a refused station was charged to it as its own. At 09:41:06 on 24.09.2026 five of The Blade's
                // company — Kerrin, Aric, Corwin, Delwyn, Garrow — failed "no way through" to the places their stations
                // had been set, in the ranks, in the same second. Only for work that runs Alongside: work that walks and
                // works by turns keeps its walk, or every turn would buy a fresh search.
                if (deed.Alongside && resolve.Sent.Kind == BotDoingKind.Walk)
                {
                    resolve.Sent = default;
                }

                if (Core.TickCount - resolve.StirredTick >= LabourMs)
                {
                    if (deed.Still)
                    {
                        OwnClock++;

                        return;
                    }

                    Settle(bot, BotEnding.Failed, $"nothing has come of this in {LabourMs / 60000} minutes");

                    return;
                }

                return;

            case BotDoingKind.Done:
                Settle(bot, BotEnding.Done, doing.Note);

                return;

            case BotDoingKind.Failed:
                Settle(bot, BotEnding.Failed, doing.Note);

                return;

            default:
                logger.Error("Undertaking {Kind} said nothing when asked what to do; it is given up", deed.Kind);

                Settle(bot, BotEnding.Failed);

                return;
        }
    }

    /// <summary>
    /// Collects what is on offer for this rung, scores it, and takes the best if it is worth changing to.
    /// Returns whether anything was taken on.
    /// </summary>
    private static bool Auction(IBotWilful bot, BotResolve resolve, BotStanding rung, long now)
    {
        var held = resolve.Deed;

        // Too soon to change our mind on purpose. Not applied to the rungs above: a bot that cannot walk
        // needs to put something down now, not in half a minute.
        //
        // <b>Noted rather than returned on, and that is a change with a reason.</b> Refusing to look at all
        // meant the dwell could not tell "a better field to dig" from "an ogre eight tiles away": both were
        // simply not asked about for half a minute, and a bot that is not asked walks past. So the offers are
        // collected either way and the floor is applied at the end, where the one thing that can lift it —
        // <see cref="BotDeed.Pressing"/> — is known.
        var fresh = held != null && rung == BotStanding.Free && now - resolve.SinceTick < Dwell(held);

        resolve.ReviewedTick = now;
        resolve.Due = false;

        _offers.Clear();

        // What the free rung had for this bot, kept rather than dropped. See BotResolve.Offered: the whole
        // list is computed here anyway, and asking a second time elsewhere is not a free question.
        var keep = rung == BotStanding.Free;

        if (keep)
        {
            resolve.Offered.Clear();
            resolve.OfferedTick = now;
        }

        var asked = 0;
        var largestOutlay = 0;

        for (var i = 0; i < _proposers.Count; i++)
        {
            var proposer = _proposers[i];

            if (proposer.Rung != rung)
            {
                continue;
            }

            // <b>Work this bot's class is not allowed to be offered at all, which is not the same as work it
            // would score badly.</b> See BotClass.Sworn: a class whose errands are chosen for reasons the
            // arithmetic cannot see cannot be expressed by scoring, because a price high enough to win is a
            // rigged auction and an honest one loses to every hunt on the island. So the offer is never made.
            //
            // Counted rather than silent, and counted here rather than inside the proposers: every proposer
            // on this shard tallies its own refusals by reason, and a bot that is never asked would otherwise
            // vanish from all of them at once — a Baron doing nothing would look exactly like a Baron with
            // nothing to do.
            if (!Sworn(bot, rung, proposer))
            {
                Unsworn++;

                continue;
            }

            asked++;

            BotDeed offer = null;
            var began = System.Diagnostics.Stopwatch.GetTimestamp();

            try
            {
                offer = proposer.Propose(bot);
            }
            catch (Exception e)
            {
                logger.Error(e, "Proposer {Name} threw while offering work; it is skipped", proposer.Name);
            }

            // Two timestamp reads a proposer, some fifty an auction, thirty auctions a second: nothing against the turns. See TurnMs.
            var cost = (System.Diagnostics.Stopwatch.GetTimestamp() - began) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

            TurnAuctionMs += cost;
            Charge(_proposerCosts, proposer.Name, cost, bot.Self?.Name);

            if (cost > TurnProposerMs)
            {
                TurnProposerMs = cost;
                TurnProposer = proposer.Name;
            }

            if (offer?.Map == null)
            {
                continue;
            }

            if (offer.Outlay > largestOutlay)
            {
                largestOutlay = offer.Outlay;
            }

            if (keep)
            {
                resolve.Offered.Add(proposer.Name);
            }

            Named(offer.Kind, proposer.Name);

            _offers.Add(offer);
        }

        if (asked == 0)
        {
            // Nobody at all answers this rung. On the free rung that is the same fact as "nothing was worth
            // doing" and has to be counted as it, or a shard with no proposers registered would report a
            // contented population.
            if (rung == BotStanding.Free && held == null)
            {
                Nothing(resolve, now, "nobody answers this rung at all");
            }

            Unserved(rung);

            return false;
        }

        // Need is what the plans on offer cost against what is in the purse. Computed here because this is
        // the only place both halves are known, and zero when nothing on offer costs anything.
        // What it has free, not what it owns: money its wants hold is promised, and a bot short of it is sent to work that pays
        // (02.10.2026, build 391; Patrick: "if there is no money, hunting naturally prevails"). See BotYield.Spendable.
        resolve.Urges.Weigh(BotYield.Spendable(bot.Self), largestOutlay);

        // <b>A bot that cannot take a step decides by order, not by price.</b> The carrying ceiling has been
        // a factor in the score since 08.09.2026 and the factor does not reach: BotAppraisal multiplies work
        // that needs a step by a fiftieth, and then the geometric mean takes the fifth root of the product,
        // so a fiftieth arrives at the score as 0.46. Lysa the Woodsman on 09.09.2026 stood on one tile for
        // three minutes with ten refused roads behind her, taking chop at "508 x 0.45 ... x load 0.02" =
        // 229/min over the unloading that would have freed her, and the population made ninety-five such
        // choices in the session. A multiplier is the right shape for "a poor idea" and the wrong shape for
        // "this cannot be started", and no value of it is safe: small enough to lose is one root away from
        // being a veto, and a veto here is the monument the factor was written to replace. See
        // BotAppraisal.StoppedShare.
        //
        // So the ceiling ranks rather than prices. Work that needs no step is compared only against other
        // work that needs no step, and everything else is compared below it - which is what the factor was
        // meant to say and could not. The floor the factor was given is kept exactly: when nothing on the
        // table can be done standing still, every offer is rank nought and this is the auction it has always
        // been, so a heavy bot with nothing to sell is still given the best of what is left and still tries.
        // Nothing changes at all for a bot under its ceiling - every rank is nought and the comparison
        // collapses to the one it was.
        var grounded = BotLadder.Overloaded(bot.Self);

        BotDeed best = null;
        var bestScore = 0.0;
        var bestRank = -1;
        var bestWeigh = default(BotWeigh);
        BotDeed second = null;
        var secondScore = 0.0;
        var secondRank = -1;
        var viable = 0;
        string firstVeto = null;

        BotDeed minded = null;
        var mindedScore = 0.0;
        var mindedWeigh = default(BotWeigh);
        string mindedVeto = null;

        // The best work asked of a crafter, kept beside the winner: a crafter's dig gives way to it (03.10.2026, econ13). See
        // BotCraftFirst.
        var crafter = rung == BotStanding.Free && BotCraftFirst.Running && BotCraftFirst.Crafter(bot);
        BotDeed work = null;
        var workScore = 0.0;
        var workRank = -1;
        var workWeigh = default(BotWeigh);

        for (var i = 0; i < _offers.Count; i++)
        {
            var offer = _offers[i];
            var score = BotAppraisal.Weigh(bot, offer, Share(offer.Kind), out var weigh, out var veto);

            if (offer.Kind.StartsWith("mind-", StringComparison.Ordinal))
            {
                minded = offer;
                mindedScore = score;
                mindedWeigh = weigh;
                mindedVeto = veto;
            }

            if (score <= 0.0)
            {
                firstVeto ??= veto;

                continue;
            }

            viable++;

            var rank = grounded && offer.Standing ? 1 : 0;

            if (crafter && (rank > workRank || rank == workRank && score > workScore) && BotCraftFirst.Asked(offer))
            {
                work = offer;
                workScore = score;
                workRank = rank;
                workWeigh = weigh;
            }

            if (rank > bestRank || rank == bestRank && score > bestScore)
            {
                second = best;
                secondScore = bestScore;
                secondRank = bestRank;
                best = offer;
                bestScore = score;
                bestRank = rank;
                bestWeigh = weigh;
            }
            else if (rank > secondRank || rank == secondRank && score > secondScore)
            {
                second = offer;
                secondScore = score;
                secondRank = rank;
            }
        }

        // <b>A crafter makes what is asked of it before it digs (03.10.2026, econ13; Patrick: "a crafter makes the order first, then
        // goes to mine").</b> A dig or a prospect that won gives way to the best asked work worth anything — an order's piece, a
        // delivery, its trade's material, its own material out of the bank box — never past a rank, and nothing else is reordered.
        // The dig is the runner-up the take line prints. See BotCraftFirst.
        // Counted where the choice is committed, below: a review that keeps the work in hand changes nothing.
        var gaveWay = false;

        if (crafter && best != null && BotCraftFirst.Mining(best) && work != null && workRank >= bestRank)
        {
            gaveWay = true;

            second = best;
            secondScore = bestScore;
            secondRank = bestRank;
            best = work;
            bestScore = workScore;
            bestRank = workRank;
            bestWeigh = workWeigh;
        }

        // The runner-up under this ordering is the best of everything the winner outranked, so this says
        // "the order overturned a price" and nothing else. Counted here rather than where the rank is worked
        // out: a rank is computed for every offer a heavy bot is made, and means nothing until one wins.
        if (bestRank > 0 && secondScore > bestScore)
        {
            Grounded++;
        }

        if (minded != null)
        {
            MindOffers++;

            if (mindedScore <= 0.0)
            {
                MindRefused++;

                // The first, and one in MindSampleEvery after it: which gate, in its own words.
                if (MindSampleEvery > 0 && MindRefused % MindSampleEvery == 1)
                {
                    logger.Information(
                        "A mind's choice weighed: {Name}'s {Kind} was refused: {Veto}",
                        bot.Self.Name,
                        minded.Kind,
                        mindedVeto ?? "no reason given"
                    );
                }
            }
            else if (minded == best)
            {
                MindTopped++;
            }
            else if (best != null)
            {
                MindOutscored++;
                _mindBeatenBy.TryGetValue(best.Kind, out var beaten);
                _mindBeatenBy[best.Kind] = beaten + 1;

                // Both weighings in full, because a count of who won says nothing about which factor decided it.
                if (MindSampleEvery > 0 && MindOutscored % MindSampleEvery == 1)
                {
                    logger.Information(
                        "A mind's choice weighed: {Name}'s {Kind} at {Score:F0} ({Mind}) lost to {Best} at {BestScore:F0} ({Won})",
                        bot.Self.Name,
                        minded.Kind,
                        mindedScore,
                        Factors(mindedWeigh),
                        best.Kind,
                        bestScore,
                        Factors(bestWeigh)
                    );
                }
            }
        }

        if (best == null)
        {
            if (held == null && rung == BotStanding.Free)
            {
                Nothing(
                    resolve,
                    now,
                    _offers.Count == 0
                        ? $"{asked} proposers asked, not one of them had anything to offer"
                        : $"{asked} proposers asked, {_offers.Count} offered work, and it was refused: {firstVeto}"
                );
            }

            return false;
        }

        // How the work in hand is being displaced, if it is, and what it was worth at that moment: both are
        // printed on the take line and the drop line, which are the only record of why a bot walked away.
        string how = null;
        var heldNow = 0.0;
        string heldVeto = null;

        if (held != null)
        {
            // <b>The same order has to apply to what is already in hand, or the ranking above does nothing
            // for the bot that needs it.</b> Lysa was not choosing with an empty hand - she was holding the
            // chop. Ranking the offers alone would have left the winner facing the dwell and the margin, and
            // unloading at 74/min against a chop at 229/min with a quarter added for stubbornness loses
            // every one of those comparisons: the ordering would have been right and unreachable. So the
            // rank is read on both sides and settles it before the margin is consulted at all.
            //
            // It runs both ways on purpose. A bot that has taken the unloading does not lose it again the
            // moment something better paid is offered, which is the same defect wearing the other face.
            var heldRank = grounded && held.Standing ? 1 : 0;

            if (bestRank < heldRank)
            {
                return false;
            }

            if (bestRank > heldRank)
            {
                Dislodged++;

                how = "dislodged";
                heldNow = BotAppraisal.Weigh(bot, held, Share(held.Kind), out _, out heldVeto, inHand: true);
            }
            else
            {
                // Something that will not wait, and is not more of what the bot is already doing. The second
                // half of that matters as much as the first: without it a hunter swaps quarry every time a
                // second creature wanders inside the notice, which is the impulsive bot the margin was
                // written against wearing the word "urgent".
                // A company's member hit and losing runs or mends, its enlistment put down for it. See Aid.
                var aid = Aid(held, best, rung);
                var jumps = aid || best.Pressing(bot) && !held.Kind.InsensitiveEquals(best.Kind);

                // <b>Work with a purchase in the middle of it is not weighed against a better price at all.</b>
                // Dropping a hunt costs some walking; dropping a guild's supply run after the shop leg has
                // turned its coin into goods in this bot's pack, and the auction has no way to know that,
                // because a score says what work is worth from here and never what has already been spent
                // getting here. Only what will not wait may still take it - the survival rungs and the
                // carrying ceiling above, both of which mean the promise cannot be kept anyway. See
                // BotDeed.Committed for the evening this cost.
                if (held.Committed && !jumps)
                {
                    Kept++;

                    return false;
                }

                // <b>Paperwork does not take a bot off work; it waits for the next choice.</b> See BotDeed.Paperwork for the
                // brewers who put their batches down for an order that took them a beat to post.
                if (best.Paperwork && !jumps)
                {
                    Filed++;

                    return false;
                }

                if (fresh && !jumps)
                {
                    return false;
                }

                // <b>The same work offered again is not something happening either.</b> See BotDeed.Repeats: a
                // flight offered to a bot already in flight, a mending of the patient already being mended. The
                // work in hand is already doing what the offer proposes, and replacing it resets its clock and
                // writes a drop: 21 flee drops in half an hour on 28.09.2026 were a flight replaced by the same
                // flight at a higher price, and the press path had this guard while the auction did not.
                if (held.Repeats(best))
                {
                    Repeated++;

                    return false;
                }

                // The margin, and it is the difference between a bot with intentions and a bot with
                // impulses. Waived for what will not wait — the comparison is then a plain one, "is this
                // worth more than what I am doing", with no bonus for stubbornness on either side.
                heldNow = BotAppraisal.Weigh(bot, held, Share(held.Kind), out _, out heldVeto, inHand: true);

                var heldScore = heldNow * (jumps ? 1.0 : BotAppraisal.Inertia);

                if (bestScore <= heldScore * (jumps ? 1.0 : SwitchMargin))
                {
                    return false;
                }

                // <b>A second guess of the same kind is not something happening.</b> See BotDeed.Guess. Past the
                // margin, because a margin between two guesses is exactly the bonus that only moves the point where they
                // trade places — to the end of the dwell, where 32 prowls on build 54 turned round.
                if (GuessHolds && held.Guess && best.Guess && held.Kind.InsensitiveEquals(best.Kind))
                {
                    SecondGuesses++;

                    return false;
                }

                // <b>And then the hold, which asks the question the margin never could: has anything happened,
                // or has something merely been offered?</b> Everything above is arithmetic between two prices,
                // and a price that clears it clears it whether the bot is half a minute or half a trip into the
                // work in hand. That is Kinny and Georgeff's cautious agent, and on this shard it walked off three
                // trips in ten at the two-minute mark and went back to the same trade four times in five.
                // Steadfast work is held for its own reckoning against ordinary offers now; what gets through is
                // what will not wait, a call from outside the bot's own business, and trouble in the work itself
                // (inside Holding). Counted here, past the margin, so the number is the decisions the hold
                // changed and not the reviews it sat through.
                var summoned = !jumps && best.Summons && !held.Kind.InsensitiveEquals(best.Kind);

                if (!jumps && !summoned && Holding(resolve, held, rung, now))
                {
                    Held++;
                    TallyOf(held.Kind).Held++;

                    return false;
                }

                how = (rung, jumps, summoned) switch
                {
                    (not BotStanding.Free, _, _) => "interrupted",
                    (_, true, _) => "jumped",
                    (_, _, true) => "summoned",
                    _ => "outbid"
                };
            }
        }

        if (held != null && Aid(held, best, rung))
        {
            Aided++;
        }

        // A crafter's take: asked work in a dig's place, or a dig with nothing asked worth anything (econ13). See BotCraftFirst.
        if (gaveWay)
        {
            BotCraftFirst.Gave(best);
        }
        else if (crafter && BotCraftFirst.Mining(best))
        {
            BotCraftFirst.Kept();
        }

        Commit(bot, resolve, rung, best, bestWeigh, second, secondScore, now, _offers.Count, viable, firstVeto, how, heldNow, heldVeto);

        return true;
    }

    private static void Commit(
        IBotWilful bot,
        BotResolve resolve,
        BotStanding rung,
        BotDeed deed,
        BotWeigh weigh,
        BotDeed instead,
        double insteadScore,
        long now,
        int table,
        int viable,
        string refused,
        string how = null,
        double heldNow = 0.0,
        string heldVeto = null
    )
    {
        var dropped = resolve.Deed;
        var gone = "";

        if (dropped != null)
        {
            var heldFor = (now - resolve.SinceTick) / 60000.0;
            var tally = TallyOf(dropped.Kind);

            switch (how)
            {
                case "jumped":
                    Jumped++;
                    tally.Jumped++;

                    break;

                case "summoned":
                    Summoned++;
                    tally.Jumped++;

                    break;

                case "interrupted":
                    Interrupted++;
                    tally.Jumped++;

                    break;

                case "outbid":
                    Outbid++;
                    tally.Outbid++;

                    break;
            }

            // What the bot walked away from, in the numbers that decided it: how far into its own reckoning, what
            // it was taken at and what it was worth by then. "Dropped mine" named the work and nothing about the
            // choice, so a trip abandoned at two minutes with the ore in the pack read exactly like a trip
            // abandoned because the seam was empty.
            // <b>And what made it worth nought, when something did.</b> Work in hand weighed at nought is put down for any
            // offer at all, and the line said "worth 0/min" and nothing about why: Wynn's own bandaging, put down nine
            // times in 47 seconds on build 48, read as a margin fault until two weighings a second apart were compared.
            var vetoed = heldVeto == null ? "" : "; vetoed: " + heldVeto;
            var account =
                $"{how ?? "replaced"} by {deed.Kind} at {weigh.Score:F0}/min after {heldFor:F1} of {resolve.Expected:F1} minutes reckoned, taken at {resolve.TakenAt:F0}/min and worth {heldNow:F0}/min by then{vetoed}";

            // <b>A prowl that meets the fighting it went for has done its work, and the drop said it had not.</b> A prowl is
            // a walk to find a fight. When a band forms round the bot, a company takes it in, a hunt is offered or a rescue is
            // called against something beside it, the purpose is met — and on build 63, 29 prowls "jumped by band" and 11
            // "interrupted by rescue" in 45 minutes counted against the finished share. Settled finished, but credited to no
            // place: the bot never stood on its dart, so the ground is neither cleared of a refusal nor priced, and nobody's
            // contest is told. Fable's option, taken on Patrick's word (DECISIONS §6); MetCounts puts it back.
            // <b>And a walk home that meets work on the way has done its work too.</b> BotHomeward is a few coins a minute so
            // that it loses to every real offer: it is what a bot does when nothing here is worth doing, and its purpose is to
            // be somewhere that offers something. On build 94 (20:41–21:26) 81 walks home were taken, 39 arrived and 42 were
            // dropped — 14 for an escort and 10 for a house call, most of them healers hired or called on the way, the median
            // 0.8 minutes in — which was 28% of the window's 151 drops, each one the designed outcome counted against the
            // finished share. Only for an offer the bot chose or a call from outside; not for a rung above (flight, a full
            // pack), which is trouble rather than work. Counted apart (MetHome), so the share can be read both ways.
            var metHome = MetCounts && dropped is BotHomeward && how is "outbid" or "summoned";

            // <b>And a prowl or a stroll that is outbid has found what it was for (29.09.2026).</b> Both are what a bot does when
            // nothing here is worth doing — a prowl is a few coins a minute on purpose, so that it loses to every real offer —
            // and a real offer arriving is their designed end, as it is for the walk home above. Between 16:37 and 19:00 on
            // builds 313–315, 62 endings were fillers "dropped: outbid by restock/peddle/glean", the largest single lever on
            // the finished share. Counted apart (MetFiller), so the share can be read both ways; MetCounts puts it back.
            var metFiller = MetCounts && how is "outbid" && (dropped is BotProwl || dropped is BotRounds);
            var met = metHome || metFiller || MetCounts && dropped is BotProwl && Meets(bot, deed);

            // <b>Put down, not thrown away, when what took the bot off it would not wait.</b> An interruption is
            // not a change of mind: the seam is still there after the rescue and the ore is still in the pack
            // after the fight. Only steadfast work, only one piece at a time, and never for an outbidding.
            // <b>A company's member runs or binds its wounds and comes back to its company (01.10.2026, build next).</b> A levied
            // member's own work is already put down for the enlistment (BotSquads.Enrol presses it), so when the enlistment was
            // jumped by a flight or a bandage (Aid) there was no second place to put it down, and it was dropped: 92 enlistments
            // "dropped: interrupted by flee" and 38 "by mend" on build 384 (00:42–09:55), each a member out of its company for
            // good. The work beneath is kept beneath (Under) and the enlistment is put down over it; taken up again, the
            // enlistment goes on with that work still put down below it, as before.
            var stack = Resume && StackAid && dropped is BotEnlist && resolve.Paused != null && resolve.Under == null
                        && how is "jumped" or "interrupted" && Aid(dropped, deed, rung);

            var pause = !met
                        && Resume
                        && dropped.Resumes
                        && (resolve.Paused == null || stack)
                        && how is "jumped" or "summoned" or "dislodged" or "interrupted";

            if (met)
            {
                if (metHome)
                {
                    MetHome++;
                }
                else if (metFiller)
                {
                    MetFiller++;
                }
                else
                {
                    Met++;
                }

                // As a drop would: a change of mind takes whatever was put down with it.
                if (how == "outbid" && resolve.Paused != null)
                {
                    DropPaused(bot, resolve, $"the bot changed its mind to {deed.Kind}");
                }

                Settle(bot, BotEnding.Done, $"met by {deed.Kind}: {account}", unpause: false, credit: false);

                gone = $"; finished {dropped}, met by {deed.Kind}";
            }
            else if (pause)
            {
                if (resolve.Paused != null)
                {
                    if (resolve.Under != null)
                    {
                        // Two deep already (07.10.2026, second pass of arena27 review B): a brawl put down for a mid-bout
                        // bandage would overwrite the work sheltered beneath the arena's wait, and that work would never be
                        // settled. The one on top is settled; the work beneath stays where it is.
                        var beneath = resolve.Under;
                        resolve.Under = null;
                        DropPaused(bot, resolve, $"put down two deep for {deed.Kind}");
                        resolve.Under = beneath;
                    }
                    else
                    {
                        resolve.Under = resolve.Paused;
                        resolve.Paused = null;
                        Stacked++;
                    }
                }

                Pause(bot, resolve, deed.Kind, now, keeps: deed.Keeps);

                gone = $"; put down {dropped} to take up again ({account})";
            }
            else
            {
                resolve.DroppedKind = dropped.Kind;
                resolve.DroppedTick = now;

                // A change of mind takes whatever was put down with it: the bot is not coming back through the
                // interruption it was in the middle of, so it is not coming back to what that interrupted either.
                if (how == "outbid" && resolve.Paused != null)
                {
                    DropPaused(bot, resolve, $"the bot changed its mind to {deed.Kind}");
                }

                // Chosen against, not failed. The takings are recorded honestly and no blame attaches to the
                // place — the first version's answer here was a five-minute ban on the whole activity.
                Settle(bot, BotEnding.Dropped, account, unpause: false);

                gone = $"; dropped {dropped}";
            }
        }

        // Coming straight back to what it dropped, which is the measure of whether dropping it was a decision.
        if (resolve.DroppedKind != null
            && deed.Kind.InsensitiveEquals(resolve.DroppedKind)
            && now - resolve.DroppedTick < ReturnMs
            && dropped?.Kind.InsensitiveEquals(deed.Kind) != true)
        {
            Returned++;
            TallyOf(deed.Kind).Returned++;
            resolve.DroppedKind = null;
        }

        resolve.Deed = deed;
        resolve.Took = rung;
        resolve.Stake = BotYield.Take(bot, deed);

        // Told it won, so it can speak for what it needs before the next bot is asked. See BotDeed.Taken.
        try
        {
            deed.Taken(bot);
        }
        catch (Exception e)
        {
            logger.Error(e, "Undertaking {Kind} threw when it was taken on; carried on without it", deed.Kind);
        }

        // The stopwatch: a decision taken is what stops the clock an event started. See BotReflex (06.10.2026).
        BotReflex.Took(bot, deed);

        // Bank before a trip out of town (02.10.2026, build next). Wealth does not move, so the stake above is unchanged. See BotPurse.
        BotPurse.BeforeTrip(bot, deed);

        resolve.SinceTick = now;
        resolve.ReviewedTick = now;
        resolve.StirredTick = now;
        resolve.Due = false;
        resolve.Aside = false;
        resolve.Sent = default;
        resolve.TakenAt = weigh.Score;
        resolve.Bent = false;
        resolve.Expected = Math.Max(0.1, deed.Minutes)
                           + (bot.Self is { } walker && walker.Map == deed.Map
                               ? BotAppraisal.Travel(walker.Location, deed.Where)
                               : 0.0);
        // <b>How many were on the table, because absence of a runner-up read as a rich choice.</b> The take
        // line named a second place only when one existed, so "chosen over five better ideas" and "the only
        // thing anybody offered" printed identically. On 02.09.2026 Quill took acquire twelve times inside two
        // seconds, each at 0/min, and went from 400gp to 8gp; not one of those lines said whether anything
        // else had been offered at all, and the answer changes what the finding is.
        // <b>And what was refused, by name, because "4 of 5 offers worth anything" names nothing.</b> Merrick,
        // 242 of 240 stones and no stamina, took cook, hunt and mine for eight minutes on 14.09.2026 with one
        // offer refused every review, and neither the log nor the door could say whether the refused one was
        // the unload that would have freed it. The first veto is what an idle bot is already told; a busy
        // bot now carries it too.
        resolve.Because = refused == null
            ? $"{weigh.Describe()}; {viable} of {table} offers worth anything"
            : $"{weigh.Describe()}; {viable} of {table} offers worth anything; refused: {refused}";

        // Cleared here for the same reason Because is written here: a stale one reads as the present one.
        resolve.Empty = null;
        resolve.Urges.Fruitful();

        Count(deed.Kind, 1);
        Taken++;

        var taken = TallyOf(deed.Kind);

        taken.Taken++;
        taken.Auctioned++;

        // Told to whoever is listening — the voices and the page — before the log, and whether or not the log is chatty.
        var self = bot.Self;

        if (self != null)
        {
            BotEvents.Post(
                "work",
                self,
                $"took on {deed.Kind}: {deed.Stage ?? deed.Kind}",
                null,
                BotJson.Object(
                    w =>
                    {
                        w.WriteString("work", deed.Kind);
                        w.WriteString("ending", "took");
                        w.WriteString("stage", deed.Stage ?? "");
                        w.WriteNumber("expects", Math.Round(resolve.TakenAt, 1));
                    }
                )
            );

            // Counted by the part of the day it was taken in, for the Day: line (PLAN-life A1).
            BotDay.Took(self, deed);

            try
            {
                Started?.Invoke(self, deed, resolve.Because);
            }
            catch (Exception e)
            {
                logger.Error(e, "A listener threw when {Kind} was taken up", deed.Kind);
            }
        }

        if (!Chatty)
        {
            return;
        }

        logger.Information(
            "{Name} took on {Deed}: {Why}{Instead}{Dropped}",
            bot.Self?.Name,
            deed,
            resolve.Because,
            instead == null ? "" : $"; over {instead} at {insteadScore:F0}/min",
            gone
        );
    }

    /// <summary>
    /// The takings are counted, the ledger is told, and the undertaking is let go. The one place an
    /// undertaking ends, whichever of the four ways it ended.
    /// </summary>
    /// <param name="why">
    /// What the undertaking said as it ended, in its own words.
    ///
    /// <b>It used to be thrown away, and that was a hole in the one thing this project is built to do.</b>
    /// Every undertaking already explains itself — "the fire would not take the ore", "that stall is empty
    /// now", "nothing left to mend with" — and the settlement logged only the arithmetic, so a hundred and
    /// forty-five identical failures were indistinguishable from each other and from bad luck. A reason that
    /// is written and then discarded is worse than no reason at all: it looks like the question was answered.
    /// </param>
    /// <summary>
    /// Takes a piece of work off a bot that has stopped getting anywhere with it. See <c>BotStall</c>.
    ///
    /// <para>
    /// <b>A work that answers "still going" for ever is immortal, and this shard has produced several.</b>
    /// Every leg of every errand must end in done, failed, or a step — but a step that cannot be taken is
    /// none of the three: the bot asks the same walk, the journey gives it up, the work asks for the same
    /// walk again, and the pair of them will do that until the world reloads. A crafter stood four minutes
    /// on "after Cloth" that way, and nothing in either subsystem was wrong on its own.
    /// </para>
    ///
    /// <para>
    /// Ended as a failure rather than quietly dropped, so the ledger learns the place was no good and the
    /// bot is less inclined to take the same errand to the same spot again. That is the same treatment any
    /// other unreachable destination gets, and it is what stops this from being a loop with a longer period.
    /// </para>
    ///
    /// <para>
    /// <b>"The ledger learns the place was no good" means <c>Ledger.Note</c> here and not
    /// <see cref="BotDeed.Bend"/>, and that distinction was worth a reverted change to find out.</b> Note
    /// folds a nought-a-minute reading into the band; Bend writes the outright caution that
    /// <c>Ledger.Cautious</c> reads, and it is reached only from
    /// <see cref="Note(IBotWilful,BotWalkResult)"/> — the walker's own refusal. A journey that
    /// <em>follows</em> a mobile never refuses, so a shopkeeper nobody can reach is followed until this
    /// method takes the errand away, and the shop is never cautioned. Calling Bend from here looks like the
    /// missing line and is not: this island has four fires and <b>two counters</b>, so cautioning one on a
    /// single stall takes half of a bot's counters away for the span, and "can reach nothing from here, and
    /// it is standing at home" went from one to three in the matched window on 05.09.2026 while nothing got
    /// better. The claimed benefit was not there either — no bot repeats a vendor even without it, because
    /// the queue at an unreachable shopkeeper is a different bot each time and a per-bot ledger cannot see
    /// that. Whatever the cure for a followed mobile is, it is not a stronger mark in one bot's own book.
    /// </para>
    /// </summary>
    /// <param name="unreached">
    /// True when the errand is being taken away because the bot was not getting anywhere — the stall watch,
    /// the debugger's shake. Those are road failures as surely as a refused plan is, and pricing the ground
    /// on them is the same mistake. See <see cref="Settle"/>.
    /// </param>
    /// <summary>Pieces of work put into a bot's hands by <see cref="Press"/> rather than won at auction.</summary>
    public static long Pressed { get; private set; }

    /// <summary>Presses let alone because the work in hand was already the same work (<see cref="BotDeed.Repeats"/>).</summary>
    public static long Repressed { get; private set; }

    /// <summary>Offers let alone at auction because the work in hand was already the same work (<see cref="BotDeed.Repeats"/>).</summary>
    public static long Repeated { get; private set; }

    /// <summary>
    /// Puts a piece of work into a bot's hands without an auction: a duel it is called to, a cell it is put in, a
    /// murderer's victim's fight back.
    ///
    /// <para>
    /// <b>The auction chooses what a bot wants; this is what the world does to it.</b> Everything else on this shard
    /// is an offer weighed against every other offer, which is right for work and wrong for a summons: a bot called
    /// into a championship ring, or thrown into a cell, is not choosing. The bookkeeping is <c>Commit</c>'s — the
    /// stake, the clocks, the counts, the take line — so the ledger and the census read a pressed deed exactly as a
    /// chosen one, and the deed's own <c>Committed</c> keeps the next review from bidding it away. Whatever the bot
    /// held is dropped with the reason; a paused errand stays paused for afterwards.
    /// </para>
    /// </summary>
    public static bool Press(IBotWilful bot, BotDeed deed, string why, bool stack = false)
    {
        var resolve = bot?.Resolve;
        var body = bot?.Self;

        if (resolve == null || body is not { Deleted: false, Alive: true } || deed == null)
        {
            return false;
        }

        var now = Core.TickCount;
        var putDown = "";

        if (resolve.Deed != null)
        {
            if (ReferenceEquals(resolve.Deed, deed) || resolve.Deed.Repeats(deed))
            {
                Repressed++;

                return true;
            }

            var held = resolve.Deed;

            // <b>Put down, not thrown away, when the bot is pressed.</b> A duel called by the tournament, the Baron's patrol,
            // a temptation and a sentence are all things that will not wait and none of them is a change of mind about the
            // work in hand, which is the line A11 draws; the auction's own interruptions have put steadfast work down since
            // 14.09.2026, and this door into the same bot dropped it. On build 94 at 21:09:10 on 16.09.2026 Nyla let a board
            // errand four minutes into its walk go "pressed to duel: a duel against Marek", and the errand went back on the
            // board for the next bot to start over. The pressed work settles like any other, and settling takes this up again.
            if (Resume && held.Resumes && resolve.Paused == null)
            {
                putDown = $"; put down {held} to take up again";
                PressPaused++;

                Pause(bot, resolve, deed.Kind, now, deed.Shelters, deed.Keeps);
            }
            else if (stack && Resume && held.Resumes && resolve.Paused != null && resolve.Under == null)
            {
                // <b>Put down over work already put down (06.10.2026, react16).</b> The slot beneath (Under) was made for a
                // company's enlistment put down for a flight (build 384) and nothing about it is the enlistment's: whatever
                // is taken up again finds the work beneath it and puts it back (Unpause, Surface). Only for a caller that
                // asks, which is a call to somebody's aid: a calling never throws work away.
                putDown = $"; put down {held} to take up again, over {resolve.Paused.Deed} already put down";
                resolve.Under = resolve.Paused;
                resolve.Paused = null;
                Stacked++;
                PressPaused++;

                Pause(bot, resolve, deed.Kind, now, deed.Shelters, deed.Keeps);
            }
            else
            {
                Settle(bot, BotEnding.Dropped, $"pressed to {deed.Kind}: {why}", unpause: false);
            }
        }

        resolve.Deed = deed;
        resolve.Took = BotStanding.Busy;
        resolve.Stake = BotYield.Take(bot, deed);

        try
        {
            deed.Taken(bot);
        }
        catch (Exception e)
        {
            logger.Error(e, "Undertaking {Kind} threw when it was pressed; carried on without it", deed.Kind);
        }

        BotReflex.Took(bot, deed);

        // A press is work taken up too, for the Day: line's table by part of the day: the walks to bed are presses (PLAN-life A1).
        BotDay.Took(bot.Self, deed);

        // A company's call out of town is a trip like any other. See BotPurse.BeforeTrip.
        BotPurse.BeforeTrip(bot, deed);

        resolve.SinceTick = now;
        resolve.ReviewedTick = now;
        resolve.StirredTick = now;
        resolve.Due = false;
        resolve.Aside = false;
        resolve.Sent = default;
        resolve.TakenAt = deed.Expects;
        resolve.Bent = false;
        resolve.Expected = Math.Max(0.1, deed.Minutes)
                           + (body.Map == deed.Map ? BotAppraisal.Travel(body.Location, deed.Where) : 0.0);
        resolve.Because = $"pressed: {why}";
        resolve.Empty = null;
        resolve.Urges.Fruitful();

        Count(deed.Kind, 1);
        Taken++;
        Pressed++;
        TallyOf(deed.Kind).Taken++;

        logger.Information("{Name} was pressed to {Deed}: {Why}{PutDown}", body.Name, deed, why, putDown);

        return true;
    }

    public static void Abandon(IBotWilful bot, string why, bool unreached = true)
    {
        if (bot?.Resolve?.Deed == null)
        {
            return;
        }

        Settle(bot, BotEnding.Failed, why, unreached);
    }

    /// <summary>
    /// Puts down whatever the bot holds for a reason outside the work — a player logging off, see <see cref="BotRest"/> —
    /// so it is counted as dropped, not as failed.
    /// </summary>
    public static void PutDown(IBotWilful bot, string why)
    {
        if (bot?.Resolve?.Deed == null)
        {
            return;
        }

        Settle(bot, BotEnding.Dropped, why, unpause: false);
    }

    /// <summary>
    /// Ends the undertaking in hand and writes down what it came to.
    /// </summary>
    /// <param name="unreached">
    /// <b>True when the errand ended because the bot could not get there, and false when it ended on what
    /// the work itself did.</b> The distinction is the whole of the repair made on the morning of
    /// 08.09.2026, and it took a night of measurement to see.
    ///
    /// <para>
    /// A walk that never arrived produced nothing, so <see cref="BotYield.Settle"/> hands back nought a
    /// minute — and three separate records then wrote that nought down as a fact about the world:
    /// <c>Ledger.Note</c> priced the ground for this bot, <c>BotCommons.Note</c> priced it for everybody,
    /// and <c>BotCommons.Claimed</c> corrected the trade's own expectation shard-wide. Cooking was reading
    /// <b>-309.9 gold a minute in the middle of Britain</b>; herbs failed 2940 times in one session and
    /// dragged their own expectation towards zero, which is why the auction stopped offering them; and
    /// "nothing was worth doing" climbed past seven thousand in a single session while the shard sat on
    /// twenty thousand gold and full stalls.
    /// </para>
    ///
    /// <para>
    /// None of those three numbers is about money. A road that does not exist says nothing whatever about
    /// what a fire is worth to cook at, or what a patch of woods yields, and letting it speak on those
    /// subjects poisons a district for every trade at once — the ledger bands ground 64 tiles wide.
    /// <see cref="BotLedger.Beware"/> is the record that <em>is</em> about roads, it is still written below,
    /// and it is read by the samplers that choose where to go.
    /// </para>
    /// </param>
    /// <param name="credit">
    /// False when the work is counted as finished without being credited to its place: a prowl whose purpose was met on the
    /// way by the fighting it went to find (see <c>Commit</c>). Nothing is priced, nothing is cleared, nothing is walked
    /// into anybody's yard — the bot never stood on that ground — and the ending still counts.
    /// </param>
    private static void Settle(
        IBotWilful bot, BotEnding ending, string why = null, bool unreached = false, bool ground = true,
        bool unpause = true, bool credit = true, bool echo = false
    )
    {
        var resolve = bot?.Resolve;
        var deed = resolve?.Deed;

        if (deed == null)
        {
            return;
        }

        // The stopwatch: a fight that ends starts the clock to whatever is taken next. See BotReflex (06.10.2026).
        BotReflex.Ended(bot, deed, ending);

        var takings = BotYield.Settle(bot, deed, resolve.Stake, ending);

        // What the work paid, for the page's money panel (build 327). See BotEarnings.
        BotEarnings.Note(bot.Self, takings.Coin, takings.Made);

        // <b>A tithe on held ground.</b> Coin that arrived in the pack from work that braved a square some guild holds
        // pays the holder a tenth. See BotChest: Patrick's fourth Quad idea of 16.09.2026, the money half.
        var tithed = 0;

        if (credit && deed.Braves && takings.Coin > 0)
        {
            tithed = BotChest.Tithe(bot.Self, deed.Map, deed.Where, takings.Coin);
        }

        // <b>And the guild's dues on whatever the work brought in</b>, less what it just tithed to the same chest: the one
        // store a guild's hall can be saved in. See BotDues (29.09.2026, evening).
        if (credit && takings.Coin > 0)
        {
            BotDues.Pay(bot.Self, takings.Coin, tithed);
        }

        if (unreached)
        {
            Unpriced++;

            // <b>And the place is written down where every chooser can read it.</b> Same test, two records:
            // the ledger is not told what the ground is worth (above), and the shard is told that nobody
            // could get there (here). See BotRefused for why neither of the three records that already
            // existed could carry this.
            if (ground && !BotOutlaw.KeptRound(bot.Self))
            {
                BotRefused.Refuse(deed.Map, deed.Where);
            }
        }
        else if (credit)
        {
            resolve.Ledger.Note(deed.Kind, deed.Map, deed.Where, takings.PerMinute);
        }

        // And the same outcome on the population's board, which is a different record answering a different
        // question: this one is what the island is like, and it is what a bot who has never been here reads.
        // See BotCommons. The mind's flag rides along so the report can say how much of what the shard knows
        // it was told by a bot that thinks — which is the whole of what those three are for.
        var told = bot.Self is BotMobile { Minded: true };

        if (!unreached && credit)
        {
            BotCommons.Note(deed.Kind, deed.Map, deed.Where, takings.PerMinute, told);

            // And what the trade claimed against what it came to, which is how the shard corrects the
            // constants in its own source. See BotCommons.Corrected. A trade whose errands keep failing on
            // the road would otherwise correct its own expectation to nothing and stop being offered at all,
            // which is what happened to herbs and cooking overnight.
            BotCommons.Claimed(deed.Kind, deed.Expects, takings.PerMinute, told);
        }

        // <b>Wary of a place that paid, which is the opposite of what caution is for.</b>
        //
        // Beware exists to stop a bot going back to somewhere the work does not happen. A great many
        // undertakings end as failures having nonetheless done the work: a tool wears through - the engine
        // gives one 25 to 75 uses and destroys it at zero - a seam empties, a shelf runs out, the light goes.
        // On the night of 03.09.2026 the scribes ended 89 errands with "nothing to write with" and those 89
        // endings carried 15358 gold of scrolls between them, at rates up to 730 a minute. Every one of them
        // taught the bot to be wary of the shop it had just earned that in.
        //
        // The takings are the test and they are already in hand a few lines above. Nothing was produced is
        // what "this place was no good" means; a death says it regardless, because coming back from one with
        // a full pack does not make the ground safe.
        if (ending == BotEnding.Died || (ending == BotEnding.Failed && takings.Worth <= 0))
        {
            resolve.Ledger.Beware(deed.Kind, deed.Map, deed.Where);
        }
        else if (ending == BotEnding.Done && credit)
        {
            // Told to whoever is keeping score, if anybody is. See BotAppraisal.Revelry for why this is a
            // hook rather than a call: the watcher lives in an assembly that depends on this one.
            Completed?.Invoke(bot.Self, deed.Kind);

            // Arriving anywhere disproves a refusal of that ground, which is the property that keeps this
            // record from swallowing the island the way the quadrant baulks did.
            BotRefused.Arrived(deed.Map, deed.Where);

            // Only Done, and never Dropped: a bot that walked away from this to do something better has
            // learned nothing whatever about the place, and letting that clear a suspicion would let a busy
            // bot forgive the same wall over and over. See BotLedger.Worked.
            resolve.Ledger.Worked(deed.Kind, deed.Map, deed.Where);

            // And whose ground it was. Per finished errand rather than per beat, which is what keeps a
            // guild's opinion of its neighbours proportional to what they actually took out of its yard
            // rather than to how long they loitered in it. See BotRegard.Trespassed.
            var whose = BotLand.Holder(deed.Map, deed.Where);

            if (whose != null && whose != bot.Self?.Guild?.Name)
            {
                BotRegard.Trespassed(bot.Self?.Guild?.Name, whose);
            }
        }

        resolve.Urges.Paid(takings.Worth);

        // And whose minutes they were: the bot's own trade, anybody's work, or another class's. See BotCalling.
        BotCalling.Worked(bot.Self, deed, takings.Minutes);

        Count(deed.Kind, -1);

        var tallied = TallyOf(deed.Kind);

        // A defence is kept out of the headline and never rested by the breaker. See DefenceFinished.
        var defence = deed is BotRescue { Own: true };

        // The same deed failing at once against the same target, or a fight ending on a losing judgement, is rested against that
        // target for a few seconds, defences included (07.10.2026, loops18). See BotTreadmill.
        if (ending == BotEnding.Failed)
        {
            BotTreadmill.Failed(bot.Self, deed, why, Core.TickCount - resolve.SinceTick, takings.Made, takings.Skill);
        }

        switch (ending)
        {
            case BotEnding.Done:
                if (defence)
                {
                    DefenceFinished++;
                }
                else if (deed.KeptOut)
                {
                    // No work and no failure: kept out of the headline and the trade's own share alike. See BotDeed.KeptOut.
                    KeptOut++;
                    tallied.KeptOut++;

                    break;
                }
                else
                {
                    Finished++;
                }

                tallied.Finished++;
                BotBreaker.Finished(bot.Self, deed.Kind);

                break;

            case BotEnding.Failed when defence:
                DefenceFailed++;
                tallied.Failed++;

                break;

            case BotEnding.Failed:
                Failed++;
                tallied.Failed++;

                // A failure that produced something did the work, and a pen worn through after fourteen scrolls is the
                // end of a batch rather than a loop. See BotBreaker.
                //
                // <b>Produced, which is goods or skill — and until 21.09.2026 this asked whether it was worth anything,
                // which coin alone can answer.</b> A bot's coin moves for reasons that have nothing to do with the work
                // in its hands: a stall of its own sells, a want of its own is filled. On 20.09.2026 Hale, the richest
                // bot on the island with the most stalls, failed at the forge 160 times — "24 attempts, 0 made … nothing
                // came of the iron", every twenty-four seconds, seventeen times in one ten-minute stretch — and each
                // ending read "7 coin", "14 coin", "394 coin" from something sold meanwhile. Worth above nought, so not
                // one of the 160 reached the breaker, which did not trip once all session. The better a bot's trade on
                // the market, the more immune its loops were.
                if (takings.Made <= 0 && takings.Skill <= 0)
                {
                    BotBreaker.Failed(bot.Self, deed.Kind, why);
                }
                else
                {
                    BotBreaker.Excused++;
                }

                break;

            case BotEnding.Dropped:
                if (defence)
                {
                    DefenceDropped++;
                }
                else
                {
                    Dropped++;
                }

                tallied.Dropped++;

                break;

            case BotEnding.Died:
                // <b>One death is one death (29.09.2026, build 330).</b> The work a bot had put down for the flight it died in
                // is settled died as well (build 272 stopped it being called dropped), and both went into the headline: 56 of
                // 151 "died doing" lines between 20:16 and 21:18 were such echoes of 95 deaths, each counted twice against the
                // finished share. The echo is kept with its trade and counted apart; the headline counts the death once.
                if (echo)
                {
                    EchoDeaths++;
                }
                else if (defence)
                {
                    DefenceDeaths++;

                    // The stand's honest price, counted on the death itself and never on the echo of a stand put down for the flight
                    // the bot died in (13.10.2026, w5 review A). See BotDefender.StoodDied.
                    if (deed is BotRescue { ToTheEnd: true })
                    {
                        BotDefender.StoodDied++;
                    }
                }
                else
                {
                    Deaths++;
                }

                // The breaker rests work a bot keeps dying at — the work in hand, and the work put down for the flight it died in,
                // which is the errand that walked it there (build 352: Ulaine died four times with "restock: after 1 Scissors" put
                // down for a flight, none of them counted). The flight itself never is. See BotBreaker.Fell.
                BotBreaker.Fell(bot.Self, deed.Kind);

                tallied.Died++;

                break;
        }

        resolve.Deed = null;
        resolve.Sent = default;
        resolve.Aside = false;

        // <b>And the road it was on, which nothing else was going to put down.</b> A journey is completed by
        // the walker, and the walker is only asked while a bot is walking — so an undertaking that ends any
        // other way (done on the spot, failed, dropped for something better) leaves its errand standing in
        // the queue for ever. For a bot that immediately takes new work this is invisible: the next walk
        // rebases the journey over it. For a bot that takes nothing it is a lie that never expires.
        //
        // Measured 02.09.2026: Joss the Warrior, holding no work at all for four minutes and motionless for
        // three of them, reported as "walking to 1440,1470, 15 tiles off, set out 5m ago" — an errand from a
        // walk home that had finished five minutes earlier. Six bots beside it were in the same state. Every
        // instrument on this shard read them as travelling, which is exactly the sort of quiet wrong number
        // that costs an evening to unpick.
        //
        // Only when there is nothing left to do: a deed that ended by handing over to another leg of its own
        // queue still owns the rest of it, and Finish would throw that away.
        if (bot.Journey is { Active: true } road && resolve.Deed == null)
        {
            road.Finish();
        }

        // Due to look again at once. A bot that has just finished a job should not stand about waiting for a
        // review it does not need: there is nothing to change its mind about. A flag rather than a zeroed
        // stamp, because a tick count of zero is a legitimate reading on this shard's hosts.
        resolve.Due = true;

        // And asked between turns, not at the next one, when what ended was a fight, a flight or a mending: what comes after a
        // fight is decided in the beat it ends. Not for every ending - work that is taken and fails at once would turn at the
        // clock's pace instead of the bot's. See BotMobile.Hurry.
        if (deed.Foe != null || deed is BotBolt || deed is BotSalve)
        {
            (bot.Self as BotMobile)?.Hurry();
        }

        try
        {
            deed.Drop(bot);
        }
        catch (Exception e)
        {
            logger.Error(e, "Undertaking {Kind} threw while being let go", deed.Kind);
        }

        if (bot.Self is { } ended)
        {
            BotEvents.Post(
                "work",
                ended,
                why == null ? $"{Word(ending)} {deed.Kind}" : $"{Word(ending)} {deed.Kind}: {why}",
                null,
                BotJson.Object(
                    w =>
                    {
                        w.WriteString("work", deed.Kind);
                        w.WriteString(
                            "ending",
                            ending switch
                            {
                                BotEnding.Done => "finished",
                                BotEnding.Failed => "failed",
                                BotEnding.Dropped => "dropped",
                                _ => "died"
                            }
                        );
                        w.WriteString("reason", why ?? "");
                        w.WriteNumber("minutes", Math.Round(takings.Minutes, 2));
                        w.WriteNumber("perMin", Math.Round(takings.PerMinute, 1));
                        w.WriteNumber("coin", takings.Coin);
                        w.WriteNumber("made", takings.Made);
                    }
                )
            );

            try
            {
                Ended?.Invoke(ended, deed, ending, why, takings.Minutes, takings.PerMinute, takings.Coin);
            }
            catch (Exception e)
            {
                logger.Error(e, "A listener threw when {Kind} ended", deed.Kind);
            }
        }

        if (Chatty)
        {
            logger.Information(
                "{Name} {Ending} {Deed}: {Takings}{Why}",
                bot.Self?.Name,
                Word(ending),
                deed,
                takings,
                why == null ? "" : $" — {why}"
            );
        }

        // The debugger's short memory of this bot, so that "why did it stop" has an answer at the door.
        resolve.Remember(
            why == null
                ? $"{Word(ending)} {deed.Kind} after {takings.Minutes:F1} min at {takings.PerMinute:F0}/min"
                : $"{Word(ending)} {deed.Kind} after {takings.Minutes:F1} min at {takings.PerMinute:F0}/min — {(why.Length > 140 ? why[..140] : why)}"
        );

        // And whatever this work had interrupted, taken up again. Here, after everything above, so that the work
        // that ended is wholly settled and logged before the bot goes back to the one it put down.
        if (unpause && resolve.Paused != null && resolve.Deed == null)
        {
            Unpause(bot, resolve, ending, deed.Kind);
        }
    }

    /// <summary>Work displaced by a rung above the bot's own business: failing, being hunted, being overloaded.</summary>
    public static long Interrupted { get; private set; }

    /// <summary>
    /// Walks sent again, unchanged, because the road no longer carried them: something else at the bottom of it - a
    /// company's station left behind - or nothing at all.
    /// </summary>
    public static long Resent { get; private set; }

    /// <summary>
    /// Whether the work in hand is still inside its hold, and so may not be outbid by an ordinary offer.
    ///
    /// <para>
    /// Only steadfast work, only on the free rung, and only work the free rung handed out. The hold is the work's
    /// own reckoning — the walk it expected and the work itself — stretched by <see cref="CommitStretch"/>,
    /// capped by <see cref="CommitCapMs"/> and floored by the dwell it extends.
    /// </para>
    ///
    /// <para>
    /// <b>Lifted by trouble, because trouble is an event.</b> Kinny and Georgeff's reactive bold agent drops its
    /// plan when the target vanishes; the equivalents here are a walk that has been told its way is blocked and a
    /// walk that has stopped closing for a third of the watchdog's patience. Anything worse, the work ends by
    /// itself, and the watchdogs — the trek limit, the labour clock, the stall watch — run whether a hold stands
    /// or not, which is why a hold as long as eight minutes can hide nothing stuck.
    /// </para>
    /// </summary>
    private static bool Holding(BotResolve resolve, BotDeed held, BotStanding rung, long now)
    {
        if (CommitStretch <= 0.0 || rung != BotStanding.Free || resolve.Took != BotStanding.Free || !held.Steadfast)
        {
            return false;
        }

        if (resolve.Bent || resolve.Trudged >= TrekLimit * TroubleShare)
        {
            Troubled++;

            return false;
        }

        return now - resolve.SinceTick < HoldMs(resolve);
    }

    /// <summary>How long, in milliseconds, the work in hand is held for. See <see cref="Holding"/>.</summary>
    private static long HoldMs(BotResolve resolve)
    {
        var held = (long)Math.Clamp(resolve.Expected * CommitStretch * 60000.0, DwellMs, CommitCapMs);
        var own = resolve.Deed?.HoldsFor ?? 0.0;

        // Work that names its own hold is held at least that long, cap or no cap. See BotDeed.HoldsFor.
        return own > 0.0 ? Math.Max(held, (long)(own * 60000.0)) : held;
    }

    /// <summary>
    /// Puts the work in hand down so that something which will not wait can be taken on, keeping everything
    /// needed to take it up again. The caller assigns the new work straight after.
    /// </summary>
    private static void Pause(IBotWilful bot, BotResolve resolve, string forKind, long now, bool sheltered = false, bool keeps = false)
    {
        var deed = resolve.Deed;
        var body = bot.Self;

        // Read here, not at the taking up, so that the switch turned off on a running shard leaves pauses already made as
        // they were made and changes only the next ones; the counter says how many are kept.
        var kept = keeps && Keep;

        if (kept)
        {
            KeptForCompany++;
        }

        resolve.Paused = new BotPause
        {
            Deed = deed,
            Took = resolve.Took,
            Stake = resolve.Stake,
            Since = resolve.SinceTick,
            At = now,
            Because = resolve.Because,
            TakenAt = resolve.TakenAt,
            Expected = resolve.Expected,
            Wealth = BotYield.Standing(body),
            Aside = BotYield.AsideOf(body),
            Skill = BotYield.SkillOf(body, deed.Trains),
            For = forKind,
            Sheltered = sheltered,
            Kept = kept
        };

        Count(deed.Kind, -1);
        Paused++;
        TallyOf(deed.Kind).Paused++;

        try
        {
            deed.Paused(bot);
        }
        catch (Exception e)
        {
            logger.Error(e, "Undertaking {Kind} threw when it was put down; put down anyway", deed.Kind);
        }


        resolve.Deed = null;
        resolve.Sent = default;
    }

    /// <summary>
    /// The stake of work that was put down, moved by whatever changed while the bot was away, so that an
    /// interruption's takings and minutes are not booked to it. See <see cref="BotPause"/>.
    /// </summary>
    private static BotStake Moved(BotPause pause, Mobile body, long away) =>
        new(
            pause.Stake.Tick + away,
            pause.Stake.Skill + (BotYield.SkillOf(body, pause.Deed.Trains) - pause.Skill),
            pause.Stake.Wealth + (BotYield.Standing(body) - pause.Wealth),
            pause.Stake.Made,
            // Moved with the wealth, or a levy while the work was put down would be taken out of the stake above and
            // put back by the ledger at the settling — counted twice.
            pause.Stake.Aside + (BotYield.AsideOf(body) - pause.Aside)
        );

    /// <summary>
    /// Takes up what was put down, or settles it as dropped when going back to it would be wrong: the bot died,
    /// came back too hurt, is on another map, or was away longer than anything set aside may be.
    /// </summary>
    private static void Unpause(IBotWilful bot, BotResolve resolve, BotEnding ending, string after)
    {
        var pause = resolve.Paused;

        resolve.Paused = null;

        var body = bot.Self;
        var deed = pause.Deed;
        var now = Core.TickCount;
        var away = now - pause.At;

        string refusal = null;
        var died = ending == BotEnding.Died || body is not { Deleted: false, Alive: true };

        // <b>A kept pause is timed from the moment the company let the bot go (12.10.2026, w2).</b> The first time the bot
        // is asked to take it up is that moment; a bot sent to be mended first is asked again when the bandage settles, and
        // only then does the aside clock run. See Keep and BotDeed.Keeps for the numbers.
        var kept = pause.Kept;
        var idle = kept ? (pause.Freed ? now - pause.FreedAt : 0L) : away;

        // Which kept refusal this is, counted only where the work is settled dropped: a prowl or a stroll kept past the
        // ceiling is settled Done below (MetFiller), and a counter that said "lapsed" over an ending that said "finished"
        // would be two words for one thing (review A, finding 4).
        var keptLapsed = false;
        var keptLate = false;
        var keptStale = false;

        if (died)
        {
            refusal = "the bot died while it was put down";
        }
        else if (kept && away >= KeptCapMs)
        {
            keptLapsed = true;
            refusal = $"it was put down for {away / 60000.0:F0} minutes, longer than a company may keep it";
        }
        // Not for work put down under a hold that shelters it (07.10.2026, arena27): an arena entrant's work waits out the championship.
        else if (idle >= AsideCapMs && !pause.Sheltered)
        {
            if (kept)
            {
                // "Since it was first free", not "since the company let it go": the bot sent to be mended may have been levied
                // into a second company during the bandage, and that company's minutes are on this clock too (review B, §3.6).
                keptLate = true;
                refusal = $"{idle / 60000.0:F0} minutes since it was first free to take it up, and it did not";
            }
            else
            {
                refusal = $"it was put down for {away / 60000.0:F0} minutes";
            }
        }
        else if (!deed.ResumesHurt && body.HitsMax > 0 && body.Hits < body.HitsMax * ResumeHealth)
        {
            // <b>Left put down, not dropped (29.09.2026).</b> A hunt put down for a flight and refused for the bot's
            // health was settled dropped the moment the flight ended, and the bot was offered its own bandage in the
            // same beat. Kept put down instead, it is asked again when whatever the bot does next — the bandage,
            // most often — settles; AsideCapMs above is the clock that ends the waiting, and it is the only one.
            if (kept && !pause.Freed)
            {
                pause.Freed = true;
                pause.FreedAt = now;
            }

            resolve.Paused = pause;
            HeldHurt++;

            return;
        }
        else if (deed.Map != body.Map)
        {
            refusal = "the bot is on another map now";
            keptStale = kept;
        }
        else if (deed.Stale(bot) is { } stale)
        {
            // The work's own word that the world moved on while it was put down. See BotDeed.Stale.
            refusal = stale;
            keptStale = kept;
        }

        // Restored either way: a refused resumption is settled as the dropped work it now is, and settling needs
        // the stake it was taken with.
        resolve.Deed = deed;
        resolve.Took = pause.Took;
        resolve.Stake = Moved(pause, body, away);
        resolve.SinceTick = pause.Since + away;
        resolve.TakenAt = pause.TakenAt;
        resolve.Expected = pause.Expected;
        resolve.Because = pause.Because;
        resolve.ReviewedTick = now;
        resolve.StirredTick = now;
        resolve.Sent = default;
        resolve.Nearest = int.MaxValue;
        resolve.Trudged = 0;
        resolve.Started = -1;
        resolve.Bent = false;
        resolve.Aside = false;
        resolve.Due = false;

        Count(deed.Kind, 1);

        // <b>A filler whose time real work took has found what it was for (29.09.2026, build 322).</b> A prowl or a stroll put
        // down for a company's call and not taken up again half an hour later ("dropped prowl: not taken up again after enlist:
        // it was put down for 30 minutes", 18 of 316 failed and dropped endings on 313–315, 15 of them prowls) ended exactly as
        // one outbid by a real offer does (MetFiller): the bot had better to do. Settled finished and counted with those; a
        // death is still a death, and any other work put down that long is still dropped.
        if (refusal != null && !died && MetCounts && away >= AsideCapMs && (deed is BotProwl || deed is BotRounds))
        {
            Unresumed++;
            MetFiller++;
            Settle(bot, BotEnding.Done, $"its time went to {after} instead", unpause: false);
            Surface(bot, resolve, ending, after);

            return;
        }

        if (refusal != null)
        {
            Unresumed++;

            if (keptLapsed)
            {
                KeptLapsed++;
            }
            else if (keptLate)
            {
                KeptLate++;
            }
            else if (keptStale)
            {
                KeptStale++;
            }

            // <b>A death is a death, not a change of mind (29.09.2026).</b> The flight the bot died in settled as died;
            // the work it had put down for that flight settled as dropped, and one death counted twice against the
            // finished share — about ninety times in three hours of build 272 ("the bot died while it was put down").
            Settle(bot, died ? BotEnding.Died : BotEnding.Dropped, $"not taken up again after {after}: {refusal}", unpause: false, echo: died);
            Surface(bot, resolve, ending, after);

            return;
        }

        try
        {
            deed.Resumed(bot);
        }
        catch (Exception e)
        {
            logger.Error(e, "Undertaking {Kind} threw when it was taken up again; it is given up", deed.Kind);

            Settle(bot, BotEnding.Failed, unpause: false);
            Surface(bot, resolve, ending, after);

            return;
        }

        // The work the enlistment had put down goes back beneath it. See Stacked.
        if (resolve.Under != null)
        {
            resolve.Paused = resolve.Under;
            resolve.Under = null;
        }

        Resumed++;
        TallyOf(deed.Kind).Resumed++;

        // What the kept clock bought: before 12.10.2026 this piece of work was a drop. See KeptPastCap.
        if (kept && away >= AsideCapMs)
        {
            KeptPastCap++;
        }

        BotReflex.Took(bot, deed, resumed: true);

        if (Chatty)
        {
            logger.Information(
                "{Name} took up {Deed} again after {After}, {Seconds:F0}s after putting it down",
                body.Name,
                deed,
                after,
                away / 1000.0
            );
        }
    }

    /// <summary>
    /// The work beneath a put-down enlistment that was not taken up again comes up in its turn, and is taken up or settled by
    /// the same rules (01.10.2026, build next). See <see cref="Stacked"/>.
    /// </summary>
    private static void Surface(IBotWilful bot, BotResolve resolve, BotEnding ending, string after)
    {
        if (resolve.Under == null || resolve.Paused != null || resolve.Deed != null)
        {
            return;
        }

        resolve.Paused = resolve.Under;
        resolve.Under = null;

        Unpause(bot, resolve, ending, after);
    }

    /// <summary>
    /// Whether a company's enlistment jumped by a flight or a bandage is put down over work it had already put down, rather
    /// than dropped (01.10.2026, build next). A switch, so it can be taken off a running shard. See <see cref="Stacked"/>.
    /// </summary>
    public static bool StackAid { get; set; } = true;

    /// <summary>Enlistments put down for a flight or a bandage over work they had already put down. See the note in Commit.</summary>
    public static long Stacked { get; private set; }

    /// <summary>Endings kept out of the finished share for being no work at all (BotDeed.KeptOut): levied enlistments let go before they served.</summary>
    public static long KeptOut { get; private set; }

    /// <summary>
    /// Settles what was put down as dropped without taking it up, because the bot has changed its mind about the
    /// interruption itself and is not going back through it. The work in hand is not touched.
    /// </summary>
    private static void DropPaused(IBotWilful bot, BotResolve resolve, string why)
    {
        var pause = resolve.Paused;

        if (pause == null)
        {
            return;
        }

        resolve.Paused = null;
        Unresumed++;

        // And whatever was beneath it goes the same way, after it (01.10.2026, build next). See Stacked.
        if (resolve.Under != null)
        {
            resolve.Paused = resolve.Under;
            resolve.Under = null;
            DropPaused(bot, resolve, why);
            resolve.Paused = null;
        }

        var body = bot.Self;
        var deed = pause.Deed;
        var takings = BotYield.Settle(bot, deed, Moved(pause, body, Core.TickCount - pause.At), BotEnding.Dropped);
        var told = body is BotMobile { Minded: true };

        // The same three records any dropped work writes, and the same release. Its census count went when it was
        // put down, so it is not given back here.
        resolve.Ledger.Note(deed.Kind, deed.Map, deed.Where, takings.PerMinute);
        BotCommons.Note(deed.Kind, deed.Map, deed.Where, takings.PerMinute, told);
        BotCommons.Claimed(deed.Kind, deed.Expects, takings.PerMinute, told);
        resolve.Urges.Paid(takings.Worth);

        if (deed is BotRescue { Own: true })
        {
            DefenceDropped++;
        }
        else
        {
            Dropped++;
        }

        TallyOf(deed.Kind).Dropped++;

        try
        {
            deed.Drop(bot);
        }
        catch (Exception e)
        {
            logger.Error(e, "Undertaking {Kind} threw while being let go", deed.Kind);
        }

        resolve.Remember($"dropped {deed.Kind} while it was put down — {why}");

        if (Chatty)
        {
            logger.Information(
                "{Name} dropped {Deed}: {Takings} — put down for {For}, and {Why}",
                body?.Name,
                deed,
                takings,
                pause.For,
                why
            );
        }
    }

    /// <summary>
    /// One bot's commitment in a sentence: what it holds, how far into its own reckoning, whether a better offer
    /// could take it off that now and why not, what it put down, and how its last few pieces of work ended. The
    /// debugger's <c>resolve</c>.
    /// </summary>
    public static string Explain(BotMobile bot)
    {
        var resolve = bot?.Resolve;

        if (resolve == null)
        {
            return "that bot has no resolve to read.";
        }

        var now = Core.TickCount;
        var deed = resolve.Deed;

        using var line = ValueStringBuilder.Create(512);

        if (deed == null)
        {
            line.Append($"{bot.Name} holds nothing ({resolve.Empty ?? "nothing recorded about why"})");
        }
        else
        {
            var age = (now - resolve.SinceTick) / 60000.0;
            var worth = BotAppraisal.Weigh(bot, deed, Share(deed.Kind), out _, out _, inHand: true);
            var left = (HoldMs(resolve) - (now - resolve.SinceTick)) / 60000.0;

            string hold;

            if (!deed.Steadfast)
            {
                hold = "not steadfast work, so any offer that clears the margin takes it once the first half minute is up";
            }
            else if (CommitStretch <= 0.0)
            {
                hold = "steadfast, but holds are switched off";
            }
            else if (resolve.Took != BotStanding.Free)
            {
                hold = "handed out by a rung above the bot's own business, which holds are not for";
            }
            else if (resolve.Bent || resolve.Trudged >= TrekLimit * TroubleShare)
            {
                hold = "in trouble on its walk, so its hold is lifted";
            }
            else if (left > 0.0)
            {
                hold = $"held against ordinary offers for {left:F1} more minutes";
            }
            else
            {
                hold = "past its hold, so any offer that clears the margin takes it";
            }

            line.Append(
                $"{bot.Name} holds {deed}: {age:F1} of {resolve.Expected:F1} minutes reckoned; {hold}; taken at {resolve.TakenAt:F0}/min, worth {worth:F0}/min now"
            );
        }

        if (resolve.Paused is { } pause)
        {
            line.Append($"; put down {pause.Deed} for {pause.For} {(now - pause.At) / 1000} seconds ago");
        }

        var first = true;

        foreach (var ending in resolve.Recent())
        {
            line.Append(first ? "; lately: " : " | ");
            line.Append(ending);
            first = false;
        }

        return line.ToString();
    }

    private static int Percent(long part, long whole) =>
        whole <= 0 ? 0 : (int)Math.Round(100.0 * part / whole);

    /// <summary>
    /// How much of what the population takes on it sees through, and what takes it off the rest. The census line
    /// <c>Resolve:</c>, and the debugger's <c>resolves</c>.
    ///
    /// <para>
    /// Cumulative since start, like every summary line. The per-trade half is ordered by how much of the trade was
    /// taken on and reads, per trade, the share of its endings that were finishes and the share that were drops,
    /// then how often a hold refused a better offer for it, and how often it was put down and taken up again.
    /// </para>
    /// </summary>
    public static string DescribeResolve()
    {
        using var line = ValueStringBuilder.Create(1024);

        var ended = Finished + Failed + Dropped + Deaths;

        line.Append(
            $"{Finished} of {ended} endings finished ({Percent(Finished, ended)}%), {Failed} failed, {Dropped} dropped, {Deaths} died ({EchoDeaths} more pieces of work that had been put down died with their bot, counted with their trades only); self-defence kept out of the share: {DefenceFinished} finished, {DefenceFailed} failed, {DefenceDropped} dropped, {DefenceDeaths} died; of the drops {Jumped} for something that would not wait, {Summoned} for a call from outside, {Interrupted} for a rung above, {Dislodged} for a full pack and {Outbid} outbid; {Held} better offers refused inside a hold, {Repeated} offers of the work already in hand let alone and {Troubled} holds lifted for trouble; {Paused} put down ({PressPaused} of them for a press), {Resumed} taken up again, {Unresumed} not and {HeldHurt} left put down until the bot was mended; {KeptForCompany} put down for a company's call and kept for as long as it lasted ({KeptPastCap} of them taken up again past the {AsideCapMs / 60000}-minute cap once the company let the bot go, {KeptLapsed} lapsed for outlasting {KeptCapMs / 60000} minutes in a company, {KeptLate} not taken up within {AsideCapMs / 60000} minutes of first being free to, {KeptStale} refused by the work's own word or another map{(Keep ? "" : "; off")}); {Returned} kinds of work taken back within {ReturnMs / 60000} minutes of being dropped; {Pressed} pieces pressed into hands, {Repressed} presses let alone as the work already in hand; {Stacked} enlistments put down for a flight or a bandage over work they had already put down; {KeptOut} endings kept out of the share for being no work at all (levied members let go before they ever stood with their company); by trade, taken: finished/dropped %, held, put down/taken up:"
        );

        var rows = new List<KeyValuePair<string, Tally>>(_tallies);

        rows.Sort((a, b) => b.Value.Taken.CompareTo(a.Value.Taken));

        for (var i = 0; i < rows.Count && i < 16; i++)
        {
            var (kind, t) = rows[i];
            var ends = t.Finished + t.Failed + t.Dropped + t.Died;

            line.Append($" {kind} {t.Taken}: {Percent(t.Finished, ends)}/{Percent(t.Dropped, ends)}");

            if (t.KeptOut > 0)
            {
                line.Append($" ({t.KeptOut} kept out)");
            }

            if (t.Held > 0 || t.Paused > 0)
            {
                line.Append($", held {t.Held}, {t.Paused}/{t.Resumed}");
            }

            line.Append(i + 1 < rows.Count && i + 1 < 16 ? ";" : "");
        }

        return line.ToString();
    }

    private static string Word(BotEnding ending) =>
        ending switch
        {
            BotEnding.Done => "finished",
            BotEnding.Failed => "failed at",
            BotEnding.Dropped => "dropped",
            _ => "died doing"
        };

    /// <summary>
    /// What share of the population's undertakings are this kind of work. The crowd every bot can see,
    /// derived from a common fact rather than sent to anybody.
    /// </summary>
    private static double Share(string kind)
    {
        if (kind == null || !_busy.TryGetValue(kind, out var mine) || mine <= 0)
        {
            return 0.0;
        }

        var total = 0;

        foreach (var (_, count) in _busy)
        {
            if (count > 0)
            {
                total += count;
            }
        }

        return total <= 0 ? 0.0 : (double)mine / total;
    }

    /// <summary>
    /// A bot looked for work and there was none.
    ///
    /// Counted once per drought rather than once per look, so the figure means "this many times a bot ran out
    /// of things worth doing" and not "this many beats went by while it had nothing".
    /// </summary>
    /// <summary>
    /// Nothing was taken, and the two numbers that say why.
    ///
    /// <para>
    /// <b>The count alone sent whoever read it back to the raw log.</b> Until 02.09.2026 this recorded a
    /// boolean and a tally, so fifteen idle bots in the camp — 300gp each, two minutes apiece — could be
    /// seen but not explained: an empty table and a table nobody would pay for read identically. The words
    /// are kept on the resolve rather than logged, because a barren bot is barren every tick and a line per
    /// tick per bot is not a measurement, it is a flood.
    /// </para>
    /// </summary>
    private static void Nothing(BotResolve resolve, long now, string why)
    {
        if (!resolve.Urges.IsBarren)
        {
            Barren++;
        }

        resolve.Empty = why;
        resolve.Urges.Barren(now);
    }

    private static void Count(string kind, int by)
    {
        if (kind == null)
        {
            return;
        }

        _busy.TryGetValue(kind, out var count);

        count += by;

        if (count <= 0)
        {
            _busy.Remove(kind);

            return;
        }

        _busy[kind] = count;
    }

    /// <summary>
    /// A rung that should have somebody answering it and has not. Said once, by name, in the same voice the
    /// module loader uses for a subsystem that ought to be running: silence about a missing answer is how
    /// the first version's gaps survived whole sessions.
    /// </summary>
    private static void Unserved(BotStanding rung)
    {
        // Only the rungs that are meant to have somebody on them. Being hit is answered by BotThreat and a
        // squad answers for itself, so silence on those two is the design rather than a gap.
        if (rung is not (BotStanding.Free or BotStanding.Failing) || !_mute.Add(rung))
        {
            return;
        }

        if (rung == BotStanding.Free)
        {
            logger.Error(
                "Nothing proposes any work at all, so every bot will find nothing worth doing. A kind of work is a folder with an IBotProposer in it, handed to BotWill.Offer"
            );

            return;
        }

        logger.Error(
            "Nothing proposes work for the {Rung} rung; bots on it will keep what they have and wait it out",
            rung
        );
    }

    private static void Census(long now)
    {
        if (!_censused)
        {
            _censused = true;
            _censusTick = now;

            return;
        }

        if (now - _censusTick < CensusMs)
        {
            return;
        }

        _censusTick = now;

        logger.Information("Will: {State}", Describe());
        logger.Information("Resolve: {State}", DescribeResolve());
        logger.Information("Roles: {State}", BotCalling.Describe());
    }

    /// <summary>
    /// One line about what the population is up to. For the census and for the boot log.
    ///
    /// <see cref="ValueStringBuilder"/> rather than <c>System.Text.StringBuilder</c>, which this shard does
    /// not use anywhere: it writes into a stack buffer and grows into a pooled one only if it has to.
    /// </summary>
    public static string Describe()
    {
        using var line = ValueStringBuilder.Create(256);

        line.Append(
            $"{Taken} taken on, {Finished} finished, {Failed} failed, {Dropped} dropped, {Deaths} died doing it; {Barren} times nothing was worth doing, {Unsworn} offers withheld from classes sworn elsewhere, {Trudges} given up for a walk that stopped closing, {OwnClock} beats of work that stands still on purpose left to its own clock past {LabourMs / 60000} minutes; holding now:"
        );

        var kinds = 0;

        foreach (var (kind, count) in _busy)
        {
            if (count <= 0)
            {
                continue;
            }

            // The separator is appended on its own rather than chosen inside the interpolation: a ternary
            // with interpolated branches is built before the call and allocates a string, which is the one
            // thing the zero-allocation handler is for.
            if (kinds > 0)
            {
                line.Append(",");
            }

            line.Append($" {kind} {count}");
            kinds++;
        }

        if (kinds == 0)
        {
            line.Append(" nothing");
        }

        // Printed on the clock rather than only when something goes wrong: a gate that lets work through
        // needs a tally somebody can read, or the day it starts letting through too much looks exactly like
        // the day before. See BotDeed.Unpaid.
        if (Unpriced > 0)
        {
            line.Append($"; {Unpriced} endings were not allowed to price the ground because the bot never got there, {Unblamed} of which blamed nothing because what they could not reach was somebody rather than somewhere, and {Rerouted} places refused on the way were written down as the work went elsewhere");
        }

        if (Foreign > 0)
        {
            line.Append($"; {Foreign} refusals of a road the work in hand had not sent - a company's station, nearly always - were not charged to it");
        }

        if (BotAppraisal.Stopped > 0)
        {
            line.Append(
                $"; {BotAppraisal.Stopped} offers marked down because the bot was carrying more than it can walk with and the work needed a step"
            );
        }

        if (BotAppraisal.Pocketless > 0 || BotUnload.Jammed > 0)
        {
            line.Append(
                $"; {BotAppraisal.Pocketless} offers refused because the work goes through a counter and the pack had no room for a coin, and {BotUnload.Jammed} times the trip that makes that room was offered"
            );
        }

        if (BotAppraisal.Restless > 0)
        {
            line.Append($"; {BotAppraisal.Restless} prowls let in at the unpaid floor for bots out of work past {BotAppraisal.RestlessAfter:F1} minutes");
        }

        if (BotAppraisal.Undergroundish > 0)
        {
            line.Append($"; {BotAppraisal.Undergroundish} offers refused for lying across a dungeon's edge from the bot");
        }

        if (BotAppraisal.Calms > 0)
        {
            line.Append(
                $"; {BotAppraisal.Calms} walks gave up having closed nothing, and {BotAppraisal.Becalmed} offers of another long walk from the same footing were refused for it"
            );
        }

        if (BotBreaker.Trips > 0)
        {
            line.Append($"; {BotBreaker.Describe()}");
        }

        if (BotTreadmill.Any)
        {
            line.Append($"; {BotTreadmill.Describe()}");
        }

        if (Resent > 0)
        {
            line.Append($"; {Resent} walks sent again because the road no longer carried them");
        }

        if (BotPeril.Overwhelmed > 0)
        {
            line.Append(
                $"; {BotPeril.Overwhelmed} offers refused because the hostile strength where the work lay was {BotPeril.Overwhelm:F1} times what was going there or more ({BotPeril.Reckoned} places reckoned)"
            );
        }

        if (Grounded > 0 || Dislodged > 0)
        {
            line.Append(
                $"; {Grounded} times a full pack put the errand that needs no step ahead of better-paid work that does, and {Dislodged} times it took what the bot was holding away for it"
            );
        }

        if (Kept > 0)
        {
            line.Append($"; {Kept} better offers were turned down because the work in hand had already been paid for");
        }

        if (Aided > 0)
        {
            line.Append($"; {Aided} times a member of a company put its enlistment down to run or to bind its wounds");
        }

        if (RaiseHeld > 0)
        {
            line.Append($"; {RaiseHeld} beats a company member's own work waited while it raised one of ours");
        }

        if (Filed > 0)
        {
            line.Append($"; {Filed} offers of paperwork left for the next choice rather than taking a bot off its work");
        }

        if (SecondGuesses > 0)
        {
            line.Append($"; {SecondGuesses} second guesses of the same kind turned down for the one in hand");
        }

        if (Met > 0)
        {
            line.Append($"; {Met} prowls finished by meeting the fighting they went for");
        }

        if (MetHome > 0)
        {
            line.Append($"; {MetHome} walks home finished by work found on the way");
        }

        if (MetFiller > 0)
        {
            line.Append($"; {MetFiller} prowls and strolls finished by a real offer outbidding them");
        }

        if (BotAppraisal.Unpaid > 0)
        {
            line.Append($"; {BotAppraisal.Unpaid} times work that is paid nothing on purpose was let past the earnings veto");
        }

        // Where the deciding half of the clock goes, by proposer and by the work advanced (01.10.2026, build 382). See TurnMs.
        line.Append($"; the proposers cost {Costliest(_proposerCosts)}; advancing work cost {Costliest(_advanceCosts)}");

        return line.ToString();
    }

    /// <summary>
    /// Everything about the last world back to nothing. Proposers stay — they are code — and so does the
    /// switch; the counters and the census do not, because they describe a population that no longer exists.
    /// </summary>
    public static void Reset()
    {
        _offers.Clear();
        _busy.Clear();
        _mute.Clear();

        _censused = false;

        Taken = 0;
        Finished = 0;
        Failed = 0;
        Dropped = 0;
        Deaths = 0;
        EchoDeaths = 0;
        Barren = 0;
        Unsworn = 0;
        Trudges = 0;
        OwnClock = 0;
        Grounded = 0;
        Dislodged = 0;
        Kept = 0;
        Aided = 0;
        RaiseHeld = 0;
        Filed = 0;
        SecondGuesses = 0;
        MindOffers = 0;
        MindRefused = 0;
        MindOutscored = 0;
        MindTopped = 0;
        _mindBeatenBy.Clear();
        Met = 0;
        MetHome = 0;
        Jumped = 0;
        Summoned = 0;
        Interrupted = 0;
        Outbid = 0;
        Held = 0;
        Troubled = 0;
        Paused = 0;
        PressPaused = 0;
        Repressed = 0;
        Repeated = 0;
        Resumed = 0;
        Unresumed = 0;
        HeldHurt = 0;
        KeptForCompany = 0;
        KeptPastCap = 0;
        KeptLapsed = 0;
        KeptLate = 0;
        KeptStale = 0;
        Returned = 0;
        Stacked = 0;
        KeptOut = 0;

        _tallies.Clear();
        _proposerCosts.Clear();
        _advanceCosts.Clear();
        BotCalling.Forget();
        BotYield.Reset();
    }
}
