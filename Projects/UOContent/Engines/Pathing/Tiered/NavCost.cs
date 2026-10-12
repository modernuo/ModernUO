using System;

namespace Server.Engines.Pathing.Tiered;

/// <summary>
/// The one cost model every tier of the navigation shares, in integer "ticks" of a step.
///
/// <para>
/// A step takes the same time straight or diagonal (the engine's movement delay does not depend on the direction), so
/// the distance a walker covers in a given time is Chebyshev, not octile. A diagonal costs one tick more than a straight
/// step only to break ties: at equal time the straighter path zig-zags less. Every tier prices a step the same way, or
/// the tiers argue and the path jitters between them.
/// </para>
/// </summary>
public static class NavCost
{
    /// <summary>Any step, straight or diagonal.</summary>
    public const int Step = 100;

    /// <summary>
    /// A cost past this is refused by the planners' relaxations: a penalty hook (<c>NavigationService.Danger</c>) that hands
    /// out hundreds of thousands a node over a wide barred box overflowed the int costs on 29.09.2026 and hung the loop,
    /// because an overflowed cost is negative and a negative cost relaxes for ever.
    /// </summary>
    public const int Unaffordable = int.MaxValue / 4;

    /// <summary>Added to a diagonal step: at equal time, prefer the straight line.</summary>
    public const int DiagonalTie = 1;

    /// <summary>A door on the way: opening it costs about a step's delay.</summary>
    public const int Door = 200;

    /// <summary>The cost of one step in direction <paramref name="direction"/> (0 = North, odd = diagonal).</summary>
    public static int Of(int direction) => (direction & 1) == 0 ? Step : Step + DiagonalTie;

    /// <summary>
    /// The admissible estimate between two tiles on one map with no portal between them: a step for each tile of the larger
    /// axis distance. Never more than the true cost, since every step costs at least <see cref="Step"/>.
    /// </summary>
    public static int Estimate(int x1, int y1, int x2, int y2) =>
        Step * Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2));
}
