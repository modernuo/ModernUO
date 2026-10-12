using System.Collections.Generic;
using Server.Guilds;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// Buying one workbench for the guild's hall and setting it up.
///
/// <para>
/// <b>Patrick's order of 08.09.2026: the tools are bought, not given.</b> The halls used to come furnished,
/// which made a hall a single decision and then nothing. A guild that must buy its forge, then its anvil,
/// then its loom has something to spend on for the rest of the evening — and this shard's oldest economic
/// complaint is that its money has almost nowhere to go: a captain's till reached ninety-eight thousand gold
/// doing nothing at all.
/// </para>
///
/// <para>
/// Same levy as the hall itself, same order of business: check, take the money, set the thing down, and hand
/// the money back if it will not fit. What is different is that it is small and repeatable, so it is the
/// first piece of work on this shard a guild does over and over as it gets richer.
/// </para>
/// </summary>
public sealed class BotBench : BotDeed
{
    /// <summary>The ledger key.</summary>
    public const string Trade = "fit";

    /// <summary>
    /// What fitting out a hall is reckoned at per minute.
    ///
    /// <para>
    /// Below a hall's four hundred and well above the ninety a hall was first given — that ninety was offered
    /// seven times in twelve minutes and taken none, because an offer arrives while its bot is already at
    /// work worth two or three hundred a minute. A bench is a smaller thing than a hall and this says so,
    /// but it still has to beat cooking.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 300.0;

    /// <summary>How long setting one up takes once the bot is standing there.</summary>
    public static double WorkMinutes { get; set; } = 1.0;

    /// <summary>How near the hall the bot must be to set it down.</summary>
    public static int Reach { get; set; } = 4;

    private readonly Guild _guild;

    private readonly BaseHouse _hall;

    private readonly BotFittings.Bench _bench;

    private int _paid;

    public BotBench(Guild guild, BaseHouse hall, BotFittings.Bench bench)
    {
        _guild = guild;
        _hall = hall;
        _bench = bench;
    }

    public override string Kind => Trade;

    public override Map Map => _hall?.Map;

    public override Point3D Where => _hall?.BanLocation ?? Point3D.Zero;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    /// <summary>Nothing out of this bot's own pocket: the guild is buying. See <see cref="BotHall.Outlay"/>.</summary>
    public override int Outlay => 0;

    public override double Coin => 0.0;

    /// <summary>Not about money, and not to be refused for failing to earn any.</summary>
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
        _paid > 0 ? $"set {_bench.Name} up for {_paid}gp" : $"to the hall of {_guild?.Name} for {_bench.Name}";

    public override bool Bend(IBotWilful bot) => false;

    /// <summary>Whatever happened, the guild is no longer waiting on this bot.</summary>
    public override void Drop(IBotWilful bot) => BotFitter.Release(_guild);

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _guild == null || _hall is not { Deleted: false })
        {
            return BotDoing.Failed("the hall is gone");
        }

        BotFitter.Hold(_guild);

        if (!body.InRange(_hall.BanLocation, Reach))
        {
            return BotDoing.Walk(
                _hall.Map,
                _hall.BanLocation,
                BotArrival.Within(Reach),
                $"to the hall for {_bench.Name}"
            );
        }

        // Somebody else may have got here first — the claim is a timeout, not a lock.
        if (!BotFittings.Wanting(_hall, _guild, out var wanted) || wanted.Kind != _bench.Kind)
        {
            return BotDoing.Failed($"{_guild.Name} already has {_bench.Name}");
        }

        var paid = new List<BotEstate.Contribution>();
        var mine = BotYield.Wealth(body);
        var got = BotEstate.Levy(_guild, _bench.Price, paid, body);

        // This bot's own share of the levy, handed back as takings. See Made.
        _mine += System.Math.Max(0, mine - BotYield.Wealth(body));

        if (got < _bench.Price)
        {
            BotEstate.Refund(paid, body);

            return BotDoing.Failed($"{_guild.Name} could only raise {got} of {_bench.Price}gp for {_bench.Name}");
        }

        if (!BotFittings.Install(_hall, _bench))
        {
            // The room is full, or every tile of it is taken. The money goes back: a guild that pays for a
            // forge and gets no forge is the one outcome this must never produce.
            BotEstate.Refund(paid, body);

            return BotDoing.Failed($"there is nowhere in the hall to put {_bench.Name}");
        }

        _paid = got;

        return BotDoing.Done($"{_bench.Name} stands in the hall of {_guild.Name}, {got}gp off {paid.Count} members");
    }
}
