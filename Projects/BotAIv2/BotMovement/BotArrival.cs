using System;

namespace Server.BotAI.V2;

/// <summary>
/// What counts as having got there. One definition, used by the planner and by the walker.
///
/// <para>
/// <b>It is a type rather than an <c>int</c> because in the first version it was an <c>int</c>, and that
/// cost.</b> Arrival was decided in two places — <c>BotBrain.Arrived</c> and <c>BotNav.AtGoal</c> — each
/// with its own copy of the rule, and they had to be kept in step by hand. The tolerance itself was a
/// bare number threaded through six layers of call, meaning something different at each: a doorway, a
/// market stall, a creature, a leg of a road. Long journeys quietly used two tiles while everything else
/// used one, and nothing anywhere said why.
/// </para>
///
/// <para>
/// So: the number has a name, the rule has one implementation, and both the search and the step ask the
/// same object the same question. A plan that thinks it has arrived and a bot that thinks it has not is
/// the shape of an infinite loop.
/// </para>
/// </summary>
public readonly struct BotArrival
{
    /// <summary>
    /// The vertical space a standing person occupies, as the engine's movement code has it. Two points
    /// further apart than this in Z are on different floors, however close they look from above.
    /// </summary>
    public const int PersonHeight = 16;

    private BotArrival(int tiles) => Tiles = Math.Max(0, tiles);

    /// <summary>
    /// How far off the goal the bot may stand, measured the way the engine measures adjacency: the
    /// larger of the two axis distances, so a diagonal neighbour is one tile away and not one and a half.
    /// </summary>
    public int Tiles { get; }

    /// <summary>
    /// The goal tile itself and nothing else. For standing on a thing — a corpse, a vein, a stone.
    /// </summary>
    public static BotArrival Exactly => new(0);

    /// <summary>
    /// The goal tile or any of the eight around it. For standing next to a thing that must be touched.
    ///
    /// <para>
    /// <b>Not the default, and calling it one cost this shard its largest single source of failure.</b> Nine
    /// undertakings walked to a shopkeeper, a counter or a forge with this, and every one of them had already
    /// asked <c>InRange(place, SomeReach)</c> on the line above — three tiles for a counter, three for a
    /// shopkeeper. So the work needed three tiles and the walk demanded one, from the same shelf, in nine
    /// copies. A shopkeeper's counter is furniture: the tiles against it are blocked by it, which is what a
    /// counter is for, and a bot that can trade perfectly well from two tiles away was told it had not
    /// arrived and made to redraw its route until the errand died. One counter in Britain took 56 attempts
    /// in a single hour on 26.08.2026 and 76 in eleven minutes the day before, from every bot in turn.
    /// </para>
    ///
    /// <para>
    /// So: if the work has its own reach, <b>walk to that reach</b>. Two numbers for one distance is this
    /// project's most expensive recurring shape, and here it was written out nine times.
    /// </para>
    /// </summary>
    public static BotArrival Beside => new(1);

    /// <summary>
    /// Within this many tiles. For places rather than things — "get to the market", where the market is
    /// a cluster forty tiles across and its recorded centre is one arbitrary point inside it.
    /// </summary>
    public static BotArrival Within(int tiles) => new(tiles);

    /// <summary>
    /// Whether standing at <paramref name="at"/> counts as having reached <paramref name="goal"/>.
    ///
    /// <para>
    /// <b>Height is always checked, and that is a deliberate departure from the first version.</b> There,
    /// the Z test was skipped whenever the tolerance was more than one tile — and every distance check in
    /// the project ignored height besides. The bill came in twice. A wraith on the roof of a crypt, three
    /// tiles away and twenty units up, read as prime quarry: close, dangerous, standing still. Five bots
    /// formed a party, walked over, "fought" it and took off not one point of health. And a bot on a
    /// bridge counted as having reached the path underneath it.
    /// </para>
    ///
    /// <para>
    /// Sixteen units, not zero: a tile's floor is not flat, and walking across open ground changes Z by
    /// a unit or two constantly. Sixteen is one person's height, which is the engine's own idea of
    /// "same floor".
    /// </para>
    /// </summary>
    public bool Reached(Point3D at, Point3D goal)
    {
        if (Math.Abs(at.X - goal.X) > Tiles || Math.Abs(at.Y - goal.Y) > Tiles)
        {
            return false;
        }

        return Math.Abs(at.Z - goal.Z) < PersonHeight;
    }

    public override string ToString() =>
        Tiles switch
        {
            0 => "on the tile",
            1 => "beside it",
            _ => $"within {Tiles} tiles"
        };
}
