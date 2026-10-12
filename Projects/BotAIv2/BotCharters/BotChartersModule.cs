using System;
using System.Collections.Generic;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary><c>Configuration/bot-charters.json</c>: every key optional, PascalCase; nothing written means the code's own number.</summary>
public sealed class BotChartersSettings
{
    public bool? Running { get; set; }

    public double? EveryHours { get; set; }

    public double? FirstHours { get; set; }

    public double? Levy { get; set; }

    public double? Appetite { get; set; }

    public double? SpareShare { get; set; }

    public double? LeastShare { get; set; }

    public int? LeastBid { get; set; }

    public int? FailsToLose { get; set; }

    public double? LostRegard { get; set; }

    public double? CraftSkill { get; set; }

    public bool? LootFree { get; set; }

    public bool? SayLevy { get; set; }
}

/// <summary>
/// Town charters as a module: reads its file, fills the voice's bank, and runs the clock — a look a minute (holders gone or put
/// out, early auctions, the general auction every six hours) and the <c>Charters:</c> line every five minutes. World phase, after
/// the towns, the chests, the towns' standing and the shops whose sales it levies.
/// </summary>
public sealed class BotChartersModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotChartersModule));

    private const string ConfigPath = "Configuration/bot-charters.json";

    /// <summary>How often the clock looks: a minute.</summary>
    public static int LookMs { get; set; } = 60000;

    /// <summary>How often the <c>Charters:</c> line is written: five minutes, with every other summary.</summary>
    public static int SayEveryMs { get; set; } = 300000;

    private static Timer _timer;

    private static long _said;

    public override string Name => "Charters";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population", "Will", "Travel", "Estate", "Diplomacy", "Market", "Shops", "Shopkeep"];

    public override void Start()
    {
        Load();

        var added = 0;
        Dictionary<string, string[]> missing = new(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, lines) in BotCharters.Phrases())
        {
            if (!BotVoice.Has(key))
            {
                missing[key] = lines;
                added++;
            }
        }

        if (missing.Count > 0)
        {
            BotVoice.Phrases(missing);
        }

        logger.Information(
            "Charters: {State}, every town auctions one per craft (smith, tailor, woodsman, alchemist, scribe, cook) every {Every:0.#}h, the first {First:0.#}h after a boot with nothing saved; levy {Levy:P0} on every practitioner's sale by anybody but the holder{Loot}; a guild with {LeastShare:P0} of the sales bids {Appetite:0.##} × (½ + ½ × share) of the worth, at most {Spare:P0} of what its chest spares, from {LeastBid}gp; {Fails} failed tasks in a row lose it; a lost charter is {Regard} of regard; {Charters} charters read back; {Phrases} phrases added",
            BotCharters.Running ? "ON" : "OFF",
            BotCharters.EveryMs / 3600000.0,
            BotCharters.FirstMs / 3600000.0,
            BotCharters.Levy,
            BotCharters.LootFree ? $" (a seller under {BotCharters.CraftSkill:0} in the craft sells free)" : "",
            BotCharters.LeastShare,
            BotCharters.Appetite,
            BotCharters.SpareShare,
            BotCharters.LeastBid,
            BotCharters.FailsToLose,
            BotCharters.LostRegard,
            BotCharters.Held,
            added
        );

        _said = Core.TickCount;
        _timer?.Stop();
        _timer = new ChartersTimer(TimeSpan.FromMilliseconds(Math.Max(1000, LookMs)));
        _timer.Start();
    }

    private static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotChartersSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotChartersSettings());
            logger.Information("Wrote a starter charters file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotCharters.Running = settings.Running ?? BotCharters.Running;
        BotCharters.EveryMs = Ms(settings.EveryHours) ?? BotCharters.EveryMs;
        BotCharters.FirstMs = Ms(settings.FirstHours) ?? BotCharters.FirstMs;
        BotCharters.Levy = settings.Levy ?? BotCharters.Levy;
        BotCharters.Appetite = settings.Appetite ?? BotCharters.Appetite;
        BotCharters.SpareShare = settings.SpareShare ?? BotCharters.SpareShare;
        BotCharters.LeastShare = settings.LeastShare ?? BotCharters.LeastShare;
        BotCharters.LeastBid = settings.LeastBid ?? BotCharters.LeastBid;
        BotCharters.FailsToLose = settings.FailsToLose ?? BotCharters.FailsToLose;
        BotCharters.LostRegard = settings.LostRegard ?? BotCharters.LostRegard;
        BotCharters.CraftSkill = settings.CraftSkill ?? BotCharters.CraftSkill;
        BotCharters.LootFree = settings.LootFree ?? BotCharters.LootFree;
        BotCharters.SayLevy = settings.SayLevy ?? BotCharters.SayLevy;
    }

    private static int? Ms(double? hours) => hours == null ? null : (int)Math.Max(60000, Math.Min(int.MaxValue, hours.Value * 3600000));

    public override void Reset()
    {
        logger.Information("Charters, before the reload: {State}", BotCharters.Describe());

        _timer?.Stop();
        _timer = null;

        BotCharters.Forget();
    }

    private static void Tick()
    {
        var now = Core.TickCount;

        if (BotGuilds.Count > 0)
        {
            BotCharters.Look(now);
        }

        if (now - _said >= Math.Max(10000, SayEveryMs))
        {
            _said = now;
            logger.Information("Charters: {What}", BotCharters.Describe());
        }
    }

    private sealed class ChartersTimer : Timer
    {
        public ChartersTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => Tick();
    }
}

/// <summary>The charters' way in: the module and the store registered from outside <c>BotCore</c>, after it has read its own switch.</summary>
public static class BotChartersCore
{
    /// <summary>After <c>BotCore.Configure</c> (40), which sets <c>BotCore.Enabled</c>; before the world is read, which the store needs.</summary>
    [CallPriority(45)]
    public static void Configure()
    {
        if (!BotCore.Enabled)
        {
            return;
        }

        BotChartersStore.Configure();
        BotModules.Register(new BotChartersModule());
    }
}

/// <summary>
/// Keeps the charters across restarts: who holds which town's craft, for what, and what it has taken; the period's books the next
/// auction bids by; the guilds barred from an early auction; and the clock, as running time to the next auction and into the
/// period. A shape this build cannot read is dropped whole rather than guessed at: the towns auction afresh.
/// </summary>
public sealed class BotChartersStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotChartersStore));

    private const int Shape = 1;

    private static BotChartersStore _store;

    public static void Configure() => _store ??= new BotChartersStore();

    public BotChartersStore() : base("BotCharters", 23)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        BotCharters.Save(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape != Shape)
        {
            logger.Warning("The saved charters are shape {Found} and this build reads {Wanted}; the towns will auction afresh", shape, Shape);

            return;
        }

        var (charters, books) = BotCharters.Load(reader);

        logger.Information("Charters read back: {Charters} standing, {Books} towns' crafts with a book this period", charters, books);
    }
}
