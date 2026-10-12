using System.Collections.Generic;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-population.json</c> is allowed to say.
///
/// <para>
/// <b>The one config file in this project that ships with real values rather than empty ones.</b> Everywhere
/// else absent means "keep the number the code chose", which is harmless. Here an absent class mix means no
/// bots exist at all — so the starter file is written with a small working population in it, and the first
/// boot produces a shard with somebody on it rather than a shard with a question.
/// </para>
/// </summary>
public sealed class BotPopulationSettings
{
    /// <summary>Which facet, by name — <c>Felucca</c>, <c>Trammel</c> and so on.</summary>
    public string Map { get; set; }

    /// <summary>Where on it, as <c>[x, y, z]</c>.</summary>
    public int[] Home { get; set; }

    /// <summary>
    /// How many of each class, by the class's own name. Names must match the nine — a typo is reported by
    /// name at boot rather than quietly producing a smaller population.
    /// </summary>
    public Dictionary<string, int> Classes { get; set; }

    /// <summary>How far around home bots are scattered when they are born.</summary>
    public int? Spread { get; set; }

    /// <summary>
    /// Coin every bot is born holding. See <see cref="BotOutfit.Purse"/> for why a population needs a float
    /// at all.
    ///
    /// It lives in this file rather than beside the kit because the kit has no configuration of its own, and
    /// this is the one file that already ships with real values. That makes it the same small debt the
    /// market's file owes the mine: a dial that belongs to one subsystem, read by another's file.
    /// </summary>
    public int? Purse { get; set; }

    /// <summary>
    /// Whether savings survive a restart along with skills. Off by default — see
    /// <see cref="BotProgress.Savings"/> for what turning it back on cures and what it hides.
    /// </summary>
    public bool? KeepEarnings { get; set; }

    /// <summary>How far from home the population may want anything at all.</summary>
    public int? Roam { get; set; }

    /// <summary>How often the population's clock looks at everybody. Not the pace of a bot.</summary>
    public int? BeatMs { get; set; }

    /// <summary>How long a dead bot lies there before it is put back on its feet.</summary>
    public int? ReviveMs { get; set; }

    /// <summary>Whether bots run rather than walk. Sets the beat as well as the pace.</summary>
    public bool? Run { get; set; }

    /// <summary>How far a bot reckons the fight it is in when something hits it.</summary>
    public int? NoticeRange { get; set; }

    /// <summary>How much health a bot needs to count as help in somebody else's fight.</summary>
    public double? FitFraction { get; set; }

    /// <summary>Whether tired bots go to an inn to rest. See BotInns.</summary>
    public bool? Inns { get; set; }

    /// <summary>How far a tired bot walks to an inn.</summary>
    public int? InnReach { get; set; }

    /// <summary>Gold an hour of rest costs at an inn.</summary>
    public int? InnPricePerHour { get; set; }

    /// <summary>What share of the rest the regeneration buff lasts. A third.</summary>
    public double? InnBuffShare { get; set; }

    /// <summary>The newcomers: the two-hourly growth, its ceiling, and the burst that fills the island at once. See <see cref="BotGrowth"/>.</summary>
    public BotGrowthSettings Growth { get; set; }

    /// <summary>The porter's way down from over the ceiling and its breaker (12.10.2026). See <see cref="BotUnload"/>.</summary>
    public BotUnloadSettings Unload { get; set; }
}

/// <summary>What <c>bot-population.json</c> may say under <c>Unload</c>. See <see cref="BotUnload.StuckFails"/>.</summary>
public sealed class BotUnloadSettings
{
    /// <summary>Stand-still passes that left a bot no lighter, inside <c>StuckWindowMs</c>, before the pass is withheld until its weight changes.</summary>
    public int? StuckFails { get; set; }

    /// <summary>The window those passes are counted over, in milliseconds.</summary>
    public int? StuckWindowMs { get; set; }

    /// <summary>How often a heavy bot that can still walk has its pack's piles lying apart joined, in milliseconds.</summary>
    public int? JoinMs { get; set; }

    /// <summary>Whether a bot over its ceiling on its kept things alone lets them go, to its box or the ground.</summary>
    public bool? Releases { get; set; }
}

/// <summary>What <c>bot-population.json</c> may say under <c>Growth</c>. See <see cref="BotGrowth"/>.</summary>
public sealed class BotGrowthSettings
{
    /// <summary>Whether newcomers arrive at all.</summary>
    public bool? Running { get; set; }

    /// <summary>Minutes between arrivals.</summary>
    public int? EveryMinutes { get; set; }

    /// <summary>Newcomers per arrival.</summary>
    public int? By { get; set; }

    /// <summary>The population past which nobody new arrives, resting counted. The ceiling.</summary>
    public int? Most { get; set; }

    /// <summary>The population the island is filled to at once, in a burst; nought turns the burst off.</summary>
    public int? FillTo { get; set; }

    /// <summary>Newcomers a look of the rest clock while under <c>FillTo</c>.</summary>
    public int? BurstBy { get; set; }

    /// <summary>A guild of this many with nobody of a role gets the next newcomer in that role; nought turns it off.</summary>
    public int? LeastToWant { get; set; }
}

/// <summary>Reads the population file, or writes a working one.</summary>
public static class BotPopulationConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPopulationConfig));

    private const string ConfigPath = "Configuration/bot-population.json";

    /// <summary>
    /// The population a fresh shard gets.
    ///
    /// <para>
    /// Four, and the mix is not arbitrary: two gatherers and a crafter are born with a pickaxe, which is what
    /// decides who can mine, and mining is the only work that exists yet. The warrior is there precisely
    /// <em>because</em> it has nothing to do — it is the visible proof that the census's "nothing was worth
    /// doing" counts a fact about the world and not a broken bot.
    /// </para>
    /// </summary>
    private static Dictionary<string, int> StarterMix() =>
        new()
        {
            ["Gatherer"] = 2,
            ["Crafter"] = 1,
            ["Warrior"] = 1
        };

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotPopulationSettings>(path);

        if (settings == null)
        {
            settings = new BotPopulationSettings
            {
                Map = "Felucca",
                Home = [BotPopulation.Where.X, BotPopulation.Where.Y, BotPopulation.Where.Z],
                Classes = StarterMix()
            };

            JsonConfig.Serialize(path, settings);

            logger.Information(
                "Wrote a starter population file to {Path}: four bots in Britain. Edit it to change who exists",
                ConfigPath
            );
        }

        Apply(settings);
    }

    /// <summary>What was read, or the working default where nothing was said.</summary>
    public static IReadOnlyDictionary<string, int> Mix { get; private set; } = StarterMix();

    private static void Apply(BotPopulationSettings settings)
    {
        // TryParse rather than Parse: a facet name out of a configuration file is user input, and Parse
        // throws on nonsense. A typo in a config file must produce a named complaint and a working shard, not
        // an exception on the way up.
        Map map = null;

        if (!string.IsNullOrWhiteSpace(settings.Map) && !Map.TryParse(settings.Map, null, out map))
        {
            map = null;
        }

        if (map == null || map == Map.Internal)
        {
            // Named, because "no bots appeared" is not a diagnosis and "Felucca is not a facet on this shard"
            // is. Felucca by default: it is where a population that wants a smithy and a bank should live.
            if (!string.IsNullOrWhiteSpace(settings.Map))
            {
                logger.Error("There is no facet called {Map}; the population falls back to Felucca", settings.Map);
            }

            map = Map.Felucca;
        }

        BotPopulation.Home = map;

        if (settings.Home is { Length: >= 3 })
        {
            BotPopulation.Where = new Point3D(settings.Home[0], settings.Home[1], settings.Home[2]);
        }

        BotPopulation.Spread = settings.Spread ?? BotPopulation.Spread;
        BotOutfit.Purse = settings.Purse ?? BotOutfit.Purse;
        BotProgress.Savings = settings.KeepEarnings ?? BotProgress.Savings;
        BotPopulation.Roam = settings.Roam ?? BotPopulation.Roam;
        BotInns.Running = settings.Inns ?? BotInns.Running;
        BotInns.Reach = settings.InnReach ?? BotInns.Reach;
        BotInns.PricePerHour = settings.InnPricePerHour ?? BotInns.PricePerHour;
        BotInns.BuffShare = settings.InnBuffShare ?? BotInns.BuffShare;
        BotPopulation.ReviveMs = settings.ReviveMs ?? BotPopulation.ReviveMs;

        BotBeat.IntervalMs = settings.BeatMs ?? BotBeat.IntervalMs;

        BotMobile.Runs = settings.Run ?? BotMobile.Runs;
        BotMobile.NoticeRange = settings.NoticeRange ?? BotMobile.NoticeRange;
        BotMobile.FitFraction = settings.FitFraction ?? BotMobile.FitFraction;

        // The newcomers' dials had no key until 02.10.2026; the burst to 200 is why they got one.
        {
            var c = settings.Growth;

            if (c != null)
            {
                BotGrowth.Running = c.Running ?? BotGrowth.Running;
                BotGrowth.EveryMinutes = c.EveryMinutes ?? BotGrowth.EveryMinutes;
                BotGrowth.By = c.By ?? BotGrowth.By;
                BotGrowth.Most = c.Most ?? BotGrowth.Most;
                BotGrowth.FillTo = c.FillTo ?? BotGrowth.FillTo;
                BotGrowth.BurstBy = c.BurstBy ?? BotGrowth.BurstBy;
                BotGrowth.LeastToWant = c.LeastToWant ?? BotGrowth.LeastToWant;
            }
        }

        // The porter's way down and its breaker (12.10.2026). Said at the load, because a default is not what the shard runs.
        {
            var c = settings.Unload;

            if (c != null)
            {
                BotUnload.StuckFails = c.StuckFails ?? BotUnload.StuckFails;
                BotUnload.StuckWindowMs = c.StuckWindowMs ?? BotUnload.StuckWindowMs;
                BotUnload.JoinMs = c.JoinMs ?? BotUnload.JoinMs;
                BotUnload.Releases = c.Releases ?? BotUnload.Releases;
            }

            logger.Information(
                "The porter: {Fails} stand-still passes no lighter in {Window} minutes set a bot aside until its weight changes; piles lying apart joined every {Join}s for a heavy bot; kept things let go when they alone are over the ceiling: {Releases}",
                BotUnload.StuckFails,
                BotUnload.StuckWindowMs / 60000.0,
                BotUnload.JoinMs / 1000,
                BotUnload.Releases
            );
        }

        Mix = Offices(settings.Classes is { Count: > 0 } ? settings.Classes : StarterMix());
    }

    /// <summary>
    /// The mix with every office held to one: a configured count above 1 for a class that is an office is read as 1 and said
    /// once, as a warning, at the load; 0 still means none.
    ///
    /// <para>
    /// <b>07.10.2026, Patrick: the Captain, the Baron, the Architect and the Sage exist exactly once at any population.</b> Build
    /// 425 wrote 1 into the file, and the staged tree it shipped from still said 2 — so a redeploy of that tree's configuration
    /// would have brought six captains back with the code none the wiser. The file is still read, but for an office it can only
    /// say "none" or "one". See <see cref="BotClass.Office"/>.
    /// </para>
    /// </summary>
    private static Dictionary<string, int> Offices(Dictionary<string, int> classes)
    {
        Dictionary<string, int> held = null;

        foreach (var (name, many) in classes)
        {
            if (many <= 1 || BotClasses.Find(name) is not { Office: true } klass)
            {
                continue;
            }

            logger.Warning(
                "{Class} is an office, held by one bot at any population; the {Many} in {Path} is read as 1",
                klass.Name,
                many,
                ConfigPath
            );

            held ??= new Dictionary<string, int>(classes, classes.Comparer);
            held[name] = 1;
        }

        return held ?? classes;
    }
}
