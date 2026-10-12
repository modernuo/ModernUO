using System.Collections.Generic;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-voice.json</c> may say. Everything optional; the starter file the shard writes
/// carries the whole phrase bank so that it can be edited rather than discovered.
/// </summary>
public sealed class BotVoiceSettings
{
    /// <summary>Whether the bots speak at all.</summary>
    public bool? Enabled { get; set; }

    /// <summary>The channels, one switch each.</summary>
    public bool? World { get; set; }

    public bool? Guild { get; set; }

    public bool? Local { get; set; }

    public bool? Mood { get; set; }

    public bool? Cry { get; set; }

    /// <summary>
    /// The trade channel (02.10.2026): wants, raises, offers, orders, shop and haul calls, said overhead and posted to the chat
    /// conference <see cref="TradeConference"/> instead of broadcast to every client.
    /// </summary>
    public bool? Trade { get; set; }

    /// <summary>The chat conference trade lines go to. Empty: overhead only.</summary>
    public string TradeConference { get; set; }

    /// <summary>Whether a client lands in the trade conference at login instead of the engine's first one.</summary>
    public bool? TradeAtLogin { get; set; }

    /// <summary>How long one line about an item keeps every other line about it out of the trade channel. Zero: no such limit.</summary>
    public int? TradeRepeatMs { get; set; }

    /// <summary>Whether a trade line is also said over the speaker's head.</summary>
    public bool? TradeOverhead { get; set; }

    /// <summary>Whether the hand-over lines ("here you are", "as agreed") go to the trade channel rather than staying local.</summary>
    public bool? TradeHandOvers { get; set; }

    /// <summary>The least time between two lines from one bot, in milliseconds. Cries are exempt.</summary>
    public int? SayEveryMs { get; set; }

    /// <summary>The least time between two cries from one bot.</summary>
    public int? CryEveryMs { get; set; }

    /// <summary>How often a bot says how it feels.</summary>
    public int? MoodEveryMs { get; set; }

    /// <summary>How far a line carries, in tiles: who is in earshot of a mood. See BotVoice.Earshot (01.10.2026).</summary>
    public int? Earshot { get; set; }

    /// <summary>How long a mood line due with nobody in earshot waits for somebody. See BotVoice.MoodHoldMs.</summary>
    public int? MoodHoldMs { get; set; }

    /// <summary>How often a waiting mood line looks again for an audience. See BotVoice.MoodRetryMs.</summary>
    public int? MoodRetryMs { get; set; }

    /// <summary>The chance, nought to one, that taking or finishing a piece of work is said out loud.</summary>
    public double? WorkChance { get; set; }

    /// <summary>The chance that a failure is said out loud.</summary>
    public double? FailChance { get; set; }

    /// <summary>The chance that dropping work for something better is said out loud.</summary>
    public double? DropChance { get; set; }

    /// <summary>Longest line a bot says.</summary>
    public int? MostLetters { get; set; }

    /// <summary>Hues for the kinds of overhead text.</summary>
    public int? WorldHue { get; set; }

    public int? GuildHue { get; set; }

    public int? CryHue { get; set; }

    public int? MoodHue { get; set; }

    public int? TradeHue { get; set; }

    /// <summary>Whether world lines are also broadcast to every connected client, as the town crier would.</summary>
    public bool? Broadcast { get; set; }

    /// <summary>The phrase bank, by occasion. A key given here replaces the code's list for that occasion.</summary>
    public Dictionary<string, string[]> Phrases { get; set; }
}

public static class BotVoiceConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotVoiceConfig));

    private const string ConfigPath = "Configuration/bot-voice.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotVoiceSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(
                path,
                new BotVoiceSettings
                {
                    Enabled = true,
                    World = true,
                    Guild = true,
                    Local = true,
                    Mood = true,
                    Cry = true,
                    Trade = BotVoice.TradeOn,
                    TradeConference = BotVoice.TradeConference,
                    TradeAtLogin = BotVoice.TradeAtLogin,
                    TradeRepeatMs = BotVoice.TradeRepeatMs,
                    TradeOverhead = BotVoice.TradeOverhead,
                    TradeHandOvers = BotVoice.TradeHandOvers,
                    SayEveryMs = BotVoice.SayEveryMs,
                    CryEveryMs = BotVoice.CryEveryMs,
                    MoodEveryMs = BotVoice.MoodEveryMs,
                    Earshot = BotVoice.Earshot,
                    MoodHoldMs = BotVoice.MoodHoldMs,
                    MoodRetryMs = BotVoice.MoodRetryMs,
                    WorkChance = BotVoice.WorkChance,
                    FailChance = BotVoice.FailChance,
                    DropChance = BotVoice.DropChance,
                    MostLetters = BotVoice.MostLetters,
                    WorldHue = BotVoice.WorldHue,
                    GuildHue = BotVoice.GuildHue,
                    CryHue = BotVoice.CryHue,
                    MoodHue = BotVoice.MoodHue,
                    TradeHue = BotVoice.TradeHue,
                    Broadcast = BotVoice.Broadcast,
                    Phrases = BotPhrases.Defaults()
                }
            );

            logger.Information("Wrote a starter voice file with the whole phrase bank to {Path}", ConfigPath);

            return;
        }

        BotVoice.Enabled = settings.Enabled ?? BotVoice.Enabled;
        BotVoice.WorldOn = settings.World ?? BotVoice.WorldOn;
        BotVoice.GuildOn = settings.Guild ?? BotVoice.GuildOn;
        BotVoice.LocalOn = settings.Local ?? BotVoice.LocalOn;
        BotVoice.MoodOn = settings.Mood ?? BotVoice.MoodOn;
        BotVoice.CryOn = settings.Cry ?? BotVoice.CryOn;
        BotVoice.TradeOn = settings.Trade ?? BotVoice.TradeOn;
        BotVoice.TradeConference = settings.TradeConference ?? BotVoice.TradeConference;
        BotVoice.TradeAtLogin = settings.TradeAtLogin ?? BotVoice.TradeAtLogin;
        BotVoice.TradeRepeatMs = settings.TradeRepeatMs ?? BotVoice.TradeRepeatMs;
        BotVoice.TradeOverhead = settings.TradeOverhead ?? BotVoice.TradeOverhead;
        BotVoice.TradeHandOvers = settings.TradeHandOvers ?? BotVoice.TradeHandOvers;
        BotVoice.SayEveryMs = settings.SayEveryMs ?? BotVoice.SayEveryMs;
        BotVoice.CryEveryMs = settings.CryEveryMs ?? BotVoice.CryEveryMs;
        BotVoice.MoodEveryMs = settings.MoodEveryMs ?? BotVoice.MoodEveryMs;
        BotVoice.Earshot = settings.Earshot ?? BotVoice.Earshot;
        BotVoice.MoodHoldMs = settings.MoodHoldMs ?? BotVoice.MoodHoldMs;
        BotVoice.MoodRetryMs = settings.MoodRetryMs ?? BotVoice.MoodRetryMs;
        BotVoice.WorkChance = settings.WorkChance ?? BotVoice.WorkChance;
        BotVoice.FailChance = settings.FailChance ?? BotVoice.FailChance;
        BotVoice.DropChance = settings.DropChance ?? BotVoice.DropChance;
        BotVoice.MostLetters = settings.MostLetters ?? BotVoice.MostLetters;
        BotVoice.WorldHue = settings.WorldHue ?? BotVoice.WorldHue;
        BotVoice.GuildHue = settings.GuildHue ?? BotVoice.GuildHue;
        BotVoice.CryHue = settings.CryHue ?? BotVoice.CryHue;
        BotVoice.MoodHue = settings.MoodHue ?? BotVoice.MoodHue;
        BotVoice.TradeHue = settings.TradeHue ?? BotVoice.TradeHue;
        BotVoice.Broadcast = settings.Broadcast ?? BotVoice.Broadcast;

        if (settings.Phrases != null)
        {
            BotVoice.Phrases(settings.Phrases);
        }
    }
}
