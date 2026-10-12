using System;

namespace Server.BotAI.V2;

/// <summary>
/// A crafter's walk to its own bank box for the material of an order it passed over, before it goes digging for more.
///
/// <para>
/// <b>Patrick, 03.10.2026: "a crafter makes the order first, then goes to mine" (flow9's proposal 4).</b> The porter puts a
/// crafter's ingots, leather and feathers in its bank box once its pack holds its keep (<c>BotUnload</c>), and every trade took
/// them back out only when the bot happened to stand at a bank (<see cref="BotMarket.FromBank"/>). A smith on build 396 that
/// passed a chain order over for want of iron walked to a seam for twenty more ("42 ingots away and 0 kept for somebody's
/// want") with a box of its own iron in Britain, 6,746 times "passed one over for want of iron" in eight hours. This walks to the
/// nearest bank and takes what the order lacks; the craft's own look then takes the order. Offered by <see cref="BotCraftFirst"/>.
/// </para>
///
/// <para>
/// A purchase-shaped errand: it makes nothing and costs nothing, so it is unpaid and claimed at <see cref="BotCraftFirst.DrawClaim"/>,
/// the same reasoning as the spare tool's trip (<c>BotRestock.Spare</c>): the one errand that comes before the next job.
/// </para>
/// </summary>
public sealed class BotDraw : BotDeed
{
    /// <summary>The ledger's word, and the word a bank nobody could reach is cautioned under.</summary>
    public const string Trade = "draw";

    /// <summary>How near the banker the walk ends: well inside <see cref="BotMarket.BankReach"/>.</summary>
    public static int Near { get; set; } = 4;

    private readonly Map _map;

    private readonly Point3D _bank;

    private readonly Type _kind;

    private readonly int _units;

    private int _taken;

    public BotDraw(Map map, Point3D bank, Type kind, int units)
    {
        _map = map;
        _bank = bank;
        _kind = kind;
        _units = Math.Max(1, units);
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _bank;

    public override double Expects => BotCraftFirst.DrawClaim;

    public override double Minutes => 0.5;

    public override double Coin => 0.0;

    public override int Made => 0;

    /// <summary>Its own goods moved from the box to the pack: nothing made, nothing paid. See <see cref="BotDeed.Unpaid"/>.</summary>
    public override bool Unpaid => true;

    /// <summary>Put down for a flight and taken up again: the order still waits on the material.</summary>
    public override bool Steadfast => true;

    public override string Stage =>
        _taken > 0 ? $"took {_taken} {_kind?.Name} out of its bank box" : $"to the bank for its own {_units} {_kind?.Name}";

    /// <summary>Counted once, when the walk wins the bot.</summary>
    public override void Taken(IBotWilful bot) => BotCraftFirst.Walked();

    /// <summary>The bank could not be reached: written under this errand's word at the bank, which is what the offer asks.</summary>
    public override bool Bend(IBotWilful bot)
    {
        bot?.Resolve?.Ledger?.Beware(Trade, _map, _bank);

        return false;
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _kind == null)
        {
            return BotDoing.Failed("no body");
        }

        if (!BotMarket.AtBank(body))
        {
            return BotDoing.Walk(_map, _bank, BotArrival.Within(Near), $"to the bank for its own {_units} {_kind.Name}");
        }

        _taken = BotMarket.FromBank(bot, _kind, _units);

        // The memory is served either way (the review of this build): a box the pack could not take from (a full pack, a stack
        // the weight refuses) is not walked to again until the craft passes an order over again. See BotCraftFirst.Served.
        BotCraftFirst.Served(body, _taken);

        if (_taken <= 0)
        {
            return BotDoing.Failed($"its bank box held no {_kind.Name} it could take");
        }

        return BotDoing.Done($"took {_taken} {_kind.Name} out of its bank box for an order it had passed over");
    }
}
