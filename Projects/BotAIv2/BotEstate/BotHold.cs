using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Mobiles;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// Standing in a square your guild has laid claim to, for as long as the claim takes.
///
/// <para>
/// <b>The whole of "the guild gathers in the quadrant".</b> There is nothing to do here and that is the
/// point: the claim is won by being there, in numbers, where anybody who minds can find you. See
/// <c>BotClaim.Gather</c> for why it is the peak over the window rather than a headcount at the end.
/// </para>
///
/// <para>
/// <b>No claim on the errand, unlike every other guild office.</b> The steward, the fitter, the hirer and
/// the remover are each one bot's work and <c>BotOffice</c> keeps them to one; this one wants as many of the
/// guild as will come, so nothing stops a second member taking it. That is the only difference between this
/// and the four of them, and it is the reason it exists as its own file.
/// </para>
///
/// <para>
/// Nothing is earned. See <c>BotBaron</c> for the twelve minutes a bot once stood still because unpaid work
/// was being marked down for being unpaid.
/// </para>
/// </summary>
public sealed class BotHold : BotDeed
{
    /// <summary>The ledger key.</summary>
    public const string Trade = "stake";

    /// <summary>
    /// What holding ground is reckoned at per minute before experience corrects it.
    ///
    /// Below a hunt and above ordinary trade: a guild that will not leave its forge to hold a square it has
    /// paid five thousand gold for is a guild whose claim was a waste of the money, and a bot that abandons
    /// the anvil every time anybody claims anything is a shard that stops making things.
    /// </summary>
    public static double Prior { get; set; } = 200.0;

    /// <summary>How long one of these is expected to take. The muster window, in minutes.</summary>
    public static double WorkMinutes => Math.Max(1.0, BotClaim.MusterMs / 60000.0);

    /// <summary>How near the middle of the square is near enough. Anywhere in the square counts.</summary>
    public static int Reach { get; set; } = 8;

    /// <summary>Musters stood through to the end.</summary>
    public static long Stood { get; private set; }

    /// <summary>Musters that ended early because the claim did.</summary>
    public static long Ended { get; private set; }

    private readonly Guild _guild;

    private readonly Map _map;

    private readonly Point3D _middle;

    public BotHold(Guild guild, Map map, Point3D middle)
    {
        _guild = guild;
        _map = map;
        _middle = middle;
    }

    public override string Kind => Trade;

    /// <summary>The square being held. For the staker's count of who is already on it.</summary>
    internal Point3D Middle => _middle;

    public override Map Map => _map;

    public override Point3D Where => _middle;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override bool Unpaid => true;

    public override string Stage => $"holding the square at {_middle.X},{_middle.Y} for {_guild?.Name}";

    /// <summary>The guild's muster: a call from outside the bot's own business. See <see cref="BotDeed.Summons"/>.</summary>
    public override bool Summons => true;

    /// <summary>
    /// And once answered, kept: standing for the guild is not given up for the next order on the board. See
    /// <see cref="BotDeed.Steadfast"/>.
    ///
    /// <para>
    /// In the first twenty-two minutes of build 34 this errand was still dropped 29% of the time, seven of ten
    /// drops "outbid by order" — a member walking off its guild's square to put an order on the board, which is
    /// work that would have been exactly as available when the muster closed. The claim's own clock ends it, and
    /// nothing in here keeps time, so there is nothing to put right on resuming.
    /// </para>
    /// </summary>
    public override bool Steadfast => true;

    /// <summary>
    /// Standing in the square on purpose, once the bot is in it.
    ///
    /// <para>
    /// <b>The stall watch took members off the muster for doing exactly what the muster asks.</b> Holding a
    /// square is standing on one tile saying the same thing about itself for the length of the window, which
    /// is the precise definition of stuck the watch tests for: eight members were cancelled that way on the
    /// morning of 14.09.2026, four of them inside three minutes on the Crown's square at 1305,1305. The claim
    /// has its own clock — it ends when <c>BotClaim.Making</c> stops answering — so the watch has nothing to
    /// add while the bot is in the square. On the walk there it still watches, because a member that cannot
    /// reach the square is exactly what the watch is for.
    /// </para>
    /// </summary>
    public override bool Still => _standing;

    private bool _standing;

    public override bool Bend(IBotWilful bot) => false;

    /// <summary>
    /// Where in the square to walk to: the nearest tile to its middle that a body can stand on, found once.
    ///
    /// <para>
    /// <b>The middle is arithmetic, and so was its height.</b> A claim's middle is the square's centre with z nought, and
    /// in the window 16:05–16:47 on 25.09.2026 thirty stakes failed "no way through to (1365, 1335, 0)", (1365, 1365, 0),
    /// (1335, 1335, 0) and so on — land-grabs round The Anchor's new hall, where the centres fall on roofs, walls and
    /// water. Arriving is being anywhere in the square (see Advance), so the walk only needs somewhere in it that is
    /// ground: the first tile a body fits on, ring by ring out from the middle, with its own height.
    /// </para>
    /// </summary>
    private Point3D Footing()
    {
        if (_footing != Point3D.Zero || _map == null)
        {
            return _footing == Point3D.Zero ? _middle : _footing;
        }

        for (var ring = 0; ring <= FootingRings; ring++)
        {
            for (var dx = -ring; dx <= ring; dx++)
            {
                for (var dy = -ring; dy <= ring; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != ring)
                    {
                        continue;
                    }

                    if (BotStep.Settle(_map, _middle.X + dx, _middle.Y + dy, out var z))
                    {
                        _footing = new Point3D(_middle.X + dx, _middle.Y + dy, z);

                        if (ring > 0)
                        {
                            Footed++;
                        }

                        return _footing;
                    }
                }
            }
        }

        _footing = _middle;

        return _footing;
    }

    private Point3D _footing;

    /// <summary>How far out from a square's middle a footing is looked for: half the square, so it stays inside it.</summary>
    public static int FootingRings { get; set; } = 12;

    /// <summary>Stakes that walked to ground beside a square's middle because nothing could stand on the middle itself.</summary>
    public static long Footed { get; private set; }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _guild == null)
        {
            return BotDoing.Failed("no body");
        }

        var bid = BotClaim.Making(_guild);

        // Settled, lost or struck out while this bot was walking. Either way there is nothing to stand for.
        if (bid == null)
        {
            Ended++;

            return BotDoing.Done($"the claim on {_middle.X},{_middle.Y} is decided");
        }

        // <b>Standing in the square is the point; the middle is only where to walk.</b> The muster counts
        // members by the square's own key (see BotClaim.Beat), and a square is thirty tiles across — so a
        // member already inside it has arrived, wherever the middle is. Until 06:50 on 14.09.2026 the middle
        // was the test, and the Lantern's square at 1335,1395 had the Blade's carried hall between its
        // members and its middle: Dain, Emrys and Bryn Ashdown each stood inside the square, walked at a
        // wall, gave up a dozen times and were carried home a hundred and fifty tiles.
        if (!body.InRange(_middle, Reach) && BotQuad.Key(_map, body.Location) != BotQuad.Key(_map, _middle))
        {
            _standing = false;

            return BotDoing.Walk(_map, Footing(), BotArrival.Within(Reach), $"to the square at {_middle.X},{_middle.Y}");
        }

        _standing = true;
        _arrived = true;
        Stood++;

        return BotDoing.Work($"standing for {_guild.Name} on {_middle.X},{_middle.Y}");
    }

    private bool _arrived;

    /// <summary>
    /// Over, however it ended — and if it ended without this bot ever standing in the square, the staker is told
    /// so. See <see cref="BotHolder.Missed"/>.
    /// </summary>
    public override void Drop(IBotWilful bot)
    {
        if (!_arrived && bot?.Self is Mobile body && BotClaim.Making(_guild) != null)
        {
            BotHolder.Missed(body, _middle);
        }

        base.Drop(bot);
    }

    public static string Describe() =>
        Stood + Ended == 0 ? "nobody has stood for a claim" : $"{Stood} beats stood on claimed ground, {Ended} musters that outlived their claim, {Footed} walks to ground beside a middle nothing could stand on";

    public static void Forget()
    {
        Stood = 0;
        Ended = 0;
        Footed = 0;
    }
}

/// <summary>
/// Decides which square a guild wants, declares the claim, and sends its members to stand on it.
///
/// <para>
/// <b>The declaring and the standing are one proposer because the first member to be asked does both.</b>
/// <c>BotClaim.Open</c> refuses a second claim from the same guild, so whichever member is offered work
/// first opens the claim and the rest are handed the standing. Splitting them would need an officer, a claim
/// on the officer, and a way for the officer to tell the others — which is three mechanisms to arrange what
/// one dictionary entry already arranges.
/// </para>
///
/// <para>
/// <b>What it wants, in order.</b> Open ground first while the free claims last, because free ground is the
/// cheapest thing a guild can own. Then somebody else's, if the purse will stand it — and it takes the
/// square for itself rather than merely striking a name off it whenever it can afford to, because a square
/// held is worth more than a square denied. Only then open ground at the full price.
/// </para>
/// </summary>
public sealed class BotHolder : IBotProposer
{
    /// <summary>How far from its hall a guild will look for ground to claim.</summary>
    public static int Look { get; set; } = 180;

    /// <summary>How dangerous a square may be and still be worth claiming. See <c>BotQuad.Band</c>.</summary>
    public static double Worst { get; set; } = BotQuad.Unsafe;

    /// <summary>Bots looked at.</summary>
    public static long Asked { get; private set; }

    /// <summary>Times somebody was sent to stand on ground already claimed.</summary>
    public static long Sent { get; private set; }

    /// <summary>Claims opened.</summary>
    public static long Opened { get; private set; }

    /// <summary>Times the guild had no hall, so no reason to want ground near anything.</summary>
    public static long Landless { get; private set; }

    /// <summary>How many beyond the muster may be sent to one square.</summary>
    public static int Spare { get; set; } = 1;

    /// <summary>
    /// How long a member that set out for a square and never stood in it is not sent to that square again.
    ///
    /// <para>
    /// <b>The square's own refusal memory cannot hold this, because the square is not the trouble.</b> On
    /// 21.09.2026 Fenna, founded with The Crown north-west of the river, was sent to the guild's claim at
    /// 1365,1365 from 279 tiles off: "the walk stopped closing 284 tiles short … having set out 279 off and got
    /// no nearer than 279", two minutes of a walk that went nowhere — four times in ten minutes, and the log's
    /// own account of who burned the whole search ceiling began "to the square at 1365,1365 819". The walk that
    /// gives up writes the square into <c>BotRefused</c>, which the staker reads; but three guildmates on the
    /// near bank walked to the same square and <em>arriving disproves a refusal</em>, so the note was gone before
    /// Fenna was next asked. A place nobody can reach and a place this bot cannot reach are different facts, and
    /// only the first had anywhere to be written. A stake is unpaid, so nothing is learnt from failing it, and
    /// each failure takes over a minute, so six never fall inside the breaker's five.
    /// </para>
    ///
    /// <para>
    /// Ten minutes: twice a claim's clock, so it outlasts the claim it was earned on and lapses before the guild
    /// has opened many more. Written on any ending that was not an arrival — a bot called away to a rescue on
    /// the way is kept off one claim it might have reached, which costs the guild nothing: the muster is three
    /// and a spare, out of ten.
    /// </para>
    /// </summary>
    public static int MissedMs { get; set; } = 600000;

    /// <summary>Members not sent to a square they lately set out for and never stood in.</summary>
    public static long Shy { get; private set; }

    private static readonly Dictionary<Serial, (Point3D Middle, long Tick)> _missed = [];

    /// <summary>Told by <see cref="BotHold"/> when a member's walk to a square ended somewhere else.</summary>
    public static void Missed(Mobile body, Point3D middle)
    {
        if (body != null)
        {
            _missed[body.Serial] = (middle, Core.TickCount);
        }
    }

    /// <summary>Members not sent because the square already had its muster and its spare.</summary>
    public static long Enough { get; private set; }

    /// <summary>How many of this guild are holding, or on their way to hold, the square in the middle.</summary>
    private static int Holding(Guild ours, Point3D middle)
    {
        var members = ours?.Members;

        if (members == null)
        {
            return 0;
        }

        var holding = 0;

        for (var i = 0; i < members.Count; i++)
        {
            if (members[i] is BotMobile { Deleted: false, Resolve.Deed: BotHold hold } && hold.Middle == middle)
            {
                holding++;
            }
        }

        return holding;
    }

    /// <summary>Times there was no square worth claiming within reach.</summary>
    public static long Nothing { get; private set; }

    /// <summary>Times the guild wanted one and could not pay for it.</summary>
    public static long Poor { get; private set; }

    /// <summary>Members not sent because the ledger has proved they cannot reach the square from where they stand.</summary>
    public static long Walled { get; private set; }

    /// <summary>Squares passed over because their middle cannot be reached from the hall at all.</summary>
    public static long Unreachable { get; private set; }

    /// <summary>Squares passed over because this guild lately failed to muster on them. See <c>BotClaim.Resting</c>. Counts looks.</summary>
    public static long Rested { get; private set; }

    /// <summary>
    /// Looks in which the guild passed over at least one square it lately failed to muster on: the claims held off by the rest,
    /// counted once a look rather than once a square (06.10.2026, build 408). See <c>BotClaim.DescribeMoney</c>.
    /// </summary>
    public static long RestedLooks { get; private set; }

    /// <summary>
    /// Members not sent to a claim because the refusal memory holds its square — a walk there lately gave up. A stake is
    /// unpaid and learns nothing from failing, so this is the gate its commonest failure needed (A4).
    /// </summary>
    public static long Refused { get; private set; }

    public string Name => "staker";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotEstate.Running || !BotClaim.Running)
        {
            return null;
        }

        if (bot?.Self is not BotMobile { Deleted: false, Alive: true } body || body.Map == null ||
            body.Map == Map.Internal)
        {
            return null;
        }

        if (body.Guild is not Guild ours)
        {
            return null;
        }

        // The Shadow holds no ground. See BotUnderworld.Band and build 129.
        if (BotUnderworld.Band(ours))
        {
            return null;
        }

        Asked++;

        // Already claiming something: everybody else goes and stands on it.
        var bid = BotClaim.Making(ours);

        if (bid != null)
        {
            // <b>Asked here, because the errand will ask it and refuse.</b> 858 stakes on one square in ten
            // minutes on 13.09.2026, every one ending "no way through to (1455, 1455, 0)" without a step
            // taken: the square's middle was ground the reach ledger had proved shut, the errand was refused
            // on its first plan, and the offer was made again on the next auction. See enlist's Walled for
            // the same gate, and BotHolder.Walled for how often this one shuts.
            if (BotReach.Ask(body.Map, body.Location, bid.Middle, BotArrival.Within(BotHold.Reach)) == BotReachVerdict.Sealed)
            {
                Walled++;

                return null;
            }

            // <b>And not a square whose road has lately given up on somebody.</b> A stake is unpaid, so failing it teaches
            // the ledger nothing and the offer came back unchanged: at 1065,1395 on 15.09.2026 Emrys failed six times in six
            // seconds, Dain six in six, Wystan six in 78, Gwendra six in 232 — each until the breaker rested it, and then
            // the next member was sent. The walk that gives up writes the square into the refusal memory; this reads it.
            if (BotRefused.Refusing(body.Map, bid.Middle))
            {
                Refused++;

                return null;
            }

            // And not a square this member itself lately set out for and never reached. See MissedMs.
            if (_missed.TryGetValue(body.Serial, out var miss) && miss.Middle == bid.Middle
                && Core.TickCount - miss.Tick < MissedMs)
            {
                Shy++;

                return null;
            }

            // <b>A claim needs its muster and one to spare, not the guild.</b> At the stake's full claim
            // (build 16, 02:49 on 14.09.2026) twenty-seven of eighty bots were holding squares five minutes
            // after the start, nine of the Lantern's within two tiles of one another at 1433,1461, walling
            // each other in until the stall watch carried them off. The muster is three; the fourth is a
            // spare for a death; the fifth is a bot with better things to do.
            if (Holding(ours, bid.Middle) >= BotClaim.Gather + Spare)
            {
                Enough++;

                return null;
            }

            Sent++;

            return new BotHold(ours, bid.Map, bid.Middle);
        }

        var hall = BotEstate.Hall(ours);

        if (hall is not { Deleted: false } || hall.Map != body.Map)
        {
            Landless++;

            return null;
        }

        var fund = BotEstate.Fund(ours);
        var mine = BotClaim.Holds(ours.Name);

        if (!Want(ours, hall, fund, mine, out var middle, out var want))
        {
            return null;
        }

        var opened = BotClaim.Open(ours, hall.Map, middle, want);

        if (opened == null)
        {
            return null;
        }

        Opened++;

        return new BotHold(ours, opened.Map, opened.Middle);
    }

    /// <summary>
    /// The square this guild wants next, and what it wants it for.
    ///
    /// <para>
    /// <b>Ring by ring round the hall, from 25.09.2026 — Patrick's order: "they must take territory in a circle round
    /// their hall, not make a long chain of captures".</b> The choice was the nearest square out of <c>BotQuad.All</c>,
    /// the squares the population had walked, and the population walks the roads: the nearest known square beyond a
    /// guild's own was the next one down the road, and the guilds' ground grew along them — The Blade held 49 squares
    /// across the island on 24.09. Now the first ring round the hall's own square comes first, then the second, out to
    /// <see cref="Look"/>; inside a ring, the square that touches most of the guild's ground wins, then the nearest. A
    /// square nobody has walked yet is still offered — the reach ledger refuses only ground it has proved sealed, and
    /// the settled middle and the stake's footing do the rest — and one proved dangerous is not.
    /// </para>
    /// </summary>
    private static bool Want(Guild ours, BaseHouse hall, int fund, int mine, out Point3D middle, out BotClaim.Kind want)
    {
        middle = Point3D.Zero;
        want = BotClaim.Kind.Settle;

        var map = hall.Map;
        var home = BotQuad.Key(map, hall.Location);
        var rings = Math.Max(1, Look / BotQuad.Side);
        var poor = false;
        var found = -1;
        var rested = Rested;

        for (var ring = 0; ring <= rings && found < 0; ring++)
        {
            var bestTouch = -1;
            var bestGap = int.MaxValue;

            for (var dx = -ring; dx <= ring; dx++)
            {
                for (var dy = -ring; dy <= ring; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != ring)
                    {
                        continue;
                    }

                    var at = new Point3D(
                        (home.X + dx) * BotQuad.Side + BotQuad.Side / 2,
                        (home.Y + dy) * BotQuad.Side + BotQuad.Side / 2,
                        0
                    );

                    if (!Worth(ours, hall, map, fund, at, ref poor, out var settled, out var kind))
                    {
                        continue;
                    }

                    var touch = Touching(ours.Name, map, home.X + dx, home.Y + dy);

                    // <b>Only ground that touches the guild's own.</b> A square held stays held only while it reaches the
                    // hall through the guild's squares (BotClaim.Connect, Patrick's order of 26.09.2026), so a claim on
                    // ground touching none of them would fall the moment it was won.
                    if (ring > 0 && touch == 0)
                    {
                        continue;
                    }

                    var gap = Math.Max(Math.Abs(at.X - hall.X), Math.Abs(at.Y - hall.Y));

                    if (touch < bestTouch || touch == bestTouch && gap >= bestGap)
                    {
                        continue;
                    }

                    bestTouch = touch;
                    bestGap = gap;
                    middle = settled;
                    want = kind;
                    found = ring;
                }
            }
        }

        if (Rested > rested)
        {
            RestedLooks++;
        }

        if (found < 0)
        {
            if (poor)
            {
                Poor++;
            }
            else
            {
                Nothing++;
            }

            return false;
        }

        if (found > 1)
        {
            Outward++;
        }

        return true;
    }

    /// <summary>Claims opened beyond the first ring round the hall, because that ring was the guild's already or could not be had.</summary>
    public static long Outward { get; private set; }

    /// <summary>Held squares passed over because the guild weighed the war and the price and would not seize. Counts looks. See <c>BotSeizure.Weigh</c>.</summary>
    public static long Weighed { get; private set; }

    /// <summary>How many of the eight squares round this one the guild holds.</summary>
    private static int Touching(string guild, Map map, int qx, int qy)
    {
        var touch = 0;

        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if ((dx != 0 || dy != 0)
                    && BotClaim.Owner(
                        map,
                        new Point3D((qx + dx) * BotQuad.Side + BotQuad.Side / 2, (qy + dy) * BotQuad.Side + BotQuad.Side / 2, 0)
                    ) == guild)
                {
                    touch++;
                }
            }
        }

        return touch;
    }

    /// <summary>
    /// Whether this square could be claimed by this guild now, and where and how: the old choice's tests, asked of one
    /// square at a time. See <see cref="Want"/>.
    /// </summary>
    private static bool Worth(Guild ours, BaseHouse hall, Map map, int fund, Point3D at, ref bool poor, out Point3D settled, out BotClaim.Kind want)
    {
        settled = Point3D.Zero;
        want = BotClaim.Kind.Settle;

        // Proved dangerous; a square never walked has proved nothing either way.
        if (BotQuad.Known(map, at) is { } quad && quad.Safety < Worst)
        {
            return false;
        }

        var owner = BotClaim.Owner(map, at);

        if (owner == ours.Name)
        {
            return false;
        }

        // <b>Never ground where a town's guards act</b>, open or another guild's (Patrick, 07.10.2026): such a square is not
        // claimed and not seized, so a held one is never weighed for a war either. See BotTownGround.
        if (BotTownGround.Guarded(map, at))
        {
            BotTownGround.Passed(owner != null);

            return false;
        }

        // <b>Not open ground another guild's hall stands partly in</b> (06.10.2026). A hall standing on another guild's ground is
        // carried off it (BotBounds), so a claim on the open square its walls reach into would carry a neighbour's hall away
        // without a war. Halls raised from now on stand wholly in their own square; this keeps the older ones where they are.
        if (owner == null && BotBounds.HallIn(ours.Name, map, BotQuad.Key(map, at)) != null)
        {
            BotBounds.Spare();

            return false;
        }

        // Not a square this guild lately failed to muster on. See BotClaim.UnmusteredRestMs.
        if (BotClaim.Resting(ours.Name, BotQuad.Key(map, at)))
        {
            Rested++;

            return false;
        }

        // <b>Another guild's square is a war at once and a price in everybody's regard (Patrick, 06.10.2026).</b> Weighed before
        // it is paid: a guild that could not carry the war, or whose name is already worth too little, takes open ground or
        // nothing. See BotSeizure.Weigh, which counts its refusals by cause once a weighing, not once a square.
        if (owner != null && !BotSeizure.Weigh(ours, owner))
        {
            Weighed++;

            return false;
        }

        var kind = owner == null
            ? BotClaim.Kind.Settle
            : fund >= BotClaim.Ousting || !BotClaim.Bought(map, at)
                ? BotClaim.Kind.Oust
                : BotClaim.Kind.Strip;

        if (fund < BotClaim.Cost(ours.Name, BotQuad.Key(map, at), kind))
        {
            poor = true;

            return false;
        }

        settled = BotClaim.Middle(map, at);

        if (settled == Point3D.Zero)
        {
            return false;
        }

        // A square nobody of ours can walk to is a claim that can never muster: the money goes and the members queue
        // up to be refused. The ledger only ever says Sealed about ground it has proved.
        if (BotReach.Ask(map, hall.Location, settled, BotArrival.Within(BotHold.Reach), tally: false) == BotReachVerdict.Sealed)
        {
            Unreachable++;
            settled = Point3D.Zero;

            return false;
        }

        want = kind;

        return true;
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been looked at for a claim"
            : $"the staker looked {Asked} times: {Opened} claims opened, {Sent} sent to stand on one, "
            + $"{Enough} not sent because the square already had its muster of {BotClaim.Gather} and {Spare} to spare, {Landless} guilds had no hall to want ground near, {Nothing} found no square worth claiming within "
            + $"{Look} tiles, {Poor} could not pay for the one they wanted, {Unreachable} passed over a square the hall cannot reach; {Walled} members were not sent because they cannot reach it from where they stand, {Refused} because a walk to it lately gave up and {Shy} because their own walk to it lately ended somewhere else; {Rested} squares passed over where the guild lately failed to muster, {Weighed} held squares passed over because it weighed the seizure and would not pay its price, {Outward} claims opened beyond the first ring round the hall; {BotHold.Describe()}";

    public static void Forget()
    {
        Asked = 0;
        Sent = 0;
        Opened = 0;
        Landless = 0;
        Nothing = 0;
        Poor = 0;
        Walled = 0;
        Unreachable = 0;
        Rested = 0;
        RestedLooks = 0;
        Outward = 0;
        Weighed = 0;
        Refused = 0;
        Shy = 0;
        _missed.Clear();
    }
}
