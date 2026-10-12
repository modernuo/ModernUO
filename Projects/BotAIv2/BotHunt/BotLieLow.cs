using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A murderer getting away from the body and lying low: a dozen tiles off, hidden, not moving, for a few minutes.
///
/// <para>
/// <b>Patrick's order of 17.09.2026: criminals act unseen.</b> Until then a robber that had killed went straight back to
/// what it had put down, red and in plain sight beside the corpse, and the law-abiding set on it at once (build 111c). Now a
/// robber that can hide is pressed into this the moment the corpse is gone through (<see cref="BotInquest"/> presses it on
/// the next sweep). The body is left behind for somebody to find, the killer is <see cref="AwayTiles"/> off and hidden, and
/// what finds it is a search with Detect Hidden, a passer-by with that skill within four tiles, or its own next step.
/// </para>
/// </summary>
public sealed class BotLieLow : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotLieLow));

    public const string Trade = "lielow";

    /// <summary>How far from the body the killer goes before it hides.</summary>
    public static int AwayTiles { get; set; } = 12;

    /// <summary>How many failed tries at hiding before the killer gives it up.</summary>
    public static int HideTries { get; set; } = 4;

    /// <summary>How long it lies hidden and still. Three minutes.</summary>
    public static int StillMs { get; set; } = 180000;

    /// <summary>What lying low is held at against other work, a minute: the robbery's own, as <see cref="BotSkulk.Prior"/> is, and for the same reason.</summary>
    public static double Prior { get; set; } = 400.0;

    /// <summary>Killers pressed into lying low.</summary>
    public static long Begun { get; private set; }

    /// <summary>Lay low to the end unfound.</summary>
    public static long Unfound { get; private set; }

    /// <summary>Revealed while lying low.</summary>
    public static long FoundOut { get; private set; }

    /// <summary>Could not hide in <see cref="HideTries"/> tries.</summary>
    public static long Unhidden { get; private set; }

    private readonly Map _map;

    private readonly Point3D _body;

    private readonly Point3D _aim;

    private int _tries;

    private bool _hid;

    private long _hidTick;

    private bool _counted;

    /// <param name="map">Where.</param>
    /// <param name="body">Where the body lies.</param>
    /// <param name="from">Where the killer stands now; it goes on away from the body in that direction.</param>
    public BotLieLow(Map map, Point3D body, Point3D from)
    {
        _map = map;
        _body = body;

        var dx = from.X - body.X;
        var dy = from.Y - body.Y;

        if (dx == 0 && dy == 0)
        {
            dx = Utility.RandomMinMax(-1, 1);
            dy = dx == 0 ? Utility.RandomBool() ? 1 : -1 : Utility.RandomMinMax(-1, 1);
        }

        var length = Math.Sqrt(dx * dx + dy * dy);
        var x = body.X + (int)Math.Round(dx / length * AwayTiles);
        var y = body.Y + (int)Math.Round(dy / length * AwayTiles);

        _aim = new Point3D(x, y, map?.GetAverageZ(x, y) ?? body.Z);
    }

    public override string Kind => Trade;

    public override bool Committed => true;

    public override bool Unpaid => true;

    /// <summary>Hidden and still is what this looks like once it has hidden, and it ends on its own clock.</summary>
    public override bool Still => _hid;

    /// <summary>One lying low is enough: a second body found nearby would otherwise start the wait over.</summary>
    public override bool Repeats(BotDeed other) => other is BotLieLow;

    public override Map Map => _map;

    public override Point3D Where => _aim;

    public override double Expects => Prior;

    public override double Minutes => StillMs / 60000.0 + 1.0;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override int Made => 0;

    public override string Stage => _hid ? "lying low" : "getting away from the body";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || body.Map != _map)
        {
            return BotDoing.Failed("no body");
        }

        if (!_counted)
        {
            _counted = true;
            Begun++;
        }

        var now = Core.TickCount;

        if (!_hid)
        {
            if (!body.InRange(_aim, 2))
            {
                return BotDoing.Walk(_map, _aim, BotArrival.Within(2), "away from the body");
            }

            if (!BotShadow.Ready(body))
            {
                return BotDoing.Work("catching its breath");
            }

            if (BotShadow.Hide(body))
            {
                _hid = true;
                _hidTick = now;

                return BotDoing.Work("lying low");
            }

            if (++_tries >= HideTries)
            {
                Unhidden++;

                return BotDoing.Failed($"could not hide in {HideTries} tries");
            }

            return BotDoing.Work("trying to hide");
        }

        if (!body.Hidden)
        {
            FoundOut++;

            logger.Information(
                "{Name}, red, was found out lying low at ({X}, {Y}), {Seconds}s after it hid {Away} tiles from the body",
                body.Name,
                body.X,
                body.Y,
                (now - _hidTick) / 1000,
                (int)body.GetDistanceToSqrt(_body)
            );

            return BotDoing.Done("found out while lying low");
        }

        if (now - _hidTick >= StillMs)
        {
            Unfound++;
            body.RevealingAction();

            return BotDoing.Done($"lay low for {StillMs / 60000} minutes unfound");
        }

        return BotDoing.Work("lying low");
    }

    public static string Describe() =>
        $"{Begun} killers lay low: {Unfound} unfound to the end, {FoundOut} found out, {Unhidden} could not hide";

    public static void Forget()
    {
        Begun = 0;
        Unfound = 0;
        FoundOut = 0;
        Unhidden = 0;
    }
}
