using System.Collections.Generic;
using Server.Guilds;
using Server.Mobiles;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// Raising the guild's hall: walk to the plot, call the levy, put the house up.
///
/// <para>
/// <b>The engine's own order of business, kept exactly.</b> <c>HousePlacementTool</c> checks the ground,
/// builds the house, takes the money and only then moves it into the world — and deletes it again if the
/// payment fails. Every step of that ordering is there to make sure a house and the coin for it cannot both
/// exist, and this follows it rather than inventing a shorter version.
/// </para>
///
/// <para>
/// <b>The money is the guild's, not the bot's.</b> That is why <see cref="Outlay"/> is nothing: it is the
/// number the decision layer measures a bot's own poverty against, and a buyer made to feel five thousand
/// gold short would refuse every errand on the shard until it stopped. What can be afforded is the
/// proposer's question, asked of <see cref="BotEstate.Fund"/>, and it is asked before the offer is made.
/// </para>
/// </summary>
public sealed class BotHall : BotDeed
{
    /// <summary>The ledger key.</summary>
    public const string Trade = "hall";

    /// <summary>
    /// What raising a hall is reckoned at per minute.
    ///
    /// <para>
    /// <b>Four hundred, and the first ninety were measured wrong.</b> The reasoning behind ninety was that a
    /// hall need only beat walking the town, which is what a Baron or a Captain would otherwise be doing.
    /// What actually happens is that the offer arrives while its bot is brewing, cooking or carrying a full
    /// pack — work worth two or three hundred a minute — and the floor that protects work already in hand
    /// does the rest: offered seven times in twelve minutes at ninety, taken none. At four hundred it was
    /// taken within the minute and the Blade's hall went up.
    /// </para>
    ///
    /// <para>
    /// High is honest here rather than greedy. A hall is bought once by each guild and stands for the life
    /// of the island; the ten minutes of digging it displaces are not the comparison.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 400.0;

    /// <summary>How long the raising itself takes once the bot is standing there.</summary>
    public static double WorkMinutes { get; set; } = 1.0;

    /// <summary>How near the plot the bot must be. Outside the footprint, in sight of it.</summary>
    public static int Reach { get; set; } = 5;

    private readonly Map _map;

    private readonly Point3D _plot;

    private readonly Guild _guild;

    private int _paid;

    private int _fitted;

    public BotHall(Guild guild, Map map, Point3D plot)
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

    /// <summary>Nothing out of this bot's own pocket. See the note on the class.</summary>
    public override int Outlay => 0;

    /// <summary>Not a coin comes back, and none should: the guild has bought a building.</summary>
    public override double Coin => 0.0;

    /// <summary>
    /// Not about money, and it must not be refused for failing to earn any.
    ///
    /// The Baron's rounds needed this flag and got it after standing still for twelve minutes; a hall is the
    /// second piece of work on this shard whose whole point is something other than takings.
    /// </summary>
    public override bool Unpaid => true;

    /// <summary>
    /// What the bot's own purse put into this, handed straight back as takings.
    ///
    /// The levy comes partly out of the acting bot's own pocket, and takings are measured as the change in
    /// that pocket — so without this the ledger recorded "raised The Lantern for 5000gp: -20 in 0.4 min
    /// (-50/min)" and <c>BotCommons.Corrected</c> dragged the trade's estimate down after it. Goods are
    /// worth what they cost, which is the rule <c>BotRestock</c> has always stated; a hall and a bench are
    /// goods. See the long note in <c>BotSupply.Made</c>.
    /// </summary>
    public override int Made => _mine;

    /// <summary>Coin out of this bot's own purse, measured across the levy rather than assumed.</summary>
    private int _mine;


    public override string Stage =>
        _paid > 0 ? $"raised {_guild?.Name} for {_paid}gp" : $"to the plot at {_plot.X},{_plot.Y}";

    /// <summary>
    /// The way to the plot turned out not to exist.
    ///
    /// Giving up is right, and it costs nothing: the plot search keeps its own place in the spiral and has
    /// already moved past this one, so the next offer is about different ground rather than the same
    /// unreachable field again.
    /// </summary>
    public override bool Bend(IBotWilful bot) => false;

    /// <summary>
    /// Whatever happened, the guild is no longer waiting on this bot. See <c>BotSteward.Release</c>.
    /// </summary>
    public override void Drop(IBotWilful bot) => BotSteward.Release(_guild);

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _guild == null)
        {
            return BotDoing.Failed("no body");
        }

        if (BotEstate.Hall(_guild) != null)
        {
            return BotDoing.Failed($"{_guild.Name} already has a hall");
        }

        // Still on it. Said every beat, because the guild's claim is a timeout on being heard from rather
        // than a deadline: a walk across the island is longer than any deadline worth setting.
        BotSteward.Hold(_guild);

        if (!body.InRange(_plot, Reach))
        {
            return BotDoing.Walk(_map, _plot, BotArrival.Within(Reach), $"to the plot at {_plot.X},{_plot.Y}");
        }

        // Ground that changed hands while the bot walked: not raised on another guild's. See BotBounds.
        if (BotBounds.Refuse(BotBounds.Placing.Hall, _guild.Name, _map, _plot, BotPlot.MultiID, out var foreign))
        {
            BotPlot.Spend();
            BotEstate.Lost();

            return BotDoing.Failed(foreign);
        }

        var result = HousePlacement.Check(body, BotPlot.MultiID, _plot, out var toMove, Direction.South);

        if (result != HousePlacementResult.Valid)
        {
            // Whatever was remembered about this spot is wrong now, so it is forgotten rather than handed
            // to the next bot that asks — which would be an errand that walks across the island to fail.
            BotPlot.Spend();
            BotEstate.Lost();

            return BotDoing.Failed($"the ground would not take a hall: {result}");
        }

        // Mobiles in the way are the engine's business — it puts them outside by the sign, and one of them
        // is very likely this bot. Items are not: shoving somebody's ore pile under a wall to make room is
        // not something this population is entitled to do.
        for (var i = 0; i < toMove.Count; i++)
        {
            if (toMove[i] is Item)
            {
                BotEstate.Lost();

                return BotDoing.Failed("there are things lying on the plot");
            }
        }

        var paid = new List<BotEstate.Contribution>();
        var mine = BotYield.Wealth(body);
        var got = BotEstate.Levy(_guild, BotEstate.Price, paid, body);

        // This bot's own share of the levy, handed back as takings. See Made.
        _mine += System.Math.Max(0, mine - BotYield.Wealth(body));

        if (got < BotEstate.Price)
        {
            BotEstate.Refund(paid, body);
            BotEstate.Lost();

            return BotDoing.Failed($"{_guild.Name} could only raise {got} of {BotEstate.Price}gp");
        }

        BotPlot.Spend();

        // The smallest size; a guild moves into a bigger one as it fills it. See BotHallKind and BotEnlarge.
        var house = BotHallKind.First.Make(body);

        if (house.Deleted)
        {
            BotEstate.Refund(paid, body);
            BotEstate.Lost();

            return BotDoing.Failed("the house would not be built");
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

        // Owner, co-owners, sign, open doors, decay refreshed: everything that makes it the guild's rather
        // than this one bot's, and everything that lets the next population find it again.
        BotEstate.Take(_guild, house);

        _fitted = BotFittings.Furnish(house, _guild);
        BotEstate.Fit(_fitted);
        BotEstate.Register(_guild, house, true);

        // Where the guild lives now: its members rise here, and its company musters here. See BotSeat.Set.
        BotSeat.Set(_guild.Name, house.Location, false);

        BotVoice.ToGuild(_guild.Name, "hall", null);

        _paid = got;

        return BotDoing.Done(
            $"raised the hall of {_guild.Name} at {_plot.X},{_plot.Y} for {got}gp off {paid.Count} members, {_fitted} things inside"
        );
    }
}
