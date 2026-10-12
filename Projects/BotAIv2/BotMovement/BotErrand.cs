using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// One thing a bot is trying to get to. A fixed point, or something that walks.
///
/// <para>
/// Movement does not know or care <em>why</em>. An errand to a market stall and an errand to a skeleton
/// that just hit the bot are the same object with different fields, and that is what lets the decision
/// layer put one on top of the other without movement needing an opinion about combat.
/// </para>
/// </summary>
public sealed class BotErrand
{
    public Map Map { get; init; }

    /// <summary>Where, when the destination stands still.</summary>
    public Point3D Where { get; init; }

    /// <summary>
    /// What, when the destination walks. Null for an ordinary journey.
    ///
    /// A reference rather than a copied position: a monster that is being chased is a monster that is
    /// moving, and a plan drawn to where it was is a plan to an empty tile.
    /// </summary>
    public Mobile Follow { get; init; }

    public BotArrival Arrival { get; init; }

    /// <summary>Why, in words. For the log and for the diagnostics gump — never branched on.</summary>
    public string Reason { get; init; }

    /// <summary>
    /// Whether this errand interrupted another rather than being chosen for its own sake.
    ///
    /// Movement uses it for one thing only: when the queue is full, the deepest <em>ordinary</em> errand is
    /// the one dropped. An interruption is by definition the thing happening now.
    /// </summary>
    public bool Interruption { get; init; }

    /// <summary>
    /// A walk after a bot that is about its own business — a buyer at its work, a trader on its road — which the work walking
    /// it ends by its own clock: a plan that reaches the target is not counted against getting closer (12.10.2026, w7). See
    /// <see cref="BotJourney.KeepUp"/>; written only through <c>BotDoing.WalkAfter</c>.
    /// </summary>
    public bool KeepsUp { get; init; }

    /// <summary>Where to walk, this instant.</summary>
    public Point3D Target => Follow != null ? Follow.Location : Where;

    /// <summary>
    /// The route toward this errand's target when it is far, one point a cluster (BotJourney.Leg): kept with the errand,
    /// so a fight pushed on top of it does not throw it away and the fight gets a route of its own.
    /// </summary>
    internal List<Point3D> Route { get; } = [];

    /// <summary>The point of <see cref="Route"/> being walked to.</summary>
    internal int Leg;

    /// <summary>Where the route was drawn to, or nought when none has been.</summary>
    internal Point3D RouteGoal;

    /// <summary>Routes drawn again because a leg failed or the bot strayed.</summary>
    internal int Reroutes;

    /// <summary>Given up on routing: the errand is walked straight, as it was before the tiers.</summary>
    internal bool RouteSpent;

    /// <summary>A leg failed: draw the route again at the next plan.</summary>
    internal bool Redraw;

    /// <summary>The point the plan in hand walks to.</summary>
    internal Point3D Aim;

    /// <summary>The navigation graph found no way on the ground to the target.</summary>
    internal bool Unreachable;

    /// <summary>The gate this errand's route ends on, when the target lies in another land; nought otherwise. See <see cref="BotGates"/>.</summary>
    internal Point3D Gate;

    /// <summary>Where that gate puts a body down: standing within <c>BotGates.Through</c> of it, the walker has been through.</summary>
    internal Point3D GateTo;

    /// <summary>The route came from the old chart round home rather than the navigation graph.</summary>
    internal bool FromChart;

    /// <summary>The graph's "no way" for this errand has been written to the log once already.</summary>
    internal bool NoWaySaid;

    /// <summary>
    /// Whether this errand still means anything.
    ///
    /// A followed mobile that has died or been deleted takes its errand with it — which is precisely how a
    /// "kill that thing" interruption ends and the road underneath it resumes. Movement reports the fact; it
    /// does not decide what follows from it.
    /// </summary>
    public bool Lapsed => Follow != null && (Follow.Deleted || !Follow.Alive || Follow.Map != Map);

    public override string ToString() =>
        Follow != null
            ? $"{Reason} after {Follow.Name} at {Target} ({Arrival})"
            : $"{Reason} to {Where} ({Arrival})";
}
