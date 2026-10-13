using System;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Keeps the stories across restarts (PLAN-life A3, 13.10.2026): every bot's book — when it was first seen or born, what it was
/// then, its notes, what the walk last saw of it — the chapters the model retold with the facts' hash each was told from, and
/// the Chronicle's days. In the world save, never a loose file under <c>Saves/</c> (that folder is moved to a backup at every
/// save); emptied by <c>do reset</c> through <see cref="BotStory.Wipe"/>.
/// </summary>
public sealed class BotStoryStore : GenericPersistence
{
    private static BotStoryStore _store;

    /// <summary>Called from <see cref="BotStoryCore.Configure"/>: a persistence must exist before the world is read.</summary>
    public static void Open() => _store ??= new BotStoryStore();

    public BotStoryStore() : base("BotStory", 25)
    {
    }

    public override void Serialize(IGenericWriter writer) => BotStory.Save(writer);

    public override void Deserialize(IGenericReader reader) => BotStory.Load(reader);
}

/// <summary>
/// The stories as a module: reads <c>Configuration/bot-story.json</c>, hangs <see cref="BotStory.Heard"/> on the event stream,
/// starts the story's clock and the <c>Stories:</c> line. It offers no work and asks nothing of any bot.
/// </summary>
public sealed class BotStoryModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotStoryModule));

    /// <summary>How often the <c>Stories:</c> line is written. The population's own five minutes.</summary>
    public static int SayEveryMs { get; set; } = 300000;

    private static Timer _clock;

    private static Timer _summary;

    public override string Name => "Story";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population", "Character"];

    public override void Start()
    {
        BotStoryConfig.Load();

        Stop();

        BotEvents.Heard = BotStory.Heard;
        BotStory.Started(Core.TickCount);

        _clock = Timer.DelayCall(
            TimeSpan.FromMilliseconds(Math.Max(250, BotStory.TickMs)),
            TimeSpan.FromMilliseconds(Math.Max(250, BotStory.TickMs)),
            () => BotStory.Tick(Core.TickCount)
        );

        _summary = Timer.DelayCall(
            TimeSpan.FromMilliseconds(Math.Max(10000, SayEveryMs)),
            TimeSpan.FromMilliseconds(Math.Max(10000, SayEveryMs)),
            () => logger.Information("Stories: {What}", BotStory.Describe())
        );

        logger.Information(
            "The stories are {State}: {Books} books kept; every bot looked at once in {Every}s ({Tick}ms a slice), at most {Notes} notes a bot and {Most} books; "
            + "old age after {Old} days; the model {Voice}; the Chronicle keeps {Days} days of at most {PerDay} entries",
            BotStory.Running ? "on" : "off",
            BotStory.Count,
            BotStory.EveryMs / 1000,
            BotStory.TickMs,
            BotStory.MostNotes,
            BotStory.MostBooks,
            BotStory.OldAgeDays,
            BotStory.Voiced
                ? $"retells at most {BotStory.VoicePerHour} chapters or days an hour (chapters of {BotStory.VoiceLeastFacts} facts or more, {BotStory.VoiceMostWords} words), with {(string.IsNullOrWhiteSpace(BotStory.VoiceModel) ? "the duke's model" : BotStory.VoiceModel)}"
                : "is not asked",
            BotChronicle.MostDays,
            BotChronicle.MostPerDay
        );
    }

    private static void Stop()
    {
        _clock?.Stop();
        _clock = null;
        _summary?.Stop();
        _summary = null;
    }

    public override void Reset()
    {
        logger.Information("Stories, before the reload: {What}", BotStory.Describe());

        Stop();
        BotEvents.Heard = null;
        BotStory.Forget();
    }
}

/// <summary>The stories' way in: the store and the module registered from outside <c>BotCore</c>, after it has read its own switch.</summary>
public static class BotStoryCore
{
    /// <summary>After <c>BotCore.Configure</c> (40) and the character's (41); before the world is read, which the store needs.</summary>
    [CallPriority(47)]
    public static void Configure()
    {
        if (!BotCore.Enabled)
        {
            return;
        }

        BotStoryStore.Open();
        BotModules.Register(new BotStoryModule());
    }
}

/// <summary><c>Configuration/bot-story.json</c>: every key optional, PascalCase, nothing written means the code's own number.</summary>
public sealed class BotStorySettings
{
    public bool? Running { get; set; }

    public int? TickMs { get; set; }

    public int? EveryMs { get; set; }

    public int? PublishEveryMs { get; set; }

    public int? SettleMs { get; set; }

    public int? MostNotes { get; set; }

    public int? MostBooks { get; set; }

    public int? OldAgeDays { get; set; }

    public bool? Voiced { get; set; }

    public int? VoicePerHour { get; set; }

    public string VoiceModel { get; set; }

    public int? VoiceLeastFacts { get; set; }

    public int? VoiceMostChars { get; set; }

    public int? VoiceMostWords { get; set; }

    public int? MostWanted { get; set; }

    public int? ChronicleDays { get; set; }

    public int? ChroniclePerDay { get; set; }

    public int? ChronicleEveryMs { get; set; }

    public int? ChronicleVoiceLeast { get; set; }

    public int? ChronicleVoiceMostFacts { get; set; }

    public int? SayEveryMs { get; set; }
}

public static class BotStoryConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotStoryConfig));

    private const string ConfigPath = "Configuration/bot-story.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var s = JsonConfig.Deserialize<BotStorySettings>(path);

        if (s == null)
        {
            JsonConfig.Serialize(path, new BotStorySettings());

            logger.Information("Wrote a starter story file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotStory.Running = s.Running ?? BotStory.Running;
        BotStory.TickMs = s.TickMs ?? BotStory.TickMs;
        BotStory.EveryMs = s.EveryMs ?? BotStory.EveryMs;
        BotStory.PublishEveryMs = s.PublishEveryMs ?? BotStory.PublishEveryMs;
        BotStory.SettleMs = s.SettleMs ?? BotStory.SettleMs;
        BotStory.MostNotes = s.MostNotes ?? BotStory.MostNotes;
        BotStory.MostBooks = s.MostBooks ?? BotStory.MostBooks;
        BotStory.OldAgeDays = s.OldAgeDays ?? BotStory.OldAgeDays;
        BotStory.Voiced = s.Voiced ?? BotStory.Voiced;
        BotStory.VoicePerHour = s.VoicePerHour ?? BotStory.VoicePerHour;
        BotStory.VoiceModel = string.IsNullOrWhiteSpace(s.VoiceModel) ? BotStory.VoiceModel : s.VoiceModel.Trim();
        BotStory.VoiceLeastFacts = s.VoiceLeastFacts ?? BotStory.VoiceLeastFacts;
        BotStory.VoiceMostChars = s.VoiceMostChars ?? BotStory.VoiceMostChars;
        BotStory.VoiceMostWords = s.VoiceMostWords ?? BotStory.VoiceMostWords;
        BotStory.MostWanted = s.MostWanted ?? BotStory.MostWanted;

        BotChronicle.MostDays = s.ChronicleDays ?? BotChronicle.MostDays;
        BotChronicle.MostPerDay = s.ChroniclePerDay ?? BotChronicle.MostPerDay;
        BotChronicle.EveryMs = s.ChronicleEveryMs ?? BotChronicle.EveryMs;
        BotChronicle.VoiceLeast = s.ChronicleVoiceLeast ?? BotChronicle.VoiceLeast;
        BotChronicle.VoiceMostFacts = s.ChronicleVoiceMostFacts ?? BotChronicle.VoiceMostFacts;

        BotStoryModule.SayEveryMs = s.SayEveryMs ?? BotStoryModule.SayEveryMs;
    }
}
