using System;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-delve.json</c> may say. Everything optional; empty keeps the code's numbers.
///
/// <para>
/// <b>PascalCase, and it is not a style question.</b> The deserialiser matches these names as written, so a
/// key in lower case is not an error and not a warning — it is a value silently left at its default.
/// </para>
/// </summary>
public sealed class BotDelveSettings
{
    /// <summary>Whether a delve may be offered at all.</summary>
    public bool? Enabled { get; set; }

    /// <summary>How many go down, the leader included.</summary>
    public int? Company { get; set; }

    /// <summary>How many of them must be able to hold a line.</summary>
    public int? Fighters { get; set; }

    /// <summary>Corpses that end a delve early.</summary>
    public int? Quota { get; set; }

    /// <summary>How long one delve lasts.</summary>
    public int? CapMs { get; set; }

    /// <summary>How long the leader stands calling for a party.</summary>
    public int? MusterMs { get; set; }

    /// <summary>How far round the muster bodies are called up.</summary>
    public int? Reach { get; set; }

    /// <summary>How long the party works one room before moving on.</summary>
    public int? RoomMs { get; set; }

    /// <summary>How far the party looks for something to fight.</summary>
    public int? Sight { get; set; }

    /// <summary>How long a band leaves delving alone after one.</summary>
    public int? BetweenMs { get; set; }

    /// <summary>How much stronger than a dungeon's worst inhabitant a band wants to be.</summary>
    public double? Odds { get; set; }

    /// <summary>Resurrections a party has between them.</summary>
    public int? Raisings { get; set; }

    /// <summary>How long a fallen delver lies before its party puts it back up.</summary>
    public int? RaiseMs { get; set; }

    /// <summary>The leader's share of the pot, as a fraction.</summary>
    public double? LeadersShare { get; set; }

    /// <summary>What a delve is reckoned at per minute before the ledger has measured one.</summary>
    public double? Prior { get; set; }

    /// <summary>The least a body is worth, as a share of its dungeon's worst, to hold an ordinary seat in a party.</summary>
    public double? WeakFloor { get; set; }

    /// <summary>The share of a party's seats, the leader's among them, that bodies under the floor may hold.</summary>
    public double? WeakShare { get; set; }

    /// <summary>Whether a party that lost two inside a minute is lifted out where it stands rather than walked out.</summary>
    public bool? LiftWhenBled { get; set; }

    public int? MaxStrays { get; set; }

    public int? HeadsPerParty { get; set; }

    public int? Margin { get; set; }
}

/// <summary>
/// Dungeons as a module: one offer registered, the numbers read, the party watchdog started and one line
/// every few minutes saying what it has all come to.
///
/// <para>
/// <see cref="BotPhase.World"/>, because a dungeon is a place on the map and the rooms are read out of the
/// world's own spawners. It requires <c>Squads</c> for the plainest possible reason — the whole of this
/// subsystem is five bots standing together — and <c>Population</c>, because a delve is called by the
/// maker of a guild and there are no guilds until the population has been dealt out.
/// </para>
///
/// <para>
/// <b>The timer is not only for the log.</b> It also walks <see cref="BotDelveParty.Watch"/>, which is the
/// net under the one failure this feature can produce that nothing else on the shard could recover from: a
/// bot left in a place with no road out. It runs on its own clock rather than on the delve's, precisely so
/// that a broken delve cannot stop it.
/// </para>
/// </summary>
public sealed class BotDelveModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDelveModule));

    private const string ConfigPath = "Configuration/bot-delve.json";

    public override string Name => "Delve";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Squads", "Population"];

    /// <summary>How often the delves are summed up, and how often the watchdog walks the parties.</summary>
    public static int SayEveryMs { get; set; } = 300000;

    /// <summary>How often the watchdog walks. Far shorter than the summary: it is a safety net.</summary>
    public static int WatchMs { get; set; } = 10000;

    /// <summary>Whether a delve may be offered at all.</summary>
    public static bool Enabled { get; set; } = true;

    private static Timer _timer;

    private static Timer _watch;

    public override void Start()
    {
        Load();

        if (Enabled)
        {
            BotWill.Offer(new BotDelver());
            BotWill.Offer(new BotVenturer());
        }

        logger.Information(
            "Delving is {State}: any member of a guild may take {Multiple} for each creature of a dungeon's worst room down it, {Company} at the fewest and {Most} at the most — {Fighters} of them able to hold a line, its own band called first, and more than the worst room holds or it does not go — for {Quota} corpses or {Minutes} minutes, after {Muster} minutes of calling at the home ground. It goes to the richest dungeon whose worst inhabitant the band is ×{Odds} the strength of, walks in by the cave mouth where the world's teleporters join it to the island and walks out by it (put down by hand only where none does), and has {Raisings} raisings between them; a party keeps {WeakShare} of its seats, the leader's among them and never a medic's, for bodies under {WeakFloor} of its dungeon's worst and levies no more of them, and one that loses two inside a minute is {Bled}; a fighter alone may venture into a dungeon whose worst it is ×{Alone} the strength of, once an hour; what it takes is swept into one pot and divided at the end, {Share} to the leader and the rest evenly. A band waits {Between} minutes between delves; a delve is reckoned at {Prior} a minute until the ledger has measured one",
            Enabled ? "on" : "off",
            BotProving.PartyMultiple,
            BotDelve.Company,
            Math.Max(BotDelve.Company, BotDelver.HeadsPerParty),
            BotDelve.Fighters,
            BotDelve.Quota,
            BotDelve.CapMs / 60000,
            BotDelve.MusterMs / 60000,
            BotDelver.Odds.ToString("F2"),
            BotDelveParty.Raisings,
            BotDelve.WeakShare.ToString("P0"),
            BotDelve.WeakFloor.ToString("P0"),
            BotDelve.LiftWhenBled ? "lifted out where it stands" : "walked out by the mouth",
            BotVenturer.Odds.ToString("F2"),
            BotDelveParty.LeadersShare.ToString("P0"),
            BotDelver.BetweenMs / 60000,
            BotDelve.Prior.ToString("F0")
        );

        _timer?.Stop();
        _timer = new DelveTimer(TimeSpan.FromMilliseconds(SayEveryMs));
        _timer.Start();

        _watch?.Stop();
        _watch = new WatchTimer(TimeSpan.FromMilliseconds(WatchMs));
        _watch.Start();
    }

    private static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotDelveSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotDelveSettings());

            logger.Information("Wrote a starter delve file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        Enabled = settings.Enabled ?? Enabled;
        BotDelve.Company = settings.Company ?? BotDelve.Company;
        BotDelve.Fighters = settings.Fighters ?? BotDelve.Fighters;
        BotDelve.Quota = settings.Quota ?? BotDelve.Quota;
        BotDelve.CapMs = settings.CapMs ?? BotDelve.CapMs;
        BotDelve.MusterMs = settings.MusterMs ?? BotDelve.MusterMs;
        BotDelve.Reach = settings.Reach ?? BotDelve.Reach;
        BotDelve.RoomMs = settings.RoomMs ?? BotDelve.RoomMs;
        BotDelve.Sight = settings.Sight ?? BotDelve.Sight;
        BotDelve.Prior = settings.Prior ?? BotDelve.Prior;
        BotDelve.WeakFloor = settings.WeakFloor ?? BotDelve.WeakFloor;
        BotDelve.WeakShare = settings.WeakShare ?? BotDelve.WeakShare;
        BotDelve.LiftWhenBled = settings.LiftWhenBled ?? BotDelve.LiftWhenBled;
        BotDelve.MaxStrays = settings.MaxStrays ?? BotDelve.MaxStrays;
        BotDelver.HeadsPerParty = settings.HeadsPerParty ?? BotDelver.HeadsPerParty;
        BotDungeon.Margin = settings.Margin ?? BotDungeon.Margin;
        BotDelver.BetweenMs = settings.BetweenMs ?? BotDelver.BetweenMs;
        BotDelver.Odds = settings.Odds ?? BotDelver.Odds;
        BotDelveParty.Raisings = settings.Raisings ?? BotDelveParty.Raisings;
        BotDelveParty.RaiseMs = settings.RaiseMs ?? BotDelveParty.RaiseMs;
        BotDelveParty.LeadersShare = settings.LeadersShare ?? BotDelveParty.LeadersShare;
    }

    public static string Summarise() => $"{BotDelver.Describe()}; alone: {BotVenturer.Describe()}";

    private sealed class DelveTimer : Timer
    {
        public DelveTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => logger.Information("Delving: {What}", Summarise());
    }

    private sealed class WatchTimer : Timer
    {
        public WatchTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick()
        {
            BotDelveParty.Watch();

            // The dungeons put their dead back faster than their spawners' own clocks. See BotDungeon.RestockMs.
            BotDungeon.Restock();
        }
    }

    public override void Reset()
    {
        logger.Information("Delving, before the reload: {State}", Summarise());

        _timer?.Stop();
        _timer = null;

        _watch?.Stop();
        _watch = null;

        BotDelver.Forget();
    }
}
