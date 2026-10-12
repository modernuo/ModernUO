using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;
using Server.Targets;

namespace Server.BotAI.V2;

/// <summary>
/// Making camp: kindling in hand, a hearth picked on wild ground, the engine's flame struck on it, and a seat by it kept
/// until the evening is over.
///
/// <para>
/// <b>Kindling comes the engine's three ways, in the order a player in the woods would reach for them.</b> Out of the pack
/// first — bought at a provisioner for two gold a stick (<see cref="BotCamper"/> offers the trip when a fire-lighter in
/// town has none). Then made from logs, one log a stick, with a fletcher's kit (<c>DefBowFletching</c>, the recipe with no
/// skill to it). Then cut off a tree with a knife or a sword — the engine's own use of a blade on a trunk
/// (<c>BladedItemTarget</c>), five of the tree's wood for one stick. Nothing is conjured.
/// </para>
///
/// <para>
/// <b>Striking is the engine's roll and it is honest about a beginner.</b> <c>Kindling</c> rolls Camping between nought and
/// a hundred, so a bot that has never made camp fumbles for half a minute or so, gaining a little with every strike, before
/// the first flame; the strike gives up after <see cref="MostFumbles"/>. The fumbles are counted (<c>BotCamp.Fumbles</c>).
/// </para>
/// </summary>
public sealed class BotKindle : BotDeed
{
    public const string Trade = "camp";

    /// <summary>
    /// What making camp is worth a minute before tiredness and boredom are added (<c>BotCamp.Claim</c>). Eighteen: above a
    /// journey's eight and a shopping trip's twelve, below every trade, so a bot makes camp when it has nothing better to do
    /// or is tired and bored enough for the fire to outweigh the work.
    /// </summary>
    public static double Prior { get; set; } = 18.0;

    /// <summary>How many sticks a keeper wants before it lays a fire: a stick a minute and change for a six-minute evening.</summary>
    public static int KindlingWant { get; set; } = 5;

    /// <summary>How far round itself a keeper with a blade looks for a tree. Twelve tiles; further, and the camp would be a trip.</summary>
    public static int TreeReach { get; set; } = 12;

    /// <summary>Cuts at trees before the keeper lays the fire with whatever it has. Twelve.</summary>
    public static int MostCuts { get; set; } = 12;

    /// <summary>Kindling made from logs before the keeper lays the fire with whatever it has. Eight swings.</summary>
    public static int MostFletches { get; set; } = 8;

    /// <summary>A cut at a tree, a second and a bit, as a player swings.</summary>
    public static int CutMs { get; set; } = 1200;

    /// <summary>A swing at the fletcher's kit; the stick arrives on the craft system's timer, and is counted on a later beat.</summary>
    public static int FletchMs { get; set; } = 2500;

    /// <summary>How long gathering may take before the keeper lays the fire with what it has. Two minutes.</summary>
    public static int GatherMs { get; set; } = 120000;

    /// <summary>A strike at the kindling, a second and a half.</summary>
    public static int StrikeMs { get; set; } = 1500;

    /// <summary>
    /// Strikes before the keeper gives up. Sixty: from nought Camping the engine's roll catches within twenty-five strikes
    /// half the time and within sixty ninety-nine times in a hundred, since every strike under ten points gains.
    /// </summary>
    public static int MostFumbles { get; set; } = 60;

    /// <summary>How far from where it stands a keeper looks for a hearth when its own tile will not do.</summary>
    public static int PickReach { get; set; } = 6;

    /// <summary>How many other tiles it tries.</summary>
    public static int PickTries { get; set; } = 8;

    /// <summary>The fewest seats round a hearth for it to be one: the keeper and two guests.</summary>
    public static int LeastSeats { get; set; } = 3;

    public static long Camps { get; private set; }

    public static long Cut { get; private set; }

    public static long Fletched { get; private set; }

    public static long NoKindling { get; private set; }

    public static long NoHearth { get; private set; }

    public static long NoCatch { get; private set; }

    public static long Kept { get; private set; }

    public static long SleptBy { get; private set; }

    private const int Gathering = 0;

    private const int Picking = 1;

    private const int Striking = 2;

    private const int Sitting = 3;

    private readonly Map _map;

    private readonly Point3D _from;

    private readonly double _claim;

    private readonly bool _sleep;

    private int _stage;

    private long _began;

    private long _tick;

    private bool _ticked;

    private Item _blade;

    private IPoint3D _tree;

    private bool _noTree;

    private readonly HashSet<(int X, int Y)> _shunned = [];

    private int _cuts;

    private int _dry;

    private int _fletches;

    private int _had;

    private int _fumbles;

    private BotCamp.Fire _fire;

    private int _seat;

    private string _walkNote;

    private bool _still;

    public BotKindle(Map map, Point3D from, double claim, bool sleep)
    {
        _map = map;
        _from = from;
        _claim = claim;
        _sleep = sleep;
    }

    /// <summary>The fire this keeper has laid, once it has laid one.</summary>
    public BotCamp.Fire Fire => _fire;

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _fire?.Centre ?? _from;

    public override double Expects => _claim;

    public override double Minutes => BotCamp.SitMinutes;

    /// <summary>Nobody's trade: the engine trains Camping on every strike, and the auction reads it as anybody's work.</summary>
    public override SkillName? Trains => null;

    public override int Outlay => 0;

    /// <summary>Counted as coin, like a journey: a bot born poor may still sit by a fire.</summary>
    public override double Coin => 1.0;

    public override int Made => 0;

    /// <summary>Not about money, and not to be refused for earning none. See <c>BotDeed.Unpaid</c>; the proposer asks every reason it can fail first.</summary>
    public override bool Unpaid => true;

    /// <summary>Held for the evening, put down for a rescue or a flight and taken up again.</summary>
    public override bool Steadfast => true;

    /// <summary>Sitting still is what a camp looks like when it is going well.</summary>
    public override bool Still => _stage == Sitting;

    /// <summary>A walk into the woods, not a run.</summary>
    public override bool Hurries => false;

    public override string Stage =>
        _stage switch
        {
            Gathering => _tree != null ? $"cutting kindling ({_had} sticks)" : $"gathering kindling ({_had} sticks)",
            Picking => "looking for a place for a fire",
            Striking => $"striking a light ({_fumbles} tries)",
            _ => _fire == null ? "by a fire" : $"keeping {_fire.Name}, {_fire.SeatedCount()} sitting"
        };

    public override void Taken(IBotWilful bot)
    {
        _began = Core.TickCount;
        Camps++;
        BotCamper.Went(bot?.Self);
    }

    public override void Resumed(IBotWilful bot)
    {
        _began = Core.TickCount;
        _ticked = false;
        _still = false;
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile body || !body.Alive || body.Map != _map || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body, or not on the map the camp was for");
        }

        var now = Core.TickCount;

        if (_fire is { Closed: true })
        {
            return _fire.Lit
                ? BotDoing.Done($"{_fire.Name} is out: {_fire.Ended}")
                : BotDoing.Failed($"the fire was given up: {_fire.Ended}");
        }

        return _stage switch
        {
            Gathering => Gather(body, now),
            Picking => Pick(body),
            Striking => Strike(body, now),
            _ => Sit(body, now)
        };
    }

    /// <summary>Kindling enough, from the pack, from logs, or off a tree.</summary>
    private BotDoing Gather(BotMobile body, long now)
    {
        var have = BotCamp.Sticks(body);

        // What the last swing or cut brought, counted on the beat after it: crafting arrives on a timer.
        if (have > _had)
        {
            if (_tree != null)
            {
                Cut += have - _had;
            }
            else
            {
                Fletched += have - _had;
            }
        }

        _had = have;

        if (have >= Math.Max(1, KindlingWant) || now - _began >= GatherMs)
        {
            return Done(have);
        }

        if (Ticks(now, _tree != null ? CutMs : FletchMs))
        {
            return BotDoing.Work(_tree != null ? "cutting kindling" : "making kindling");
        }

        // From logs, with a fletcher's kit: one log, one stick, no skill asked.
        var kit = BotFletching.Kit(body);

        if (kit != null && BotFletching.Logs(body) > 0 && _fletches < MostFletches && Recipe() is { } recipe)
        {
            _fletches++;
            _tick = now;
            _ticked = true;
            BotCraftwork.Swing(body, BotFletching.System, recipe, typeof(Log), kit);

            return BotDoing.Work("making kindling from logs");
        }

        // Off a tree, with a blade.
        _blade ??= BotCamp.Blade(body);

        if (_blade is { Deleted: false } && _cuts < MostCuts && !_noTree)
        {
            _tree ??= FindTree(body, TreeReach, _shunned);

            if (_tree == null)
            {
                _noTree = true;

                return Done(have);
            }

            var trunk = new Point3D(_tree.X, _tree.Y, _tree.Z);

            if (!body.InRange(trunk, 2))
            {
                return BotDoing.Walk(_map, trunk, BotArrival.Within(1), "to a tree for kindling");
            }

            _cuts++;
            _tick = now;
            _ticked = true;

            var before = have;

            new BladedItemTarget(_blade).Invoke(body, _tree);

            // The blade's work is not on a timer: the stick is in the pack now, or the tree had no wood left to give.
            var after = BotCamp.Sticks(body);

            if (after > before)
            {
                Cut += after - before;
                _had = after;
                _dry = 0;
            }
            else if (++_dry >= 2)
            {
                _shunned.Add((_tree.X, _tree.Y));
                _tree = null;
                _dry = 0;
            }

            return BotDoing.Work("cutting kindling");
        }

        return Done(have);

        BotDoing Done(int sticks)
        {
            if (sticks <= 0)
            {
                NoKindling++;

                return BotDoing.Failed(
                    _blade == null
                        ? "no kindling in the pack, no logs to make it and no blade to cut it"
                        : $"no kindling, and no tree within {TreeReach} tiles would give any"
                );
            }

            _stage = Picking;

            return BotDoing.Work("looking for a place for a fire");
        }
    }

    /// <summary>A hearth: this tile if it will do, else one of a few near it.</summary>
    private BotDoing Pick(BotMobile body)
    {
        if (!BotCamp.Hearth(body, _map, PickReach, PickTries, out var centre, out var seats, out var why))
        {
            NoHearth++;

            return BotDoing.Failed($"no place for a fire near ({body.X}, {body.Y}): {why}");
        }

        _fire = BotCamp.Lay(body, _map, centre, seats);
        _seat = 0;
        _fumbles = 0;
        _ticked = false;
        _stage = Striking;
        _walkNote = $"to the hearth at ({centre.X}, {centre.Y})";

        return BotDoing.Work("laying a fire");
    }

    /// <summary>At the hearth, striking until it catches or the fumbles run out.</summary>
    private BotDoing Strike(BotMobile body, long now)
    {
        if (!Beside(body))
        {
            _still = false;

            return BotDoing.Walk(_map, _fire.Places[_seat].Spot, BotArrival.Exactly, _walkNote);
        }

        StandStill(body);
        Face(body, _fire.Centre);

        if (Ticks(now, StrikeMs))
        {
            return BotDoing.Work("striking a light");
        }

        _tick = now;
        _ticked = true;

        if (!BotCamp.TryLight(_fire, body, out var caught))
        {
            NoKindling++;

            return BotDoing.Failed($"the kindling ran out before it caught ({_fumbles} strikes)");
        }

        if (!caught)
        {
            if (++_fumbles >= MostFumbles)
            {
                NoCatch++;

                return BotDoing.Failed($"the kindling would not catch in {_fumbles} strikes (Camping {body.Skills[SkillName.Camping].Value:F1})");
            }

            return BotDoing.Work("striking a light");
        }

        BotCamp.Kindled(_fire, body);
        BotCamp.Sat(_fire, body);
        BotCampTalk.Lit(_fire, body);
        _stage = Sitting;

        return BotDoing.Work("keeping a fire");
    }

    /// <summary>Beside the fire until the evening is over, it is scattered, or the keeper is tired enough to sleep by it.</summary>
    private BotDoing Sit(BotMobile body, long now)
    {
        if (!Beside(body))
        {
            _still = false;

            return BotDoing.Walk(_map, _fire.Places[_seat].Spot, BotArrival.Exactly, _walkNote);
        }

        StandStill(body);
        Face(body, _fire.Centre);
        BotCamp.Sat(_fire, body);
        BotCamp.Seated(_fire, body, now);

        if (body.Tired && BotCamp.Secure(body))
        {
            SleptBy++;

            return BotDoing.Done($"secure by {_fire.Name}, and asleep there");
        }

        if (!ReferenceEquals(_fire.Keeper, body))
        {
            Kept++;

            return BotDoing.Done($"left {_fire.Name} to {_fire.Keeper?.Name ?? "burn down"}");
        }

        return BotDoing.Work("keeping a fire");
    }

    public override bool Bend(IBotWilful bot)
    {
        // The tree could not be got at: another one, once or twice.
        if (_stage == Gathering && _tree != null)
        {
            _shunned.Add((_tree.X, _tree.Y));
            _tree = null;

            return _shunned.Count <= 3;
        }

        // The keeper's seat could not be got at: another seat round the same hearth.
        if (_fire != null && bot?.Self is BotMobile body)
        {
            var next = -1;

            for (var i = 0; i < _fire.Places.Length && next < 0; i++)
            {
                if (i != _seat && _fire.Places[i].Bot == null)
                {
                    next = i;
                }
            }

            if (next < 0)
            {
                return false;
            }

            _fire.Places[_seat].Bot = null;
            _fire.Places[next].Bot = body;
            _seat = next;

            return true;
        }

        return false;
    }

    public override void Drop(IBotWilful bot)
    {
        if (_fire != null && bot?.Self is BotMobile body)
        {
            BotCamp.Let(_fire, body, "its keeper's camp ended");
        }
    }

    /// <summary>Within two tiles of the hearth — the reach the engine's kindling asks of whoever strikes it — and not on it.</summary>
    private bool Beside(Mobile body)
    {
        var away = BotCamp.Tiles(body.Location, _fire.Centre);

        return away is >= 1 and <= 2 && Math.Abs(body.Z - _fire.Centre.Z) < BotArrival.PersonHeight;
    }

    /// <summary>
    /// Beside the hearth, the walk to the seat is finished once: a keeper that struck from a tile short of its seat would
    /// otherwise walk on under the striking, since work leaves the journey alone.
    /// </summary>
    private void StandStill(BotMobile body)
    {
        if (_still)
        {
            return;
        }

        _still = true;

        if (body.Journey.Active)
        {
            body.Journey.Finish();
        }
    }

    /// <summary>Whether the clock set by the last swing or strike is still running. A flag rather than a zero tick.</summary>
    private bool Ticks(long now, int ms) => _ticked && now - _tick < ms;

    private static void Face(Mobile body, Point3D at)
    {
        var facing = body.GetDirectionTo(at);

        if ((body.Direction & Direction.Mask) != facing)
        {
            body.Direction = facing;
        }
    }

    /// <summary>The fletcher's recipe for a stick of kindling out of one log.</summary>
    private static CraftItem Recipe()
    {
        var items = BotFletching.System?.CraftItems;

        if (items == null)
        {
            return null;
        }

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].ItemType == typeof(Kindling))
            {
                return items[i];
            }
        }

        return null;
    }

    /// <summary>
    /// The nearest tree within reach that is not shunned, as the target the engine wants, or null. Rings outward, as the
    /// woodsman's own search does, but twelve rings rather than eighty: a camp is where the bot is.
    /// </summary>
    internal static IPoint3D FindTree(Mobile bot, int reach, HashSet<(int X, int Y)> shun)
    {
        var map = bot?.Map;
        var system = Lumberjacking.System;

        if (map == null || map == Map.Internal || system == null)
        {
            return null;
        }

        for (var radius = 1; radius <= reach; radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                    {
                        continue;
                    }

                    var found = BotOre.Examine(map, bot.X + dx, bot.Y + dy, system);

                    if (found == null || shun != null && shun.Contains((found.X, found.Y)))
                    {
                        continue;
                    }

                    if (BotRefused.Refusing(map, new Point3D(found.X, found.Y, found.Z)))
                    {
                        continue;
                    }

                    return found;
                }
            }
        }

        return null;
    }

    /// <summary>Whether this bot has anything to make a fire of: sticks, logs and a kit, or a blade (the tree is asked later).</summary>
    public static bool Means(Mobile bot) =>
        BotCamp.Sticks(bot) > 0
        || BotFletching.Kit(bot) != null && BotFletching.Logs(bot) > 0
        || BotCamp.Blade(bot) != null;

    public static string Describe() =>
        $"{Camps} camps made, {Cut} sticks cut off trees and {Fletched} made from logs; {NoKindling} found no kindling, {NoHearth} no place for a fire, "
        + $"{NoCatch} could not make it catch in {MostFumbles} strikes; {Kept} keepers left their fire to another, {SleptBy} slept by their own";

    public static void Forget()
    {
        Camps = 0;
        Cut = 0;
        Fletched = 0;
        NoKindling = 0;
        NoHearth = 0;
        NoCatch = 0;
        Kept = 0;
        SleptBy = 0;
    }
}

/// <summary>
/// Offers a fire-lighter a camp where it stands, when it is tired or bored or has had nothing to do; and, in town with no
/// kindling, a trip to a provisioner for some.
///
/// <para>
/// <b>Every reason a camp fails is asked before it is offered, because it is unpaid work.</b> An unpaid deed is priced at
/// its claim and the ledger never learns it is failing (MAP §4, "taking the price off a piece of work takes its brake off
/// too"), so the ground, the kindling, the skill and anything hostile nearby are all asked here, and a bot that has made
/// camp is not offered another for <see cref="EveryMs"/> whatever came of it.
/// </para>
/// </summary>
public sealed class BotCamper : IBotProposer
{
    /// <summary>How long one bot waits between camps. Forty-five minutes.</summary>
    public static int EveryMs { get; set; } = 2700000;

    /// <summary>How long a look round for trees and hostiles is kept. Twenty seconds: at most one spatial look per bot that often.</summary>
    public static int LookMs { get; set; } = 20000;

    /// <summary>How long a bot must have had nothing to do before a camp is offered on that account alone. A minute.</summary>
    public static double IdleMinutes { get; set; } = 1.0;

    /// <summary>How near a provisioner a fire-lighter in town must be to be offered a trip for kindling. Forty tiles.</summary>
    public static int ShopReach { get; set; } = 40;

    /// <summary>Sticks bought on such a trip. Five, a fire's worth.</summary>
    public static int KindlingBuy { get; set; } = 5;

    public static long Asked { get; private set; }

    public static long NotLighter { get; private set; }

    public static long TooSoon { get; private set; }

    public static long Hurt { get; private set; }

    public static long Content { get; private set; }

    public static long NotWild { get; private set; }

    public static long Barred { get; private set; }

    public static long Bloodied { get; private set; }

    public static long TooMany { get; private set; }

    public static long FireNear { get; private set; }

    public static long NoMeans { get; private set; }

    public static long NoTree { get; private set; }

    public static long CannotLight { get; private set; }

    public static long Hostile { get; private set; }

    public static long Offered { get; private set; }

    public static long Stocked { get; private set; }

    public static long NoShop { get; private set; }

    public static long Poor { get; private set; }

    public static long BuyOffered { get; private set; }

    private static readonly Dictionary<Serial, long> _last = [];

    private static readonly Dictionary<Serial, (long Tick, bool Hostile, bool Tree)> _looked = [];

    private static readonly Dictionary<Serial, (long Tick, BaseVendor Shop)> _shopLooked = [];

    public string Name => "Camper";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>A camp was taken: this bot's clock starts. From <c>BotKindle.Taken</c>, never at the offer.</summary>
    public static void Went(Mobile who)
    {
        if (who != null)
        {
            _last[who.Serial] = Core.TickCount;
        }
    }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (!BotCamp.Running || map == null || map == Map.Internal || body is not BotMobile who || !who.Alive || who.Tired)
        {
            return null;
        }

        if (who.Squad != null || BotDelveParty.Delving(who) || BotOutlaw.Jailed(who) || BotDuel.Duelling(who))
        {
            return null;
        }

        Asked++;

        if (!BotCamp.Lighter(who))
        {
            NotLighter++;

            return null;
        }

        var now = Core.TickCount;

        if (_last.TryGetValue(who.Serial, out var when) && now - when < EveryMs)
        {
            TooSoon++;

            return null;
        }

        if (who.HitsMax <= 0 || who.Hits < who.HitsMax * BotHunter.FitAt)
        {
            Hurt++;

            return null;
        }

        var land = BotCamp.LandOf(map, who.Location);

        // In town a fire-lighter with no kindling is offered the provisioner's; nobody lights a fire in the square.
        if (land == BotCamp.Land.Town)
        {
            return Buy(bot, who);
        }

        switch (land)
        {
            case BotCamp.Land.Barred:
                {
                    Barred++;

                    return null;
                }
            case BotCamp.Land.Bloodied:
                {
                    Bloodied++;

                    return null;
                }
            case not BotCamp.Land.Wild:
                {
                    NotWild++;

                    return null;
                }
        }

        if (!Wants(who, now))
        {
            Content++;

            return null;
        }

        if (BotCamp.Count() >= BotCamp.MostFires)
        {
            TooMany++;

            return null;
        }

        if (BotCamp.Crowded(map, who.Location))
        {
            FireNear++;

            return null;
        }

        if (!BotKindle.Means(who))
        {
            NoMeans++;

            return null;
        }

        if (!BotCamp.Lights(who))
        {
            CannotLight++;

            return null;
        }

        // The two spatial looks, kept for LookMs: something hostile near, and — for a bot with only a blade — a tree.
        if (!_looked.TryGetValue(who.Serial, out var look) || now - look.Tick >= LookMs)
        {
            var needsTree = BotCamp.Sticks(who) <= 0 && !(BotFletching.Kit(who) != null && BotFletching.Logs(who) > 0);

            look = (now, BotThreat.Anything(who, BotCamp.HostileReach), !needsTree || BotKindle.FindTree(who, BotKindle.TreeReach, null) != null);
            _looked[who.Serial] = look;
        }

        if (look.Hostile)
        {
            Hostile++;

            return null;
        }

        if (!look.Tree)
        {
            NoTree++;

            return null;
        }

        Offered++;

        return new BotKindle(map, who.Location, BotCamp.Claim(who, BotKindle.Prior), false);
    }

    /// <summary>Tired, bored, or with nothing to do for a minute: the three reasons Patrick's order gives a bot to sit down.</summary>
    private static bool Wants(BotMobile who, long now)
    {
        var urges = who.Resolve?.Urges;

        return BotCamp.Weariness(who) >= BotCamp.WearyFrom
               || urges != null && (urges.IsRestless || who.Resolve.Deed == null && urges.BarrenMinutes(now) >= IdleMinutes);
    }

    /// <summary>A trip to the nearest provisioner for a fire's worth of kindling, for a fire-lighter in town with none.</summary>
    private static BotDeed Buy(IBotWilful bot, BotMobile who)
    {
        if (BotCamp.Sticks(who) > 0)
        {
            Stocked++;

            return null;
        }

        // The shops are walked at most once per LookMs per bot: an idle bot in town is asked every two seconds.
        var now = Core.TickCount;

        if (!_shopLooked.TryGetValue(who.Serial, out var look) || now - look.Tick >= LookMs)
        {
            look = (now, BotShops.Nearest(bot, typeof(Kindling)));
            _shopLooked[who.Serial] = look;
        }

        var shop = look.Shop;

        if (shop is not { Deleted: false } || !who.InRange(shop.Location, ShopReach))
        {
            NoShop++;

            return null;
        }

        var price = BotShops.Price(shop, typeof(Kindling));
        var amount = Math.Max(1, KindlingBuy);

        if (price <= 0 || BotYield.Spendable(who) < price * amount * 4)
        {
            Poor++;

            return null;
        }

        BuyOffered++;

        return new BotRestock(shop, typeof(Kindling), amount, price);
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been asked to make camp"
            : $"{Asked} asked to make camp: {Offered} offered one; {NotLighter} not fire-lighters ({BotCamp.LighterShare:P0} are), {TooSoon} within {EveryMs / 60000} minutes of their last, {Hurt} hurt, "
              + $"{Content} neither tired nor bored nor idle, {NotWild} not on wild ground, {Barred} on barred ground, {Bloodied} on bloodied ground, {TooMany} with {BotCamp.MostFires} fires burning already, "
              + $"{FireNear} with a fire near enough to sit at, {NoMeans} with nothing to make a fire of, {NoTree} with only a blade and no tree within {BotKindle.TreeReach}, "
              + $"{CannotLight} whose Camping cannot rise, {Hostile} with something hostile near; in town {BuyOffered} sent to buy kindling, {Stocked} had some, {NoShop} no provisioner within {ShopReach}, {Poor} too poor";

    public static void Forget()
    {
        _last.Clear();
        _looked.Clear();
        _shopLooked.Clear();
        Asked = 0;
        NotLighter = 0;
        TooSoon = 0;
        Hurt = 0;
        Content = 0;
        NotWild = 0;
        Barred = 0;
        Bloodied = 0;
        TooMany = 0;
        FireNear = 0;
        NoMeans = 0;
        NoTree = 0;
        CannotLight = 0;
        Hostile = 0;
        Offered = 0;
        Stocked = 0;
        NoShop = 0;
        Poor = 0;
        BuyOffered = 0;
        BotKindle.Forget();
    }
}
