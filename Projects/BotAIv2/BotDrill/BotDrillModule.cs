using System;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-drill.json</c> may say. Everything optional; empty keeps the code's numbers.
///
/// <para>
/// <b>PascalCase, and it is not a style question.</b> The deserialiser matches these names as written, so a
/// key in lower case is not an error and not a warning — it is a value silently left at its default, and a
/// configuration file that appears to have been read is worse than one that fails to load.
/// </para>
/// </summary>
public sealed class BotDrillSettings
{
    /// <summary>
    /// Where the training field is, as X, Y, Z on the population's own facet.
    ///
    /// <para>
    /// Ordered as (1479, 1629, 20). If the ground turns out to be somewhere else, this is the one key that
    /// moves it — no rebuild, and nothing else in the subsystem holds a coordinate.
    /// </para>
    /// </summary>
    public int[] Ground { get; set; }

    /// <summary>Tiles between one student and the next. Two makes the block a chessboard.</summary>
    public int? Pace { get; set; }

    /// <summary>How many stand in one rank.</summary>
    public int? Rank { get; set; }

    /// <summary>Most students one captain takes at once.</summary>
    public int? Most { get; set; }

    /// <summary>How long the captain waits on the field for people to arrive.</summary>
    public int? GatherMs { get; set; }

    /// <summary>How long one class runs.</summary>
    public int? LessonMs { get; set; }

    /// <summary>How often points are handed out and the captain moves round the ring.</summary>
    public int? BeatMs { get; set; }

    /// <summary>How near the captain has to be for a student to get the whole of a beat.</summary>
    public int? Voice { get; set; }

    /// <summary>Points a beat at the bottom of a skill with the captain standing over you.</summary>
    public double? Rate { get; set; }

    /// <summary>What a beat is worth to a student the captain is nowhere near.</summary>
    public double? Distant { get; set; }

    /// <summary>What a lesson costs before the student's own standing is added.</summary>
    public int? Fee { get; set; }

    /// <summary>What each point the student already holds adds to the bill.</summary>
    public int? FeePerPoint { get; set; }

    /// <summary>How far a captain will be from the field and still call a class.</summary>
    public int? Range { get; set; }
}

/// <summary>
/// The captain's other office: a field, a fee, and an hour of being shouted at.
///
/// <para>
/// <b>Two proposers rather than one, and they are on two different bots.</b> Teaching cannot be a thing the
/// captain does <em>to</em> people — a bot standing in a square for a quarter of an hour has given up
/// everything else it could have been doing, and on this shard that has to be its own decision, weighed by
/// the same auction against the same alternatives. So the captain offers to hold a class and each student
/// offers itself a place, and either half can lose. A class nobody comes to is a real answer.
/// </para>
/// </summary>
public sealed class BotDrillModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDrillModule));

    private const string ConfigPath = "Configuration/bot-drill.json";

    public override string Name => "Drill";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Classes", "Will", "Population"];

    public override void Start()
    {
        Load();

        // Tried here and re-tried on first use: the craft systems this reads are built by the engine's own
        // content initialisation, and whether that has happened by the time a bot module starts is not ours
        // to decide. See BotHarness.Survey.
        BotHarness.Survey();

        BotWill.Offer(new BotDrill());
        BotWill.Offer(new BotStudent());

        // The first demand for armour this shard has ever had, offered to every bot. See BotArmourer:
        // nobody here wore any, because nothing ever asked for it.
        BotWill.Offer(new BotArmourer());

        // The captain's fourth office, and the only errand on this shard whose product is knowledge. See
        // BotScout: it belongs to the captain because it is the same thing a patrol is — a company raised on
        // the spot and walked somewhere — with the destination chosen by what nobody knows rather than by
        // what has gone wrong.
        BotWill.Offer(new BotScoutmaster());

        // The numbers it is actually running with. A belief about behaviour built on the defaults in the
        // source can be wrong by a factor of two without anything looking odd.
        logger.Information(
            "The drill field is at ({X}, {Y}, {Z}): up to {Most} in ranks of {Rank} at {Pace} tiles, the roll open {Gather}ms and the class {Lesson}ms, a beat every {Beat}ms worth {Rate:F2} points within {Voice} tiles and {Distant:P0} of that beyond; a lesson costs {Fee} + {Per} a point and a master teaches only as far as it has got itself; one field and one master at a time, a captain for those who swing and shoot and a sage for those who cast, whose lessons cost {Magic:F2} times as much",
            BotSchool.Ground.X,
            BotSchool.Ground.Y,
            BotSchool.Ground.Z,
            BotSchool.Most,
            BotSchool.Rank,
            BotSchool.Pace,
            BotSchool.GatherMs,
            BotSchool.LessonMs,
            BotSchool.BeatMs,
            BotSchool.Rate,
            BotSchool.Voice,
            BotSchool.Distant,
            BotSchool.Fee,
            BotSchool.FeePerPoint,
            BotSchool.MagicFee
        );

        _timer?.Stop();
        _timer = new CaptainTimer(TimeSpan.FromMilliseconds(SayEveryMs));
        _timer.Start();
    }

    private static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotDrillSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotDrillSettings());

            logger.Information("Wrote a starter drill file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        if (settings.Ground is { Length: >= 3 })
        {
            BotSchool.Ground = new Point3D(settings.Ground[0], settings.Ground[1], settings.Ground[2]);
        }

        BotSchool.Pace = settings.Pace ?? BotSchool.Pace;
        BotSchool.Rank = settings.Rank ?? BotSchool.Rank;
        BotSchool.Most = settings.Most ?? BotSchool.Most;
        BotSchool.GatherMs = settings.GatherMs ?? BotSchool.GatherMs;
        BotSchool.LessonMs = settings.LessonMs ?? BotSchool.LessonMs;
        BotSchool.BeatMs = settings.BeatMs ?? BotSchool.BeatMs;
        BotSchool.Voice = settings.Voice ?? BotSchool.Voice;
        BotSchool.Rate = settings.Rate ?? BotSchool.Rate;
        BotSchool.Distant = settings.Distant ?? BotSchool.Distant;
        BotSchool.Fee = settings.Fee ?? BotSchool.Fee;
        BotSchool.FeePerPoint = settings.FeePerPoint ?? BotSchool.FeePerPoint;
        BotDrill.Range = settings.Range ?? BotDrill.Range;
    }

    /// <summary>
    /// How often the captain's three offices are summed up in the shard's own log.
    ///
    /// <para>
    /// <b>Printed on a clock rather than only on a reload, and this project has the note about why.</b>
    /// <c>BotBeat.Summarise</c> carries it: numbers that exist and are only printed when the world is
    /// reloaded are numbers that are never printed, and the one budget everybody shares was invisible while
    /// it was being spent. A captain is one bot in twenty doing three things nobody else does; if its
    /// counters are silent, "the captain is not patrolling" and "the captain patrols and you have not been
    /// watching" are the same log.
    /// </para>
    /// </summary>
    public static int SayEveryMs { get; set; } = 300000;

    private static Timer _timer;

    private sealed class CaptainTimer : Timer
    {
        public CaptainTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() =>
            logger.Information("The captain: {What}", Summarise());
    }

    public override void Reset()
    {
        logger.Information("The drill field, before the reload: {State}", BotSchool.Describe());

        _timer?.Stop();
        _timer = null;

        BotSchool.Forget();
        BotDrill.Forget();
        BotStudent.Forget();
        BotArmourer.Forget();
        BotScoutmaster.Forget();
    }

    /// <summary>
    /// Everything the captain is and does, in one line, every case counted separately.
    ///
    /// The patrol belongs to the squad module by file and to the captain by office, and it is read here
    /// because a person asking "what is the captain doing" should not have to read two lines in two places
    /// and join them up.
    /// </summary>
    public static string Summarise() =>
        $"{BotPatrol.Describe()}; {BotScoutmaster.Describe()}; {BotDrill.Describe()}; {BotStudent.Describe()}; {BotArmourer.Describe()}";
}
