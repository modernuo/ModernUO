using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Server.Engines.Chat;
using Server.Guilds;
using Server.Logging;
using Server.Network;
using Server.Text;
using ChatChannel = Server.Engines.Chat.Channel;

namespace Server.BotAI.V2;

/// <summary>
/// The bots' voices: what they say out loud, to whom, and how often.
///
/// <para>
/// <b>Speech is a by-product of doing, never a thing a bot decides to do.</b> Nothing here is offered to
/// the auction and nothing costs a beat. A line rides on an event that was happening anyway — work taken
/// up or finished, a blow landed, a war declared — and the only decisions taken here are whether to say it
/// and in which channel. That keeps the voices from ever changing what the population does, which is the
/// invariant every observer on this shard has to keep.
/// </para>
///
/// <para>
/// <b>Six channels, and the channel is the audience.</b> <c>local</c> is said overhead where the bot stands;
/// <c>guild</c> goes to the guild's chat and overhead with the guild's letters; <c>trade</c> is what is bought,
/// sold and carried, said overhead and posted to the engine's chat conference <see cref="TradeConference"/>,
/// which a player joins or leaves in the client; <c>world</c> is shouted and, if allowed, broadcast to every
/// client as the crier would; <c>mood</c> is a local line about how the bot feels; <c>cry</c> is a yell for
/// help. Every line, whatever the channel, is also an event on the stream and a line in
/// <c>logs/bot-speech.log</c>, because the person watching is more often at a page than in the world.
/// </para>
///
/// <para>
/// <b>Trade has its own room (02.10.2026; Patrick: "let the bots create a Trade chat and write into it,
/// because they write into the global one and clog half the screen").</b> On build 389 every one of the 4018 lines
/// the world channel carried in its first hour was a want, a raise, an offer, an order or a shop's call — 3780 an
/// hour, each one a system message on every connected client. The global line is a broadcast nobody can leave;
/// a chat conference is a room a player walks into from the client's chat window and out of again. The client on
/// this shard prints a conference's lines in the same corner as a broadcast, so the room is not joined at login
/// (<see cref="TradeAtLogin"/>), and the same item is not called twice in <see cref="TradeRepeatMs"/> by anybody:
/// thirty buyers calling for raw ribs in one minute read as one buyer calling thirty times.
/// </para>
///
/// <para>
/// <b>Rationed per bot, not per shard.</b> Fifty bots each saying something every fifteen seconds is three
/// lines a second, which reads as a crowd; one bot saying something every second is a fault. The throttle
/// is by bot and channel, and a cry for help is exempt from everything but its own shorter clock.
/// </para>
/// </summary>
public static class BotVoice
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotVoice));

    public static bool Enabled { get; set; } = true;

    public static bool WorldOn { get; set; } = true;

    public static bool GuildOn { get; set; } = true;

    public static bool LocalOn { get; set; } = true;

    public static bool MoodOn { get; set; } = true;

    public static bool CryOn { get; set; } = true;

    /// <summary>Whether the trade channel speaks at all: wants, raises, offers, orders, shop and haul calls (02.10.2026).</summary>
    public static bool TradeOn { get; set; } = true;

    /// <summary>
    /// The engine chat conference trade lines are posted to. Made at boot as a standing conference beside the engine's own
    /// "Newbie Help"; a player joins it from the client's chat window (ClassicUO: a macro, Open → Chat) and leaves with the
    /// window's Leave button or by joining another. Empty: trade lines are said overhead only.
    /// </summary>
    public static string TradeConference { get; set; } = "Trade";

    /// <summary>
    /// Whether a client lands in the trade conference when it logs in. The engine puts a new chat user into the first conference
    /// on its list; off, that stays the engine's "Newbie Help" and the trade room waits to be joined. Off by default because
    /// ClassicUO prints a conference's lines in the same corner of the screen as a broadcast — on, the clutter is back.
    /// </summary>
    public static bool TradeAtLogin { get; set; } = false;

    /// <summary>
    /// How long a line about one item keeps every other line about the same item out of the trade channel, from any bot. A minute.
    /// Zero: no such limit, only each bot's own <see cref="SayEveryMs"/>.
    /// </summary>
    public static int TradeRepeatMs { get; set; } = 60000;

    /// <summary>Whether a trade line is also said over the speaker's head, for whoever is standing by: the market by the bank still sounds like one.</summary>
    public static bool TradeOverhead { get; set; } = true;

    /// <summary>
    /// Whether the lines said across a hand-over — "here you are", "as agreed, thank you", a keeper's "there you are" — go to the
    /// trade channel too. Off: they stay local, said to the one bot standing beside the speaker, as they always were.
    /// </summary>
    public static bool TradeHandOvers { get; set; } = false;

    public static int TradeHue { get; set; } = 0x59;

    public static int SayEveryMs { get; set; } = 15000;

    public static int CryEveryMs { get; set; } = 20000;

    public static int MoodEveryMs { get; set; } = 900000;

    public static double WorkChance { get; set; } = 0.35;

    public static double FailChance { get; set; } = 0.6;

    public static double DropChance { get; set; } = 0.15;

    public static int MostLetters { get; set; } = 160;

    public static int WorldHue { get; set; } = 0x59;

    public static int GuildHue { get; set; } = 0x3B2;

    public static int CryHue { get; set; } = 0x22;

    public static int MoodHue { get; set; } = 0x3B2;

    public static bool Broadcast { get; set; } = true;

    /// <summary>Below this share of health a cry is the desperate kind.</summary>
    public static double Desperate { get; set; } = 0.3;

    /// <summary>Below this share of health a fresh foe is worth a cry at all.</summary>
    public static double Hurt { get; set; } = 0.7;

    /// <summary>How far a line carries, in tiles: who is in earshot of a mood (01.10.2026, build 387). Twelve, about a screen.</summary>
    public static int Earshot { get; set; } = 12;

    /// <summary>
    /// How long a mood line that falls due with nobody in earshot waits for somebody before it is said anyway. Ten minutes.
    ///
    /// <para>
    /// <b>A mood is said to somebody, when there is somebody (01.10.2026, build 387).</b> On build 386 a bot said how it felt once in
    /// fifteen minutes wherever it stood, and at any moment about half the living bots had nobody within twelve tiles — so half the
    /// moods were said to the log. Waiting changes only when the line is said, never what the bot does, which is the voice's one
    /// invariant; what it buys is that "I am broke" is heard by a neighbour who can answer it. See <see cref="BotSocial"/>.
    /// </para>
    /// </summary>
    public static int MoodHoldMs { get; set; } = 600000;

    /// <summary>How often a waiting mood line looks again for an audience. A minute: one spatial look per waiting bot that often.</summary>
    public static int MoodRetryMs { get; set; } = 60000;

    /// <summary>
    /// A mood line was said with somebody in earshot: the speaker, the tag the line carried (<see cref="BotMoods"/>), the line, and
    /// everybody within <see cref="Earshot"/>, hostile or not. The social layer listens here and reads the mood from the tag, never
    /// from the speaker.
    /// </summary>
    public static Action<BotMobile, string, string, List<BotMobile>> MoodSaid { get; set; }

    public static long MoodsHeard { get; private set; }

    public static long MoodsAlone { get; private set; }

    public static long MoodsWaited { get; private set; }

    public static long Said { get; private set; }

    public static long Held { get; private set; }

    public static long Cries { get; private set; }

    public static long Moods { get; private set; }

    public static long Shouts { get; private set; }

    public static long GuildLines { get; private set; }

    /// <summary>Lines said in the trade channel.</summary>
    public static long Trades { get; private set; }

    /// <summary>
    /// Attempts at a trade line held because some bot said a line about the same item within <see cref="TradeRepeatMs"/>. Attempts, not
    /// lines: a held want is tried again on every market beat until its turn comes.
    /// </summary>
    public static long TradeRepeats { get; private set; }

    /// <summary>Trade lines posted to the conference while at least one player was in it, and the deliveries that made (line × player).</summary>
    public static long TradeHeard { get; private set; }

    public static long TradeDeliveries { get; private set; }

    /// <summary>Trade lines said while nobody was in the conference, or with no conference to post to: overhead, the page and the log only.</summary>
    public static long TradeUnheard { get; private set; }

    /// <summary>The channel a hand-over line is said in. See <see cref="TradeHandOvers"/>.</summary>
    public static string HandOver => TradeHandOvers ? "trade" : "local";

    /// <summary>Lines said, by channel, since the voices opened.</summary>
    private static readonly Dictionary<string, long> _byChannel = [];

    /// <summary>When the voices opened, so the summary can say lines an hour.</summary>
    private static long _openedTick;

    private static bool _opened;

    /// <summary>When a trade line about each item was last said, by any bot. See <see cref="TradeRepeatMs"/>.</summary>
    private static readonly Dictionary<string, long> _topicTick = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string[]> _phrases = BotPhrases.Defaults();

    private static readonly Dictionary<(Serial, string), long> _last = [];

    private static readonly Dictionary<Serial, string> _lastLine = [];

    private static readonly Dictionary<Serial, long> _moodTick = [];

    /// <summary>When each waiting mood line first fell due, so the wait is bounded. See <see cref="MoodHoldMs"/>.</summary>
    private static readonly Dictionary<Serial, long> _moodHeld = [];

    /// <summary>Who was in earshot of the last mood line. One game thread, so one list; handed to <see cref="MoodSaid"/> and cleared on the next look.</summary>
    private static readonly List<BotMobile> _audience = [];

    private static readonly Dictionary<Serial, Serial> _lastFoe = [];

    private static StreamWriter _log;

    public static string LogPath { get; private set; }

    public static void Open()
    {
        if (!_opened)
        {
            _opened = true;
            _openedTick = Core.TickCount;
        }

        try
        {
            var folder = Path.GetFullPath(Path.Combine(Core.BaseDirectory, "..", "logs"));

            Directory.CreateDirectory(folder);

            LogPath = Path.Combine(folder, "bot-speech.log");
            _log = new StreamWriter(new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false))
            {
                AutoFlush = true
            };

            _log.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} --- the shard is up; the bots may speak ---");
        }
        catch (Exception e)
        {
            logger.Warning("The speech log could not be opened: {Message}", e.Message);
        }
    }

    /// <summary>Replaces the phrases for every occasion the file names, and keeps the code's for the rest.</summary>
    public static void Phrases(Dictionary<string, string[]> bank)
    {
        foreach (var (key, lines) in bank)
        {
            if (!string.IsNullOrWhiteSpace(key) && lines is { Length: > 0 })
            {
                _phrases[key] = lines;
            }
        }
    }

    public static bool Has(string occasion) => _phrases.ContainsKey(occasion);

    /// <summary>A line for the occasion, with the placeholders filled, or null when there is nothing to say.</summary>
    public static string Phrase(string occasion, string fallback, Mobile who, Dictionary<string, string> fill)
    {
        if (!_phrases.TryGetValue(occasion, out var lines) && (fallback == null || !_phrases.TryGetValue(fallback, out lines)))
        {
            return null;
        }

        if (lines == null || lines.Length == 0)
        {
            return null;
        }

        var line = lines[Utility.Random(lines.Length)];

        // Not the same line twice running from one mouth: with four lines to a key, a third of the time
        // the random pick is the one just said.
        if (lines.Length > 1 && who != null && _lastLine.TryGetValue(who.Serial, out var last) && last == line)
        {
            line = lines[(Array.IndexOf(lines, line) + 1) % lines.Length];
        }

        return Fill(line, who, fill);
    }

    private static string Fill(string line, Mobile who, Dictionary<string, string> fill)
    {
        var text = line;

        if (who != null)
        {
            text = text.Replace("{name}", who.Name ?? "somebody");
            text = text.Replace("{class}", (who as BotMobile)?.Class?.Name ?? "");
            text = text.Replace("{guild}", (who.Guild as Guild)?.Name ?? "the guild");
            text = text.Replace("{place}", $"({who.X}, {who.Y})");
        }

        if (fill != null)
        {
            foreach (var (key, value) in fill)
            {
                text = text.Replace("{" + key + "}", value ?? "");
            }
        }

        // A placeholder nobody filled reads as a fault; it is cut rather than shown.
        var open = text.IndexOf('{');

        while (open >= 0)
        {
            var close = text.IndexOf('}', open);

            if (close < 0)
            {
                break;
            }

            text = text.Remove(open, close - open + 1);
            open = text.IndexOf('{');
        }

        text = text.Replace("  ", " ").Trim();

        // A line that begins with a creature's article — "a troll is on me" — begins with a capital, as a line does.
        return text.Length > 0 && char.IsLower(text[0]) ? char.ToUpperInvariant(text[0]) + text[1..] : text;
    }

    /// <summary>
    /// Says a line in a channel, if the bot may speak now. Returns whether it was said.
    /// </summary>
    /// <param name="force">Ignore the per-bot clock: for a line that is the whole point, such as a thinking bot's remark.</param>
    /// <param name="tag">The mood the line carries, for a mood line: written behind the text on the event stream (<c>data</c>) and in the log, never into the words.</param>
    /// <param name="topic">
    /// For a trade line, the item it is about — the type's name — so that no two bots call the same item within
    /// <see cref="TradeRepeatMs"/>. Null: the line is about somebody in particular, and only the bot's own clock holds it.
    /// </param>
    public static bool Say(Mobile who, string channel, string text, bool force = false, string tag = null, string topic = null)
    {
        if (!Enabled || who is not { Deleted: false } || string.IsNullOrWhiteSpace(text) || who.Map == null || who.Map == Map.Internal)
        {
            return false;
        }

        channel ??= "local";

        var on = channel switch
        {
            "world" => WorldOn,
            "trade" => TradeOn,
            "guild" => GuildOn,
            "mood" => MoodOn,
            "cry" => CryOn,
            _ => LocalOn
        };

        if (!on)
        {
            return false;
        }

        var now = Core.TickCount;
        var every = channel == "cry" ? CryEveryMs : SayEveryMs;

        if (!force && _last.TryGetValue((who.Serial, channel), out var last) && now - last < every)
        {
            Held++;

            return false;
        }

        // One call for an item across the whole market, not one per buyer (02.10.2026). Checked after the bot's own clock, so a line
        // its speaker could not have said anyway is counted there and not here; and before anything is stamped, so a line held here
        // leaves the bot free to say its next one. A held want is tried again on the market's next beat, so the count below is of
        // attempts, not of wants; and what is held is speech, never the trading — no carrier reads the chat.
        if (!force && topic != null && channel == "trade" && TradeRepeatMs > 0 && _topicTick.TryGetValue(topic, out var called)
            && now - called < TradeRepeatMs)
        {
            TradeRepeats++;

            return false;
        }

        _last[(who.Serial, channel)] = now;

        if (topic != null && channel == "trade")
        {
            _topicTick[topic] = now;
        }

        var line = text.Replace('\n', ' ').Replace('\r', ' ').Trim();

        if (line.Length > MostLetters)
        {
            line = line[..MostLetters];
        }

        _lastLine[who.Serial] = line;

        try
        {
            Deliver(who, channel, line);
        }
        catch (Exception e)
        {
            logger.Warning("{Name} could not say a line: {Message}", who.Name, e.Message);
        }

        Said++;

        switch (channel)
        {
            case "cry":
                Cries++;

                break;
            case "mood":
                Moods++;

                break;
            case "world":
                Shouts++;

                break;
            case "guild":
                GuildLines++;

                break;
            case "trade":
                Trades++;

                break;
        }

        _byChannel[channel] = _byChannel.TryGetValue(channel, out var count) ? count + 1 : 1;

        BotEvents.Post("say", who, line, channel, tag == null ? null : $"{{\"mood\":\"{tag}\"}}");
        Record(who, channel, line, tag);

        return true;
    }

    /// <summary>A line said by an existing mechanism, passed through so that the page and the log see it. Never held back.</summary>
    public static void Aloud(Mobile who, string text) => Say(who, "local", text, force: true);

    private static void Deliver(Mobile who, string channel, string line)
    {
        var guild = who.Guild as Guild;

        switch (channel)
        {
            case "world":
                who.PublicOverheadMessage(MessageType.Yell, WorldHue, true, line);

                if (Broadcast && NetState.Instances.Count > 0)
                {
                    var tag = guild == null ? "" : $" [{guild.Abbreviation}]";

                    World.Broadcast(WorldHue, true, $"{who.Name}{tag}: {line}");
                }

                break;
            case "trade":
                // Over the head in the hue the world's yell had, so the bank sounds as it did; the island hears it only in the room.
                if (TradeOverhead)
                {
                    who.PublicOverheadMessage(MessageType.Regular, TradeHue, true, line);
                }

                PostTrade(who, guild, line);

                break;
            case "guild":
                if (guild != null)
                {
                    guild.GuildChat(who, GuildHue, line);
                    who.PublicOverheadMessage(MessageType.Regular, GuildHue, true, $"[{guild.Abbreviation}] {line}");
                }
                else
                {
                    who.PublicOverheadMessage(MessageType.Regular, GuildHue, true, line);
                }

                break;
            case "cry":
                who.PublicOverheadMessage(MessageType.Yell, CryHue, true, line);

                break;
            case "mood":
                who.PublicOverheadMessage(MessageType.Regular, MoodHue, true, line);

                break;
            default:
                who.Say(line);

                break;
        }
    }

    // ---- the trade room -----------------------------------------------------------------------------------

    /// <summary>The conference trade lines go to, or null when there is none (no name, or it has not been made yet).</summary>
    public static ChatChannel TradeRoom() =>
        string.IsNullOrWhiteSpace(TradeConference) ? null : ChatChannel.FindChannelByName(TradeConference);

    /// <summary>
    /// Makes the trade conference a standing one and puts it where <see cref="TradeAtLogin"/> says on the engine's list. After the
    /// engine's own <c>Initialize</c>, which is where "Newbie Help" is made: the bots' modules start on the world's load, before it,
    /// and a conference made then would be first on the list and every login would land in it.
    /// </summary>
    public static ChatChannel OpenTradeRoom()
    {
        if (!TradeOn || string.IsNullOrWhiteSpace(TradeConference))
        {
            return null;
        }

        var room = TradeRoom();

        if (room == null)
        {
            ChatChannel.AddStaticChannel(TradeConference);
            room = TradeRoom();
        }
        else
        {
            room.AlwaysAvailable = true;
        }

        if (room == null)
        {
            return null;
        }

        var list = ChatChannel.Channels;

        list.Remove(room);

        if (TradeAtLogin)
        {
            list.Insert(0, room);
        }
        else
        {
            list.Add(room);
        }

        // <b>Leaving a room in the client (02.10.2026).</b> ClassicUO's chat window sends action 0x43 for its Leave button and the
        // engine has no handler for it — "Unknown chat action 0x43" and the player stays in. Added here only where nothing else
        // has claimed it: the player leaves whichever conference it is in, as the button says.
        if (ChatActionHandlers.GetHandler(LeaveAction) == null)
        {
            ChatActionHandlers.Register(LeaveAction, false, true, LeaveConference);
        }

        return room;
    }

    /// <summary>The chat action ClassicUO sends for "leave this conference".</summary>
    public const int LeaveAction = 0x43;

    private static void LeaveConference(ChatUser from, ChatChannel channel, string param) => channel?.RemoveUser(from);

    /// <summary>How many players are in the trade conference now.</summary>
    public static int TradeListeners()
    {
        var room = TradeRoom();

        if (room == null)
        {
            return 0;
        }

        var count = 0;

        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile is { } m && ChatUser.GetChatUser(m)?.CurrentChannel == room)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// A trade line into the conference, as a member's own words arrive: message 57 of the chat, "%1: %2". The first character of
    /// %1 is the speaker's standing in the room (0, an ordinary member), which the client reads and drops before the name. A bot
    /// is not a chat user — that needs a connection — so it posts without joining, and nobody in the room can ignore it by name.
    /// </summary>
    private static void PostTrade(Mobile who, Guild guild, string line)
    {
        var room = TradeRoom();
        var listeners = room == null ? 0 : TradeListeners();

        if (listeners == 0)
        {
            TradeUnheard++;

            return;
        }

        var name = guild == null ? who.Name : $"{who.Name} [{guild.Abbreviation}]";

        room.SendMessage(57, null, ChatUser.NormalColorCharacter + name, line);

        TradeHeard++;
        TradeDeliveries += listeners;
    }

    private static void Record(Mobile who, string channel, string line, string tag)
    {
        if (_log == null)
        {
            return;
        }

        try
        {
            var guild = (who.Guild as Guild)?.Abbreviation;

            // The tag rides behind the line, after a hash, so the line itself stays the words and the mood stays greppable.
            _log.WriteLine($"{DateTime.Now:HH:mm:ss} [{channel}] {who.Name}{(guild == null ? "" : $" ({guild})")} @{who.X},{who.Y}: {line}{(tag == null ? "" : $" #{tag}")}");
        }
        catch
        {
            // The log is a courtesy; the event stream has the line.
        }
    }

    // ---- occasions ----------------------------------------------------------------------------------------

    /// <summary>Work taken up. Orders are said in the trade channel, because they are a question to the whole market.</summary>
    public static void Took(Mobile who, BotDeed deed, string because)
    {
        if (!Enabled || who == null || deed == null)
        {
            return;
        }

        if (deed is BotOrder order)
        {
            var fill = new Dictionary<string, string>
            {
                ["item"] = Words(order.Wanted?.Name),
                ["amount"] = order.Units.ToString(),
                ["price"] = order.Price.ToString(),
                ["stage"] = deed.Stage ?? ""
            };

            var seek = Phrase("seek", null, who, fill);

            if (seek != null)
            {
                Say(who, "trade", seek, topic: order.Wanted?.Name);
            }

            return;
        }

        if (Utility.RandomDouble() >= WorkChance)
        {
            return;
        }

        var line = Phrase($"took:{deed.Kind}", "took:*", who, WorkFill(deed, null, 0, 0));

        if (line != null)
        {
            Say(who, "local", line);
        }
    }

    /// <summary>Work ended, one way or another.</summary>
    public static void Ended(Mobile who, BotDeed deed, BotEnding ending, string why, double minutes, double perMinute, int coin)
    {
        if (who == null || deed == null)
        {
            return;
        }

        // Pride is a record of work that paid; the mood reads it. See BotMoods.
        if (ending == BotEnding.Done)
        {
            BotMoods.Won(who, perMinute);
        }

        if (!Enabled || ending == BotEnding.Died)
        {
            return;
        }

        var (occasion, chance) = ending switch
        {
            BotEnding.Done => ("finished", WorkChance),
            BotEnding.Failed => ("failed", FailChance),
            _ => ("dropped", DropChance)
        };

        // A flight is worth a word when it ends; the rest are rationed.
        if (deed.Kind != BotBolt.Trade && Utility.RandomDouble() >= chance)
        {
            return;
        }

        var line = Phrase($"{occasion}:{deed.Kind}", $"{occasion}:*", who, WorkFill(deed, why, minutes, coin));

        if (line != null)
        {
            Say(who, "local", line);
        }
    }

    private static Dictionary<string, string> WorkFill(BotDeed deed, string why, double minutes, int coin) =>
        new()
        {
            ["work"] = deed.Kind,
            ["stage"] = deed.Stage ?? deed.Kind,
            ["reason"] = Short(why),
            ["minutes"] = minutes.ToString("F0"),
            ["coin"] = coin.ToString()
        };

    /// <summary>A blow landed. A cry when it is the first from this foe, or the bot is losing, and the clock allows.</summary>
    public static void Struck(BotMobile who, Mobile from)
    {
        if (!Enabled || !CryOn || who == null || from == null || !who.Alive || who.HitsMax <= 0)
        {
            return;
        }

        var share = (double)who.Hits / who.HitsMax;
        var fresh = !_lastFoe.TryGetValue(who.Serial, out var foe) || foe != from.Serial;

        _lastFoe[who.Serial] = from.Serial;

        // A cry is for trouble, not for every creature that swings once: a fresh foe is worth one when the bot is
        // already hurt, and anything is worth one when the bot is nearly dead. 209 cries in the first fifteen minutes
        // of build 269 were a company's every new opponent announced by every member.
        if (!(share <= Desperate || (fresh && share < Hurt)))
        {
            return;
        }

        var fill = new Dictionary<string, string> { ["foe"] = Words(from.Name) };
        var line = share <= Desperate ? Phrase("cry:low", "cry", who, fill) : Phrase("cry", null, who, fill);

        if (line != null)
        {
            Say(who, "cry", line);
        }
    }

    /// <summary>Last words, and a line to the guild.</summary>
    public static void Died(BotMobile who, Mobile killer)
    {
        if (!Enabled || who == null)
        {
            return;
        }

        _lastFoe.Remove(who.Serial);
        BotMoods.Fell(who);

        var fill = new Dictionary<string, string> { ["killer"] = Words(killer?.Name) ?? "something" };
        var line = Phrase("died", null, who, fill);

        if (line != null)
        {
            Say(who, "local", line, force: true);
        }

        if (who.Guild is Guild guild)
        {
            var speaker = Spokesman(guild, who);

            if (speaker != null)
            {
                var fell = Phrase("guild:fell", null, speaker, new Dictionary<string, string> { ["name"] = who.Name, ["place"] = $"({who.X}, {who.Y})" });

                if (fell != null)
                {
                    Say(speaker, "guild", fell, force: true);
                }
            }
        }
    }

    /// <summary>How the bot feels, said on its own clock. Returns whether it was due.</summary>
    public static bool Mood(BotMobile who, long now)
    {
        if (!Enabled || !MoodOn || who is not { Deleted: false, Alive: true })
        {
            return false;
        }

        if (!_moodTick.TryGetValue(who.Serial, out var last))
        {
            // Staggered from the start, so fifty bots do not all speak in the same minute.
            _moodTick[who.Serial] = now - Utility.Random(Math.Max(1, MoodEveryMs));

            return false;
        }

        if (now - last < MoodEveryMs)
        {
            return false;
        }

        // <b>One word for the mood, and the line is said to somebody (01.10.2026, build 387).</b> The tag is read off facts the shard
        // keeps (BotMoods) and rides behind the line; a line due with nobody in earshot waits, up to MoodHoldMs, looking again
        // every MoodRetryMs. Hostile neighbours are in the audience list — the social layer refuses them and counts it — but do
        // not count as somebody to wait for.
        var (tag, occasion) = BotMoods.Read(who, now);
        var friendly = Audience(who);

        if (friendly == 0 && MoodHoldMs > 0)
        {
            if (!_moodHeld.TryGetValue(who.Serial, out var since))
            {
                since = now;
                _moodHeld[who.Serial] = now;
            }

            if (now - since < MoodHoldMs)
            {
                _moodTick[who.Serial] = now - MoodEveryMs + Math.Max(1000, MoodRetryMs);
                MoodsWaited++;

                return false;
            }
        }

        _moodHeld.Remove(who.Serial);
        _moodTick[who.Serial] = now;

        var line = Phrase(occasion, "mood:mid", who, new Dictionary<string, string> { ["mood"] = who.Mood.ToString("P0") });

        if (line == null || !Say(who, "mood", line, false, tag))
        {
            return true;
        }

        if (friendly == 0)
        {
            MoodsAlone++;
        }
        else
        {
            MoodsHeard++;
            MoodSaid?.Invoke(who, tag, line, _audience);

            // Now and then a tale after it, to the same ears (07.10.2026, traits17). See BotBiography.AfterMood.
            BotBiography.AfterMood(who);
        }

        return true;
    }

    /// <summary>Everybody within earshot of this bot into the audience list, and how many of them are not of a hostile guild.</summary>
    private static int Audience(BotMobile who)
    {
        _audience.Clear();

        var map = who.Map;

        if (map == null || map == Map.Internal)
        {
            return 0;
        }

        var friendly = 0;

        foreach (var near in map.GetMobilesInRange<BotMobile>(who.Location, Earshot))
        {
            if (ReferenceEquals(near, who) || near is not { Deleted: false, Alive: true, Hidden: false })
            {
                continue;
            }

            _audience.Add(near);

            if (!BotRegard.Hostile(who, near))
            {
                friendly++;
            }
        }

        return friendly;
    }

    /// <summary>A guild's spokesman says something to the guild: war, a hall, a loss.</summary>
    public static void ToGuild(string guildName, string occasion, Dictionary<string, string> fill)
    {
        if (!Enabled || !GuildOn || guildName == null)
        {
            return;
        }

        if (BaseGuild.FindByName(guildName) is not Guild guild)
        {
            return;
        }

        var speaker = Spokesman(guild, null);

        if (speaker == null)
        {
            return;
        }

        fill ??= [];
        fill.TryAdd("guild", guild.Name);

        var line = Phrase(occasion, null, speaker, fill);

        if (line != null)
        {
            Say(speaker, "guild", line, force: true);
        }
    }

    /// <summary>The leader if it can speak, else any living member. Null when nobody can.</summary>
    private static Mobile Spokesman(Guild guild, Mobile except)
    {
        if (guild.Leader is { Deleted: false, Alive: true } leader && leader != except && leader.Map != Map.Internal)
        {
            return leader;
        }

        var members = guild.Members;

        for (var i = 0; i < members.Count; i++)
        {
            var m = members[i];

            if (m is { Deleted: false, Alive: true } && m != except && m.Map != Map.Internal)
            {
                return m;
            }
        }

        return null;
    }

    public static (string Text, DateTime At)? LastOf(Mobile who)
    {
        var e = BotEvents.LastSaid(who?.Name);

        return e == null ? null : (e.Text, e.At);
    }

    private static string Short(string why)
    {
        if (string.IsNullOrEmpty(why))
        {
            return "";
        }

        var cut = why.IndexOf(':');
        var text = cut > 0 && cut < 60 ? why[..cut] : why;

        if (text.Length <= 60)
        {
            return text;
        }

        // At a word, not inside one (build 338): the speech log read "…in 3 minu." where a reason ran past sixty.
        var space = text.LastIndexOf(' ', 60);

        return space > 30 ? text[..space] : text[..60];
    }

    /// <summary>A type or creature name as words: "IronIngot" reads as "iron ingot".</summary>
    public static string Words(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        using var sb = ValueStringBuilder.Create(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
            {
                sb.Append(' ');
            }

            sb.Append(i == 0 ? c : char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    public static void Forget()
    {
        _last.Clear();
        _lastLine.Clear();
        _moodTick.Clear();
        _moodHeld.Clear();
        _audience.Clear();
        _lastFoe.Clear();
        _topicTick.Clear();
        BotMoods.Forget();
    }

    /// <summary>The order the summary names the channels in: the two a player sees on screen first.</summary>
    private static readonly string[] _channels = ["world", "trade", "guild", "local", "mood", "cry"];

    /// <summary>
    /// What the voices said, channel by channel, with lines an hour since the shard came up — the number that says whether the global
    /// line is clear (02.10.2026; there was no Voice line in the five-minute summary at all before this, Describe had no caller).
    /// </summary>
    public static string Describe()
    {
        if (!Enabled)
        {
            return "the bots are silent";
        }

        var hours = _opened ? Math.Max(1.0 / 60, (Core.TickCount - _openedTick) / 3600000.0) : 1.0;

        using var sb = ValueStringBuilder.Create(512);

        sb.Append($"{Said} lines said in {hours:F1} h ({Said / hours:F0}/h): ");

        var first = true;

        foreach (var channel in _channels)
        {
            var n = _byChannel.TryGetValue(channel, out var c) ? c : 0;

            sb.Append(first ? "" : ", ");
            sb.Append($"{channel} {n} ({n / hours:F0}/h)");
            first = false;
        }

        foreach (var (channel, n) in _byChannel)
        {
            if (Array.IndexOf(_channels, channel) < 0)
            {
                sb.Append($", {channel} {n} ({n / hours:F0}/h)");
            }
        }

        sb.Append($"; {Held} held back by a bot's own clock; ");

        var room = TradeRoom();

        sb.Append(
            room == null
                ? $"trade: no conference to post to ({(TradeOn ? $"\"{TradeConference}\" not made" : "the channel is off")}), "
                : $"trade: the \"{room.Name}\" conference has {TradeListeners()} players in it now{(ChatSystem.Enabled ? "" : " (the chat system is off)")}, "
        );

        sb.Append(
            $"{TradeHeard} lines posted while somebody was there ({TradeDeliveries} deliveries), {TradeUnheard} to an empty room, "
            + $"{TradeRepeats} attempts held as the same item called within {TradeRepeatMs / 1000}s; "
        );

        sb.Append(
            $"{MoodsHeard} moods said to somebody within {Earshot} tiles, {MoodsAlone} to nobody after waiting, {MoodsWaited} waits for an audience"
        );

        return sb.ToString();
    }
}
