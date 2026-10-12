using System;
using System.Collections.Generic;
using Server.Items;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a bored bot with a house of its own the walk home, to pass some time there. See <see cref="BotRepose"/> and
/// <see cref="BotAbode"/>.
///
/// <para>
/// Bored is the population's own word for it — <c>BotUrges.Boredom</c>, the half of <c>BotMobile.Mood</c> that grows when
/// nothing comes of the day — and home is only offered when it is within <see cref="Reach"/> and the last visit was more
/// than <see cref="AgainMs"/> ago, so a house is somewhere a bot goes back to, not somewhere it lives instead of working.
/// </para>
/// </summary>
public sealed class BotReposer : IBotProposer
{
    /// <summary>How bored a bot must be before home is worth the walk.</summary>
    public static double Bored { get; set; } = 0.4;

    /// <summary>How far from home a bot may be and still be offered the walk.</summary>
    public static int Reach { get; set; } = 400;

    /// <summary>How long after one visit home the next may be offered. An hour.</summary>
    public static int AgainMs { get; set; } = 3600000;

    public static long Asked { get; private set; }

    public static long Offered { get; private set; }

    public static long Content { get; private set; }

    public static long Far { get; private set; }

    public static long Lately { get; private set; }

    private static readonly Dictionary<Serial, long> _last = [];

    public string Name => "home";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>A visit home was taken: the next one waits <see cref="AgainMs"/>.</summary>
    public static void Went(Mobile bot)
    {
        if (bot != null)
        {
            _last[bot.Serial] = Core.TickCount;
        }
    }

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotAbode.Running || bot?.Self is not BotMobile { Deleted: false, Alive: true } body || body.Map == null
            || body.Map == Map.Internal)
        {
            return null;
        }

        var house = BotAbode.Of(body);

        if (house == null || house.Map != body.Map)
        {
            return null;
        }

        Asked++;

        if ((body.Resolve?.Urges?.Boredom ?? 0.0) < Bored)
        {
            Content++;

            return null;
        }

        if (_last.TryGetValue(body.Serial, out var last) && Core.TickCount - last < AgainMs)
        {
            Lately++;

            return null;
        }

        if (!body.InRange(house.BanLocation, Reach))
        {
            Far++;

            return null;
        }

        Offered++;

        return new BotRepose(house);
    }

    public static string Describe() =>
        Asked == 0
            ? "no bot with a house of its own has been asked about going home"
            : $"{Asked} asks of bots with a house: {Offered} offered the walk home, {Content} not bored enough, {Far} too far, {Lately} home too lately; {BotRepose.Describe()}";

    public static void Forget()
    {
        Asked = 0;
        Offered = 0;
        Content = 0;
        Far = 0;
        Lately = 0;
        _last.Clear();
        BotRepose.Forget();
    }
}

/// <summary>
/// A bot at home: walks in, puts its spare armour away in the chest, and spends a few minutes there while its boredom
/// falls — its mood rises (<c>BotMobile.Mood</c>). See <see cref="BotAbode"/>.
///
/// <para>
/// <b>What goes in the chest is what the bot owns and does not wear</b>: armour, shields and clothes in the pack that are not
/// bound to it (the binding is its kit for life, <c>BotBinding</c>). Weapons stay — a pickaxe and a hatchet are weapons to
/// the engine and tools to a bot — and so does everything a bot works or heals with. Put straight into the chest, not dropped
/// on it: the engine refuses a lockdown whose item is still inside a pack (<c>BaseHouse.LockDown</c>).
/// </para>
/// </summary>
public sealed class BotRepose : BotDeed
{
    public const string Trade = "home";

    /// <summary>What an evening at home is reckoned at per minute. Leisure is not pay; it is set to be chosen by a bored bot.</summary>
    public static double Prior { get; set; } = 150.0;

    /// <summary>How long a bot stays once it is in.</summary>
    public static int StayMs { get; set; } = 480000;

    /// <summary>How much every minute at home is worth against boredom, in the urges' own coin (<c>BotUrges.Paid</c>).</summary>
    public static double Cheer { get; set; } = 100.0;

    /// <summary>The most things put away in one visit.</summary>
    public static int MostStowed { get; set; } = 10;

    public static long Arrived { get; private set; }

    public static long Stayed { get; private set; }

    /// <summary>Tired bots sent home to rest there, and those that got there.</summary>
    public static long SentToRest { get; private set; }

    public static long RestedAtHome { get; private set; }

    /// <summary>How long a bot that could not get home is left to rest where it stands before it is sent home again.</summary>
    public static int RestRetryMs { get; set; } = 600000;

    private static readonly Dictionary<Serial, long> _sentHome = [];

    /// <summary>
    /// A tired bot with a house of its own is walked home before it leaves the world, so it leaves — and comes back — under its
    /// own roof. True when a walk home was pressed; false when it is home already, has no house in reach, or was sent lately
    /// and did not get there (it then rests where it stands). From <c>BotRest</c>, when a tired bot is free.
    /// </summary>
    public static bool HomeToRest(BotMobile bot)
    {
        if (!BotAbode.Running || bot == null || BotAbode.Of(bot) is not { } house || house.Map != bot.Map)
        {
            return false;
        }

        if (house.IsInside(bot) || bot.InRange(house.BanLocation, 2))
        {
            if (_sentHome.Remove(bot.Serial))
            {
                RestedAtHome++;
            }

            return false;
        }

        if (!bot.InRange(house.BanLocation, BotReposer.Reach)
            || _sentHome.TryGetValue(bot.Serial, out var sent) && Core.TickCount - sent < RestRetryMs)
        {
            return false;
        }

        if (!BotWill.Press(bot, new BotRepose(house, true), "tired, and going home to rest"))
        {
            return false;
        }

        _sentHome[bot.Serial] = Core.TickCount;
        SentToRest++;

        return true;
    }

    private readonly bool _resting;

    private readonly BaseHouse _house;

    private Point3D _spot;

    private bool _in;

    private long _inTick;

    private long _cheeredTick;

    public BotRepose(BaseHouse house) : this(house, false)
    {
    }

    /// <param name="house">The bot's own.</param>
    /// <param name="resting">Walked home to leave the world from there: done on arrival, no stay.</param>
    public BotRepose(BaseHouse house, bool resting)
    {
        _house = house;
        _resting = resting;
    }

    public override string Kind => Trade;

    public override Map Map => _house?.Map;

    public override Point3D Where => _house?.BanLocation ?? Point3D.Zero;

    public override double Expects => Prior;

    public override double Minutes => StayMs / 60000.0;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override bool Unpaid => true;

    public override string Stage => _in ? "at home, resting" : _resting ? "on the way home to rest" : "on the way home";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _house is not { Deleted: false })
        {
            return BotDoing.Failed("the house is gone");
        }

        if (_spot == Point3D.Zero)
        {
            _spot = BotFittings.Spot(_house);

            if (_spot == Point3D.Zero)
            {
                _spot = _house.BanLocation;
            }
        }

        var now = Core.TickCount;

        if (!_in)
        {
            if (!body.InRange(_spot, 1))
            {
                return BotDoing.Walk(_house.Map, _spot, BotArrival.Within(1), "home");
            }

            // Home to rest: in at the door, and the rest itself is BotRest's, which takes it out of the world from here.
            if (_resting)
            {
                _house.RefreshDecay();

                return BotDoing.Done("home, to rest");
            }

            _in = true;
            _inTick = now;
            _cheeredTick = now;
            Arrived++;
            BotAbode.Visited();
            BotReposer.Went(body);
            _house.RefreshDecay();

            var stowed = Stow(body);

            BotAbode.Stow(stowed);
        }

        // Every minute in, the day's boredom falls; the mood follows (BotMobile.Mood).
        if (now - _cheeredTick >= 60000)
        {
            _cheeredTick = now;

            if (body is BotMobile { Resolve.Urges: { } urges })
            {
                urges.Paid(Cheer);
            }
        }

        if (now - _inTick >= StayMs)
        {
            Stayed++;

            return BotDoing.Done($"spent {StayMs / 60000} minutes at home, content at {(body as BotMobile)?.Mood ?? 0.0:P0}");
        }

        return BotDoing.Work("at home, resting");
    }

    /// <summary>Puts away what the bot owns and does not wear. Returns how many things.</summary>
    private static int Stow(Mobile body)
    {
        var chest = BotAbode.Chest(BotAbode.Of(body));
        var pack = body.Backpack;

        if (chest == null || pack == null)
        {
            return 0;
        }

        var bond = (body as BotMobile)?.Bond;
        List<Item> spare = null;
        var items = pack.Items;

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is { Deleted: false } item && item is BaseArmor or BaseClothing
                && !BotBinding.IsBound(item, bond))
            {
                (spare ??= []).Add(item);
            }
        }

        if (spare == null)
        {
            return 0;
        }

        var put = 0;

        for (var i = 0; i < spare.Count && put < MostStowed; i++)
        {
            chest.DropItem(spare[i]);
            put++;
        }

        return put;
    }

    public static string Describe() =>
        Arrived + SentToRest == 0
            ? "nobody has gone home yet"
            : $"{Arrived} came home and {Stayed} stayed the whole while; {SentToRest} tired bots sent home to rest, {RestedAtHome} left the world from there";

    public static void Forget()
    {
        Arrived = 0;
        Stayed = 0;
        SentToRest = 0;
        RestedAtHome = 0;
        _sentHome.Clear();
    }
}
