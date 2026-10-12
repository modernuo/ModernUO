using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The voices as a module: reads the file, opens the speech log, and hooks the occasions.
///
/// After the population and the will, because every occasion here is one of theirs. The mood clock runs on
/// its own timer, once every few seconds over the roster, and each bot decides for itself whether it is due.
/// </summary>
public sealed class BotVoiceModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotVoiceModule));

    /// <summary>How often the roster is walked for moods that are due.</summary>
    public static int TickMs { get; set; } = 5000;

    private static MoodTimer _timer;

    public override string Name => "Voice";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population", "Will"];

    public override void Start()
    {
        BotVoiceConfig.Load();

        if (!BotVoice.Enabled)
        {
            logger.Information("The bots are silent: bot-voice.json says Enabled is false");

            return;
        }

        BotVoice.Open();

        BotWill.Started += BotVoice.Took;
        BotWill.Ended += BotVoice.Ended;

        _timer ??= new MoodTimer();
        _timer.Start();

        // The trade room is made once the engine has made its own conferences (see BotVoice.OpenTradeRoom); on a world reload the
        // server is already up and it is made, or put back in its place, at once.
        if (_serverUp)
        {
            OpenTradeRoom();
        }
        else if (!_hooked)
        {
            _hooked = true;
            EventSink.ServerStarted += OnServerStarted;
        }

        logger.Information(
            "The bots speak: world {World}, trade {Trade}, guild {Guild}, local {Local}, mood {Mood}, cry {Cry}; one line per bot every {Every}s, a mood every {MoodMin} min, {Chance:P0} of work said aloud; the transcript is {Log}",
            BotVoice.WorldOn,
            BotVoice.TradeOn,
            BotVoice.GuildOn,
            BotVoice.LocalOn,
            BotVoice.MoodOn,
            BotVoice.CryOn,
            BotVoice.SayEveryMs / 1000,
            BotVoice.MoodEveryMs / 60000,
            BotVoice.WorkChance,
            BotVoice.LogPath ?? "nowhere"
        );
    }

    private static bool _hooked;

    private static bool _serverUp;

    private static void OnServerStarted()
    {
        _serverUp = true;

        if (BotVoice.Enabled)
        {
            OpenTradeRoom();
        }
    }

    private static void OpenTradeRoom()
    {
        var room = BotVoice.OpenTradeRoom();

        if (room == null)
        {
            logger.Information(
                "Trade lines are said overhead only: {Why}",
                BotVoice.TradeOn ? "bot-voice.json names no TradeConference" : "the trade channel is off"
            );

            return;
        }

        logger.Information(
            "The trade room is the chat conference \"{Room}\" ({Place} of {Count} on the list, {Login}; the chat system is {Chat}): wants, raises, offers, orders, shop and haul calls go there instead of to every client, one call for an item every {Repeat}s across the market{Over}; hand-overs {HandOvers}",
            room.Name,
            Server.Engines.Chat.Channel.Channels.IndexOf(room) + 1,
            Server.Engines.Chat.Channel.Channels.Count,
            BotVoice.TradeAtLogin ? "joined at login" : "joined from the client's chat window",
            Server.Engines.Chat.ChatSystem.Enabled ? "on" : "OFF — nobody can join it (chat.enabled in modernuo.json)",
            BotVoice.TradeRepeatMs / 1000,
            BotVoice.TradeOverhead ? ", and said overhead" : "",
            BotVoice.TradeHandOvers ? "go to the room too" : "stay local"
        );
    }

    public override void Reset()
    {
        BotVoice.Forget();
    }

    private static void Tick()
    {
        if (!BotVoice.Enabled || !BotVoice.MoodOn)
        {
            return;
        }

        var now = Core.TickCount;
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is { Deleted: false, Alive: true } && bot.Map != Map.Internal)
            {
                BotVoice.Mood(bot, now);
            }
        }
    }

    private sealed class MoodTimer : Timer
    {
        public MoodTimer() : base(TimeSpan.FromMilliseconds(TickMs), TimeSpan.FromMilliseconds(TickMs))
        {
        }

        protected override void OnTick() => Tick();
    }
}
