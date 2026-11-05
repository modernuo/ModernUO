using System;
using System.Collections.Generic;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// Two bots that met in the open, stopped, and are having a word: facing each other until the talk is over, then back to
/// whatever each was doing.
///
/// <para>
/// <b>Pressed, and the work in hand put down rather than dropped.</b> A meeting is not an offer weighed against a seam — it
/// is something that happens to two bots on the road — so both are pressed into it (<c>BotWill.Press</c>), which puts the
/// errand in hand down to be taken up again when the talk settles, exactly as a duel's call or a summons does. That is why
/// <see cref="BotMeetings"/> only stops bots whose work resumes: a hunt or a flight would be thrown away, and is not stopped.
/// </para>
/// </summary>
public sealed class BotChat : BotDeed
{
    public const string Trade = "chat";

    /// <summary>What a word on the road is worth: nothing much. It is pressed, never auctioned; this is only what the ledger reads.</summary>
    public static double Prior { get; set; } = 6.0;

    /// <summary>How far apart the two may drift before the talk is over. Five tiles.</summary>
    public static int Apart { get; set; } = 5;

    private readonly BotMeeting _meeting;

    private readonly BotMobile _other;

    private readonly Map _map;

    private readonly Point3D _where;

    private bool _stopped;

    public BotChat(BotMeeting meeting, BotMobile other)
    {
        _meeting = meeting;
        _other = other;
        _map = other?.Map;
        _where = other?.Location ?? Point3D.Zero;
    }

    public BotMeeting Meeting => _meeting;

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _where;

    public override double Expects => Prior;

    public override double Minutes => 0.5;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 1.0;

    public override int Made => 0;

    public override bool Unpaid => true;

    /// <summary>Half a minute: kept from the next review's bidding, which would otherwise end it for any errand at all.</summary>
    public override bool Committed => true;

    public override bool Still => true;

    public override string Stage => $"having a word with {_other?.Name}";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _meeting == null)
        {
            return BotDoing.Failed("nobody to talk to");
        }

        if (_meeting.Over)
        {
            return BotDoing.Done($"had a word with {_other?.Name}: {_meeting.Lines} lines");
        }

        if (_other is not { Deleted: false, Alive: true } || _other.Map != body.Map || !body.InRange(_other.Location, Apart))
        {
            BotMeetings.End(_meeting, $"{_other?.Name} walked on");

            return BotDoing.Done($"{_other?.Name} walked on");
        }

        // <b>A bot pressed mid-walk keeps walking unless the walk is finished.</b> Work leaves the journey alone, and the
        // errand put down left its road in it; the delve's hold learned the same (build 314). Once, on the first beat: the
        // errand taken up again afterwards asks for its own road afresh.
        if (!_stopped)
        {
            _stopped = true;
            (body as BotMobile)?.Journey.Finish();
        }

        var facing = body.GetDirectionTo(_other.Location);

        if ((body.Direction & Direction.Mask) != facing)
        {
            body.Direction = facing;
        }

        return BotDoing.Work("having a word");
    }

    public override void Drop(IBotWilful bot) => BotMeetings.End(_meeting, $"{bot?.Self?.Name} broke off");
}

/// <summary>One word on the road between two bots: who, how far it has got, and when the next line is due.</summary>
public sealed class BotMeeting
{
    public BotMeeting(BotMobile first, BotMobile second, long now)
    {
        First = first;
        Second = second;
        BeganTick = now;
        NextTick = now;
    }

    public BotMobile First { get; }

    public BotMobile Second { get; }

    public long BeganTick { get; }

    public long NextTick { get; internal set; }

    public int Step { get; internal set; }

    public int Lines { get; internal set; }

    public bool Over { get; internal set; }

    public string Why { get; internal set; }
}

/// <summary>
/// Chance meetings: two free bots passing within a few tiles in the open, and a small chance they stop and talk.
///
/// <para>
/// <b>Cheap by construction.</b> Each bot is looked at once every <see cref="EveryMs"/> — a slice of the population on each
/// of the camp's seconds, never the whole of it — and only a bot that passes every cheap test (free, its work resumable,
/// not in a company or a fight, in the open, not met lately) costs one spatial query of <see cref="Reach"/> tiles. A pair
/// that rolled no is not rolled again for <see cref="MissRestMs"/>, so two miners at one seam are not a meeting every
/// eight seconds.
/// </para>
/// </summary>
public static class BotMeetings
{
    /// <summary>Whether bots meet on the road at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How often each bot is looked at for a meeting. Eight seconds: a pass on the road lasts about that long.</summary>
    public static int EveryMs { get; set; } = 8000;

    /// <summary>How near two bots must pass. Three tiles: near enough to speak without raising the voice.</summary>
    public static int Reach { get; set; } = 3;

    /// <summary>The chance two that pass stop to talk. A fifth: "a small chance".</summary>
    public static double Chance { get; set; } = 0.2;

    /// <summary>How long a bot that has had a word goes before another. Fifteen minutes.</summary>
    public static int RestMs { get; set; } = 900000;

    /// <summary>How long before the same two stop to talk again. An hour.</summary>
    public static int PairRestMs { get; set; } = 3600000;

    /// <summary>How long before a pair that rolled no is rolled again. Two minutes.</summary>
    public static int MissRestMs { get; set; } = 120000;

    /// <summary>Whether bots stop to talk inside a town's walls too. Off: the order is "in the open", and a square full of bots would never stop talking.</summary>
    public static bool InTowns { get; set; }

    /// <summary>Between one line and the next. Three and a half seconds.</summary>
    public static int LineMs { get; set; } = 3500;

    /// <summary>The chance the talk goes past the greeting to a line of news. Seven in ten.</summary>
    public static double NewsChance { get; set; } = 0.7;

    /// <summary>The longest a word on the road lasts. Thirty seconds.</summary>
    public static int MostMs { get; set; } = 30000;

    public static long Looked { get; private set; }

    public static long Alone { get; private set; }

    public static long Passed { get; private set; }

    public static long Met { get; private set; }

    public static long Refused { get; private set; }

    public static long Finished { get; private set; }

    public static long BrokenOff { get; private set; }

    private static readonly List<BotMeeting> _meetings = [];

    private static readonly Dictionary<Serial, long> _rest = [];

    private static readonly Dictionary<(Serial, Serial), long> _pairs = [];

    private static readonly List<(Serial, Serial)> _stalePairs = [];

    private static readonly List<Serial> _staleRest = [];

    private static int _cursor;

    private static long _sweptTick;

    private static bool _swept;

    /// <summary>The camp's second: the talk on the road, then a slice of the population looked at for new meetings.</summary>
    public static void Tick(long now, int tickMs)
    {
        Talk(now);

        if (!Running || !BotCamp.Running)
        {
            return;
        }

        var bots = BotPopulation.Bots;
        var count = bots.Count;

        if (count == 0)
        {
            return;
        }

        var slice = Math.Clamp((int)Math.Ceiling(count * (double)Math.Max(1, tickMs) / Math.Max(1000, EveryMs)), 1, count);

        for (var k = 0; k < slice; k++)
        {
            if (_cursor >= count)
            {
                _cursor = 0;
            }

            if (bots[_cursor++] is { Deleted: false } bot)
            {
                Consider(bot, now);
            }
        }

        Sweep(now);
    }

    /// <summary>Whether this bot may be stopped for a word: free or at work that resumes, and none of the things that forbid it.</summary>
    private static bool Free(BotMobile bot, long now)
    {
        if (bot is not { Deleted: false, Alive: true, Tired: false, Hidden: false, Warmode: false } || bot.Map == null || bot.Map == Map.Internal)
        {
            return false;
        }

        if (bot.Combatant != null || bot.Squad != null || _rest.TryGetValue(bot.Serial, out var rested) && now - rested < RestMs)
        {
            return false;
        }

        var resolve = bot.Resolve;

        if (resolve == null || resolve.Paused != null || resolve.Standing is not (BotStanding.Free or BotStanding.Busy))
        {
            return false;
        }

        // Not a healer on its way to a patient, nor a patient waiting for one (build 321): on 320 Liesl and Sylvaris each put down
        // "mending Osric" to have a word with the other, and Osric waited for neither.
        if (resolve.Deed is { } deed
            && (!deed.Resumes || deed.Committed || deed.Foe != null || deed.Braves || deed is BotChat or BotFireside or BotKindle
                || deed is BotSalve or BotAwaitMend or BotAccompany))
        {
            return false;
        }

        if (BotDuel.Duelling(bot) || BotOutlaw.Jailed(bot) || BotDelveParty.Delving(bot) || BotDungeon.Under(bot.Location))
        {
            return false;
        }

        var region = Region.Find(bot.Location, bot.Map);

        return !region.IsPartOf<HouseRegion>() && (InTowns || !region.IsPartOf<GuardedRegion>());
    }

    private static void Consider(BotMobile bot, long now)
    {
        if (!Free(bot, now))
        {
            return;
        }

        Looked++;

        BotMobile other = null;

        foreach (var near in bot.Map.GetMobilesInRange<BotMobile>(bot.Location, Reach))
        {
            if (ReferenceEquals(near, bot) || !Free(near, now) || Resting(bot, near, now) || !BotCamp.Welcome(bot, near))
            {
                continue;
            }

            other = near;

            break;
        }

        if (other == null)
        {
            Alone++;

            return;
        }

        if (Utility.RandomDouble() >= Chance)
        {
            Passed++;
            _pairs[Pair(bot, other)] = now - PairRestMs + MissRestMs;

            return;
        }

        Start(bot, other, now);
    }

    private static void Start(BotMobile first, BotMobile second, long now)
    {
        var meeting = new BotMeeting(first, second, now);

        _rest[first.Serial] = now;
        _rest[second.Serial] = now;
        _pairs[Pair(first, second)] = now;

        if (!BotWill.Press(first, new BotChat(meeting, second), $"met {second.Name} on the way"))
        {
            Refused++;

            return;
        }

        if (!BotWill.Press(second, new BotChat(meeting, first), $"met {first.Name} on the way"))
        {
            // The first is already standing; its chat sees the meeting over on its next beat and takes its errand up again.
            Refused++;
            End(meeting, $"{second.Name} would not stop");

            return;
        }

        _meetings.Add(meeting);
        Met++;

        BotEvents.Post("meet", first, $"stopped to talk with {second.Name} at ({first.X}, {first.Y})");
    }

    /// <summary>The next line of every word on the road that is due one; a meeting whose talk is done is over.</summary>
    private static void Talk(long now)
    {
        for (var i = _meetings.Count - 1; i >= 0; i--)
        {
            var meeting = _meetings[i];

            if (!meeting.Over && now - meeting.BeganTick >= MostMs)
            {
                End(meeting, "the talk ran long");
            }

            if (meeting.Over)
            {
                _meetings.RemoveAt(i);

                continue;
            }

            if (now - meeting.NextTick < 0)
            {
                continue;
            }

            meeting.NextTick = now + Math.Max(1000, LineMs);

            var said = meeting.Step switch
            {
                0 => BotCampTalk.Say(meeting.First, "meet:greet", null, meeting.Second, null),
                1 => BotCampTalk.Say(meeting.Second, "meet:answer", null, meeting.First, null),
                2 when Utility.RandomDouble() < NewsChance => News(meeting),
                _ => false
            };

            if (said)
            {
                meeting.Lines++;
            }

            meeting.Step++;

            if (meeting.Step > 2)
            {
                Finished++;
                meeting.Over = true;
                meeting.Why = "had their word";
            }
        }
    }

    /// <summary>One of the two says what it knows: the same news a fire hears.</summary>
    private static bool News(BotMeeting meeting)
    {
        var (speaker, listener) = Utility.RandomBool() ? (meeting.First, meeting.Second) : (meeting.Second, meeting.First);
        var fill = new Dictionary<string, string>();
        var topic = BotCampTalk.Topic(speaker, speaker.Map, speaker.Location, fill, "meet:small", out var told, new[] { listener });

        if (!BotCampTalk.Say(speaker, topic, "meet:small", listener, fill))
        {
            return false;
        }

        // A tale told on the road is remembered by the one it was told to (PLAN-life A6).
        if (told != null)
        {
            BotBiography.Heard(listener, speaker, told, BotBiography.Place(speaker.Map, speaker.Location));
        }

        return true;
    }

    /// <summary>The word is over, however it ended; each side's chat ends on its own next beat.</summary>
    public static void End(BotMeeting meeting, string why)
    {
        if (meeting == null || meeting.Over)
        {
            return;
        }

        meeting.Over = true;
        meeting.Why = why;
        BrokenOff++;
    }

    private static bool Resting(Mobile a, Mobile b, long now) =>
        _pairs.TryGetValue(Pair(a, b), out var when) && now - when < PairRestMs;

    private static (Serial, Serial) Pair(Mobile a, Mobile b) =>
        a.Serial.Value <= b.Serial.Value ? (a.Serial, b.Serial) : (b.Serial, a.Serial);

    /// <summary>Clocks long run out are forgotten once a minute.</summary>
    private static void Sweep(long now)
    {
        if (_swept && now - _sweptTick < 60000)
        {
            return;
        }

        _swept = true;
        _sweptTick = now;
        _stalePairs.Clear();
        _staleRest.Clear();

        foreach (var (pair, when) in _pairs)
        {
            if (now - when >= PairRestMs)
            {
                _stalePairs.Add(pair);
            }
        }

        foreach (var (serial, when) in _rest)
        {
            if (now - when >= RestMs)
            {
                _staleRest.Add(serial);
            }
        }

        for (var i = 0; i < _stalePairs.Count; i++)
        {
            _pairs.Remove(_stalePairs[i]);
        }

        for (var i = 0; i < _staleRest.Count; i++)
        {
            _rest.Remove(_staleRest[i]);
        }
    }

    public static string Describe() =>
        !Running
            ? "bots do not stop to talk on the road"
            : $"{Looked} free bots looked round for company in the open, {Alone} with nobody within {Reach} tiles, {Passed} passed by ({Chance:P0} stop), "
              + $"{Met} stopped to talk ({Finished} had their word, {BrokenOff} broke off or ran long), {Refused} could not be stopped";

    public static void Forget()
    {
        _meetings.Clear();
        _rest.Clear();
        _pairs.Clear();
        _cursor = 0;
        Looked = 0;
        Alone = 0;
        Passed = 0;
        Met = 0;
        Refused = 0;
        Finished = 0;
        BrokenOff = 0;
    }
}
