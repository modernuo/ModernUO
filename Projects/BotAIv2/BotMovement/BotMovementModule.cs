using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Movement as a module: reads its numbers, lets the population walk, and puts its counters back on a
/// world reload.
///
/// <para>
/// <see cref="BotPhase.World"/>, because everything under it asks the map questions — which tile has a
/// floor, what is standing on it, where the doors are — and none of that can be answered before the world
/// is in memory. Requires nothing: a path does not care what class the bot is.
/// </para>
///
/// <para>
/// <b>Its switch is the useful one.</b> Turned off, every bot stands where it is and everything else goes
/// on running. Half the first version's investigations were the same question — is this navigation, or is
/// navigation covering for something else? — and the honest answer took hours of watching a live shard.
/// Four of the things eventually found underneath it were not navigation at all: bots blocking each other
/// in a doorway, a stuck-detector that punished progress, a caster whose own spell read as a wall, and a
/// bot at a counter being fined for standing at a counter.
/// </para>
/// </summary>
public sealed class BotMovementModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMovementModule));

    /// <summary>Draws the road map a slice at a time, and stops itself when it is drawn. See <see cref="BotRoads"/>.</summary>
    private static RoadsTimer _roads;

    public override string Name => "Movement";

    public override BotPhase Phase => BotPhase.World;

    public override void Start()
    {
        BotMovementConfig.Load();

        BotWalk.Walking = true;

        // Every number that decides behaviour is in this line, because the config file silently wins over the
        // code and a threshold nobody can read is a threshold nobody can argue with.
        logger.Information(
            "Movement ready: a search is charged {PerTile}ms a tile of distance, never less than {Short}ms and never more than {Ceiling}ms, the population {Window}ms a second, floor {Floor}ms; a plan is trusted {Stale}ms and a journey is given up after {Stall} fruitless attempts at stepping or {NoCloser} plans that get no closer (a reached plan after somebody about its own business starting that count again: {KeepUp}); after {FarSide} of those the far side of the destination is looked at, at most every {Gap}ms, for a pocket of up to {Cells} tiles costing at most {Look}ms",
            BotPath.MsPerTile,
            BotPath.ShortMs,
            BotPath.CeilingMs,
            BotPath.WindowMs,
            BotPath.FloorMs,
            BotJourney.PlanStaleMs,
            BotJourney.StallAttempts,
            BotJourney.MaxPlansWithoutCloser,
            BotJourney.KeepUp,
            BotWalk.PlansBeforeAskingTheFarSide,
            BotPath.EnclosureGapMs,
            BotPath.EnclosureCells,
            BotPath.EnclosureCeilingMs
        );

        // Its own line, because it is the shard's newest record of the ground and the one whose numbers are
        // least settled: a refusal rests a square {RestMs} and doubles per refusal to {MostRestMs}, and
        // whether those are right is a question the morning after answers.
        logger.Information(
            "Refused ground: one entry per {Grain} tiles, resting {RestMs}ms doubling to {MostRestMs}ms, at most {MostPlaces} squares remembered; arriving anywhere clears one",
            BotRefused.Grain,
            BotRefused.RestMs,
            BotRefused.MostRestMs,
            BotRefused.MostPlaces
        );

        // What a bot sees and goes round, said with its numbers (03.10.2026, sight13). See BotSight.
        logger.Information(
            "Sight {State}: each bot looks every {Every}ms within {Tiles} tiles; a creature that attacks on sight and comes, at its kind's price, past ×{Odds} what the bot brings is gone round at its reach plus {Margin} tiles (+{Step} a step in a plan, +{Route} tiles a node of a route), waited out {Wait}ms and then turned back from; work left standing {Standing}",
            BotSight.Running ? "on" : "off",
            BotSight.EveryMs,
            BotSight.SightTiles,
            BotSight.Odds,
            BotSight.Margin,
            BotSight.StepCost,
            BotDanger.SightCost,
            BotSight.WaitMs,
            BotSight.Standing ? "on" : "off"
        );

        // And what is barred outright, which is a different kind of fact and gets a different line. See
        // BotBarred: the refusal ledger above is measured and expires, and this does neither.
        BotBarred.Load();
        BotDanger.Hook();
        BotBarred.Announce();

        // The road map is drawn after the world is in memory and before anybody needs it, a slice a tick; until it is
        // finished it answers nothing and nothing reading it changes. See BotRoads.
        _roads?.Stop();
        _roads = new RoadsTimer(TimeSpan.FromMilliseconds(Math.Max(10, BotRoads.SliceEveryMs)));
        _roads.Start();
    }

    /// <summary>
    /// A world reload is a different world. The reach ledger describes ground that may not be there any
    /// more, and the counters describe a population that is about to be rebuilt.
    /// </summary>
    public override void Reset()
    {
        BotWalk.Walking = false;

        logger.Information("Movement, before the reload: {Paths}; {Walk}; {Reach}; {Refused}; {Roads}",
            BotPath.Describe(),
            BotWalk.Describe(),
            BotReach.Describe(),
            BotRefused.Describe(),
            BotRoads.Describe()
        );

        _roads?.Stop();
        _roads = null;

        BotPath.Reset();
        BotWalk.Reset();
        BotReach.Reset();
        BotRefused.Forget();
        BotRoads.Forget();
        BotChart.Forget();
        BotGates.Forget();
        BotSight.Reset();
    }

    /// <summary>Everything the summary wants to say about getting about, in three clauses.</summary>
    public static string Summarise() =>
        $"{BotPath.Describe()}; {BotWalk.Describe()}; {BotReach.Describe()}; {BotRefused.Describe()}; {BotChart.Describe()}";

    private sealed class RoadsTimer : Timer
    {
        public RoadsTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick()
        {
            if (!BotRoads.Ready && !BotRoads.Failed)
            {
                BotRoads.Slice();

                return;
            }

            // Then the chart, drawn from the finished road map on the same clock. See BotChart.
            if (BotRoads.Failed || !BotChart.Running || BotChart.Slice())
            {
                Stop();
            }
        }
    }
}
