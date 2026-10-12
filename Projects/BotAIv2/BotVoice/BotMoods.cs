using System;
using System.Collections.Generic;
using Server.Guilds;

namespace Server.BotAI.V2;

/// <summary>
/// A bot's mood in one word, read off facts the shard already keeps, so that a mood line carries a tag that the person at
/// the log and the bot in earshot can both read (01.10.2026, build 387; Patrick's point 4 of the night plan: "bots understand
/// each other's mood from the phrases, and depending on the mood other bots offer company or help").
///
/// <para>
/// <b>Seven moods that ask for something, and three shades of content that do not.</b> <c>hurt</c>, <c>tired</c>,
/// <c>grieving</c>, <c>poor</c>, <c>lonely</c>, <c>bored</c> and <c>proud</c> are each a fact with a remedy somebody else
/// can offer — a bandage, a fire, a word, a hunt, a company, a cheer. <c>content</c>, <c>middling</c> and <c>glum</c> are
/// the old contentment scale (<see cref="BotMobile.Mood"/>) and ask nothing. One tag per line, worst first: a bot that is
/// bleeding and broke says it is bleeding.
/// </para>
///
/// <para>
/// <b>Every reading is a field read.</b> Hurt is the mender's own test; tired is the rest's record; poor is the hunter's own
/// poverty line (<c>BotHunter.PoorBelow</c>), one line for the whole shard rather than a second number on the same shelf;
/// lonely and bored are the urges; grief and pride are two small records fed from the death and the ending hooks the voice
/// already has. Nothing here walks the map.
/// </para>
///
/// <para>
/// <b>Bored only when idle.</b> Boredom holds while a bot works and falls only when it is paid, so a miner two minutes into a
/// seam still carries the boredom of the wait before it. A bot at a seam is not bored; it is waiting to be paid. The tag is
/// given only to a bot with nothing in hand or on a guess (a prowl), which is also what keeps the answers to it — a company's
/// call — from taking a bot off paying work.
/// </para>
/// </summary>
public static class BotMoods
{
    public const string Hurt = "hurt";

    public const string Tired = "tired";

    public const string Grieving = "grieving";

    public const string Poor = "poor";

    public const string Lonely = "lonely";

    public const string Bored = "bored";

    public const string Proud = "proud";

    public const string Content = "content";

    public const string Middling = "middling";

    public const string Glum = "glum";

    /// <summary>Need (how short the purse is of what the bot's own plans want) at or above which a bot says it is poor, besides the hunter's gold line.</summary>
    public static double PoorNeed { get; set; } = 0.5;

    /// <summary>Boredom at or above which a bot with no company and no guild (or nothing at all to do) says it is lonely.</summary>
    public static double LonelyFrom { get; set; } = 0.3;

    /// <summary>How long after a death — its own, or one in its guild — a bot grieves. Twenty minutes.</summary>
    public static int GrieveMs { get; set; } = 1200000;

    /// <summary>How long after finishing something that paid well a bot is proud of it. Ten minutes.</summary>
    public static int ProudMs { get; set; } = 600000;

    /// <summary>What a finished piece of work must have paid a minute to be worth being proud of.</summary>
    public static double ProudPerMinute { get; set; } = 40.0;

    private static readonly Dictionary<Serial, long> _fell = [];

    private static readonly Dictionary<string, long> _guildFell = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<Serial, long> _won = [];

    private static readonly List<Serial> _stale = [];

    private static long _sweptTick;

    private static bool _swept;

    /// <summary>A bot died. Its own grief, and its guild's. From <see cref="BotVoice.Died"/>.</summary>
    public static void Fell(BotMobile who)
    {
        if (who == null)
        {
            return;
        }

        var now = Core.TickCount;

        _fell[who.Serial] = now;

        if (who.Guild is Guild guild && guild.Name != null)
        {
            _guildFell[guild.Name] = now;
        }
    }

    /// <summary>A bot finished something. Pride, when it paid. From <see cref="BotVoice.Ended"/>.</summary>
    public static void Won(Mobile who, double perMinute)
    {
        if (who != null && perMinute >= ProudPerMinute)
        {
            _won[who.Serial] = Core.TickCount;
        }
    }

    /// <summary>Whether this tag is one of the seven that ask for something.</summary>
    public static bool Asks(string tag) => tag is Hurt or Tired or Grieving or Poor or Lonely or Bored or Proud;

    /// <summary>The bot's mood now: the tag, and the occasion the voice says it under.</summary>
    public static (string Tag, string Occasion) Read(BotMobile who, long now)
    {
        if (who == null)
        {
            return (Content, "mood:high");
        }

        // A bot in a fight cries rather than complains; see BotVoice.Struck.
        if (BotMend.Wants(who) && !BotMend.Embattled(who))
        {
            return (Hurt, "mood:hurt");
        }

        if (who.Tired || BotCamp.Weariness(who) >= BotCamp.WearyFrom)
        {
            return (Tired, "mood:tired");
        }

        if (Grieves(who, now))
        {
            return (Grieving, "mood:grieving");
        }

        var urges = who.Resolve?.Urges;

        if (BotYield.Spendable(who) < BotHunter.PoorBelow || (urges?.Need ?? 0.0) >= PoorNeed)
        {
            return (Poor, "mood:poor");
        }

        var deed = who.Resolve?.Deed;
        var idle = deed == null || deed.Guess;

        if (urges != null && who.Squad == null && (who.Guild == null || urges.IsBarren) && urges.Boredom >= LonelyFrom && idle)
        {
            return (Lonely, "mood:lonely");
        }

        if (urges is { IsRestless: true } && idle)
        {
            return (Bored, "mood:bored");
        }

        if (_won.TryGetValue(who.Serial, out var won) && now - won < ProudMs)
        {
            return (Proud, "mood:proud");
        }

        var mood = who.Mood;

        return mood >= 0.66 ? (Content, "mood:high") : mood >= 0.33 ? (Middling, "mood:mid") : (Glum, "mood:low");
    }

    private static bool Grieves(BotMobile who, long now)
    {
        if (_fell.TryGetValue(who.Serial, out var own) && now - own < GrieveMs)
        {
            return true;
        }

        return who.Guild is Guild guild && guild.Name != null && _guildFell.TryGetValue(guild.Name, out var lost) && now - lost < GrieveMs;
    }

    /// <summary>Records long run out are forgotten once a minute, so the two dictionaries stay the size of the living.</summary>
    public static void Sweep(long now)
    {
        if (_swept && now - _sweptTick < 60000)
        {
            return;
        }

        _swept = true;
        _sweptTick = now;
        _stale.Clear();

        foreach (var (serial, when) in _fell)
        {
            if (now - when >= GrieveMs)
            {
                _stale.Add(serial);
            }
        }

        for (var i = 0; i < _stale.Count; i++)
        {
            _fell.Remove(_stale[i]);
        }

        _stale.Clear();

        foreach (var (serial, when) in _won)
        {
            if (now - when >= ProudMs)
            {
                _stale.Add(serial);
            }
        }

        for (var i = 0; i < _stale.Count; i++)
        {
            _won.Remove(_stale[i]);
        }
    }

    public static void Forget()
    {
        _fell.Clear();
        _guildFell.Clear();
        _won.Clear();
        _stale.Clear();
    }
}
