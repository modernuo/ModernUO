using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a captain an afternoon of teaching, when there is anybody on the island worth teaching.
///
/// <para>
/// <b>It asks whether there are pupils before it offers a class, and that ordering is the whole design.</b>
/// A captain that opened a school whenever it was idle would spend its life standing on an empty field:
/// nobody is obliged to come, and a class with no pupils costs the shard a captain for a quarter of an hour
/// and returns nothing. So the question asked here is the same one a student will ask itself — is there
/// somebody who is behind this captain in a skill they both care about, and can they pay for it — and it is
/// asked of the whole population, cheaply, once per review.
/// </para>
/// </summary>
public sealed class BotDrill : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDrill));

    /// <summary>How far a captain will go to hold a class. The field is where it is; this is the leash.</summary>
    public static int Range { get; set; } = 400;

    /// <summary>Fewest pupils worth opening a field for.</summary>
    public static int Least { get; set; } = 1;

    public string Name => "Drill";

    public BotStanding Rung => BotStanding.Free;

    public static long Asked { get; private set; }

    public static long NotACaptain { get; private set; }

    public static long Held { get; private set; }

    public static long Busy { get; private set; }

    public static long Nobody { get; private set; }

    public static long TooFar { get; private set; }

    /// <summary>
    /// Classes <em>called for</em>, which is not classes held.
    ///
    /// The two differ and the first summary printed them as if they did not: six here against one field
    /// actually opened, because a deed is offered every review and most of them are outbid or dropped on the
    /// walk. A counter named for the outcome when it counts the attempt is the shape of lie this shard has
    /// been bitten by twice today already.
    /// </summary>
    public static long Called { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        // Either office. A captain teaches those who swing and shoot; a sage teaches those who cast. See
        // BotSchool.Suits, which is the one place that decides which is which.
        if (body is not BotMobile { Class: { } klass } captain || !klass.Leads && !klass.Tutors)
        {
            NotACaptain++;

            return null;
        }

        Asked++;

        // A captain in a company has somewhere else to be, and a class it walked out of halfway through
        // would be a fee taken for nothing.
        if (bot is IBotSquadMember { Squad: not null })
        {
            Held++;

            return null;
        }

        // Somebody already has the field — possibly this same captain, whose class is running.
        if (BotSchool.Master is { Deleted: false })
        {
            Busy++;

            return null;
        }

        // Asked once, the first time anybody looks: the height in the file is a person's reading and the
        // height a bot stands at is the map's. See BotSchool.Standing.
        BotSchool.Standing(map);

        if (!body.InRange(BotSchool.Ground, Range))
        {
            TooFar++;

            return null;
        }

        if (Pupils(captain) < Least)
        {
            Nobody++;

            return null;
        }

        Called++;

        return new BotLesson(map);
    }

    /// <summary>
    /// How many bots on the island this captain could actually teach something to, and who could pay.
    ///
    /// <para>
    /// Both halves, and leaving the money out was the tempting version. A population of eager, penniless
    /// warriors would have a captain opening a field every fifteen minutes, waiting ninety seconds, and
    /// closing it again — which reads in every summary as a captain hard at work and is a captain achieving
    /// precisely nothing.
    /// </para>
    /// </summary>
    private static int Pupils(BotMobile captain)
    {
        // Asked of this captain by name rather than by installing it as the master for the length of the
        // count: a question that has to mutate the world to be answered is a question that leaves a mark
        // when it throws. See BotSchool.Teachable.
        var counted = 0;
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var other = bots[i];

            if (other?.Map != captain.Map || !BotSchool.Teachable(captain, other))
            {
                continue;
            }

            if (BotYield.Spendable(other) < BotSchool.Bill(captain, other))
            {
                continue;
            }

            counted++;
        }

        return counted;
    }

    public static string Describe() =>
        Asked == 0
            ? "no captain has ever been offered a class to hold"
            : $"{Asked} offers to a captain: {Called} classes called for, {Held} were in a company, {Busy} found the field already held, {TooFar} were too far from it, {Nobody} found nobody worth teaching who could pay; {BotLesson.Skipped} posts of the ring walked past for having no road, {BotAttend.Unstationed} students taught where they stood because their station had none; {BotSchool.Describe()}";

    public static void Forget()
    {
        Asked = 0;
        NotACaptain = 0;
        Held = 0;
        Busy = 0;
        Nobody = 0;
        TooFar = 0;
        Called = 0;
    }
}

/// <summary>
/// Offers a warrior or an archer a place in whatever class is being called.
///
/// <para>
/// <b>Every refusal is a different fact and every one of them is counted.</b> "No bot ever goes to be
/// taught" has at least five distinct causes — no class is open, the roll is closed, this bot is the wrong
/// sort, it has nothing left to learn from this captain, it cannot afford the fee — and a single silent
/// nought would be equally consistent with all of them. That is the failure this shard has paid for more
/// often than any other.
/// </para>
/// </summary>
public sealed class BotStudent : IBotProposer
{
    /// <summary>
    /// How much longer than a straight line a walk to the field is reckoned to take.
    ///
    /// A third again, because a straight line is the best case and nothing walks one: a bot that would
    /// arrive on the last second of the joining window is a bot that arrives late, and the whole point of
    /// asking is to not spend the walk.
    /// </summary>
    public static double Punctual { get; set; } = 1.33;

    /// <summary>
    /// Times a bot was not offered a lesson because it could not have reached the field before the roll
    /// closed.
    ///
    /// Its own bucket beside <c>Sealed</c>: "there is no way there" and "there is a way and not enough time"
    /// are different faults with different repairs, and this one is expected to be large for a few seconds
    /// after a class opens and nought the rest of the time. Reading it high and flat means the field is
    /// somewhere the population cannot reach in ninety seconds, which is a fact about where it stands.
    /// </summary>
    public static long Belated { get; private set; }

    public string Name => "Student";

    public BotStanding Rung => BotStanding.Free;

    public static long Asked { get; private set; }

    public static long NoClass { get; private set; }

    public static long Closed { get; private set; }

    public static long WrongSort { get; private set; }

    public static long NothingToLearn { get; private set; }

    public static long Broke { get; private set; }

    /// <summary>
    /// The fattest purse among those who could not pay the fee. See <see cref="BotStable.Richest"/>: a
    /// refusal that does not say how short it fell cannot tell a fee set too high from a population with
    /// no money at all.
    /// </summary>
    public static long Richest { get; private set; }

    public static long Full { get; private set; }

    /// <summary>Of <see cref="Full"/>, the times the field still had room and every place left was spoken for by a bot on its way.</summary>
    public static long Spoken { get; private set; }

    public static long Came { get; private set; }

    /// <summary>
    /// Bots that could not have walked to the field from where they were standing.
    ///
    /// A named nought, and it was the difference between "nobody wanted a lesson" and "somebody wanted one
    /// eight times and could not get there". Nessa took the errand and failed it every five seconds for
    /// fifty seconds on 27.08.2026, each time on the same refusal; the ledger's caution damped it in the
    /// end, which is the machinery working, but eight walks to prove a fact the shard already knew is eight
    /// too many.
    /// </summary>
    public static long Sealed { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        // Counted before anything else so that "nobody was asked" and "nobody wanted to come" stay apart.
        Asked++;

        var master = BotSchool.Master;

        if (master is not { Deleted: false, Alive: true })
        {
            NoClass++;

            return null;
        }

        if (!BotSchool.Gathering)
        {
            Closed++;

            return null;
        }

        // <b>Counting the places already spoken for by bots on their way, not only the ones standing on the field.</b>
        // See BotSchool.Promise: at a cold start eighteen took six places and twelve were turned away at the field.
        var coming = BotSchool.Coming(body as BotMobile);

        if (BotSchool.Students.Count + coming >= BotSchool.Most)
        {
            if (BotSchool.Students.Count < BotSchool.Most)
            {
                Spoken++;
            }

            Full++;

            return null;
        }

        if (body is not BotMobile student || student.Map != master.Map)
        {
            WrongSort++;

            return null;
        }

        if (!BotSchool.Suits(master, student.Class))
        {
            WrongSort++;

            return null;
        }

        if (BotSchool.Lacking(student) == null)
        {
            NothingToLearn++;

            return null;
        }

        // Once a day and no oftener. Asked before the purse is, so a bot that has already been taught is
        // never offered a lesson it would only be refused at the gate.
        if (!BotSchool.Rested(student))
        {
            BotSchool.Rested_Away++;

            return null;
        }

        var bill = BotSchool.Bill(student);
        var wealth = BotYield.Spendable(student);

        if (wealth < bill)
        {
            Broke++;

            if (wealth > Richest)
            {
                Richest = wealth;
            }

            return null;
        }

        // <b>Asked of the reach ledger, which already knows.</b> The same question the patrol and the
        // harrowing both ask before they offer anywhere, and it costs a dictionary lookup: the answer comes
        // from pockets already proved closed by searches that failed, never from a fresh search. A place a
        // bot has just been unable to walk to is not a place to offer it again fifteen seconds later.
        if (BotReach.Ask(map, body.Location, BotSchool.Ground, BotArrival.Within(BotSchool.Pace * BotSchool.Rank))
            == BotReachVerdict.Sealed)
        {
            Sealed++;

            return null;
        }

        // <b>Whether it can get there before the roll closes, which nothing asked.</b> The same shape as
        // BotSupplier.Fits and for the same reason: work a bot cannot finish is not work priced low, it is
        // work not offered. See BotSchool.Left for the burst of twenty this costs at every cold start.
        //
        // The walk is estimated the way BotAppraisal estimates every other one — tiles at the engine's own
        // step delay — and then allowed a margin, because a straight line is the best case and bots do not
        // walk in straight lines. A bot that would arrive on the last second is a bot that arrives late.
        var dx = Math.Abs(body.Location.X - BotSchool.Ground.X);
        var dy = Math.Abs(body.Location.Y - BotSchool.Ground.Y);
        var walk = (dx > dy ? dx : dy) * BotWalk.StepDelayMs(false) * Punctual;

        if (walk > BotSchool.Left)
        {
            Belated++;

            return null;
        }

        Came++;

        return new BotAttend(map, student, bill);
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been offered a place in a class"
            : $"{Asked} asked: {Came} offered a place, {NoClass} found no class open, {Closed} came after the roll closed, {Full} found it full ({Spoken} of them with every place left spoken for by bots on their way), {WrongSort} were neither warrior nor archer, {NothingToLearn} had nothing left to learn from the master, {Broke} could not afford the fee (the fattest purse among them held {Richest}gp), {Sealed} could not have walked there at all, {Belated} could not have got there before the roll closed";

    public static void Forget()
    {
        Asked = 0;
        NoClass = 0;
        Closed = 0;
        WrongSort = 0;
        NothingToLearn = 0;
        Broke = 0;
        Belated = 0;
        Richest = 0;
        Full = 0;
        Came = 0;
        Sealed = 0;
    }
}
