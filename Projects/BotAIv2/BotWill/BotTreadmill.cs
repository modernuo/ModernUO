using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// The same deed against the same thing, failing the moment it is taken, is not offered to that bot again for a few seconds.
///
/// <para>
/// <b>Orlando's treadmill (07.10.2026, loops18).</b> Build 418, 11:47:34-11:47:42: Orlando the WarriorMage took "rescue: hitting
/// back at a troll" 39 times in eight seconds, five a second, and every one failed at once "a troll was winning", 0 coin. Since
/// react14 a fight that ends is decided again at the clock's next look (<c>BotWill.Settle</c> hurries it), so a deed that fails
/// on its first beat is offered and failed at the clock's pace. The breaker (<see cref="BotBreaker"/>) never rests a defence,
/// on Patrick's "it must hit back in a fight", and its six-in-five-minutes count is far too slow for a loop of ten a second.
/// The cause of that loop is mended where it starts (<c>BotDefender</c> read the standing flight share, the fight the bot's own,
/// leaned by its nature: a cautious bot between 40 and 44 % of its health was offered a fight that gave itself up on its first
/// beat). This is the rule under it, for every deed, so the next loop of this shape stops at its second turn.
/// </para>
///
/// <para>
/// <b>Two rules, keyed on the bot, the kind of work and its target</b> (what it fights when it fights something, the place
/// otherwise). (1) A fight that ends on a <em>losing judgement</em> ("X was winning": the bot's health under its own flight share)
/// rests that fight against that foe for <see cref="LosingRestMs"/> at once — fight what you beat, leave what you do not — and
/// for those seconds the flight is what the Hunted rung offers (<c>BotFugitive</c>, its early flight judged as on the failing
/// rung). The fight carried on to the end (<c>BotDefender.Stand</c>, which no losing judgement ends) is not rested by it: a bot
/// with no flight on offer runs or fights, never neither (react14). (2) Any deed that fails inside <see cref="InstantMs"/> of
/// being taken, having made nothing, twice inside <see cref="TwiceMs"/> against the same target, is rested for
/// <see cref="RestMs"/>. A flight never is: running away is never rested (<see cref="BotBreaker"/>), and a cornered flight
/// has its own rest (<c>BotFugitive.CorneredRestMs</c>).
/// </para>
///
/// <para>
/// <b>A rested offer is not a failure.</b> It is refused in the appraisal like the breaker's rests, so the hurried decision asks
/// the same question and cannot pass it; the work in hand is never refused. Everything is counted in the Will line.
/// </para>
/// </summary>
public static class BotTreadmill
{
    /// <summary>Whether the two rules run at all. Off, offers are made as before.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How long a fight is rested against the foe it was just judged to be losing to. Fifteen seconds.</summary>
    public static int LosingRestMs { get; set; } = 15000;

    /// <summary>A failure this soon after the deed was taken is an instant failure. Two seconds: one or two of the bot's turns.</summary>
    public static int InstantMs { get; set; } = 2000;

    /// <summary>How close together two instant failures against one target must fall to rest it.</summary>
    public static int TwiceMs { get; set; } = 2000;

    /// <summary>How long a deed is rested against a target after failing at once twice. Fifteen seconds.</summary>
    public static int RestMs { get; set; } = 15000;

    /// <summary>Fights rested against one foe after a losing judgement.</summary>
    public static long LosingRests { get; private set; }

    /// <summary>Deeds rested against one target after two instant failures inside <see cref="TwiceMs"/>.</summary>
    public static long InstantRests { get; private set; }

    /// <summary>Flights that failed at once twice and were not rested, because running away never is.</summary>
    public static long FlightsSpared { get; private set; }

    /// <summary>Offers refused while a rest stood, by the rule that rested them. Appraisals, not decisions.</summary>
    public static long RefusedLosing { get; private set; }

    public static long RefusedInstant { get; private set; }

    /// <summary>Fights carried on to the end let through a losing judgement's rest. See the class note.</summary>
    public static long StandsLetThrough { get; private set; }

    /// <summary>Flights offered to a bot as on the failing rung, because its fight had just judged itself lost. Counted by BotFugitive when taken.</summary>
    public static long FlightsAfterLosing { get; set; }

    private static readonly Dictionary<string, long> _restsByKind = new(StringComparer.Ordinal);

    private readonly struct Rest
    {
        public readonly long Until;

        public readonly bool Losing;

        public Rest(long until, bool losing)
        {
            Until = until;
            Losing = losing;
        }
    }

    private static readonly Dictionary<(Serial Bot, string Kind, long Target), Rest> _rests = [];

    private static readonly Dictionary<(Serial Bot, string Kind, long Target), long> _instants = [];

    /// <summary>The last losing judgement of each bot: when, and against whom.</summary>
    private static readonly Dictionary<Serial, (long Tick, Serial Foe)> _lost = [];

    private static readonly List<(Serial Bot, string Kind, long Target)> _stale = [];

    private static long _sweptTick;

    private static bool _swept;

    /// <summary>What a deed is aimed at: the serial of what it fights, or its place on its map.</summary>
    private static long Target(BotDeed deed)
    {
        if (deed.Foe is { } foe)
        {
            return (long)(uint)foe.Serial.Value;
        }

        var where = deed.Where;
        var map = deed.Map?.MapID ?? 0;

        // Bit 62 set: never equal to a serial. 16 bits a coordinate, 8 for z and 8 for the map.
        return (1L << 62) | ((long)(where.X & 0xFFFF) << 32) | ((long)(where.Y & 0xFFFF) << 16) | ((long)(where.Z & 0xFF) << 8) | (long)(map & 0xFF);
    }

    /// <summary>Whether a failure's reason is the fight's losing judgement. The sentence <c>BotSlay</c> writes when the bot's health is under its own flight share.</summary>
    public static bool LosingJudgement(string why) => why != null && why.EndsWith(" was winning", StringComparison.Ordinal);

    /// <summary>A deed has just failed. Called once by <c>BotWill.Settle</c>, with how long it was held and what it made.</summary>
    public static void Failed(Mobile bot, BotDeed deed, string why, long heldMs, int made, double skill)
    {
        if (!Running || bot == null || deed == null || string.IsNullOrEmpty(deed.Kind))
        {
            return;
        }

        var now = Core.TickCount;

        Sweep(now);

        var key = (bot.Serial, deed.Kind, Target(deed));

        if (deed.Foe is { } foe && LosingJudgement(why) && LosingRestMs > 0)
        {
            _rests[key] = new Rest(now + LosingRestMs, true);
            _lost[bot.Serial] = (now, foe.Serial);
            LosingRests++;
            Count(deed.Kind);

            return;
        }

        if (heldMs > InstantMs || made > 0 || skill > 0 || RestMs <= 0)
        {
            return;
        }

        if (_instants.TryGetValue(key, out var last) && now - last <= TwiceMs)
        {
            _instants.Remove(key);

            if (deed is BotBolt)
            {
                FlightsSpared++;

                return;
            }

            _rests[key] = new Rest(now + RestMs, false);
            InstantRests++;
            Count(deed.Kind);

            return;
        }

        _instants[key] = now;
    }

    private static void Count(string kind) => _restsByKind[kind] = _restsByKind.GetValueOrDefault(kind) + 1;

    /// <summary>Whether this offer is rested for this bot against its target, and for how much longer. Counted as a refusal.</summary>
    public static bool Resting(Mobile bot, BotDeed deed, out bool losing, out long leftMs)
    {
        losing = false;
        leftMs = 0;

        if (!Running || bot == null || deed == null || _rests.Count == 0 || string.IsNullOrEmpty(deed.Kind))
        {
            return false;
        }

        var key = (bot.Serial, deed.Kind, Target(deed));

        if (!_rests.TryGetValue(key, out var rest))
        {
            return false;
        }

        leftMs = rest.Until - Core.TickCount;

        if (leftMs <= 0)
        {
            _rests.Remove(key);
            leftMs = 0;

            return false;
        }

        losing = rest.Losing;

        // The fight to the end gives nothing up for health, so a losing judgement is no reason to refuse it. See the class note.
        if (losing && deed is BotRescue { Own: true, ToTheEnd: true })
        {
            StandsLetThrough++;

            return false;
        }

        if (losing)
        {
            RefusedLosing++;
        }
        else
        {
            RefusedInstant++;
        }

        return true;
    }

    /// <summary>Whether this bot's own fight judged itself lost inside <see cref="LosingRestMs"/>. Asked by <c>BotFugitive</c>'s early flight.</summary>
    public static bool Losing(Mobile bot)
    {
        if (!Running || bot == null || LosingRestMs <= 0 || !_lost.TryGetValue(bot.Serial, out var lost))
        {
            return false;
        }

        if (Core.TickCount - lost.Tick < LosingRestMs)
        {
            return true;
        }

        _lost.Remove(bot.Serial);

        return false;
    }

    /// <summary>A bot that is gone takes its rests with it.</summary>
    public static void Forget(Mobile bot)
    {
        if (bot == null)
        {
            return;
        }

        _lost.Remove(bot.Serial);

        foreach (var key in _rests.Keys)
        {
            if (key.Bot == bot.Serial)
            {
                _stale.Add(key);
            }
        }

        foreach (var key in _instants.Keys)
        {
            if (key.Bot == bot.Serial)
            {
                _stale.Add(key);
            }
        }

        for (var i = 0; i < _stale.Count; i++)
        {
            _rests.Remove(_stale[i]);
            _instants.Remove(_stale[i]);
        }

        _stale.Clear();
    }

    /// <summary>Lapsed rests and lone instant failures are dropped, at most once a minute.</summary>
    private static void Sweep(long now)
    {
        if (_swept && now - _sweptTick < 60000)
        {
            return;
        }

        _swept = true;
        _sweptTick = now;

        foreach (var (key, rest) in _rests)
        {
            if (rest.Until - now <= 0)
            {
                _stale.Add(key);
            }
        }

        for (var i = 0; i < _stale.Count; i++)
        {
            _rests.Remove(_stale[i]);
        }

        _stale.Clear();

        foreach (var (key, tick) in _instants)
        {
            if (now - tick > TwiceMs)
            {
                _stale.Add(key);
            }
        }

        for (var i = 0; i < _stale.Count; i++)
        {
            _instants.Remove(_stale[i]);
        }

        _stale.Clear();

        if (_lost.Count > 1024)
        {
            _lost.Clear();
        }
    }

    /// <summary>Whether anything has happened worth a clause in the Will line.</summary>
    public static bool Any => LosingRests + InstantRests + FlightsSpared > 0;

    /// <summary>The clause, for the Will line.</summary>
    public static string Describe()
    {
        var kinds = "";

        foreach (var (kind, n) in _restsByKind)
        {
            kinds += (kinds.Length == 0 ? "" : ", ") + $"{kind} {n}";
        }

        return $"{LosingRests} fights rested {LosingRestMs / 1000}s against the foe after a losing judgement (\"was winning\") and {InstantRests} deeds rested {RestMs / 1000}s against their target after failing within {InstantMs / 1000}s of being taken twice in {TwiceMs / 1000}s (by kind: {(kinds.Length == 0 ? "none" : kinds)}), "
            + $"{RefusedLosing} and {RefusedInstant} offers refused while those rests stood, {StandsLetThrough} fights to the end let through a losing judgement's rest, {FlightsAfterLosing} flights taken as on the failing rung after a losing judgement, {FlightsSpared} flights that failed at once twice and were not rested";
    }
}
