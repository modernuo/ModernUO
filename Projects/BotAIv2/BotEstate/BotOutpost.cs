using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Multis;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// A guild's second house, out on the far edge of its land: where its members rise when they fall nearer to it than to the
/// hall, and a yard of the guild's own out there — land the toll can be charged on.
///
/// <para>
/// <b>Patrick's order of 26.09.2026 ("different houses for a guild, expansions"), the outposts he chose among its three
/// parts:</b> a second house on the guild's far squares — a place to rise and a store nearer the hunting, and something to
/// hold the hunting toll up by. One to a guild, a small house, raised on the guild's held square farthest from its hall once
/// that square is at least <see cref="Far"/> out and the guild can pay <see cref="Price"/>.
/// </para>
///
/// <para>
/// <b>Its own register, and its own sign.</b> The halls are found by the guild's name on the sign and there is one to a
/// guild (<c>BotEstate</c>); an outpost carries "The Blade outpost", which no guild is called, so the halls' adoption passes it
/// over and this one takes it (<see cref="Adopt"/>). It makes a yard exactly as a hall does (<c>BotLand.Holder</c>), and a
/// fallen member of its guild rises there when it fell nearer to it than to the hall (<c>BotSeat.Home</c>).
/// </para>
/// </summary>
public static class BotOutpost
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotOutpost));

    public static bool Running { get; set; } = true;

    /// <summary>What an outpost costs its guild.</summary>
    public static int Price { get; set; } = 8000;

    /// <summary>How far from the hall the held square must lie for an outpost to be worth raising there.</summary>
    public static int Far { get; set; } = 90;

    public const string Suffix = " outpost";

    public static long Raised { get; private set; }

    public static long Adopted { get; private set; }

    public static long Rose { get; private set; }

    private static readonly Dictionary<string, BaseHouse> _posts = new(StringComparer.Ordinal);

    public static IEnumerable<KeyValuePair<string, BaseHouse>> Held => _posts;

    public static IEnumerable<BaseHouse> Houses => _posts.Values;

    /// <summary>This guild's outpost, or null.</summary>
    public static BaseHouse Of(string guild)
    {
        if (guild == null || !_posts.TryGetValue(guild, out var house))
        {
            return null;
        }

        if (house is { Deleted: false })
        {
            return house;
        }

        _posts.Remove(guild);

        return null;
    }

    /// <summary>
    /// Where a fallen member of this guild rises, if its outpost is nearer to where it fell than the hall is; otherwise
    /// <c>Point3D.Zero</c>. See <c>BotSeat.Home</c>.
    /// </summary>
    public static Point3D Nearer(Guild guild, BaseHouse hall, Mobile fallen)
    {
        if (!Running || fallen == null || Of(guild?.Name) is not { } post || post.Map != fallen.Map)
        {
            return Point3D.Zero;
        }

        var toPost = fallen.GetDistanceToSqrt(post.BanLocation);
        var toHall = hall is { Deleted: false } && hall.Map == fallen.Map ? fallen.GetDistanceToSqrt(hall.BanLocation) : double.MaxValue;

        if (toPost >= toHall)
        {
            return Point3D.Zero;
        }

        Rose++;

        return post.BanLocation;
    }

    /// <summary>The middle of this guild's held square farthest from its hall, at least <see cref="Far"/> out, or Zero.</summary>
    public static Point3D FarSquare(Guild guild, BaseHouse hall)
    {
        if (guild == null || hall is not { Deleted: false } || hall.Map == null)
        {
            return Point3D.Zero;
        }

        var best = Point3D.Zero;
        var farthest = Far - 1;
        var side = BotQuad.Side;

        foreach (var (key, owner, _) in BotClaim.Owned())
        {
            if (owner != guild.Name || key.Map != hall.Map.MapID)
            {
                continue;
            }

            var middle = new Point3D(key.X * side + side / 2, key.Y * side + side / 2, 0);
            var gap = Math.Max(Math.Abs(middle.X - hall.X), Math.Abs(middle.Y - hall.Y));

            if (gap > farthest)
            {
                farthest = gap;
                best = middle;
            }
        }

        return best;
    }

    /// <summary>Gives the house its guild: the leader its owner, every member a co-owner, doors unlocked, decay refreshed.</summary>
    public static void Keep(Guild guild, BaseHouse house)
    {
        if (guild == null || house is not { Deleted: false })
        {
            return;
        }

        if (guild.Leader is BotMobile leader && house.Owner != leader)
        {
            house.Owner = leader;
        }

        house.CoOwners ??= [];
        house.CoOwners.Clear();

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is BotMobile { Deleted: false } member && member != house.Owner)
            {
                house.CoOwners.Add(member);
            }
        }

        house.Public = true;
        BotEstate.Unlock(house);
        house.RefreshDecay();
    }

    public static void Register(Guild guild, BaseHouse house)
    {
        if (guild?.Name != null && house != null)
        {
            _posts[guild.Name] = house;
            Raised++;
        }
    }

    /// <summary>Finds every outpost by its sign at boot and gives it back to its guild.</summary>
    public static void Adopt()
    {
        if (!Running)
        {
            return;
        }

        foreach (var house in BaseHouse.AllHouses)
        {
            var sign = house?.Sign?.Name;

            if (house == null || house.Deleted || sign == null || !sign.EndsWith(Suffix, StringComparison.Ordinal))
            {
                continue;
            }

            var name = sign[..^Suffix.Length];

            if (BotGuilds.Named(name) is not { Disbanded: false } guild || _posts.ContainsKey(name))
            {
                continue;
            }

            Keep(guild, house);
            _posts[name] = house;
            Adopted++;
        }

        if (_posts.Count > 0)
        {
            logger.Information("Outposts taken back from the world: {Posts}", string.Join(", ", _posts.Keys));
        }
    }

    /// <summary>Every outpost off the island. For <c>do raze</c> and a wipe.</summary>
    public static int Raze()
    {
        var gone = 0;

        foreach (var house in _posts.Values)
        {
            if (house is { Deleted: false })
            {
                house.Delete();
                gone++;
            }
        }

        _posts.Clear();

        return gone;
    }

    public static string Describe()
    {
        if (!Running)
        {
            return "no outposts";
        }

        using var sb = ValueStringBuilder.Create(256);

        sb.Append($"outposts: {_posts.Count} standing, {Raised} raised, {Adopted} taken back at a boot, {Rose} risen at one");

        foreach (var (guild, house) in _posts)
        {
            sb.Append($"; {guild} at {house.X},{house.Y}");
        }

        return sb.ToString();
    }

    public static void Forget()
    {
        _posts.Clear();
        Raised = 0;
        Adopted = 0;
        Rose = 0;
    }
}

/// <summary>
/// Offers a member of a guild with a hall, far-flung held land and the price the raising of its outpost there. See
/// <see cref="BotOutpost"/>.
/// </summary>
public sealed class BotOutposter : IBotProposer
{
    public const string Office = "outposter";

    /// <summary>How often one guild is looked at for an outpost.</summary>
    public static int LookMs { get; set; } = 120000;

    public static long Asked { get; private set; }

    public static long Offered { get; private set; }

    public static long Near { get; private set; }

    public static long Poor { get; private set; }

    public static long Groundless { get; private set; }

    private static readonly Dictionary<string, long> _looked = [];

    public string Name => Office;

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotOutpost.Running || !BotEstate.Running || bot?.Self is not BotMobile { Deleted: false, Alive: true } body
            || body.Map == null || body.Map == Map.Internal || body.Guild is not Guild ours || BotUnderworld.Band(ours))
        {
            return null;
        }

        var hall = BotEstate.Hall(ours);

        if (hall is not { Deleted: false } || BotOutpost.Of(ours.Name) != null || BotOffice.Busy(Office, ours))
        {
            return null;
        }

        var now = Core.TickCount;

        if (_looked.TryGetValue(ours.Name, out var looked) && now - looked < LookMs)
        {
            return null;
        }

        _looked[ours.Name] = now;
        Asked++;

        var far = BotOutpost.FarSquare(ours, hall);

        if (far == Point3D.Zero)
        {
            Near++;

            return null;
        }

        // Fund counts the chest since 29.09.2026; it was added here by hand before that, and would now count twice.
        if (BotEstate.Fund(ours) < BotOutpost.Price)
        {
            Poor++;

            return null;
        }

        if (!BotPlot.Find(body, far, Point3D.Zero, 0, BotHallKind.First.Multi, null, out var plot))
        {
            Groundless++;

            return null;
        }

        Offered++;
        BotOffice.Offering(Office, ours);

        return new BotOutpostRaise(ours, BotPopulation.Home, plot);
    }

    public static string Describe() =>
        Asked == 0
            ? $"no guild has been looked at for an outpost; {BotOutpost.Describe()}"
            : $"{Asked} looks for an outpost: {Offered} offered, {Near} held no land far enough out, {Poor} could not pay {BotOutpost.Price}gp, {Groundless} found no ground; {BotOutpostRaise.Describe()}; {BotOutpost.Describe()}";

    public static void Forget()
    {
        Asked = 0;
        Offered = 0;
        Near = 0;
        Poor = 0;
        Groundless = 0;
        _looked.Clear();
        BotOutpostRaise.Forget();
    }
}

/// <summary>A member raises its guild's outpost: the ground proved, the price levied, a small house with the outpost's sign.</summary>
public sealed class BotOutpostRaise : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotOutpostRaise));

    public const string Trade = "outpost";

    public static double Prior { get; set; } = 350.0;

    public static double WorkMinutes { get; set; } = 2.0;

    public static int ClaimMs { get; set; } = 240000;

    public static long Done { get; private set; }

    public static long Refused { get; private set; }

    public static long Short { get; private set; }

    private readonly Guild _guild;

    private readonly Map _map;

    private readonly Point3D _plot;

    public BotOutpostRaise(Guild guild, Map map, Point3D plot)
    {
        _guild = guild;
        _map = map;
        _plot = plot;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _plot;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override bool Unpaid => true;

    public override string Stage => $"raising the outpost of {_guild?.Name} at {_plot.X},{_plot.Y}";

    public override bool Bend(IBotWilful bot) => false;

    public override void Drop(IBotWilful bot) => BotOffice.Release(BotOutposter.Office, _guild);

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _guild == null)
        {
            return BotDoing.Failed("no body");
        }

        if (BotOutpost.Of(_guild.Name) != null)
        {
            return BotDoing.Done($"{_guild.Name} has an outpost already");
        }

        BotOffice.Hold(BotOutposter.Office, _guild, ClaimMs);

        if (!body.InRange(_plot, BotHall.Reach))
        {
            return BotDoing.Walk(_map, _plot, BotArrival.Within(BotHall.Reach), $"to the ground for the outpost at {_plot.X},{_plot.Y}");
        }

        var kind = BotHallKind.First;

        // Ground that changed hands while the bot walked: not raised on another guild's. See BotBounds.
        if (BotBounds.Refuse(BotBounds.Placing.Outpost, _guild.Name, _map, _plot, kind.Multi, out var foreign))
        {
            BotPlot.Spend();

            return BotDoing.Failed(foreign);
        }

        var result = HousePlacement.Check(body, kind.Multi, _plot, out var toMove, Direction.South);

        if (result != HousePlacementResult.Valid)
        {
            BotPlot.Spend();
            Refused++;

            return BotDoing.Failed($"the ground would not take the outpost: {result}");
        }

        for (var i = 0; i < toMove.Count; i++)
        {
            if (toMove[i] is Item)
            {
                Refused++;

                return BotDoing.Failed("there are things lying on the ground");
            }
        }

        var paid = new List<BotEstate.Contribution>();
        var got = BotEstate.Levy(_guild, BotOutpost.Price, paid, body);

        if (got < BotOutpost.Price)
        {
            BotEstate.Refund(paid, body);
            Short++;

            return BotDoing.Failed($"{_guild.Name} raised {got} of the {BotOutpost.Price}gp an outpost costs");
        }

        BotPlot.Spend();

        var house = kind.Make(body);

        if (house == null || house.Deleted)
        {
            BotEstate.Refund(paid, body);
            Refused++;

            return BotDoing.Failed("the outpost would not go up");
        }

        house.Price = got;
        house.MoveToWorld(_plot, _map);

        for (var i = 0; i < toMove.Count; i++)
        {
            if (toMove[i] is Mobile mobile)
            {
                mobile.Location = house.BanLocation;
            }
        }

        if (house.Sign != null)
        {
            house.Sign.Name = $"{_guild.Name}{BotOutpost.Suffix}";
        }

        BotOutpost.Keep(_guild, house);

        var chest = new WoodenChest();
        var spot = BotFittings.Spot(house);

        chest.MoveToWorld(spot == Point3D.Zero ? house.BanLocation : spot, _map);

        if (house.Owner == null || !house.LockDown(house.Owner, chest))
        {
            chest.Movable = false;
        }

        BotOutpost.Register(_guild, house);
        Done++;

        var hall = BotEstate.Hall(_guild);

        logger.Information(
            "{Guild} has raised an outpost at {X},{Y}, {Gap} tiles from its hall, for {Gold}gp: its fallen nearer to it rise there, and its yard is the guild's land",
            _guild.Name,
            _plot.X,
            _plot.Y,
            hall == null ? 0 : (int)hall.GetDistanceToSqrt(_plot),
            got
        );

        return BotDoing.Done($"raised the outpost of {_guild.Name} at {_plot.X},{_plot.Y}");
    }

    public static string Describe() =>
        Done + Refused + Short == 0
            ? "no outpost raised"
            : $"{Done} outposts raised, {Refused} refused by the ground, {Short} short of the price";

    public static void Forget()
    {
        Done = 0;
        Refused = 0;
        Short = 0;
    }
}
