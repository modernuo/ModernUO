using System;
using System.Collections.Generic;
using System.Text.Json;
using Server.Items;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// <b>What the bots earn, for the page (29.09.2026, build 327; Patrick: "add money to the web dashboard — how much the bots
/// earn; leave the special classes out").</b>
///
/// <para>
/// Two numbers, because they answer different questions. <b>Earned</b> is the coin every settled undertaking brought into
/// the pack (<see cref="BotTakings.Coin"/>, the "N coin" of each ending line), summed over the last hour: what work pays.
/// <b>Net</b> is how the population's purses and bank balances together moved over the same hour, sampled once a minute:
/// what work pays less what the bots spend on supplies, repairs, inns and ships. A shard where the first is healthy and the
/// second is negative is one where the shopkeepers take everything.
/// </para>
///
/// <para>
/// The special classes are left out (<see cref="Excluded"/>): the Baron, the Captain, the Architect and the Sage hold posts
/// — stipends, a till, the drill field, houses — and one of them moves more coin in an afternoon than a class of twelve,
/// which would make the page about them.
/// </para>
/// </summary>
public static class BotEarnings
{
    /// <summary>Classes whose coin is not counted. See the class note.</summary>
    public static string[] Excluded { get; set; } = ["Baron", "Captain", "Architect", "Sage"];

    /// <summary>The window the rates are taken over.</summary>
    public static int WindowMs { get; set; } = 3600000;

    /// <summary>How often purses and banks are sampled.</summary>
    public static int SampleMs { get; set; } = 60000;

    private static readonly Queue<(long Tick, string Name, string Class, int Coin, int Made)> _recent = new();

    private static readonly Queue<(long Tick, long Wealth, int Bots, Dictionary<string, (long Wealth, int Bots)> ByClass)> _samples = new();

    private static readonly Dictionary<string, (long Coin, long Made, long Endings)> _sinceBoot = [];

    private static long _firstTick;

    /// <summary>Whether anything has been counted yet; a tick is never its own sentinel (CLAUDE.md rule 20).</summary>
    private static bool _started;

    private static long _sampledTick;

    private static bool _sampledEver;

    public static bool Counts(string cls) => cls != null && Array.IndexOf(Excluded, cls) < 0;

    /// <summary>A settled undertaking's takings. From <c>BotWill.Settle</c>.</summary>
    public static void Note(Mobile body, int coin, int made)
    {
        if (body is not BotMobile bot || !Counts(bot.Class?.Name))
        {
            return;
        }

        var now = Core.TickCount;

        if (!_started)
        {
            _started = true;
            _firstTick = now;
        }

        var cls = bot.Class.Name;

        _recent.Enqueue((now, bot.Name, cls, coin, made));
        _sinceBoot.TryGetValue(cls, out var had);
        _sinceBoot[cls] = (had.Coin + Math.Max(0, coin), had.Made + made, had.Endings + 1);

        Trim(now);
    }

    private static void Trim(long now)
    {
        while (_recent.Count > 0 && now - _recent.Peek().Tick > WindowMs)
        {
            _recent.Dequeue();
        }

        while (_samples.Count > 1 && now - _samples.Peek().Tick > WindowMs)
        {
            _samples.Dequeue();
        }
    }

    private static long WealthOf(BotMobile bot) => (bot.Backpack?.GetAmount(typeof(Gold)) ?? 0) + Banker.GetBalance(bot);

    /// <summary>Purses and banks, once a minute at most. From the page's snapshot, on the loop.</summary>
    public static void Sample()
    {
        var now = Core.TickCount;

        if (_sampledEver && now - _sampledTick < SampleMs)
        {
            return;
        }

        _sampledEver = true;
        _sampledTick = now;

        if (!_started)
        {
            _started = true;
            _firstTick = now;
        }

        var byClass = new Dictionary<string, (long Wealth, int Bots)>();
        long wealth = 0;
        var count = 0;

        void Add(IReadOnlyList<BotMobile> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var bot = list[i];

                if (bot is not { Deleted: false } || !Counts(bot.Class?.Name))
                {
                    continue;
                }

                var w = WealthOf(bot);

                wealth += w;
                count++;
                byClass.TryGetValue(bot.Class.Name, out var had);
                byClass[bot.Class.Name] = (had.Wealth + w, had.Bots + 1);
            }
        }

        Add(BotPopulation.Bots);
        Add(BotPopulation.Away);

        _samples.Enqueue((now, wealth, count, byClass));
        Trim(now);
    }

    /// <summary>The "money" object of the page's snapshot.</summary>
    public static void Write(Utf8JsonWriter w)
    {
        Sample();

        var now = Core.TickCount;

        Trim(now);

        // The rate is over what has been watched: an hour, or less since the boot.
        var watchedMs = !_started ? 0 : Math.Min(WindowMs, now - _firstTick);
        var hours = Math.Max(watchedMs, 60000) / 3600000.0;

        var coin = new Dictionary<string, long>();
        var spent = new Dictionary<string, long>();
        var made = new Dictionary<string, long>();
        var byBot = new Dictionary<string, (string Class, long Coin)>();
        long coinAll = 0;
        long spentAll = 0;
        long madeAll = 0;

        // Coin in and coin out apart: an ending's coin is what the pack gained over the undertaking, and a restock's is the
        // price it paid. Earned is what came in; spent at work is what went out on the work itself.
        foreach (var (_, name, cls, c, m) in _recent)
        {
            if (c >= 0)
            {
                coin[cls] = (coin.TryGetValue(cls, out var x) ? x : 0) + c;
                coinAll += c;
                byBot.TryGetValue(name, out var had);
                byBot[name] = (cls, had.Coin + c);
            }
            else
            {
                spent[cls] = (spent.TryGetValue(cls, out var s) ? s : 0) - c;
                spentAll -= c;
            }

            made[cls] = (made.TryGetValue(cls, out var y) ? y : 0) + m;
            madeAll += m;
        }

        var last = _samples.Count > 0 ? _samples.ToArray()[^1] : default;
        var first = _samples.Count > 0 ? _samples.Peek() : default;
        var sampledHours = _samples.Count > 1 ? Math.Max(last.Tick - first.Tick, 60000) / 3600000.0 : 0.0;

        w.WritePropertyName("money");
        w.WriteStartObject();
        w.WriteNumber("windowMin", Math.Round(watchedMs / 60000.0, 1));
        w.WritePropertyName("excluded");
        w.WriteStartArray();

        foreach (var cls in Excluded)
        {
            w.WriteStringValue(cls);
        }

        w.WriteEndArray();
        w.WriteNumber("bots", last.Bots);
        w.WriteNumber("coinPerHour", Math.Round(coinAll / hours));
        w.WriteNumber("spentPerHour", Math.Round(spentAll / hours));
        w.WriteNumber("madePerHour", Math.Round(madeAll / hours));
        w.WriteNumber("perBotPerHour", last.Bots > 0 ? Math.Round(coinAll / hours / last.Bots, 1) : 0);
        w.WriteNumber("wealth", last.Wealth);
        w.WriteNumber("wealthPerBot", last.Bots > 0 ? Math.Round((double)last.Wealth / last.Bots) : 0);

        if (sampledHours > 0)
        {
            w.WriteNumber("netPerHour", Math.Round((last.Wealth - first.Wealth) / sampledHours));
        }
        else
        {
            w.WriteNull("netPerHour");
        }

        w.WritePropertyName("classes");
        w.WriteStartArray();

        var names = new SortedSet<string>(coin.Keys);

        if (last.ByClass != null)
        {
            names.UnionWith(last.ByClass.Keys);
        }

        foreach (var cls in names)
        {
            var bots = last.ByClass != null && last.ByClass.TryGetValue(cls, out var now1) ? now1 : default;
            var then = first.ByClass != null && first.ByClass.TryGetValue(cls, out var then1) ? then1 : default;
            var c = coin.TryGetValue(cls, out var cc) ? cc : 0;
            var sp = spent.TryGetValue(cls, out var ss) ? ss : 0;
            var m = made.TryGetValue(cls, out var mm) ? mm : 0;

            w.WriteStartObject();
            w.WriteString("cls", cls);
            w.WriteNumber("bots", bots.Bots);
            w.WriteNumber("coinPerHour", Math.Round(c / hours));
            w.WriteNumber("perBotPerHour", bots.Bots > 0 ? Math.Round(c / hours / bots.Bots, 1) : 0);
            w.WriteNumber("spentPerHour", Math.Round(sp / hours));
            w.WriteNumber("madePerHour", Math.Round(m / hours));
            w.WriteNumber("wealthPerBot", bots.Bots > 0 ? Math.Round((double)bots.Wealth / bots.Bots) : 0);

            if (sampledHours > 0 && then.Bots > 0 && bots.Bots > 0)
            {
                // Per bot, so a class that grew or shrank in the hour is not read as having got richer or poorer.
                w.WriteNumber("netPerBotPerHour", Math.Round(((double)bots.Wealth / bots.Bots - (double)then.Wealth / then.Bots) / sampledHours, 1));
            }
            else
            {
                w.WriteNull("netPerBotPerHour");
            }

            _sinceBoot.TryGetValue(cls, out var boot);
            w.WriteNumber("coinSinceBoot", boot.Coin);
            w.WriteEndObject();
        }

        w.WriteEndArray();

        var top = new List<(string Name, string Class, long Coin)>();

        foreach (var (name, (cls, c)) in byBot)
        {
            top.Add((name, cls, c));
        }

        top.Sort((a, b) => b.Coin.CompareTo(a.Coin));

        w.WritePropertyName("top");
        w.WriteStartArray();

        for (var i = 0; i < top.Count && i < 8; i++)
        {
            w.WriteStartObject();
            w.WriteString("name", top[i].Name);
            w.WriteString("cls", top[i].Class);
            w.WriteNumber("coin", top[i].Coin);
            w.WriteEndObject();
        }

        w.WriteEndArray();
        w.WriteEndObject();
    }

    public static void Forget()
    {
        _recent.Clear();
        _samples.Clear();
        _sinceBoot.Clear();
        _started = false;
        _sampledEver = false;
    }
}
