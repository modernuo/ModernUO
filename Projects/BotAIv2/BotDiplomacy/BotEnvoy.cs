using System;

namespace Server.BotAI.V2;

/// <summary>
/// Going to a meeting for one's guild: its envoy walking to the other guild's town or to its own town's hall, or its answerer
/// walking to where the envoy has come — and standing there until the word is given.
///
/// <para>
/// <b>Patrick's order of 29.09.2026, point 4: the envoy is any bot of the guild; a guild of one sends its founder.</b> So the
/// errand is offered to every member (<see cref="BotHerald"/>) and the first to take it holds it (<see cref="Taken"/>, never
/// at the offer — DECISIONS A7), renewing its hold on every beat; a member that dies, rests or is taken off it lets the
/// meeting look for another, and one that could not get there is not asked again (<see cref="BotParley.Let"/>).
/// </para>
///
/// <para>
/// <b>Honest endings, because this is counted in the finished share.</b> Finished when the word was given at the meeting it
/// went to; failed when the meeting lapsed, when the road beat it, or when somebody else went in its place. It is unpaid —
/// a guild's business, like the reeve's company — and steadfast, held against better-paid work for the walk and the meeting
/// (<see cref="HoldsFor"/>), put down for a flight or a rescue and taken up again after. It stands still on purpose once it
/// is there (<see cref="Still"/>), so the stall watch does not take it off a hearing.
/// </para>
/// </summary>
public sealed class BotEnvoy : BotDeed
{
    public const string Trade = "envoy";

    /// <summary>
    /// What going is worth a minute. A hundred and fifty: above an ordinary trade, below the reeve's three hundred and the
    /// calls that will not wait — a guild's word is its business, but not before a guildmate bleeding in front of it.
    /// </summary>
    public static double Prior { get; set; } = 150.0;

    public static long Began { get; private set; }

    public static long Hosts { get; private set; }

    public static long Arrived { get; private set; }

    public static long Finished { get; private set; }

    public static long Lapsed { get; private set; }

    public static long Unreached { get; private set; }

    public static long Replaced { get; private set; }

    private readonly BotHearing _m;

    private readonly bool _host;

    private readonly Point3D _from;

    private long _began;

    private bool _there;

    private bool _unreached;

    private BotRecall _recall;

    public BotEnvoy(BotHearing meeting, bool asHost, Point3D from)
    {
        _m = meeting;
        _host = asHost;
        _from = from;
        _began = Core.TickCount;
    }

    public BotHearing Meeting => _m;

    public override string Kind => Trade;

    public override Map Map => _m.Map;

    public override Point3D Where => _m.Place;

    public override double Expects => Prior;

    public override bool Unpaid => true;

    public override bool Steadfast => true;

    public override bool Still => _there;

    /// <summary>
    /// Once at the meeting, the walk is spent and a better-paid offer does not take it off the hearing — only what will not
    /// wait does (<c>BotWill</c>'s rule for committed work). On the road it is merely held (<see cref="HoldsFor"/>).
    /// </summary>
    public override bool Committed => _there;

    public override SkillName? Trains => null;

    /// <summary>The walk, as the walk home declares it, and five minutes of meeting.</summary>
    public override double Minutes =>
        5.0 + Math.Max(0.2, Math.Sqrt(Math.Pow(_m.Place.X - _from.X, 2) + Math.Pow(_m.Place.Y - _from.Y, 2)) * BotWalk.StepDelayMs(BotMobile.Runs) / 60000.0);

    /// <summary>Held for the walk and the meeting, whatever the ordinary cap says.</summary>
    public override double HoldsFor => Minutes + 10.0;

    public override string Stage =>
        _there
            ? $"{(_host ? "answering for" : "envoy of")} {(_host ? _m.Host : _m.Guest)} at {_m.Town?.Name ?? "the seat"}: {_m.Stage.ToString().ToLowerInvariant()}"
            : $"walking to {_m.Town?.Name ?? "the seat"} {(_host ? "to answer" : "as envoy")} over {BotDuke.Word(_m.Topic)}";

    public override void Taken(IBotWilful bot)
    {
        _began = Core.TickCount;

        if (bot?.Self is not BotMobile body)
        {
            return;
        }

        BotParley.Took(_m, body, _host);

        if (_host)
        {
            Hosts++;
        }
        else
        {
            Began++;
        }
    }

    public override void Resumed(IBotWilful bot) => _began = Core.TickCount;

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile body || !body.Alive)
        {
            return BotDoing.Failed("no body to go with");
        }

        if (_m.Stage == BotHearingStage.Over)
        {
            if (_m.Heard)
            {
                Finished++;

                return BotDoing.Done($"the word was given at {_m.Town?.Name ?? "the seat"}: {_m.Ending}");
            }

            Lapsed++;

            return BotDoing.Failed($"the meeting lapsed: {_m.Ending}");
        }

        if (_host ? !ReferenceEquals(_m.HostRep, body) : !ReferenceEquals(_m.Envoy, body))
        {
            Replaced++;

            return BotDoing.Failed(_host ? $"somebody else answers for {_m.Host}" : $"somebody else went for {_m.Guest}");
        }

        BotParley.Still(_m, body, _host);

        if (BotParley.There(_m, body))
        {
            if (!_there)
            {
                _there = true;
                Arrived++;
            }

            return BotDoing.Work(Stage);
        }

        _there = false;

        if (Core.TickCount - _began >= BotParley.MarchMs)
        {
            Unreached++;
            _unreached = true;

            return BotDoing.Failed($"could not reach {_m.Town?.Name ?? "the seat"} in {BotParley.MarchMs / 60000} minutes");
        }

        // Runes (29.09.2026): a mage holding a rune for the town recalls instead of walking.
        if (BotRecall.Instead(bot, _m.Map, _m.Place, BotParley.ArriveTiles, $"the meeting at {_m.Town?.Name}", ref _recall) is { } cast)
        {
            return cast;
        }

        return BotDoing.Walk(_m.Map, _m.Place, BotArrival.Within(Math.Max(1, BotParley.ArriveTiles - 1)), $"to the meeting at {_m.Town?.Name ?? "the seat"}");
    }

    /// <summary>The road is refused: this bot is not asked to this meeting again, and the meeting may find another.</summary>
    public override bool Bend(IBotWilful bot)
    {
        _unreached = true;
        Unreached++;

        return false;
    }

    public override void Drop(IBotWilful bot)
    {
        if (bot?.Self is BotMobile body)
        {
            BotParley.Let(_m, body, _host, _unreached);
        }
    }

    public static string Describe() =>
        $"envoys: {Began} set out and {Hosts} went to answer, {Arrived} reached the meeting, {Finished} saw the word given, {Lapsed} saw it lapse, {Unreached} could not get there, {Replaced} were replaced";

    public static void Forget()
    {
        Began = 0;
        Hosts = 0;
        Arrived = 0;
        Finished = 0;
        Lapsed = 0;
        Unreached = 0;
        Replaced = 0;
    }
}
