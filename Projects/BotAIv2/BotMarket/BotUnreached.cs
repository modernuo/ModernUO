using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Buyers down the dungeon block, and the ground they stood on, that a carrier's walk or a trader's haul from outside found no way
/// to — remembered for every carrier and trader — and the walk out of the block that the island's walkers do not know
/// (03.10.2026, build next).
///
/// <para>
/// <b>Measured on live 396 (<c>session-2026-10-03_04-19.log</c>, 04:19–05:40, 210 bots; <c>haul11-tools/orders.py</c>).</b> 35 of
/// the 224 orders traders wrote were struck "no way through to the buyer", and 18 of the 21 hauls that failed failed on it. 33 of
/// the 35 touched the dungeon block (x ≥ <see cref="BotGates.Edge"/>): 19 written on the island for a buyer down there, 8 by a
/// trader standing down there for a buyer on the island, 6 down there between its lands. Of the 38 orders that touched the
/// block, 4 were handed over (11 %); of the 186 on the island, 149 (80 %). Seven traders in 26 minutes bought and walked for
/// Edith, who stood in a pocket at (6139, 1432) the reach ledger had walked to its edges at 04:19:36 ("journeys in or out of it
/// will be refused"); each bought first, walked, and learned it alone. The reach map calls that ground open — a gate leads in —
/// and the gates join it, so neither question the haul already asks (<see cref="BotReach.Ask"/>, <see cref="BotGates.Joined"/>)
/// could say no; and the trader's ledger (<c>Beware</c>) and the haul's shun are its own, so the next trader did not know.
/// </para>
///
/// <para>
/// <b>What is kept.</b> A walk or a haul that set out from outside the block and found no way to a buyer standing in it (a
/// carrier's or a trader's <c>Bend</c>; not a walker that never moved, nor one already in the buyer's own dungeon, square or land)
/// marks that buyer and the ground it stood on — its dungeon's box (<see cref="BotDungeon.Holding"/>), else a
/// square of <see cref="Grain"/> tiles of the block — for <see cref="RestMs"/>, doubling with every failure there up to
/// <see cref="MostRestMs"/>. While either rests, carriers and traders pass the want over ("no way through to the buyer",
/// <see cref="BotPass.Sealed"/>): the want stands, for a bot that reaches it — a looker already standing in the buyer's own land
/// down there is never refused — and for when its buyer comes up: a buyer back on the island is never refused, and its mark is
/// dropped. Something handed over there by a walk that set out from outside
/// the block (<see cref="Reached"/>) clears both: the way exists. Replayed against live 396's orders
/// (<c>haul11-tools/simulate_unreached.py</c>), 15 of the 19 island-to-block orders struck "no way through" would not have been
/// written, and one of the two handed over would have been lost (Galadrion's ribs to Adela, whose mark was twelve minutes old).
/// </para>
///
/// <para>
/// <b>And the way out.</b> A carrier or a trader standing in the dungeon block is offered only a walk inside its own land of the
/// walkers' graph (<see cref="Shut"/>, <see cref="BotGates.LandOf"/>): 14 of the 16 orders written down there were struck "no way
/// through", most by traders standing in Edith's pocket after following a buyer in — a gate leads walkers into it and no walk
/// leads out (Norbert "has not moved or changed what it is doing for 4 minutes" there at 04:52).
/// </para>
/// </summary>
public static class BotUnreached
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotUnreached));

    /// <summary>Whether the block's unreached buyers are remembered and the walk out of it asked at all. Off, nothing here refuses.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How long one failure rests a buyer and its ground. Doubles with each failure there, up to <see cref="MostRestMs"/>.</summary>
    public static int RestMs { get; set; } = 1800000;

    /// <summary>The ceiling on that doubling: two hours, the longest a want stands.</summary>
    public static int MostRestMs { get; set; } = 7200000;

    /// <summary>The side of a square of the dungeon block outside every dungeon's box, in tiles. The block is 1,024 wide.</summary>
    public static int Grain { get; set; } = 512;

    /// <summary>Most buyers or grounds remembered at once; past it the record starts again.</summary>
    public static int MostKept { get; set; } = 1024;

    // ---- Counted where they happen ---------------------------------------------------------------------------------------------

    /// <summary>Walks and hauls that found no way to a buyer standing in the dungeon block, and were marked.</summary>
    public static long Marked { get; private set; }

    /// <summary>Looks that passed a want over for its buyer or its ground resting here; of them, by the buyer's own mark.</summary>
    public static long Refused { get; private set; }

    public static long ByBuyer { get; private set; }

    /// <summary>Marks cleared by something handed over there by a walk from outside the block.</summary>
    public static long Cleared { get; private set; }

    /// <summary>Buyers' marks dropped because the buyer was back on the island.</summary>
    public static long Surfaced { get; private set; }

    private sealed class Row
    {
        public int Fails;

        public long Until;

        public long MarkedTick;
    }

    /// <summary>Failures at one buyer or ground this close together are one failure, not a doubling: a haul strikes each of its orders.</summary>
    public static int SameFailureMs { get; set; } = 60000;

    private static readonly Dictionary<(int Map, string Deep, int X, int Y), Row> _ground = [];

    private static readonly Dictionary<Serial, Row> _buyers = [];

    /// <summary>Whether a place lies in the dungeon block: east of <see cref="BotGates.Edge"/>, where no ground joins the island.</summary>
    public static bool Deep(Point3D at) => at.X >= BotGates.Edge;

    private static (int Map, string Deep, int X, int Y) Key(Map map, Point3D at) =>
        BotDungeon.Holding(at) is { } deep ? (map.MapID, deep.Name, 0, 0) : (map.MapID, null, at.X / Math.Max(16, Grain), at.Y / Math.Max(16, Grain));

    /// <summary>
    /// A carrier's walk or a trader's haul found no way to this buyer: when the buyer stands in the dungeon block and the walk set
    /// out (<paramref name="setOut"/>) from outside the block, the buyer and its ground are marked for every carrier and trader.
    /// Called from the work's own <c>Bend</c>, so the walker's proof is the evidence.
    ///
    /// <para>
    /// <b>Only a walk from outside the block marks it (review of build next, F2).</b> A walk that began down there, or a walker that
    /// stands in the buyer's own dungeon, square or land of the graph when it fails, met a wall, a ledge or a stair — a fact about
    /// the ground the shard's other records keep, not about the way down (Osric "could not get nearer to Wilma" three tiles off in
    /// the Orc Caves on live 396 would have rested the whole cave). And never a walker that never moved (<c>Stalled</c>: a fact
    /// about its own tile, the rule <c>BotWill</c> keeps for ground). Every failure that marked anything useful on 396 began on the
    /// island.
    /// </para>
    /// </summary>
    public static void NoWay(Map map, Mobile buyer, Point3D setOut, Mobile walker)
    {
        if (!Running || map == null || map == Map.Internal || buyer is not { Deleted: false } || !Deep(buyer.Location) || Deep(setOut))
        {
            return;
        }

        if (walker is BotMobile { LastWalk: BotWalkResult.Stalled })
        {
            return;
        }

        if (walker is { Deleted: false } && Deep(walker.Location))
        {
            var land = BotGates.LandOf(map, buyer.Location);

            if (Key(map, walker.Location) == Key(map, buyer.Location) || land >= 0 && land == BotGates.LandOf(map, walker.Location))
            {
                return;
            }
        }

        var now = Core.TickCount;
        var key = Key(map, buyer.Location);
        var rest = Mark(_ground, key, now);

        Mark(_buyers, buyer.Serial, now);
        Marked++;

        logger.Information(
            "No way was found to {Buyer} at {Where} in {Ground}: carriers and traders leave the want for {Minutes} minutes, or until it comes up or something is handed over there",
            buyer.Name,
            buyer.Location,
            key.Deep ?? $"the dungeon block's square at ({key.X * Math.Max(16, Grain)}, {key.Y * Math.Max(16, Grain)})",
            rest / 60000
        );
    }

    private static long Mark<TKey>(Dictionary<TKey, Row> rows, TKey key, long now)
    {
        if (!rows.TryGetValue(key, out var row))
        {
            if (rows.Count >= Math.Max(16, MostKept))
            {
                rows.Clear();
            }

            row = new Row();
            rows[key] = row;
        }

        // A mark that lapsed longer ago than the longest rest is a fresh start, not a third strike (review F6).
        if (row.Fails > 0 && now - row.Until >= Math.Max(1000L, MostRestMs))
        {
            row.Fails = 0;
        }

        if (row.Fails == 0 || now - row.MarkedTick >= SameFailureMs)
        {
            row.Fails = Math.Min(row.Fails + 1, 16);
        }

        row.MarkedTick = now;

        var first = Math.Max(1000L, RestMs);
        var rest = Math.Min(first << (row.Fails - 1), Math.Max(first, MostRestMs));

        row.Until = now + rest;

        return rest;
    }

    /// <summary>
    /// Whether a carrier or a trader looking from <paramref name="from"/> should pass this buyer over where it stands now: down the
    /// dungeon block, and it or its ground marked lately by a walk or a haul from outside that found no way. A buyer on the island
    /// is never refused, and its mark goes; nor is a looker standing in the buyer's own land down there (review of build next, F1):
    /// the mark is about the way down, and a delve-mate in the same cave or a trader in Papua is already down (and, by
    /// <see cref="Shut"/>, offered nothing else).
    /// </summary>
    public static bool Refuses(Map map, Mobile buyer, Point3D from)
    {
        if (!Running || map == null || buyer == null || _ground.Count == 0 && _buyers.Count == 0)
        {
            return false;
        }

        if (!Deep(buyer.Location))
        {
            if (_buyers.Remove(buyer.Serial))
            {
                Surfaced++;
            }

            return false;
        }

        if (Deep(from) && !Shut(map, from, buyer.Location))
        {
            return false;
        }

        var now = Core.TickCount;

        if (_buyers.TryGetValue(buyer.Serial, out var mine) && now - mine.Until < 0)
        {
            Refused++;
            ByBuyer++;

            return true;
        }

        if (_ground.TryGetValue(Key(map, buyer.Location), out var row) && now - row.Until < 0)
        {
            Refused++;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Something was handed to this buyer by a walk that set out from <paramref name="from"/>: when that was outside the block,
    /// the way down exists, and the buyer's and its ground's marks are cleared.
    /// </summary>
    public static void Reached(Map map, Mobile buyer, Point3D from)
    {
        if (map == null || buyer == null || !Deep(buyer.Location) || Deep(from) || from == Point3D.Zero)
        {
            return;
        }

        var cleared = _ground.Remove(Key(map, buyer.Location));

        cleared |= _buyers.Remove(buyer.Serial);

        if (cleared)
        {
            Cleared++;
        }
    }

    /// <summary>
    /// Whether a walk from <paramref name="from"/>, standing in the dungeon block, to <paramref name="to"/> would have to leave the
    /// land of the walkers' graph it stands in: out of the block to the island, or to another of its lands. A land the graph cannot
    /// place is not one the walker can be sent to. Never asked of a walk that begins outside the block. The caller counts it.
    /// </summary>
    public static bool Shut(Map map, Point3D from, Point3D to)
    {
        // Until the gates are read the old answer stands (BotGates.Joined's own fallback): nothing is refused here.
        if (!Running || map == null || map == Map.Internal || !Deep(from) || !BotGates.Ready)
        {
            return false;
        }

        var a = FromLand(map, from);

        return a < 0 || a != BotGates.LandOf(map, to);
    }

    private static int _fromMap = -1;

    private static Point3D _from;

    private static int _fromLand;

    private static long _fromTick;

    /// <summary>
    /// The land the looker stands in, kept two seconds for the same tile: one look asks it of every want it weighs, and
    /// <see cref="BotGates.LandOf"/> keeps no answer of "nowhere", which costs the graph a window and a flood each time (review
    /// of build next, F7).
    /// </summary>
    private static int FromLand(Map map, Point3D from)
    {
        var now = Core.TickCount;

        if (map.MapID != _fromMap || from != _from || now - _fromTick >= 2000)
        {
            _fromMap = map.MapID;
            _from = from;
            _fromLand = BotGates.LandOf(map, from);
            _fromTick = now;
        }

        return _fromLand;
    }

    /// <summary>Buyers and grounds resting now.</summary>
    private static (int Buyers, int Grounds) Resting()
    {
        var now = Core.TickCount;
        var buyers = 0;
        var grounds = 0;

        foreach (var row in _buyers.Values)
        {
            if (now - row.Until < 0)
            {
                buyers++;
            }
        }

        foreach (var row in _ground.Values)
        {
            if (now - row.Until < 0)
            {
                grounds++;
            }
        }

        return (buyers, grounds);
    }

    /// <summary>The clause of <c>The market:</c>.</summary>
    public static string Describe()
    {
        var (buyers, grounds) = Resting();

        return $"the dungeon block: {Marked} walks and hauls from outside found no way to a buyer down there and marked it and its ground for {RestMs / 60000}–{MostRestMs / 60000} minutes ({buyers} buyers and {grounds} grounds resting now), {Refused} looks passed such a want over ({ByBuyer} of them by the buyer's own mark), {Cleared} marks cleared by something handed over there from outside, {Surfaced} buyers' marks dropped for coming up";
    }

    /// <summary>A world reload is a different world.</summary>
    public static void Forget()
    {
        _ground.Clear();
        _buyers.Clear();
        Marked = 0;
        Refused = 0;
        ByBuyer = 0;
        Cleared = 0;
        Surfaced = 0;
        _fromMap = -1;
    }
}
