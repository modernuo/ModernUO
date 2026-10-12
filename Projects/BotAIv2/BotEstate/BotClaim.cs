using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What a guild has claimed of the island itself, square by square, and how a claim is won.
///
/// <para>
/// <b>Patrick's order of 10.09.2026.</b> A guild may lay claim to a quadrant — four of them for nothing and
/// five thousand gold for each one after that. Having declared, it gathers there; if no other guild comes
/// and beats it, the square passes into that guild's zone of influence. Another guild may lay claim to a
/// square somebody already holds, and chooses as it does: five thousand to take the square for itself, or
/// two and a half to strike the holder's name off it and leave it to nobody. A square somebody took with one
/// of their free claims may be claimed against for nothing at all.
/// </para>
///
/// <para>
/// <b>Why a square rather than a circle.</b> <c>BotQuad</c> already cuts the island into thirty-tile squares
/// and the population already has an opinion about each one — how safe it is, what lives there, how often
/// somebody was hit. Land a guild can own had to be the same shape as land the population can talk about, or
/// the map would have carried two different griddings of the same island and every question would have had
/// to say which it meant.
/// </para>
///
/// <para>
/// <b>Held is a register and land near a hall is not.</b> <c>BotLand</c> computes its claim from where the
/// halls stand, deliberately, so it can never go stale. This cannot be computed from anything — it is the
/// record of something that happened — so it is kept, and it is cleared on a world reload like every other
/// board on this shard.
/// </para>
///
/// <para>
/// <b>Claiming somebody's square is an act against them and is filed as one.</b> See
/// <c>BotRegard.Claimed</c>: it costs the holder's opinion of the claimant about half what a killing costs,
/// so two claims against the same guild is roughly a war. That is the whole of how a contested claim comes
/// to be fought over — the fighting itself belongs to <c>BotFeud</c>, which needs a war and will have one.
/// Since 06.10.2026 the war is not left to the opinion: the claim is a war at once and has a price in everybody's
/// regard (<c>BotSeizure</c>), and the staker weighs both before it declares (<c>BotHolder</c>).
/// </para>
/// </summary>
public static class BotClaim
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotClaim));

    /// <summary>Whether guilds may own ground at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How many squares a guild may take without paying. By order: four.</summary>
    public static int Free { get; set; } = 4;

    /// <summary>What each one after those costs the guild.</summary>
    public static int Price { get; set; } = 5000;

    /// <summary>
    /// How many squares a guild may hold at <see cref="Price"/> before each further one costs <see cref="Step"/>
    /// more than the last. Ten, by Patrick's order of 16.09.2026: the eleventh is 6000, the twelfth 7000, and so on.
    /// </summary>
    public static int Cheap { get; set; } = 10;

    /// <summary>What each square past <see cref="Cheap"/> adds to the price of the next.</summary>
    public static int Step { get; set; } = 1000;

    /// <summary>What it costs to claim a square another guild holds and keep it.</summary>
    public static int Ousting { get; set; } = 5000;

    /// <summary>And to claim one only to strike their name off it.</summary>
    public static int Stripping { get; set; } = 2500;

    /// <summary>
    /// How long the guild has to hold the square before it is theirs.
    ///
    /// Five minutes: long enough that somebody who minds can walk over from anywhere on the island, short
    /// enough that a guild is not standing in a field for an evening. This is the window in which "did
    /// anybody come and fight them" is a question with an answer.
    /// </summary>
    public static int MusterMs { get; set; } = 300000;

    /// <summary>
    /// How many of the guild have to be seen in the square at once for the gathering to count.
    ///
    /// Three, and it is the peak over the window rather than the number standing there at the end: bots
    /// wander, an errand takes one away for thirty seconds, and a rule that samples once would be a rule
    /// about luck. A guild of five that never gets three of its members onto the same square has not
    /// gathered, whatever it declared.
    /// </summary>
    public static int Gather { get; set; } = 3;

    /// <summary>How often the claims are looked at.</summary>
    public static int LookMs { get; set; } = 5000;

    /// <summary>Claims declared.</summary>
    public static long Declared { get; private set; }

    /// <summary>Claims that ran their course and took the square.</summary>
    public static long Won { get; private set; }

    /// <summary>Claims lost because the guild never gathered.</summary>
    public static long Unmustered { get; private set; }

    /// <summary>
    /// How long a square rests, for the guild whose muster on it failed, before that guild claims it again. Doubles with
    /// each failed muster there in a row, up to <see cref="MostUnmusteredRestMs"/>; a muster that succeeds clears it.
    ///
    /// <para>
    /// <b>A failed muster is a fact about a guild and a piece of ground, and nothing remembered it.</b> At 17:04:38 on
    /// 15.09.2026 The Lantern laid claim to 1065,1395, a hundred and twenty-five tiles west of its hall. Nobody reached it —
    /// 55 walks there ended "no way round it was found in 150ms" in nine minutes — and the claim ran out "never gathered" at
    /// 17:09:38; the same guild laid claim to the same square the next second, and again at 17:14:39, because it was still
    /// the nearest square it wanted. Every window sent its members walking at it again: stakes failed 22 times in the
    /// minute 17:07 and seven to fourteen times a minute after.
    /// </para>
    /// </summary>
    public static int UnmusteredRestMs { get; set; } = 1800000;

    /// <summary>The ceiling on that doubling: four hours.</summary>
    public static int MostUnmusteredRestMs { get; set; } = 14400000;

    /// <summary>Failed musters by guild and square: how many in a row, and when the last one ran out.</summary>
    private static readonly Dictionary<(string Guild, (int Map, int X, int Y) Key), (int Failures, long Tick)> _unmustered = [];

    /// <summary>Whether this guild lately failed to muster on this square. See <see cref="UnmusteredRestMs"/>.</summary>
    public static bool Resting(string guild, (int Map, int X, int Y) key)
    {
        if (guild == null || !_unmustered.TryGetValue((guild, key), out var row))
        {
            return false;
        }

        var rest = Math.Min((long)UnmusteredRestMs << Math.Min(row.Failures - 1, 10), MostUnmusteredRestMs);

        return Core.TickCount - row.Tick < rest;
    }

    /// <summary>Claims lost because somebody came and beat them.</summary>
    public static long Beaten { get; private set; }

    /// <summary>Claims that only struck a name off a square, having been paid for at the lower price.</summary>
    public static long Stripped { get; private set; }

    /// <summary>What the guilds have paid for ground.</summary>
    public static long Paid { get; private set; }

    /// <summary>What a claim is for.</summary>
    public enum Kind
    {
        /// <summary>Ground nobody holds.</summary>
        Settle,

        /// <summary>Somebody's ground, and the claimant means to keep it.</summary>
        Oust,

        /// <summary>Somebody's ground, and the claimant only means them not to have it.</summary>
        Strip
    }

    /// <summary>A square somebody holds.</summary>
    private sealed class Holding
    {
        public string Guild;

        /// <summary>Whether it was paid for. A square taken with a free claim may be contested for nothing.</summary>
        public bool Bought;
    }

    /// <summary>A claim in progress.</summary>
    public sealed class Bid
    {
        public string Guild;

        public Kind Want;

        public Map Map;

        /// <summary>The middle of the square, which is where the gathering is asked to stand.</summary>
        public Point3D Middle;

        public (int Map, int X, int Y) Key;

        public long Began;

        /// <summary>The most of the claimant's guild seen in the square at once.</summary>
        public int Peak;

        /// <summary>Of the enemy brought down in the square during the window, and of ours.</summary>
        public int Felled;

        public int Fallen;

        /// <summary>Whom it was taken off, for the log.</summary>
        public string From;

        /// <summary>
        /// The price taken off the guild and held until the muster is decided: paid to the city if three gather, back to the
        /// guild's chest if they never do. Nought for a free claim and for a claim on held ground, which pays at the
        /// declaration. See <see cref="Open"/> and <see cref="Close"/>.
        /// </summary>
        public int Escrow;

        /// <summary>
        /// The price paid to the city at the declaration, for a claim on held ground; nought otherwise, and nought for a claim read
        /// back from the save (the store keeps only what is held). Handed back if the claim is called off for town ground.
        /// </summary>
        public int AtOnce;
    }

    // ---- The price held until the claim gathers (06.10.2026, build 408) ---------------------------------------------------

    /// <summary>Claim money paid to the city at the declaration, for claims on ground another guild holds (the seizure is the attempt).</summary>
    public static long PaidAtOnce { get; private set; }

    /// <summary>Claim money taken off guilds and held until their muster was decided, and over how many claims.</summary>
    public static long Escrowed { get; private set; }

    public static long EscrowedClaims { get; private set; }

    /// <summary>Held money paid to the city because three gathered.</summary>
    public static long Released { get; private set; }

    public static long ReleasedClaims { get; private set; }

    /// <summary>Held money handed back to the guild's chest because three never gathered.</summary>
    public static long Returned { get; private set; }

    public static long ReturnedClaims { get; private set; }

    /// <summary>Held money paid to the city because the guild it was held for was gone when the muster ended.</summary>
    public static long Orphaned { get; private set; }

    /// <summary>What is held now, over the claims still mustering.</summary>
    public static int HeldNow(out int claims)
    {
        var held = 0;

        claims = 0;

        foreach (var bid in _bids.Values)
        {
            if (bid.Escrow > 0)
            {
                held += bid.Escrow;
                claims++;
            }
        }

        return held;
    }

    /// <summary>
    /// <b>The muster decided, the held price settled.</b> Three gathered: the price goes to the city, as it always did. They
    /// never did: the whole price goes back to the guild's chest, with a line saying so. Patrick's rule of 06.10.2026, "no money
    /// is made or destroyed quietly": on build 405 28,500gp (27 % of all claim money) went on claims that never gathered three
    /// — The Anchor paid 5000gp three times for 1845,2685 and stood two, nought and nought of itself on it.
    /// </summary>
    private static void Close(Bid bid, bool gathered)
    {
        var coin = bid.Escrow;

        if (coin <= 0)
        {
            return;
        }

        bid.Escrow = 0;

        if (gathered)
        {
            Paid += coin;
            Released += coin;
            ReleasedClaims++;
            BotCity.Tax(coin);

            logger.Information(
                "{Guild} gathered {Peak} on the square at {X},{Y}: the {Coin}gp held for the claim goes to the city's treasury",
                bid.Guild,
                bid.Peak,
                bid.Middle.X,
                bid.Middle.Y,
                coin
            );

            return;
        }

        // A guild closed while its members stood in the field has no chest to take the price back; the city takes it, as the
        // price would have gone there, and the line says so.
        if (BaseGuild.FindByName(bid.Guild) is not Guild { Disbanded: false })
        {
            Paid += coin;
            Orphaned += coin;
            BotCity.Tax(coin);

            logger.Information(
                "{Guild} is gone and never gathered on the square at {X},{Y}: the {Coin}gp held for the claim goes to the city's treasury",
                bid.Guild,
                bid.Middle.X,
                bid.Middle.Y,
                coin
            );

            return;
        }

        BotChest.Put(bid.Guild, coin);
        Returned += coin;
        ReturnedClaims++;

        logger.Information(
            "{Guild} never gathered on the square at {X},{Y}: the {Coin}gp held for the claim goes back to its chest; {Held}gp in it",
            bid.Guild,
            bid.Middle.X,
            bid.Middle.Y,
            coin,
            BotChest.Holds(bid.Guild)
        );
    }

    /// <summary>Held prices of claims the boot could not put back, owed to their guilds' chests at the first beat. See <see cref="Reopen"/>.</summary>
    private static readonly List<(string Guild, (int Map, int X, int Y) Key, int Coin)> _owed = [];

    /// <summary>The held prices still owed, for the store: written as claims, so a save before the first beat loses none.</summary>
    internal static IReadOnlyList<(string Guild, (int Map, int X, int Y) Key, int Coin)> Owed => _owed;

    private static void PayOwed()
    {
        for (var i = 0; i < _owed.Count; i++)
        {
            var (guild, key, coin) = _owed[i];

            if (BaseGuild.FindByName(guild) is not Guild { Disbanded: false })
            {
                Paid += coin;
                Orphaned += coin;
                BotCity.Tax(coin);
            }
            else
            {
                BotChest.Put(guild, coin);
                Returned += coin;
                ReturnedClaims++;
            }

            logger.Information(
                "{Guild}'s claim on the square at {X},{Y} could not be put back after the boot: the {Coin}gp held for it goes to {Where}",
                guild,
                key.X * BotQuad.Side + BotQuad.Side / 2,
                key.Y * BotQuad.Side + BotQuad.Side / 2,
                coin,
                BaseGuild.FindByName(guild) is Guild { Disbanded: false } ? "its chest" : "the city's treasury, the guild being gone"
            );
        }

        _owed.Clear();
    }

    /// <summary>The failed musters still resting, for the store: guild, square, failures in a row, and how long ago the last one ran out.</summary>
    internal static IEnumerable<(string Guild, (int Map, int X, int Y) Key, int Failures, long Ago)> Rests()
    {
        var now = Core.TickCount;

        foreach (var ((guild, key), (failures, tick)) in _unmustered)
        {
            var ago = now - tick;

            if (guild != null && ago >= 0 && ago < MostUnmusteredRestMs)
            {
                yield return (guild, key, failures, ago);
            }
        }
    }

    /// <summary>
    /// A failed muster read back from the save, its age kept. <b>The rest is in time the shard has run, and a restart used to
    /// end it</b>: the register of failures lived only in memory, so every boot let every guild claim again at once the squares it
    /// had just failed on. Builds came every hour or two on 06.10.2026.
    /// </summary>
    internal static void Rest(string guild, (int Map, int X, int Y) key, int failures, long ago)
    {
        if (string.IsNullOrEmpty(guild) || failures <= 0 || ago < 0)
        {
            return;
        }

        _unmustered[(guild, key)] = (failures, Core.TickCount - ago);
    }

    /// <summary>The claims' money, for the "Treasuries:" line. See <c>BotDues.Describe</c>.</summary>
    public static string DescribeMoney()
    {
        var now = HeldNow(out var mustering);

        return $"claims: {PaidAtOnce}gp paid to the city at the declaration on ground another guild held, {Escrowed}gp held over {EscrowedClaims} claims on open ground until {Gather} gathered "
            + $"({now}gp held now over {mustering}), {Released}gp of it paid to the city for {ReleasedClaims} that gathered, {Returned}gp handed back to the chests for {ReturnedClaims} that never gathered, "
            + $"{Orphaned}gp to the city for a guild gone before its muster ended; {BotHolder.RestedLooks} staker looks held off a square their guild lately failed to muster on "
            + $"({BotHolder.Rested} squares passed over; rest {UnmusteredRestMs / 60000} minutes, doubling to {MostUnmusteredRestMs / 60000}); the city took {BotCity.AboveCap}gp of the guilds' payments above its {BotCity.Cap}gp cap, kept rather than lost; {BotTownGround.Describe()}";
    }

    private static readonly Dictionary<(int Map, int X, int Y), Holding> _held = [];

    /// <summary>Every square anybody holds, for the map on the dashboard: which guild, which square, and whether it was paid for. See <c>BotWebSnapshot</c> (29.09.2026).</summary>
    public static IEnumerable<(string Guild, int Map, int X, int Y, bool Bought)> Holdings()
    {
        foreach (var (key, held) in _held)
        {
            if (held?.Guild != null)
            {
                yield return (held.Guild, key.Map, key.X, key.Y, held.Bought);
            }
        }
    }

    /// <summary>Squares dropped because they no longer reach their guild's hall through its own ground. See <see cref="Connect"/>.</summary>
    public static long CutOff { get; private set; }

    /// <summary>Halls razed because the square they stood in was taken. See <see cref="Settle"/>.</summary>
    public static long HallsLost { get; private set; }

    private static readonly HashSet<(int Map, int X, int Y)> _reached = [];

    private static readonly Queue<(int Map, int X, int Y)> _frontier = new();

    private static readonly List<((int Map, int X, int Y) Key, string Guild)> _cut = [];

    /// <summary>
    /// The square a hall stands in is its guild's own: its yard. Patrick's order of 26.09.2026: "the yard is only the square
    /// with the guild's hall; everything else is the zone of influence". Written when a hall is raised or adopted, as a
    /// bought holding — so it is taken only by an ousting claim at the full price, and taking it razes the hall.
    /// </summary>
    public static void Seat(string guild, Map map, Point3D hall)
    {
        if (!Running || guild == null || map == null || map == Map.Internal)
        {
            return;
        }

        var key = BotQuad.Key(map, hall);

        // <b>Never over another guild's holding</b> (06.10.2026). This line used to write the yard whoever held the square, so a hall
        // raised or adopted on another guild's square took it without a claim and without a war. The square stays its holder's
        // and the hall owes a move off it (BotBounds). The placements no longer offer such ground; this is the last wall.
        if (_held.TryGetValue(key, out var was) && was?.Guild != null && was.Guild != guild)
        {
            BotBounds.YardKept(guild, was.Guild, hall);

            return;
        }

        _held[key] = new Holding { Guild = guild, Bought = true };
    }

    /// <summary>
    /// Drops every square that no longer reaches its guild's hall through squares the same guild holds (touching at a side
    /// or a corner). Patrick's order of 26.09.2026: "squares live while from the square one can reach the square of the
    /// guild's hall; if the link is broken the claim falls off — three lands in a row, take the one between the first and
    /// the third, and the second goes to the other guild while the third breaks, because it no longer reaches the guild's
    /// lands". A guild with no hall standing holds nothing. Not asked until the halls have been adopted at the boot
    /// (<see cref="BotEstate.HallsAdopted"/>), because the claims are read back before the halls are.
    /// </summary>
    public static int Connect()
    {
        if (!Running || _held.Count == 0 || !BotEstate.HallsAdopted)
        {
            return 0;
        }

        _reached.Clear();

        foreach (var (name, hall) in BotEstate.Held)
        {
            if (hall is not { Deleted: false } || hall.Map == null || hall.Map == Map.Internal)
            {
                continue;
            }

            var root = BotQuad.Key(hall.Map, hall.Location);

            if (!_held.TryGetValue(root, out var seat))
            {
                continue;
            }

            _frontier.Clear();

            if (seat.Guild == name)
            {
                _frontier.Enqueue(root);
                _reached.Add(root);
            }
            else
            {
                // <b>A hall standing in another guild's square</b> (Seat kept the square its holder's, 06.10.2026) owes a move
                // and is carried off it; until then the guild's own squares touching the hall's still reach it, rather than all
                // falling at once for a square that was never the guild's.
                for (var dx = -1; dx <= 1; dx++)
                {
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        var next = (root.Map, root.X + dx, root.Y + dy);

                        if ((dx != 0 || dy != 0) && !_reached.Contains(next) && _held.TryGetValue(next, out var touching) && touching.Guild == name)
                        {
                            _reached.Add(next);
                            _frontier.Enqueue(next);
                        }
                    }
                }
            }

            while (_frontier.Count > 0)
            {
                var at = _frontier.Dequeue();

                for (var dx = -1; dx <= 1; dx++)
                {
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }

                        var next = (at.Map, at.X + dx, at.Y + dy);

                        if (!_reached.Contains(next) && _held.TryGetValue(next, out var held) && held.Guild == name)
                        {
                            _reached.Add(next);
                            _frontier.Enqueue(next);
                        }
                    }
                }
            }
        }

        // <b>A colony is a root of its guild's ground as a hall is</b> (07.10.2026, sea20): an island square no chain of the guild's
        // squares joins to its hall on the mainland stands by the colony on it, and so does the ground joined to it. See BotColonies.
        foreach (var (name, colonyMap, at) in BotColonies.Roots())
        {
            var root = BotQuad.Key(colonyMap, at);

            if (_reached.Contains(root) || !_held.TryGetValue(root, out var seat) || seat.Guild != name)
            {
                continue;
            }

            _frontier.Clear();
            _frontier.Enqueue(root);
            _reached.Add(root);

            while (_frontier.Count > 0)
            {
                var near = _frontier.Dequeue();

                for (var dx = -1; dx <= 1; dx++)
                {
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        var next = (near.Map, near.X + dx, near.Y + dy);

                        if ((dx != 0 || dy != 0) && !_reached.Contains(next) && _held.TryGetValue(next, out var held) && held.Guild == name)
                        {
                            _reached.Add(next);
                            _frontier.Enqueue(next);
                        }
                    }
                }
            }
        }

        _cut.Clear();

        foreach (var (key, held) in _held)
        {
            if (!_reached.Contains(key))
            {
                _cut.Add((key, held.Guild));
            }
        }

        for (var i = 0; i < _cut.Count; i++)
        {
            var (key, guild) = _cut[i];

            _held.Remove(key);
            CutOff++;

            logger.Warning(
                "{Guild}'s square at {X},{Y} no longer reaches its hall through its own ground and belongs to nobody now",
                guild,
                key.X * BotQuad.Side + BotQuad.Side / 2,
                key.Y * BotQuad.Side + BotQuad.Side / 2
            );
        }

        return _cut.Count;
    }

    private static readonly Dictionary<(int Map, int X, int Y), Bid> _bids = [];

    private static readonly Dictionary<string, Bid> _byGuild = [];

    private static long _looked;

    /// <summary>
    /// Which guild holds the square this point falls in, or nothing.
    ///
    /// <b>One dictionary lookup, because <c>BotLand.Holder</c> calls it and that runs a few thousand times a
    /// second.</b> Answered as a name for the same reason that one is: turning it into a <see cref="Guild"/>
    /// means a search of the engine's roll, and a name compares against <c>bot.Guild.Name</c> just as well.
    /// </summary>
    public static string Owner(Map map, Point3D where)
    {
        if (!Running || map == null || map == Map.Internal || _held.Count == 0)
        {
            return null;
        }

        return _held.TryGetValue(BotQuad.Key(map, where), out var held) ? held.Guild : null;
    }

    /// <summary>Whether this square is held by a guild that paid for it. See <see cref="Cost"/>.</summary>
    public static bool Bought(Map map, Point3D where) =>
        _held.TryGetValue(BotQuad.Key(map, where), out var held) && held.Bought;

    /// <summary>How many squares this guild holds.</summary>
    public static int Holds(string guild)
    {
        var many = 0;

        foreach (var held in _held.Values)
        {
            if (held.Guild == guild)
            {
                many++;
            }
        }

        return many;
    }

    /// <summary>The claim this guild is making now, or nothing.</summary>
    public static Bid Making(Guild guild) =>
        guild != null && _byGuild.TryGetValue(guild.Name, out var bid) ? bid : null;

    /// <summary>The claim being made on this square, or nothing.</summary>
    public static Bid On(Map map, Point3D where) =>
        map == null ? null : _bids.GetValueOrDefault(BotQuad.Key(map, where));

    /// <summary>
    /// What this claim would cost this guild.
    ///
    /// <para>
    /// Four rules in one place because they are one decision: nothing for the first
    /// <see cref="Free"/> squares of open ground, <see cref="Price"/> for open ground after that,
    /// <see cref="Ousting"/> or <see cref="Stripping"/> for somebody else's — and <b>nothing at all</b> for
    /// somebody else's that they never paid for, which is the order's own exception and the reason
    /// <see cref="Holding.Bought"/> is remembered.
    /// </para>
    /// </summary>
    public static int Cost(string guild, (int Map, int X, int Y) key, Kind want)
    {
        if (!_held.TryGetValue(key, out var held))
        {
            // The n-th square: free to Free, Price to Cheap, and Step more for each one past that.
            var nth = Holds(guild) + 1;

            if (nth <= Free)
            {
                return 0;
            }

            // A colony's own square at the plain price, "free points first, then 5000" (Patrick, 07.10.2026). See BotColonies.FlatClaim.
            if (BotColonies.Site(guild, key))
            {
                return Price;
            }

            return nth <= Cheap ? Price : Price + (nth - Cheap) * Step;
        }

        if (!held.Bought)
        {
            return 0;
        }

        return want == Kind.Strip ? Stripping : Ousting;
    }

    /// <summary>
    /// Declares one, taking the price out of the guild's purse.
    ///
    /// <para>
    /// The money leaves the guild when the claim is made. A claim on ground another guild holds pays the city at once: the
    /// seizure is a war or an act inside one (<see cref="BotSeizure"/>), and that is the attempt bought. A claim on open
    /// ground has its price held (<see cref="Bid.Escrow"/>) until the muster is decided: paid to the city if three gather,
    /// whether or not somebody then drives them off, and handed back whole to the guild's chest if they never gather
    /// (06.10.2026, build 408; see <see cref="Close"/>).
    /// </para>
    /// </summary>
    public static Bid Open(Guild guild, Map map, Point3D middle, Kind want)
    {
        if (!Running || guild == null || map == null || map == Map.Internal)
        {
            return null;
        }

        var key = BotQuad.Key(map, middle);

        if (_bids.ContainsKey(key) || _byGuild.ContainsKey(guild.Name))
        {
            return null;
        }

        var held = _held.GetValueOrDefault(key);

        if (held?.Guild == guild.Name)
        {
            return null;
        }

        // <b>No claim of any kind on ground where a town's guards act</b> (Patrick, 07.10.2026). The staker never offers such a
        // square; this is the last wall, before a coin moves. See BotTownGround.
        if (BotTownGround.Refuses(guild.Name, map, key))
        {
            return null;
        }

        var price = Cost(guild.Name, key, want);
        var escrow = 0;

        if (price > 0)
        {
            List<BotEstate.Contribution> purse = [];

            if (BotEstate.Levy(guild, price, purse) < price)
            {
                BotEstate.Refund(purse);

                return null;
            }

            if (held != null)
            {
                Paid += price;
                PaidAtOnce += price;

                // Into the city's treasury rather than out of the world: the one drain that used to vanish. See BotCity.Tax.
                BotCity.Tax(price);
            }
            else
            {
                // Held until the muster is decided. See Close.
                escrow = price;
                Escrowed += price;
                EscrowedClaims++;
            }
        }

        var bid = new Bid
        {
            Guild = guild.Name,
            Want = want,
            Map = map,
            Middle = middle,
            Key = key,
            Began = Core.TickCount,
            From = held?.Guild,
            Escrow = escrow,
            AtOnce = held != null ? price : 0
        };

        _bids[key] = bid;
        _byGuild[guild.Name] = bid;
        Declared++;

        // An act against the holder, filed where every other act between guilds is filed. This is what makes
        // a contested claim something anybody fights over: two of them is about a war's worth of ill will.
        if (held != null)
        {
            // <b>War at once, without a meeting, and the price of it in every guild's and every town's regard</b> (Patrick,
            // 06.10.2026). See BotSeizure. Before the holder's own opinion moves, so that opinion finds the war standing and
            // calls no meeting of its own.
            BotSeizure.Seize(guild, held.Guild, middle, want, price);

            // The holder's grievance, written in its book as before (BotRegard.HallClaim by its hall; Claim anywhere else).
            BotRegard.Claimed(guild.Name, held.Guild, Important(held.Guild, map, middle));

            // And the holder calls members back from their rest to stand on its square, when the square is one that
            // matters — its hall's or next to it. See Important and BotRest.Calls.
            if (Important(held.Guild, map, middle))
            {
                BotRest.Call(BotGuilds.Named(held.Guild), $"{guild.Name} laying claim to its square at {middle.X},{middle.Y}");
            }
            else
            {
                Unwoken++;
            }
        }

        logger.Warning(
            "{Guild} lays claim to the square at {X},{Y}{From} for {Price}gp; it has {Minutes} minutes to gather {Gather} there{Held}",
            guild.Name,
            middle.X,
            middle.Y,
            held == null ? "" : $", which {held.Guild} holds",
            price,
            MusterMs / 60000,
            Gather,
            escrow > 0 ? ", the price held until they do" : ""
        );

        return bid;
    }

    /// <summary>Somebody died inside a square somebody is claiming. Counts for whichever side it was.</summary>
    public static void Bled(Map map, Point3D where, string killer, string fallen)
    {
        var bid = On(map, where);

        if (bid == null || killer == null || fallen == null)
        {
            return;
        }

        if (killer == bid.Guild)
        {
            bid.Felled++;
        }
        else if (fallen == bid.Guild)
        {
            bid.Fallen++;
        }
    }

    /// <summary>
    /// The beat: samples who is standing in each claimed square, and settles the ones whose time is up.
    /// </summary>
    public static void Look()
    {
        if (!Running)
        {
            return;
        }

        var now = Core.TickCount;

        if (now - (_looked + LookMs) < 0)
        {
            return;
        }

        _looked = now;

        PayOwed();
        Sweep();
        TownSweep();
        Connect();

        // Ground that changed hands round a standing hall, seat or house. On its own clock. See BotBounds.
        BotBounds.Look();

        var bots = BotPopulation.Bots;
        List<Bid> over = [];

        foreach (var bid in _bids.Values)
        {
            var here = 0;

            for (var i = 0; i < bots.Count; i++)
            {
                var bot = bots[i];

                if (bot is not { Deleted: false, Alive: true } || bot.Guild?.Name != bid.Guild || bot.Map != bid.Map)
                {
                    continue;
                }

                if (BotQuad.Key(bot.Map, bot.Location) == bid.Key)
                {
                    here++;
                }
            }

            if (here > bid.Peak)
            {
                bid.Peak = here;
            }

            if (now - (bid.Began + MusterMs) >= 0)
            {
                over.Add(bid);
            }
        }

        for (var i = 0; i < over.Count; i++)
        {
            Settle(over[i]);
        }
    }

    /// <summary>Decides one claim whose window has run out.</summary>
    private static void Settle(Bid bid)
    {
        _bids.Remove(bid.Key);
        _byGuild.Remove(bid.Guild);

        // Whose it was when the window closed, for the hall that may stand in it. See Lost.
        var before = _held.TryGetValue(bid.Key, out var was0) ? was0.Guild : null;

        // The held price first, on the muster alone: three gathered pays the city even if they were then driven off.
        Close(bid, bid.Peak >= Gather);

        // <b>"Unless another guild beat them" is read as blood and nothing else.</b> Somebody who came and
        // watched has not beaten anybody; somebody who came, fought and killed more of the claimants than
        // they lost has. Equal blood leaves the claim standing, because the claimant is the one who has been
        // stood in the field for five minutes.
        if (bid.Fallen > bid.Felled)
        {
            Beaten++;

            logger.Information(
                "{Guild} was driven off the square at {X},{Y}: {Fallen} of theirs down to {Felled}",
                bid.Guild,
                bid.Middle.X,
                bid.Middle.Y,
                bid.Fallen,
                bid.Felled
            );

            return;
        }

        if (bid.Peak < Gather)
        {
            Unmustered++;
            _unmustered[(bid.Guild, bid.Key)] = (
                _unmustered.TryGetValue((bid.Guild, bid.Key), out var was) ? was.Failures + 1 : 1,
                Core.TickCount
            );

            logger.Information(
                "{Guild} never gathered on the square at {X},{Y}: {Peak} of them at most, {Gather} wanted",
                bid.Guild,
                bid.Middle.X,
                bid.Middle.Y,
                bid.Peak,
                Gather
            );

            return;
        }

        if (bid.Want == Kind.Strip)
        {
            _held.Remove(bid.Key);
            Stripped++;

            logger.Warning(
                "{Guild} has struck {From} off the square at {X},{Y}; it belongs to nobody now",
                bid.Guild,
                bid.From ?? "nobody",
                bid.Middle.X,
                bid.Middle.Y
            );

            Lost(before, bid);

            return;
        }

        _held[bid.Key] = new Holding
        {
            Guild = bid.Guild,
            Bought = Cost(bid.Guild, bid.Key, bid.Want) > 0 || bid.Want != Kind.Settle
        };

        Won++;
        _unmustered.Remove((bid.Guild, bid.Key));

        logger.Warning(
            "The square at {X},{Y} is {Guild}'s{From}",
            bid.Middle.X,
            bid.Middle.Y,
            bid.Guild,
            bid.From == null ? "" : $", taken from {bid.From}"
        );

        Lost(before, bid);
    }

    /// <summary>
    /// A square has changed hands. If it held the old holder's hall, the hall comes down (Patrick, 26.09.2026: "if the square
    /// with the hall is lost, the hall is razed and it may be moved to another square"); and whatever of the old holder's
    /// ground the loss cut off from its hall falls at once rather than at the next look.
    /// </summary>
    private static void Lost(string before, Bid bid)
    {
        if (before == null || before == bid.Guild)
        {
            return;
        }

        if (BotEstate.Held.TryGetValue(before, out var hall) && hall is { Deleted: false } && hall.Map == bid.Map
            && BotQuad.Key(hall.Map, hall.Location) == bid.Key)
        {
            HallsLost++;
            BotEstate.RazeHall(before, $"the square it stood in was taken by {bid.Guild}");
        }

        Connect();
    }

    /// <summary>Claims the winner of a war had running on the loser's ground, decided for it by the loser's capitulation.</summary>
    public static long Conceded { get; private set; }

    private static readonly List<Bid> _conceding = [];

    /// <summary>
    /// <paramref name="loser"/> has capitulated to <paramref name="winner"/> (06.10.2026, <c>BotWar</c>): every claim the winner has
    /// running on the loser's ground is decided now as if its muster had stood unbeaten — the square passes by the same
    /// <see cref="Settle"/> every claim ends in, a hall standing in it razed by <see cref="Lost"/> as for any square taken. A side that
    /// has given up does not hold the field for the rest of the five minutes. Returns how many.
    /// </summary>
    public static int Concede(string winner, string loser)
    {
        if (!Running || winner == null || loser == null)
        {
            return 0;
        }

        _conceding.Clear();

        foreach (var bid in _bids.Values)
        {
            if (bid.Guild == winner && bid.From == loser)
            {
                _conceding.Add(bid);
            }
        }

        for (var i = 0; i < _conceding.Count; i++)
        {
            var bid = _conceding[i];

            bid.Peak = Math.Max(bid.Peak, Gather);
            bid.Fallen = 0;
            Conceded++;

            logger.Warning(
                "{Loser} has capitulated: {Winner}'s claim on its square at {X},{Y} is decided for {Winner}",
                loser,
                winner,
                bid.Middle.X,
                bid.Middle.Y,
                winner
            );

            Settle(bid);
        }

        return _conceding.Count;
    }

    /// <summary>The middle of the square a point falls in, on the ground. Where a gathering is asked to stand.</summary>
    public static Point3D Middle(Map map, Point3D where)
    {
        var key = BotQuad.Key(map, where);
        var x = key.X * BotQuad.Side + BotQuad.Side / 2;
        var y = key.Y * BotQuad.Side + BotQuad.Side / 2;

        return BotStep.Settle(map, x, y, out var z) ? new Point3D(x, y, z) : Point3D.Zero;
    }

    /// <summary>Who holds this square, as a pin says it, or nothing. See <c>BotMarkers</c>.</summary>
    public static string Pin(Map map, Point3D where) =>
        map == null ? null : Short(_held.GetValueOrDefault(BotQuad.Key(map, where))?.Guild);

    /// <summary>And who is claiming it right now, the same way, or nothing.</summary>
    public static string Claiming(Map map, Point3D where) =>
        map == null ? null : Short(_bids.GetValueOrDefault(BotQuad.Key(map, where))?.Guild);

    /// <summary>
    /// Puts a square back in the register as it was read off the save. See <c>BotClaimStore</c>.
    ///
    /// No logging and no counters: this is the register being restored to what it already was, not anything
    /// happening. A claim counted here would report every restart as a night of conquest.
    /// </summary>
    public static void Restore(int facet, int x, int y, string guild, bool bought)
    {
        if (string.IsNullOrEmpty(guild))
        {
            return;
        }

        _held[(facet, x, y)] = new Holding { Guild = guild, Bought = bought };
    }

    /// <summary>
    /// Puts a claim that was still being made back on the board, with its window whole again.
    ///
    /// The guild paid when it declared; a restart in the middle of the muster must not be the shard keeping
    /// the money. See <c>BotClaimStore</c> for why the remaining seconds are not what comes back.
    /// </summary>
    public static bool Reopen(int facet, int x, int y, string guild, Kind want, string from, int escrow = 0)
    {
        if (string.IsNullOrEmpty(guild))
        {
            return false;
        }

        var map = Map.Maps[facet is >= 0 and < 0x100 ? facet : 0];
        var middle = map == null || map == Map.Internal
            ? Point3D.Zero
            : Middle(map, new Point3D(x * BotQuad.Side + BotQuad.Side / 2, y * BotQuad.Side + BotQuad.Side / 2, 0));

        if (_bids.ContainsKey((facet, x, y)) || _byGuild.ContainsKey(guild) || middle == Point3D.Zero)
        {
            // A claim that cannot be put back still holds its guild's money: it goes back to the chest, never out of the world.
            // On the first beat rather than here: the chests are read back after the claims, and reading them clears them.
            if (escrow > 0)
            {
                _owed.Add((guild, (facet, x, y), escrow));
            }

            return false;
        }

        var bid = new Bid
        {
            Guild = guild,
            Want = want,
            Map = map,
            Middle = middle,
            Key = (facet, x, y),
            Began = Core.TickCount,
            // The store writes nobody as an empty name, and an empty name read back is a holder: "taken from " in the
            // log, and a rest call every twenty seconds for a guild called nothing (BotRest reads From as contested).
            From = string.IsNullOrEmpty(from) ? null : from,
            Escrow = Math.Max(0, escrow)
        };

        _bids[bid.Key] = bid;
        _byGuild[guild] = bid;

        return true;
    }

    private static readonly List<((int Map, int X, int Y) Key, string Guild, bool Bought, string Town, int Tiles)> _town = [];

    private static readonly List<(Bid Bid, string Town)> _townBids = [];

    /// <summary>
    /// <b>Town ground swept</b> (Patrick, 07.10.2026 ~10:20: "guilds may not claim town squares with guards. Remove such claims
    /// right now and give the guilds their money back if they paid for the claim"). Once the halls are adopted and every hour
    /// after (<see cref="BotTownGround.Due"/>): every held square on ground where a town's guards act is dropped and its price
    /// handed back out of the city's purse into the guild's chest — the price the logs show where
    /// <c>Configuration/bot-townground.json</c> records it, else <see cref="Price"/> for a square paid for (a holding records that
    /// it was bought, not what for), and nothing for a free one; a hall's own square, its yard, is kept
    /// (<see cref="Seat"/>). A claim still mustering on such ground is called off, its money back to the chest. Before
    /// <see cref="Connect"/>, so ground cut off from its hall by a drop falls on the same look.
    /// </summary>
    private static void TownSweep()
    {
        if (!BotTownGround.Due())
        {
            return;
        }

        BotTownGround.Begin();
        _town.Clear();
        _townBids.Clear();

        foreach (var (key, held) in _held)
        {
            var map = key.Map is >= 0 and < 0x100 ? Map.Maps[key.Map] : null;

            if (held?.Guild == null || map == null || !BotTownGround.Guarded(map, key, out var town, out var tiles))
            {
                continue;
            }

            if (BotEstate.Held.TryGetValue(held.Guild, out var hall) && hall is { Deleted: false } && hall.Map == map
                && BotQuad.Key(map, hall.Location) == key)
            {
                BotTownGround.Yard(held.Guild, key, town, tiles);

                continue;
            }

            _town.Add((key, held.Guild, held.Bought, town, tiles));
        }

        for (var i = 0; i < _town.Count; i++)
        {
            var (key, guild, bought, town, tiles) = _town[i];

            _held.Remove(key);
            BotTownGround.Drop(guild, key, bought, bought ? Price : 0, town, tiles);
        }

        foreach (var bid in _bids.Values)
        {
            if (BotTownGround.Guarded(bid.Map, bid.Key, out var town, out _))
            {
                _townBids.Add((bid, town));
            }
        }

        for (var i = 0; i < _townBids.Count; i++)
        {
            var (bid, town) = _townBids[i];
            var escrow = bid.Escrow;

            bid.Escrow = 0;
            _bids.Remove(bid.Key);
            _byGuild.Remove(bid.Guild);
            BotTownGround.CallOff(bid.Guild, bid.Key, escrow, bid.AtOnce, town);
        }

        BotTownGround.End();
    }

    /// <summary>
    /// Drops holdings whose guild no longer exists.
    ///
    /// <b>Because the register outlives the population and the population is rebuilt every restart.</b> The
    /// save is read before a single bot is made, so this cannot be checked as it is read; it is checked on
    /// the beat instead, once the roll has been mustered. A square held by a name nobody answers to is a
    /// square that is somebody else's ground to every bot on the island and nobody's home — the worst of
    /// both, and invisible.
    /// </summary>
    private static void Sweep()
    {
        if (_held.Count == 0 || BotGuilds.Count == 0)
        {
            return;
        }

        List<(int Map, int X, int Y)> gone = [];

        foreach (var (key, held) in _held)
        {
            if (BaseGuild.FindByName(held.Guild) is not Guild)
            {
                gone.Add(key);
            }
        }

        for (var i = 0; i < gone.Count; i++)
        {
            logger.Information("The square at {X},{Y} belonged to a guild that is gone; it belongs to nobody now", gone[i].X * BotQuad.Side, gone[i].Y * BotQuad.Side);
            _held.Remove(gone[i]);
        }
    }

    /// <summary>Every claim being made now, for the dashboard.</summary>
    public static IEnumerable<Bid> Bids => _bids.Values;

    /// <summary>Whether somebody is laying claim to an important square this guild holds, now. See <see cref="Important"/> and <c>BotRest.Call</c>.</summary>
    public static bool Contested(string guild)
    {
        if (guild == null)
        {
            return false;
        }

        foreach (var bid in _bids.Values)
        {
            if (string.Equals(bid.From, guild, StringComparison.OrdinalIgnoreCase) && Important(guild, bid.Map, bid.Middle))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>How many quadrants round a guild's hall its ground is important. See <see cref="Important"/>.</summary>
    public static int HallRing { get; set; } = 1;

    /// <summary>Claims laid on a held square that was not important enough to call the holder back from its rest.</summary>
    public static long Unwoken { get; private set; }

    /// <summary>
    /// Whether a square of this guild's is important: the quadrant its hall stands in, or one of the <see cref="HallRing"/>
    /// round it.
    ///
    /// <para>
    /// <b>Patrick's answer of 24.09.2026: the important squares are those within one quadrant of the guild's hall, "because
    /// the capture of the quadrant with the hall is the moving of the hall and the loss of the guild's standing".</b> Only
    /// these call members back from their rest (<c>BotRest.Call</c>); until then every square held did, and claims on the
    /// free-held squares a guild picks up across the island woke sixteen bots in forty minutes.
    /// </para>
    /// </summary>
    public static bool Important(string guild, Map map, Point3D middle)
    {
        var hall = BotEstate.Hall(BotGuilds.Named(guild));

        if (hall is not { Deleted: false } || map == null || hall.Map != map)
        {
            return false;
        }

        var at = BotQuad.Key(map, hall.Location);
        var square = BotQuad.Key(map, middle);

        return at.Map == square.Map && Math.Abs(at.X - square.X) <= HallRing && Math.Abs(at.Y - square.Y) <= HallRing;
    }

    /// <summary>How long this claim has left to run, in milliseconds. Never below nought.</summary>
    public static int Left(Bid bid) =>
        bid == null ? 0 : Math.Max(0, MusterMs - (int)(Core.TickCount - bid.Began));

    /// <summary>
    /// A guild's name as a map pin says it: no article, and shouted.
    ///
    /// <b>Patrick's order of 10.09.2026 — "0.5 - CROWN", without the "the".</b> A pin is read at a glance off
    /// a world map at this era's resolution, and "The Crown" spends four of its characters on a word that
    /// distinguishes nothing: every guild on this island is a The.
    /// </summary>
    public static string Short(string guild)
    {
        if (string.IsNullOrEmpty(guild))
        {
            return null;
        }

        var name = guild.StartsWith("The ", StringComparison.OrdinalIgnoreCase) ? guild[4..] : guild;

        return name.ToUpperInvariant();
    }

    /// <summary>Every square that is spoken for, for the dashboard and the log.</summary>
    public static IEnumerable<((int Map, int X, int Y) Key, string Guild, bool Bought)> Owned()
    {
        foreach (var (key, held) in _held)
        {
            yield return (key, held.Guild, held.Bought);
        }
    }

    public static string Describe() =>
        !Running
            ? "guilds claim no ground"
            : $"{_held.Count} squares are spoken for and {_bids.Count} being claimed now ({Free} free to a guild, "
            + $"then {Price}gp up to {Cheap}, then {Step}gp more each; {Ousting}gp to take one, {Stripping}gp to strike a name off it); {Declared} claims made, "
            + $"{Won} won, {Stripped} that only struck a name off, {Unmustered} where the guild never gathered "
            + $"{Gather} of itself, {Beaten} driven off by somebody who came; {Paid}gp paid for ground; {Unwoken} claims on a held square too far from its hall (more than {HallRing} quadrant) to call anybody back from rest; "
            + $"{CutOff} squares fell for no longer reaching their hall, {HallsLost} halls razed with the square they stood in";

    /// <summary>
    /// A world reload clears the boards — but not the register of who owns what.
    ///
    /// <b>The one board on this shard that survives, and it survives because it was paid for.</b> Everything
    /// else here is about a moment; ground is a thing a guild bought with four free claims and five thousand
    /// gold a square after that. <c>BotClaimStore</c> writes it and reads it back, and clearing it here would
    /// throw away what was just restored.
    /// </summary>
    /// <summary>
    /// Every square let go of, every bid dropped: the island unclaimed. For the population's reset, by hand; the
    /// store writes nothing at the next save.
    /// </summary>
    public static int Wipe()
    {
        var gone = _held.Count;

        _held.Clear();
        Forget();

        return gone;
    }

    public static void Forget()
    {
        // A claim dropped with the boards still holds its guild's money: back to the chest, with a line, never out of the world.
        foreach (var bid in _bids.Values)
        {
            if (bid.Escrow > 0)
            {
                _owed.Add((bid.Guild, bid.Key, bid.Escrow));
                bid.Escrow = 0;
            }
        }

        PayOwed();

        _bids.Clear();
        _byGuild.Clear();
        _unmustered.Clear();
        PaidAtOnce = 0;
        Escrowed = 0;
        EscrowedClaims = 0;
        Released = 0;
        ReleasedClaims = 0;
        Returned = 0;
        ReturnedClaims = 0;
        Orphaned = 0;
        Declared = 0;
        Won = 0;
        Unmustered = 0;
        Unwoken = 0;
        Beaten = 0;
        Stripped = 0;
        Paid = 0;
        Conceded = 0;
        _looked = 0;
    }
}
