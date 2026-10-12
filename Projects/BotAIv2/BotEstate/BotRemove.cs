using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// Carrying a beaten guild's hall out of the winner's yard and putting it down again.
///
/// <para>
/// <b>The only thing on this shard that takes a building down.</b> The engine has no way to move a house —
/// a player demolishes and places again — so this does exactly that, in that order, and the order matters:
/// the ground is proved first, and the old hall only comes down once the new one has somewhere to go.
/// A guild left with neither is a guild that lost a war and its home, which is not what was asked for.
/// </para>
///
/// <para>
/// <b>What the move costs is the fittings, and that is the whole of the punishment.</b> The old hall's
/// merchant, its counter and everything standing on it go with the building; the new hall is furnished from
/// scratch by <c>BotFittings</c> and its shelf starts empty. No levy is taken — a guild that has just lost a
/// war is not asked for five thousand gold as well, and a punishment that can be refused for want of money
/// is not a punishment.
/// </para>
///
/// <para>
/// <b>Nothing is paid for and nothing is earned</b>, so this is <c>Unpaid</c>. See <c>BotBaron</c> for the
/// twelve minutes a bot once stood still because unpaid work was marked down for being unpaid.
/// </para>
/// </summary>
public sealed class BotRemove : BotDeed
{
    /// <summary>The ledger key.</summary>
    public const string Trade = "remove";

    /// <summary>
    /// What moving the hall is reckoned at per minute before experience corrects it.
    ///
    /// As high as raising one, and for the same reason <c>BotHall.Prior</c> is: it happens perhaps once in
    /// the life of an island, the conditions are already as narrow as they get, and an errand that is rare
    /// by its conditions and also loses every auction never runs at all.
    /// </summary>
    public static double Prior { get; set; } = 400.0;

    /// <summary>How long the whole thing is expected to take.</summary>
    public static double WorkMinutes { get; set; } = 2.0;

    /// <summary>How near the plot the bot must stand to build.</summary>
    public static int Reach => BotHall.Reach;

    /// <summary>Halls taken down and put up again.</summary>
    public static long Done { get; private set; }

    /// <summary>Times the ground would not take the hall after all.</summary>
    public static long Refused { get; private set; }

    /// <summary>Halls carried to their guild's seat rather than out of a winner's yard. See <c>BotSeat</c>.</summary>
    public static long Seated { get; private set; }

    /// <summary>Merchants carried along with their hall, stock and all.</summary>
    public static long MerchantsCarried { get; private set; }

    private readonly Guild _guild;

    private readonly Map _map;

    private readonly Point3D _plot;

    /// <summary>Whether this is a move to the seat rather than a sentence. The two are the same walk.</summary>
    private readonly bool _seating;

    /// <summary>
    /// Whether this is a move off another guild's held ground (06.10.2026, <see cref="BotBounds"/>): the same walk again, to the
    /// nearest ground of the guild's own or open ground, and the seat follows the hall.
    /// </summary>
    private readonly bool _off;

    /// <summary>Halls carried off another guild's ground. See <see cref="BotBounds"/>.</summary>
    public static long Off { get; private set; }

    private int _fitted;

    public BotRemove(Guild guild, Map map, Point3D plot) : this(guild, map, plot, false)
    {
    }

    public BotRemove(Guild guild, Map map, Point3D plot, bool seating) : this(guild, map, plot, seating, false)
    {
    }

    public BotRemove(Guild guild, Map map, Point3D plot, bool seating, bool off)
    {
        _guild = guild;
        _map = map;
        _plot = plot;
        _seating = seating && !off;
        _off = off;
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

    public override string Stage =>
        _off
            ? $"carrying the hall of {_guild?.Name} off another guild's ground, to {_plot.X},{_plot.Y}"
            : _seating
                ? $"carrying the hall of {_guild?.Name} to its seat, to {_plot.X},{_plot.Y}"
                : $"carrying the hall of {_guild?.Name} out to {_plot.X},{_plot.Y}";

    public override bool Bend(IBotWilful bot) => false;

    public override void Drop(IBotWilful bot) => BotOffice.Release(BotExile.Office, _guild);

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _guild == null)
        {
            return BotDoing.Failed("no body");
        }

        // The debt may have been settled or forgiven while this bot was walking - the winner's hall can
        // burn down, and then there is nothing to be outside of. A seat, likewise, may have been moved
        // back under the hall by a hand at the keyboard.
        if (_off ? !BotBounds.Owes(_guild, out _, out _) : _seating ? !BotSeat.Moving(_guild, out _, out _) : BotExile.Owed(_guild) is not { Deleted: false })
        {
            return BotDoing.Failed($"{_guild.Name} owes nobody a move any more");
        }

        var old = BotEstate.Hall(_guild);

        if (old is not { Deleted: false })
        {
            return BotDoing.Failed($"{_guild.Name} has no hall to move");
        }

        BotOffice.Hold(BotExile.Office, _guild, BotExile.ClaimMs);

        if (!body.InRange(_plot, Reach))
        {
            return BotDoing.Walk(_map, _plot, BotArrival.Within(Reach), $"to the new ground at {_plot.X},{_plot.Y}");
        }

        // Proved before anything is knocked down. HousePlacement refuses a plot that the old hall itself
        // stands on or beside, which cannot happen here - the whole point is that it is far away - but it
        // refuses plenty else, and finding out after the demolition would leave the guild homeless.
        // A hall is carried at its own size: a guild that grew into a large house does not come back from a lost war in a
        // small one. See BotHallKind.
        var kind = BotHallKind.Of(old);

        // Ground that changed hands while the bot walked: not carried onto another guild's. See BotBounds.
        if (BotBounds.Refuse(BotBounds.Placing.Carry, _guild.Name, _map, _plot, kind.Multi, out var foreign))
        {
            BotPlot.Spend();
            Refused++;

            return BotDoing.Failed(foreign);
        }

        var result = HousePlacement.Check(body, kind.Multi, _plot, out var toMove, Direction.South);

        if (result != HousePlacementResult.Valid)
        {
            BotPlot.Spend();
            Refused++;

            return BotDoing.Failed($"the new ground would not take the hall: {result}");
        }

        for (var i = 0; i < toMove.Count; i++)
        {
            if (toMove[i] is Item)
            {
                Refused++;

                return BotDoing.Failed("there are things lying on the new ground");
            }
        }

        var price = old.Price;

        // <b>The merchant comes too, stock and all, and it is taken off the old hall's roll before the hall
        // falls.</b> A guild carried to its seat by order has lost no war, and its counter is the one thing
        // in the hall worth anything — 64,000gp across four of them on 13.09.2026. The engine keeps a vendor
        // on its house's list and a house on the vendor; setting the house to nothing takes it off the roll,
        // and setting the new house puts it on that one. The one thing a homeless vendor does on its own is
        // dismiss itself on deserialisation, which cannot happen between here and the next line.
        var merchant = BotShelf.Of(old);

        if (merchant is { Deleted: false })
        {
            merchant.House = null;
            merchant.Internalize();
        }

        // Down it comes. Everything else inside goes with it - see the note at the top on what a lost war
        // costs.
        old.Delete();

        BotPlot.Spend();

        var house = kind.Make(body);

        if (house.Deleted)
        {
            Refused++;

            // Not lost with the hall: put down where the hall was, on the ground, and re-homed by the engine
            // if the hall goes up again there. A vendor in the open dismisses itself; that is the engine's
            // rule and the whole loss of this branch.
            if (merchant is { Deleted: false })
            {
                merchant.MoveToWorld(old.Location, _map);
            }

            return BotDoing.Failed("the hall would not go up on the new ground");
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

        _fitted = BotFittings.Furnish(house, _guild);

        if (merchant is { Deleted: false })
        {
            var spot = BotFittings.Spot(house);

            merchant.House = house;
            merchant.MoveToWorld(spot == Point3D.Zero ? house.BanLocation : spot, _map);

            if (_guild.Leader is BotMobile keeper)
            {
                merchant.Owner = keeper;
            }

            MerchantsCarried++;
            _fitted++;
        }

        BotEstate.Fit(_fitted);
        BotEstate.Register(_guild, house, false);

        if (_off)
        {
            Off++;
            BotBounds.Paid(_guild.Name, _plot);

            // The guild lives here now, as after an exile: the seat follows the hall, or it would carry the hall back.
            BotSeat.Set(_guild.Name, _plot, false);
        }
        else if (_seating)
        {
            BotSeat.Moved(_guild.Name, true);
            Seated++;
        }
        else
        {
            BotExile.Paid(_guild);

            // The guild lives here now. See BotSeat.Set: without this the seat would pull the hall straight
            // back into the yard it was just carried out of.
            BotSeat.Set(_guild.Name, _plot, false);
        }

        Done++;

        return BotDoing.Done(
            _off
                ? $"the hall of {_guild.Name} now stands at {_plot.X},{_plot.Y}, off another guild's ground, {_fitted} things inside"
                : _seating
                    ? $"the hall of {_guild.Name} now stands at {_plot.X},{_plot.Y}, by its seat, {_fitted} things inside"
                    : $"the hall of {_guild.Name} now stands at {_plot.X},{_plot.Y}, out of the winner's yard, {_fitted} things inside"
        );
    }

    public static string Describe() =>
        Done + Refused == 0
            ? "no hall has been moved"
            : $"{Done} halls moved ({Seated} of them to their seat, {Off} off another guild's ground, {MerchantsCarried} merchants carried along), {Refused} refused by the new ground";

    public static void Forget()
    {
        Done = 0;
        Refused = 0;
        Seated = 0;
        Off = 0;
        MerchantsCarried = 0;
    }
}

/// <summary>
/// Offers a member of a beaten guild the work of moving its hall.
///
/// <para>
/// Offered to whoever of the guild is choosing work, not to its leader, for the reason <c>BotSteward</c>
/// carries at length: a guild whose leader is in a company is a guild that can never build anything. The
/// guild's own claim keeps it to one member at a time.
/// </para>
///
/// <para>
/// <b>The ground is looked for before anybody is offered anything</b>, and it is looked for with the
/// winner's hall shunned — see <c>BotPlot.Find</c>'s four-argument form. An offer made before the ground
/// exists is an errand that walks across the island to discover there is nowhere to go.
/// </para>
/// </summary>
public sealed class BotRemover : IBotProposer
{
    /// <summary>Bots looked at.</summary>
    public static long Asked { get; private set; }

    /// <summary>Removals offered.</summary>
    public static long Offered { get; private set; }

    /// <summary>Times the guild owed nobody a move, which is nearly always.</summary>
    public static long Settled { get; private set; }

    /// <summary>Times another member was already on it.</summary>
    public static long Claimed { get; private set; }

    /// <summary>Times there was no ground outside the winner's yard to be had.</summary>
    public static long Groundless { get; private set; }

    /// <summary>Times the hall stood too far from its seat and somebody was sent to carry it. See <c>BotSeat</c>.</summary>
    public static long Seating { get; private set; }

    /// <summary>Times the nearest ground to the seat was no nearer than where the hall already stands.</summary>
    public static long NoNearer { get; private set; }

    /// <summary>Times somebody was sent to carry a hall off another guild's held ground. See <see cref="BotBounds"/>.</summary>
    public static long Foreign { get; private set; }

    /// <summary>Times somebody was sent to shift a hall off another guild's ground onto the guild's own. See <see cref="BotShift"/>.</summary>
    public static long Shifts { get; private set; }

    /// <summary>
    /// How much further from the winner's hall a beaten guild's new hall must stand than its old one, in tiles (the larger of the
    /// two offsets, as the yard is measured). One quadrant.
    /// </summary>
    public static int FartherBy { get; set; } = 30;

    public string Name => "remover";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotEstate.Running || !BotExile.Running)
        {
            return null;
        }

        if (bot?.Self is not BotMobile { Deleted: false, Alive: true } body || body.Map == null ||
            body.Map == Map.Internal)
        {
            return null;
        }

        if (body.Guild is not Guild ours)
        {
            return null;
        }

        Asked++;

        var winner = BotExile.Owed(ours);

        // A sentence first, a seat second: the two are the same errand, and a guild that owes both walks
        // once. See BotSeat.Moving for the seat's half of the question.
        if (winner == null)
        {
            // <b>A hall on another guild's held ground before a hall far from its seat</b> (06.10.2026). First shifted within its own
            // square or onto the guild's other squares near it, the standing hall's tiles free (BotShift; build 410: the spiral below
            // never offers ground nearer than BotPlot.Near and the engine counted the standing hall in the way, so The Ash went 112
            // tiles off). Then open ground within BotBounds.OpenReach, looked for from where the hall stands; then it stands.
            if (BotBounds.Owes(ours, out var under, out var whose))
            {
                if (BotOffice.Busy(BotExile.Office, ours))
                {
                    Claimed++;

                    return null;
                }

                if (BotBounds.Shifting(ours.Name))
                {
                    Offered++;
                    Shifts++;
                    BotOffice.Offering(BotExile.Office, ours);

                    return new BotShift(ours, under.Map, under.Location);
                }

                if (!BotPlot.Find(body, under.Location, Point3D.Zero, 0, BotHallKind.Of(under).Multi, under, out var off))
                {
                    Groundless++;
                    BotBounds.Missed(ours.Name, whose, false);

                    return null;
                }

                if (BotSeat.Gap(off, under.Location) > BotBounds.OpenReach)
                {
                    Groundless++;
                    BotBounds.Missed(ours.Name, whose, true);

                    return null;
                }

                Offered++;
                Foreign++;
                BotBounds.Offered();
                BotOffice.Offering(BotExile.Office, ours);

                return new BotRemove(ours, BotPopulation.Home, off, false, true);
            }

            if (!BotSeat.Moving(ours, out var hall, out var seat))
            {
                Settled++;

                return null;
            }

            if (BotOffice.Busy(BotExile.Office, ours))
            {
                Claimed++;

                return null;
            }

            if (!BotPlot.Find(body, seat, Point3D.Zero, 0, BotHallKind.Of(hall).Multi, hall, out var ground))
            {
                Groundless++;

                return null;
            }

            // Only if it is actually nearer. Scarce ground can put the nearest plot to a seat further from
            // it than the hall already is, and carrying a hall to be no nearer is an evening of moving
            // house for nothing. The guild rests either way, so this is asked once a half hour.
            if (BotSeat.Gap(ground, seat) >= BotSeat.Gap(hall.Location, seat))
            {
                NoNearer++;
                BotSeat.Moved(ours.Name, false);

                return null;
            }

            Seating++;
            Offered++;
            BotOffice.Offering(BotExile.Office, ours);

            return new BotRemove(ours, BotPopulation.Home, ground, true);
        }

        if (BotOffice.Busy(BotExile.Office, ours))
        {
            Claimed++;

            return null;
        }

        // Looked for from the loser's own seat, shunning the winner's hall — so a beaten guild goes home,
        // not merely away.
        //
        // <b>And further from the winner than it stood, not merely out of its yard.</b> Patrick's order of 10.09.2026 was "further
        // from the winner's house". Kept out of the yard alone, the plot nearest the seat can lie towards the winner — and the seat
        // follows the hall — so on 26.09.2026 The Lantern, beaten by The Blade, was carried 163 → 121 → 109 tiles from The Blade's
        // hall in two moves and went back to war over the ground by its new door each time (10:06, 10:22, 10:37).
        var losing = BotEstate.Hall(ours);
        var clear = BotExile.Clear;

        if (losing != null)
        {
            var stood = Math.Max(Math.Abs(losing.X - winner.X), Math.Abs(losing.Y - winner.Y));

            clear = Math.Max(clear, stood + FartherBy);
        }

        if (!BotPlot.Find(body, BotSeat.Of(ours), winner.Location, clear, BotHallKind.Of(losing).Multi, losing, out var plot))
        {
            Groundless++;

            return null;
        }

        Offered++;
        BotOffice.Offering(BotExile.Office, ours);

        return new BotRemove(ours, BotPopulation.Home, plot);
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been looked at for a removal"
            : $"the remover looked {Asked} times and sent {Offered} ({Seating} of them to carry a hall to its seat, {Foreign} off another guild's ground to open ground, {Shifts} to shift one onto its own): {Settled} owed no move, {Claimed} already had somebody on it, "
            + $"{Groundless} found no ground outside the winner's yard or by the seat, {NoNearer} found none nearer the seat than the hall already is; {BotRemove.Describe()}";

    public static void Forget()
    {
        Asked = 0;
        Offered = 0;
        Settled = 0;
        Claimed = 0;
        Groundless = 0;
        Seating = 0;
        NoNearer = 0;
        Foreign = 0;
        Shifts = 0;
    }
}
