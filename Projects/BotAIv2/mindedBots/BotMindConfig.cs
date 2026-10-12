using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.Mind;

/// <summary>
/// What <c>Configuration/bot-mind.json</c> is allowed to say. Everything optional; empty means "keep the
/// numbers the code chose".
///
/// <para>
/// <b>PascalCase, and it is not a style question.</b> The deserialiser matches these names as written, so a
/// key in lower case is not an error and not a warning — it is a value silently left at its default, and a
/// configuration file that appears to have been read is worse than one that fails to load.
/// </para>
/// </summary>
public sealed class BotMindSettings
{
    /// <summary>Which model answers, as Ollama names it — for example <c>qwen3.5:9b</c>.</summary>
    public string Model { get; set; }

    /// <summary>"ollama" or "openai": the protocol the endpoint speaks. See <c>BotOllama.Api</c>.</summary>
    public string Api { get; set; }

    /// <summary>A bearer token for endpoints that want one.</summary>
    public string ApiKey { get; set; }

    /// <summary>Where the daemon listens.</summary>
    public string Endpoint { get; set; }

    /// <summary>How long the model is held in video memory between questions.</summary>
    public string KeepAlive { get; set; }

    /// <summary>How long one question may take before it is abandoned.</summary>
    public int? TimeoutMs { get; set; }

    /// <summary>What the thinking warrior is called. Its rules are kept under this name.</summary>
    public string WarriorName { get; set; }

    /// <summary>What the thinking archer is called.</summary>
    public string ArchitectName { get; set; }

    /// <summary>What the thinking mage is called.</summary>
    public string SageName { get; set; }

    /// <summary>What the Baron is called.</summary>
    public string BaronName { get; set; }

    /// <summary>
    /// What the four crafters are called, in order. However many names are here is how many crafter minds
    /// there are, so this is also where a fifth would be added — or where they would be cut to two.
    /// </summary>
    public string[] CrafterNames { get; set; }

    /// <summary>How often a free bot may be asked to choose again.</summary>
    public int? ThinkEveryMs { get; set; }

    /// <summary>How often one mind may spend a thinking-length call on a reckoning.</summary>
    public int? ReviewEveryMs { get; set; }

    /// <summary>How long a choice waits to be picked up by the auction before it goes stale.</summary>
    public int? ChoiceHoldsMs { get; set; }

    /// <summary>Most rules one mind keeps.</summary>
    public int? MostLessons { get; set; }

    /// <summary>What a mind's asking for a piece of work is worth on top of the work itself.</summary>
    public double? Insistence { get; set; }
}

/// <summary>Reads the mind file and moves the numbers it names.</summary>
public static class BotMindConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMindConfig));

    private const string ConfigPath = "Configuration/bot-mind.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotMindSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotMindSettings());

            logger.Information(
                "Wrote a starter mind file to {Path}; every number stays as the code has it",
                ConfigPath
            );

            return;
        }

        BotOllama.Model = settings.Model ?? BotOllama.Model;
        BotOllama.Api = string.IsNullOrWhiteSpace(settings.Api) ? BotOllama.Api : settings.Api.Trim();
        BotOllama.ApiKey = settings.ApiKey ?? BotOllama.ApiKey;
        BotOllama.Endpoint = settings.Endpoint ?? BotOllama.Endpoint;
        BotOllama.KeepAlive = settings.KeepAlive ?? BotOllama.KeepAlive;
        BotOllama.TimeoutMs = settings.TimeoutMs ?? BotOllama.TimeoutMs;

        BotMinds.WarriorName = settings.WarriorName ?? BotMinds.WarriorName;
        BotMinds.ArchitectName = settings.ArchitectName ?? BotMinds.ArchitectName;
        BotMinds.SageName = settings.SageName ?? BotMinds.SageName;
        BotMinds.BaronName = settings.BaronName ?? BotMinds.BaronName;

        // Length included: an empty array in the file is a legitimate way of saying "no thinking crafters",
        // and treating it as "unset" would make that instruction unspeakable.
        BotMinds.CrafterNames = settings.CrafterNames ?? BotMinds.CrafterNames;

        BotMind.ThinkEveryMs = settings.ThinkEveryMs ?? BotMind.ThinkEveryMs;
        BotMind.ReviewEveryMs = settings.ReviewEveryMs ?? BotMind.ReviewEveryMs;
        BotMind.ChoiceHoldsMs = settings.ChoiceHoldsMs ?? BotMind.ChoiceHoldsMs;
        BotMind.MostLessons = settings.MostLessons ?? BotMind.MostLessons;

        BotMindDeed.Insistence = settings.Insistence ?? BotMindDeed.Insistence;
    }
}
