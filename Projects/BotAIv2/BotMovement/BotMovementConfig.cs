using System.Collections.Generic;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-movement.json</c> is allowed to say. Everything optional; absent means the
/// number the code chose.
///
/// Only genuine knobs are here. The rules — a step climbs two units, a person is sixteen tall, a diagonal
/// needs both flanks, a floor is found within eight of the terrain — are <b>not</b> configurable, and that
/// is deliberate. They are not preferences, they are what the engine does, and a config file able to
/// disagree with the engine is a config file able to recreate every stuck bot the first version had.
/// </summary>
public sealed class BotMovementSettings
{
    /// <summary>Milliseconds one search may cost before it returns what it has.</summary>
    public double? CeilingMs { get; set; }

    /// <summary>Milliseconds a search still gets when the population's allowance for the second is spent.</summary>
    public double? FloorMs { get; set; }

    /// <summary>Milliseconds of clock a tile of distance is worth.</summary>
    public double? MsPerTile { get; set; }

    /// <summary>Milliseconds the shortest journey still gets.</summary>
    public double? ShortMs { get; set; }

    /// <summary>Milliseconds the whole population may spend searching per second.</summary>
    public double? WindowMs { get; set; }

    /// <summary>How far off the straight line a short search may look.</summary>
    public int? MinMargin { get; set; }

    /// <summary>How far off it a long one may — what it takes to round a lake rather than a building.</summary>
    public int? MaxMargin { get; set; }

    /// <summary>How long a plan is trusted before being drawn again.</summary>
    public int? PlanStaleMs { get; set; }

    /// <summary>How many fruitless attempts at stepping before the journey is given up.</summary>
    public int? StallAttempts { get; set; }

    /// <summary>How many plans in a row with nowhere to walk before the destination is given up.</summary>
    public int? MaxEmptyPlans { get; set; }

    /// <summary>How many plans in a row may fail to bring the errand any closer before it is given up.</summary>
    public int? MaxPlansWithoutCloser { get; set; }

    /// <summary>Whether a reached plan after somebody about its own business starts the closing count again (12.10.2026, w7). See <c>BotJourney.KeepUp</c>.</summary>
    public bool? KeepUp { get; set; }

    /// <summary>How long ground that nearly killed a bot stays out of its plans.</summary>
    public int? DangerAvoidMs { get; set; }

    /// <summary>The largest pocket, in tiles, worth proving from the destination's side.</summary>
    public int? EnclosureCells { get; set; }

    /// <summary>Milliseconds one look at the far side of a journey may cost.</summary>
    public double? EnclosureCeilingMs { get; set; }

    /// <summary>The shortest gap between two such looks, across the whole population.</summary>
    public int? EnclosureGapMs { get; set; }

    /// <summary>The largest pocket worth proving around a bot that has been proved stranded.</summary>
    public int? StrandedCells { get; set; }

    /// <summary>Milliseconds that one look may cost.</summary>
    public double? StrandedCeilingMs { get; set; }

    /// <summary>Plans in a row that get no closer before the far side is asked about.</summary>
    public int? PlansBeforeAskingTheFarSide { get; set; }

    /// <summary>How wide a square one refusal of the ground covers.</summary>
    public int? RefusalGrain { get; set; }

    /// <summary>How long one refusal rests a square, before the doubling.</summary>
    public int? RefusalRestMs { get; set; }

    /// <summary>The ceiling on that doubling.</summary>
    public int? MostRefusalRestMs { get; set; }

    /// <summary>Most refused squares remembered at once.</summary>
    public int? MostRefusedPlaces { get; set; }

    /// <summary>Whether ground on the barred list is enforced at all. See <see cref="BotBarred"/>.</summary>
    public bool? Barring { get; set; }

    /// <summary>
    /// Ground nobody may take work in.
    ///
    /// <para>
    /// Present and empty means "bar nothing", which is a thing somebody may well want to say and is
    /// therefore kept distinct from the key being absent. Absent leaves the code's own list standing.
    /// </para>
    /// </summary>
    public BotBarSettings[] Barred { get; set; }

    // ---- The danger on the roads, and the road before the work (01.10.2026, build 388). See BotDanger. ----

    /// <summary>Whether a route is priced for the walker that asked for it.</summary>
    public bool? DangerByWalker { get; set; }

    /// <summary>A deadly zone whose fight is this many times the walker's strength is priced deadly for it; below, hostile.</summary>
    public double? DeadlyVsWalker { get; set; }

    /// <summary>Tiles of detour for a bloodied node whose record asks more than RoadDare × the walker's strength.</summary>
    public int? BloodBeyond { get; set; }

    /// <summary>Whether nodes on ground the reach map refuses to the walker are refused to the planners outright.</summary>
    public bool? RefuseNodes { get; set; }

    /// <summary>Whether the straight road to far work is weighed before the work is offered.</summary>
    public bool? Roads { get; set; }

    /// <summary>How many times what the work brings a square on its road may ask before the road is refused.</summary>
    public double? RoadDare { get; set; }

    /// <summary>Tiles between two looks at the road.</summary>
    public int? RoadStep { get; set; }

    /// <summary>The share of a patrol zone's fight a crossing is charged with; nought reads only the record of deaths.</summary>
    public double? ZoneRoadShare { get; set; }

    /// <summary>Whether a road the straight line refuses is judged by the road the planner would draw. False is the old rule.</summary>
    public bool? RouteJudged { get; set; }

    /// <summary>Whether the planner's road is judged for a road the line passes as well.</summary>
    public bool? RouteAlways { get; set; }

    /// <summary>Roads searched for that judgement in one second at most.</summary>
    public int? RoutePerSecond { get; set; }

    /// <summary>How many times what the walker brings a record nobody died on lately may ask on a judged road.</summary>
    public double? RoadStaleDare { get; set; }

    /// <summary>Whether the walkers' searches leave out steps onto ground the reach map refuses to the walker.</summary>
    public bool? SearchPrune { get; set; }

    // ---- What a bot sees near it and goes round (03.10.2026, sight13). See BotSight. ----

    /// <summary>Whether bots look at the creatures near them at all.</summary>
    public bool? Sight { get; set; }

    /// <summary>How often one bot looks, in milliseconds.</summary>
    public int? SightEveryMs { get; set; }

    /// <summary>How far a bot sees, in tiles.</summary>
    public int? SightTiles { get; set; }

    /// <summary>How many times what the bot brings a creature may come to, at its kind's price, before it is gone round.</summary>
    public double? SightOdds { get; set; }

    /// <summary>Tiles past a creature's reach the walk keeps clear of.</summary>
    public int? SightMargin { get; set; }

    /// <summary>What one step into a creature's reach costs the precise search (ten a straight step).</summary>
    public int? SightStepCost { get; set; }

    /// <summary>Tiles of detour a route accepts to keep a node out of a creature's reach.</summary>
    public int? SightRouteCost { get; set; }

    /// <summary>How long a bot waits out of reach before it turns back, in milliseconds.</summary>
    public int? SightWaitMs { get; set; }

    /// <summary>Whether a bot at work, standing, leaves it when a danger comes within reach with a line to it.</summary>
    public bool? SightStanding { get; set; }
}

/// <summary>One rectangle of ground the population may not choose, as a configuration file writes it.</summary>
public sealed class BotBarSettings
{
    public int? X1 { get; set; }

    public int? Y1 { get; set; }

    public int? X2 { get; set; }

    public int? Y2 { get; set; }

    /// <summary>In words, so that a year from now the list says why each entry is on it.</summary>
    public string Why { get; set; }
}

/// <summary>Reads the movement file and moves the numbers it names.</summary>
public static class BotMovementConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMovementConfig));

    private const string ConfigPath = "Configuration/bot-movement.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotMovementSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotMovementSettings());

            logger.Information(
                "Wrote a starter movement file to {Path}; every number stays as the code has it",
                ConfigPath
            );

            return;
        }

        BotPath.CeilingMs = settings.CeilingMs ?? BotPath.CeilingMs;
        BotPath.FloorMs = settings.FloorMs ?? BotPath.FloorMs;
        BotPath.MsPerTile = settings.MsPerTile ?? BotPath.MsPerTile;
        BotPath.ShortMs = settings.ShortMs ?? BotPath.ShortMs;
        BotPath.WindowMs = settings.WindowMs ?? BotPath.WindowMs;
        BotPath.MinMargin = settings.MinMargin ?? BotPath.MinMargin;
        BotPath.MaxMargin = settings.MaxMargin ?? BotPath.MaxMargin;
        BotJourney.PlanStaleMs = settings.PlanStaleMs ?? BotJourney.PlanStaleMs;
        BotJourney.StallAttempts = settings.StallAttempts ?? BotJourney.StallAttempts;
        BotJourney.MaxEmptyPlans = settings.MaxEmptyPlans ?? BotJourney.MaxEmptyPlans;
        BotJourney.MaxPlansWithoutCloser = settings.MaxPlansWithoutCloser ?? BotJourney.MaxPlansWithoutCloser;
        BotJourney.KeepUp = settings.KeepUp ?? BotJourney.KeepUp;
        BotJourney.DangerAvoidMs = settings.DangerAvoidMs ?? BotJourney.DangerAvoidMs;
        BotPath.EnclosureCells = settings.EnclosureCells ?? BotPath.EnclosureCells;
        BotPath.EnclosureCeilingMs = settings.EnclosureCeilingMs ?? BotPath.EnclosureCeilingMs;
        BotPath.EnclosureGapMs = settings.EnclosureGapMs ?? BotPath.EnclosureGapMs;
        BotPath.StrandedCells = settings.StrandedCells ?? BotPath.StrandedCells;
        BotPath.StrandedCeilingMs = settings.StrandedCeilingMs ?? BotPath.StrandedCeilingMs;
        BotWalk.PlansBeforeAskingTheFarSide =
            settings.PlansBeforeAskingTheFarSide ?? BotWalk.PlansBeforeAskingTheFarSide;
        BotRefused.Grain = settings.RefusalGrain ?? BotRefused.Grain;
        BotRefused.RestMs = settings.RefusalRestMs ?? BotRefused.RestMs;
        BotRefused.MostRestMs = settings.MostRefusalRestMs ?? BotRefused.MostRestMs;
        BotRefused.MostPlaces = settings.MostRefusedPlaces ?? BotRefused.MostPlaces;
        BotBarred.Running = settings.Barring ?? BotBarred.Running;

        BotDanger.ByWalker = settings.DangerByWalker ?? BotDanger.ByWalker;
        BotDanger.DeadlyVsWalker = settings.DeadlyVsWalker ?? BotDanger.DeadlyVsWalker;
        BotDanger.BloodBeyond = settings.BloodBeyond ?? BotDanger.BloodBeyond;
        BotDanger.RefusesNodes = settings.RefuseNodes ?? BotDanger.RefusesNodes;
        BotDanger.Roads = settings.Roads ?? BotDanger.Roads;
        BotDanger.RoadDare = settings.RoadDare ?? BotDanger.RoadDare;
        BotDanger.RoadStep = settings.RoadStep ?? BotDanger.RoadStep;
        BotDanger.ZoneRoadShare = settings.ZoneRoadShare ?? BotDanger.ZoneRoadShare;
        BotDanger.RouteJudged = settings.RouteJudged ?? BotDanger.RouteJudged;
        BotDanger.RouteAlways = settings.RouteAlways ?? BotDanger.RouteAlways;
        BotDanger.RoutePerSecond = settings.RoutePerSecond ?? BotDanger.RoutePerSecond;
        BotDanger.RoadStaleDare = settings.RoadStaleDare ?? BotDanger.RoadStaleDare;
        BotReachMap.PrunesSearch = settings.SearchPrune ?? BotReachMap.PrunesSearch;

        BotSight.Running = settings.Sight ?? BotSight.Running;
        BotSight.EveryMs = settings.SightEveryMs ?? BotSight.EveryMs;
        BotSight.SightTiles = settings.SightTiles ?? BotSight.SightTiles;
        BotSight.Odds = settings.SightOdds ?? BotSight.Odds;
        BotSight.Margin = settings.SightMargin ?? BotSight.Margin;
        BotSight.StepCost = settings.SightStepCost ?? BotSight.StepCost;
        BotDanger.SightCost = settings.SightRouteCost ?? BotDanger.SightCost;
        BotSight.WaitMs = settings.SightWaitMs ?? BotSight.WaitMs;
        BotSight.Standing = settings.SightStanding ?? BotSight.Standing;

        if (settings.Barred != null)
        {
            List<BotBarred.Bar> bars = [];

            for (var i = 0; i < settings.Barred.Length; i++)
            {
                var bar = settings.Barred[i];

                // All four corners or none: three of them is a rectangle somebody meant to write and did
                // not, and guessing the fourth would bar ground nobody named.
                if (bar?.X1 == null || bar.Y1 == null || bar.X2 == null || bar.Y2 == null)
                {
                    logger.Warning(
                        "Barred entry {Index} in {Path} is missing one of X1, Y1, X2, Y2 and is ignored",
                        i,
                        ConfigPath
                    );

                    continue;
                }

                bars.Add(new BotBarred.Bar(bar.X1.Value, bar.Y1.Value, bar.X2.Value, bar.Y2.Value, bar.Why ?? "no reason given"));
            }

            BotBarred.Set(bars);
        }
    }
}
