using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-spells.json</c> is allowed to say. Everything optional.
///
/// <para>
/// What is not here, and could not be: which spells exist, what they are made of, how hard they are to write,
/// and what a spell is worth. The first three are the shard's own inscription system and a file able to
/// disagree with it would be a file able to send a scribe swinging at something it cannot make. The fourth is
/// not a number in this project at all — a spell has no price, because filling a book is what happens to a
/// scribe who gets good at writing rather than something it buys.
/// </para>
/// </summary>
public sealed class BotSpellsSettings
{
    /// <summary>How far below its own Inscribe a scribe keeps its work.</summary>
    public double? Margin { get; set; }

    /// <summary>What a scribe adds to what its materials cost when it prices what it wrote. A fifth by default.</summary>
    public double? Markup { get; set; }

    /// <summary>How many of each herb a caster keeps back from the pen, to cast with.</summary>
    public int? Reserve { get; set; }

    /// <summary>What a herb is reckoned at where no shopkeeper within reach sells one.</summary>
    public int? HerbGuess { get; set; }

    /// <summary>How many blank scrolls are bought in one go.</summary>
    public int? Batch { get; set; }

    /// <summary>How often an attempt is made, in milliseconds.</summary>
    public int? SwingMs { get; set; }

    /// <summary>How long a scribe waits for mana before taking what it has written and going.</summary>
    public int? PatienceMs { get; set; }

    /// <summary>What a session at the pen is reckoned at per minute before experience corrects it.</summary>
    public double? Expects { get; set; }

    /// <summary>How long a session is expected to take.</summary>
    public double? WorkMinutes { get; set; }

    /// <summary>What getting hold of one spell is reckoned at per minute.</summary>
    public double? SeekExpects { get; set; }

    /// <summary>How long that is expected to take.</summary>
    public double? SeekMinutes { get; set; }
}

/// <summary>Reads the spells file and moves the numbers it names.</summary>
public static class BotSpellsConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSpellsConfig));

    private const string ConfigPath = "Configuration/bot-spells.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotSpellsSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotSpellsSettings());

            logger.Information(
                "Wrote a starter spells file to {Path}; every number stays as the code has it",
                ConfigPath
            );

            return;
        }

        BotQuill.Margin = settings.Margin ?? BotQuill.Margin;
        BotQuill.Markup = settings.Markup ?? BotQuill.Markup;
        BotQuill.Reserve = settings.Reserve ?? BotQuill.Reserve;
        BotQuill.HerbGuess = settings.HerbGuess ?? BotQuill.HerbGuess;

        BotInscribe.Batch = settings.Batch ?? BotInscribe.Batch;
        BotInscribe.SwingMs = settings.SwingMs ?? BotInscribe.SwingMs;
        BotInscribe.PatienceMs = settings.PatienceMs ?? BotInscribe.PatienceMs;
        BotInscribe.Prior = settings.Expects ?? BotInscribe.Prior;
        BotInscribe.WorkMinutes = settings.WorkMinutes ?? BotInscribe.WorkMinutes;

        BotAcquire.Prior = settings.SeekExpects ?? BotAcquire.Prior;
        BotAcquire.WorkMinutes = settings.SeekMinutes ?? BotAcquire.WorkMinutes;
    }
}
