using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// Moves a guild out of the hall it has filled and into the next size up: new ground proved, the price levied, the old hall
/// taken down with its counter and benches carried across, the bigger one raised. See <see cref="BotHallKind"/>.
///
/// <para>
/// <b>Patrick's order of 26.09.2026: the hall grows with the guild.</b> The order of business is the carrier's
/// (<see cref="BotRemove"/>) for the reason written there — the new ground is proved before anything is knocked down, or a
/// failed check leaves a guild homeless — with the levy between the proof and the demolition, so a guild that cannot pay
/// keeps its hall. Two things the carrier leaves on the ground are carried here: the benches, which the engine would turn
/// into deeds lying where they stood (they are taken down first and set up again inside, <c>BotFittings.Strip</c> and
/// <c>Install</c>), and the counter, which is carried as the carrier carries it.
/// </para>
/// </summary>
public sealed class BotEnlarge : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotEnlarge));

    public const string Trade = "enlarge";

    public static double Prior { get; set; } = 400.0;

    public static double WorkMinutes { get; set; } = 2.0;

    public static int Reach => BotHall.Reach;

    /// <summary>How long the office stays held by the member walking to the new ground.</summary>
    public static int ClaimMs { get; set; } = 180000;

    public static long Done { get; private set; }

    public static long Refused { get; private set; }

    public static long Short { get; private set; }

    public static long BenchesCarried { get; private set; }

    private readonly Guild _guild;

    private readonly Map _map;

    private readonly Point3D _plot;

    private readonly BotHallKind _kind;

    public BotEnlarge(Guild guild, Map map, Point3D plot, BotHallKind kind)
    {
        _guild = guild;
        _map = map;
        _plot = plot;
        _kind = kind;
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

    public override string Stage => $"moving {_guild?.Name} into {_kind} at {_plot.X},{_plot.Y}";

    public override bool Bend(IBotWilful bot) => false;

    public override void Drop(IBotWilful bot) => BotOffice.Release(BotEnlarger.Office, _guild);

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _guild == null || _kind == null)
        {
            return BotDoing.Failed("no body");
        }

        var old = BotEstate.Hall(_guild);

        if (old is not { Deleted: false })
        {
            return BotDoing.Failed($"{_guild.Name} has no hall to move out of");
        }

        var was = BotHallKind.Of(old);

        if (was.Size >= _kind.Size)
        {
            return BotDoing.Failed($"{_guild.Name}'s hall is {was} already");
        }

        BotOffice.Hold(BotEnlarger.Office, _guild, ClaimMs);

        if (!body.InRange(_plot, Reach))
        {
            return BotDoing.Walk(_map, _plot, BotArrival.Within(Reach), $"to the ground for {_kind} at {_plot.X},{_plot.Y}");
        }

        // Ground that changed hands while the bot walked: not raised on another guild's. See BotBounds.
        if (BotBounds.Refuse(BotBounds.Placing.Bigger, _guild.Name, _map, _plot, _kind.Multi, out var foreign))
        {
            BotPlot.Spend();
            Refused++;

            return BotDoing.Failed(foreign);
        }

        var result = HousePlacement.Check(body, _kind.Multi, _plot, out var toMove, Direction.South);

        if (result != HousePlacementResult.Valid)
        {
            BotPlot.Spend();
            Refused++;

            return BotDoing.Failed($"the ground would not take {_kind}: {result}");
        }

        for (var i = 0; i < toMove.Count; i++)
        {
            if (toMove[i] is Item)
            {
                Refused++;

                return BotDoing.Failed("there are things lying on the new ground");
            }
        }

        // The price, before anything comes down: a guild that cannot pay keeps its hall.
        var paid = new List<BotEstate.Contribution>();
        var got = BotEstate.Levy(_guild, _kind.Price, paid, body);

        if (got < _kind.Price)
        {
            BotEstate.Refund(paid, body);
            Short++;

            return BotDoing.Failed($"{_guild.Name} raised {got} of the {_kind.Price}gp {_kind} costs");
        }

        var benches = BotFittings.Installed(old, _guild);

        BotFittings.Strip(old);

        var merchant = BotShelf.Of(old);

        if (merchant is { Deleted: false })
        {
            merchant.House = null;
            merchant.Internalize();
        }

        var price = old.Price + got;
        var from = old.Location;

        old.Delete();
        BotPlot.Spend();

        var house = _kind.Make(body);

        if (house == null || house.Deleted)
        {
            // Not expected — the engine's check has just passed — but a guild must not end this errand homeless and out of
            // pocket: its money back, and its old size put up again where the old one stood.
            BotEstate.Refund(paid, body);
            Refused++;

            house = was.Make(body);

            if (house is { Deleted: false })
            {
                house.Price = old.Price;
                house.MoveToWorld(from, _map);
                BotEstate.Take(_guild, house);
                BotFittings.Furnish(house, _guild);
                BotEstate.Register(_guild, house, false);
            }

            if (merchant is { Deleted: false })
            {
                merchant.MoveToWorld(from, _map);
            }

            return BotDoing.Failed($"{_kind} would not go up; {_guild.Name} keeps {was}");
        }

        house.Price = price;
        house.MoveToWorld(_plot, _map);

        for (var i = 0; i < toMove.Count; i++)
        {
            if (toMove[i] is Mobile mobile)
            {
                mobile.Location = house.BanLocation;
            }
        }

        BotEstate.Take(_guild, house);

        var fitted = BotFittings.Furnish(house, _guild);

        for (var i = 0; i < benches.Count; i++)
        {
            if (BotFittings.Install(house, benches[i]))
            {
                fitted++;
                BenchesCarried++;
            }
        }

        if (merchant is { Deleted: false })
        {
            var spot = BotFittings.Spot(house);

            merchant.House = house;
            merchant.MoveToWorld(spot == Point3D.Zero ? house.BanLocation : spot, _map);

            if (_guild.Leader is BotMobile keeper)
            {
                merchant.Owner = keeper;
            }

            fitted++;
        }

        BotEstate.Fit(fitted);
        BotEstate.Register(_guild, house, false);

        Done++;

        logger.Information(
            "{Guild} has moved out of {Was} into {Kind} at {X},{Y} for {Gold}gp: {Members} members, room now for {Room}; {Benches} benches carried across",
            _guild.Name,
            was,
            _kind,
            _plot.X,
            _plot.Y,
            got,
            _guild.Members.Count,
            _kind.Room,
            benches.Count
        );

        return BotDoing.Done($"{_guild.Name} moved into {_kind} at {_plot.X},{_plot.Y}, {fitted} things inside");
    }

    public static string Describe() =>
        Done + Refused + Short == 0
            ? "no hall has been enlarged"
            : $"{Done} halls enlarged ({BenchesCarried} benches carried across), {Refused} refused by the ground, {Short} could not raise the price";

    public static void Forget()
    {
        Done = 0;
        Refused = 0;
        Short = 0;
        BenchesCarried = 0;
    }
}

/// <summary>
/// Offers a member of a guild that has nearly filled its hall the move into the next size up, when the guild can pay for it.
/// See <see cref="BotEnlarge"/> and <see cref="BotHallKind"/>.
///
/// <para>
/// Nearly full is within <see cref="Fullness"/> of the hall's room, so the move is under way before the guild turns anybody
/// away. The money is the guild's chest and its members' spare purses together, as the levy reads them; one member at a
/// time (the office), and the ground looked for at most once a <see cref="LookMs"/> a guild, because the search for a large
/// house is the dearest question the estate asks.
/// </para>
/// </summary>
public sealed class BotEnlarger : IBotProposer
{
    public const string Office = "enlarger";

    /// <summary>How close to its hall's room a guild must be before it moves into a bigger one.</summary>
    public static int Fullness { get; set; } = 2;

    /// <summary>How often one guild's ground is looked for.</summary>
    public static int LookMs { get; set; } = 60000;

    public static long Asked { get; private set; }

    public static long Offered { get; private set; }

    public static long Roomy { get; private set; }

    public static long Top { get; private set; }

    public static long Poor { get; private set; }

    public static long Claimed { get; private set; }

    public static long Groundless { get; private set; }

    private static readonly Dictionary<string, long> _looked = [];

    public string Name => Office;

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotEstate.Running || bot?.Self is not BotMobile { Deleted: false, Alive: true } body || body.Map == null
            || body.Map == Map.Internal || body.Guild is not Guild ours || BotUnderworld.Band(ours))
        {
            return null;
        }

        var hall = BotEstate.Hall(ours);

        if (hall is not { Deleted: false })
        {
            return null;
        }

        Asked++;

        var kind = BotHallKind.Of(hall);
        var next = kind.Next;

        if (next == null)
        {
            Top++;

            return null;
        }

        if (ours.Members.Count < kind.Room - Fullness)
        {
            Roomy++;

            return null;
        }

        // Fund counts the chest since 29.09.2026; it was added here by hand before that, and would now count twice.
        if (BotEstate.Fund(ours) < next.Price)
        {
            Poor++;

            return null;
        }

        if (BotOffice.Busy(Office, ours))
        {
            Claimed++;

            return null;
        }

        var now = Core.TickCount;

        if (_looked.TryGetValue(ours.Name, out var looked) && now - looked < LookMs)
        {
            return null;
        }

        _looked[ours.Name] = now;

        if (!BotPlot.Find(body, BotSeat.Of(ours), Point3D.Zero, 0, next.Multi, hall, out var plot))
        {
            Groundless++;

            return null;
        }

        Offered++;
        BotOffice.Offering(Office, ours);

        return new BotEnlarge(ours, BotPopulation.Home, plot, next);
    }

    public static string Describe() =>
        Asked == 0
            ? $"no guild has been looked at for a bigger hall; {BotEnlarge.Describe()}"
            : $"the enlarger looked {Asked} times and sent {Offered}: {Roomy} halls had room, {Top} were the largest there is, {Poor} could not pay, "
              + $"{Claimed} already had somebody on it, {Groundless} found no ground; {BotEnlarge.Describe()}; widenings refused for a hall with no room {BotGuilds.Cramped}";

    public static void Forget()
    {
        Asked = 0;
        Offered = 0;
        Roomy = 0;
        Top = 0;
        Poor = 0;
        Claimed = 0;
        Groundless = 0;
        _looked.Clear();
        BotEnlarge.Forget();
    }
}
