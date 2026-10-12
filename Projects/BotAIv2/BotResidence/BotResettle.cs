using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Moving house: the walk to the town a bot is going to live in, and the moment it lives there.
///
/// <para>
/// <b>The road is the traveller's own (<see cref="BotTravel"/>), held inside rather than copied.</b> The walk is drawn a leg
/// at a time over the graph, through a cave mouth when one is on the way; arriving walks the road in the book and tells the
/// guild what it read on the danger map, exactly as a journey for its own sake does. So a move is also a journey in the
/// travel line's counts, and the traveller's clock is stamped, so the bot is not offered a sightseeing trip the moment it
/// has unpacked.
/// </para>
///
/// <para>
/// <b>It lives there from the moment it sets out</b> (<see cref="Taken"/>), not from the moment it arrives. A bot that dies
/// on the road rises in its new town rather than walking the road twice; one that gives up on the road has its walk home
/// lead there. And the offer itself changes nothing: a bot that never takes the move lives where it did (DECISIONS C9).
/// </para>
///
/// <para>
/// <b>Unpaid, and priced like the walk home.</b> It is worth <see cref="Prior"/> a minute — above a journey's eight and the
/// walk home's five, below any trade — so it is what a bot does when nothing else is worth doing; and like the walk home it
/// declares the walk as the work, since the auction weighs an offer by the share of its time spent working and a move is
/// nothing but the walk.
/// </para>
/// </summary>
public sealed class BotResettle : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotResettle));

    public const string Trade = "resettle";

    /// <summary>What moving house is worth a minute. Ten: above a journey for its own sake (eight) and the walk home (five), below any trade.</summary>
    public static double Prior { get; set; } = 10.0;

    /// <summary>Moves set out on, and how they ended.</summary>
    public static long Began { get; private set; }

    public static long Arrived { get; private set; }

    public static long Unreached { get; private set; }

    public static long BySettling { get; private set; }

    public static long ByPoor { get; private set; }

    public static long ByDeaths { get; private set; }

    private readonly BotTravel _road;

    private readonly BotTowns.Town _town;

    private readonly string _from;

    private readonly string _why;

    private readonly BotRelocate.Cause _cause;

    private Point3D _at;

    public BotResettle(Map map, BotTowns.Town town, string from, Point3D at, BotRelocate.Cause cause, string why)
    {
        _road = new BotTravel(map, town, from);
        _town = town;
        _from = from;
        _at = at;
        _cause = cause;
        _why = why;
    }

    public override string Kind => Trade;

    public override Map Map => _road.Map;

    public override Point3D Where => _road.Where;

    public override double Expects => Prior;

    public override bool Unpaid => true;

    public override bool Steadfast => true;

    public override SkillName? Trains => null;

    /// <summary>The walk, honestly, as the walk home declares it. See the class.</summary>
    public override double Minutes =>
        Math.Max(0.2, Math.Sqrt(Math.Pow(Where.X - _at.X, 2) + Math.Pow(Where.Y - _at.Y, 2)) * BotWalk.StepDelayMs(BotMobile.Runs) / 60000.0);

    public override string Stage => $"moving house from {_from} to {_town?.Name}: {_why}";

    public override void Taken(IBotWilful bot)
    {
        _road.Taken(bot);

        if (bot?.Self is not BotMobile body || _town == null)
        {
            return;
        }

        var rec = BotResidence.Adopt(body);
        var left = BotResidence.TownOf(rec)?.Name;

        rec = BotResidence.File(body, _town);
        rec.Pending = null;
        rec.Left = left;
        rec.Moves++;
        rec.Deaths.Clear();

        // Its living is judged afresh in the new town, from the next look.
        rec.Watched = false;

        Began++;

        switch (_cause)
        {
            case BotRelocate.Cause.Settling:
                {
                    BySettling++;

                    break;
                }
            case BotRelocate.Cause.Poor:
                {
                    ByPoor++;

                    break;
                }
            default:
                {
                    ByDeaths++;

                    break;
                }
        }

        logger.Information(
            "{Name} the {Class} is moving house from {From} to {Town}: {Why}; {Residents} live there now",
            body.Name,
            body.Class?.Name,
            left ?? _from,
            _town.Name,
            _why,
            BotResidence.Residents(_town)
        );

        BotEvents.Post("residence", body, $"moving from {left ?? _from} to {_town.Name}: {_why}");
    }

    public override void Resumed(IBotWilful bot) => _road.Resumed(bot);

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is { } body)
        {
            _at = body.Location;
        }

        var doing = _road.Advance(bot);

        if (doing.Kind == BotDoingKind.Done)
        {
            Arrived++;
        }
        else if (doing.Kind == BotDoingKind.Failed)
        {
            Unreached++;
        }

        return doing;
    }

    public static string Describe() =>
        $"{Began} moves set out on ({BySettling} settling, {ByPoor} for a poor living, {ByDeaths} for deaths), {Arrived} arrived, {Unreached} gave up on the road";

    public static void Forget()
    {
        Began = 0;
        Arrived = 0;
        Unreached = 0;
        BySettling = 0;
        ByPoor = 0;
        ByDeaths = 0;
    }
}
