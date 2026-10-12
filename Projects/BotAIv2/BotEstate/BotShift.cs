using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// Shifting a hall off another guild's held ground onto the guild's own: within its own square first, then onto the guild's
/// other squares near it, with the standing hall's own tiles counted as free.
///
/// <para>
/// <b>06.10.2026, build 410.</b> The first carry (<c>BotRemove</c>, off mode) asked <c>BotPlot.Find</c>, which never offers ground
/// nearer than <c>BotPlot.Near</c> to where it looks from and puts every candidate to the engine with the standing hall still up
/// — the engine refuses any plot touching another house or its yard. So the shift of three tiles east that would have brought
/// The Ash's walls wholly into its own square was never tried, and the hall went 112 tiles away and lost the square.
/// </para>
///
/// <para>
/// <b>Down first, then asked, and put back if nothing fits.</b> The engine cannot be asked about a plot while the hall stands on
/// half of it, so the bot walks to the hall, the hall comes down (its merchant taken off the roll first, as in a carry), the
/// candidates (<c>BotBounds.Shifts</c>, nearest first, our own tests already passed) are put to the engine up to
/// <c>BotBounds.ShiftChecks</c>, and the first it takes gets the hall. If none: the hall goes up again exactly where it stood —
/// ground it stood on a moment ago — and the next step is open ground. Either way the hall is furnished afresh, as after any carry;
/// nothing is paid.
/// </para>
/// </summary>
public sealed class BotShift : BotDeed
{
    public const string Trade = "shift";

    public static double Prior { get; set; } = 400.0;

    public static double WorkMinutes { get; set; } = 1.0;

    public static int Reach => BotHall.Reach;

    private readonly Guild _guild;

    private readonly Map _map;

    private readonly Point3D _hall;

    public BotShift(Guild guild, Map map, Point3D hall)
    {
        _guild = guild;
        _map = map;
        _hall = hall;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _hall;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override bool Unpaid => true;

    public override string Stage => $"shifting the hall of {_guild?.Name} at {_hall.X},{_hall.Y} off another guild's ground onto its own";

    public override bool Bend(IBotWilful bot) => false;

    public override void Drop(IBotWilful bot) => BotOffice.Release(BotExile.Office, _guild);

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _guild == null || _map == null)
        {
            return BotDoing.Failed("no body");
        }

        if (!BotBounds.Owes(_guild, out var old, out _) || !BotBounds.Shifting(_guild.Name))
        {
            return BotDoing.Failed($"{_guild.Name} owes no shift any more");
        }

        if (old.Location != _hall || old.Map != _map)
        {
            return BotDoing.Failed($"the hall of {_guild.Name} is not where it was");
        }

        BotOffice.Hold(BotExile.Office, _guild, BotExile.ClaimMs);

        if (!body.InRange(_hall, Reach))
        {
            return BotDoing.Walk(_map, _hall, BotArrival.Within(Reach), $"to the hall at {_hall.X},{_hall.Y}");
        }

        var kind = BotHallKind.Of(old);
        var candidates = BotBounds.Shifts(_guild.Name, _map, old, kind.Multi);

        if (candidates.Count == 0)
        {
            BotBounds.ShiftFailed(_guild.Name, "no plot on its own ground is level and clear by our own tests");

            return BotDoing.Failed("no plot on its own ground passes our own tests");
        }

        var price = old.Price;
        var oldLow = new Point2D(old.X + old.Components.Min.X, old.Y + old.Components.Min.Y);
        var oldHigh = new Point2D(old.X + old.Components.Max.X, old.Y + old.Components.Max.Y);
        var merchant = BotShelf.Of(old);

        if (merchant is { Deleted: false })
        {
            merchant.House = null;
            merchant.Internalize();
        }

        old.Delete();
        BotPlot.Spend();

        var chosen = Point3D.Zero;
        var own = false;
        List<IEntity> moving = null;
        var asked = 0;

        for (var i = 0; i < candidates.Count && asked < BotBounds.ShiftChecks; i++)
        {
            var (at, inOwn) = candidates[i];

            asked++;

            if (HousePlacement.Check(body, kind.Multi, at, out var toMove, Direction.South) != HousePlacementResult.Valid)
            {
                continue;
            }

            // Things lying there may only be the old hall's own, released when it came down; anybody else's is not a building site.
            var foreign = false;

            for (var j = 0; j < toMove.Count && !foreign; j++)
            {
                foreign = toMove[j] is Item item
                          && (item.X < oldLow.X || item.X > oldHigh.X || item.Y < oldLow.Y || item.Y > oldHigh.Y);
            }

            if (foreign)
            {
                continue;
            }

            chosen = at;
            own = inOwn;
            moving = toMove;

            break;
        }

        var shifted = chosen != Point3D.Zero;
        var plot = shifted ? chosen : _hall;
        var house = kind.Make(body);

        if (house.Deleted)
        {
            if (merchant is { Deleted: false })
            {
                merchant.MoveToWorld(_hall, _map);
            }

            BotBounds.ShiftFailed(_guild.Name, "the hall would not be made again");

            return BotDoing.Failed("the hall would not be made again");
        }

        house.Price = price;
        house.MoveToWorld(plot, _map);

        for (var i = 0; moving != null && i < moving.Count; i++)
        {
            if (moving[i] is Mobile mobile)
            {
                mobile.Location = house.BanLocation;
            }
            else if (moving[i] is Item item)
            {
                item.Location = house.BanLocation;
            }
        }

        BotEstate.Take(_guild, house);

        var fitted = BotFittings.Furnish(house, _guild);

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

        if (!shifted)
        {
            BotBounds.ShiftFailed(_guild.Name, $"none of {asked} plots on its own ground would take it ({candidates.Count} passed our own tests); it stands where it stood");

            return BotDoing.Failed($"none of {asked} plots on its own ground would take the hall; it went up again where it stood");
        }

        BotBounds.Shifted(_guild.Name, _hall, plot, own);

        // The guild lives here now: the seat follows the hall, or the seat would carry it back.
        BotSeat.Set(_guild.Name, plot, false);

        return BotDoing.Done($"the hall of {_guild.Name} now stands at {plot.X},{plot.Y}, {(own ? "within its own square" : "on another square it holds")}, {fitted} things inside");
    }
}
