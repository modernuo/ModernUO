using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The fence's run out to the chest for the band's goods.
///
/// <para>
/// <b>The other half of Patrick's keeper.</b> "Who looks after the band's goods and orders." The thieves carry their
/// takings out to the chest (<c>BotStash</c>) and cannot go a step further, because every counter and every shopkeeper
/// on this island stands inside a guarded town and a red is turned back at the wards. The fence can walk in as itself.
/// So this deed is only the carrying: it fills the fence's pack from the chest and ends, and the island's ordinary
/// machinery — the peddler's walk to a shopkeeper, the stalls, the market — turns the goods into coin without knowing
/// where they came from. The coin goes back to the chest by the same sweep that sent the thieves' takings out.
/// </para>
/// </summary>
public sealed class BotFetch : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotFetch));

    public const string Trade = "fetch";

    /// <summary>The fewest things in the chest worth the walk.</summary>
    public static int Least { get; set; } = 3;

    /// <summary>The most things carried away in one run.</summary>
    public static int Armful { get; set; } = 20;

    /// <summary>What the run is held at against other work, a minute.</summary>
    public static double Prior { get; set; } = 300.0;

    /// <summary>How often the fence is looked over for a run.</summary>
    public static int EveryMs { get; set; } = 300000;

    /// <summary>Runs begun.</summary>
    public static long Runs { get; private set; }

    /// <summary>Runs that carried something away, and what they came to.</summary>
    public static long Done { get; private set; }

    public static long Carried { get; private set; }

    /// <summary>Runs that found the chest gone or empty by the time they arrived.</summary>
    public static long Empty { get; private set; }

    /// <summary>Things drawn out of the chest by a thief for its own kit.</summary>
    public static long Drawn { get; private set; }

    private readonly Map _map;

    private readonly bool _kit;

    private Point3D _at;

    private bool _counted;

    /// <param name="map">Where.</param>
    /// <param name="at">The chest.</param>
    /// <param name="kit">
    /// True when the walk is a thief drawing its own kit rather than the fence carrying the goods to market.
    ///
    /// <para>
    /// <b>A red cannot shop.</b> The towns are shut to it (build 111b) and every shopkeeper on the island stands inside
    /// one, so a thief out of bandages, arrows or reagents has nowhere to go and slowly stops working. The chest is the
    /// one counter it can reach, and what the band stashed there is what it lives on.
    /// </para>
    /// </param>
    public BotFetch(Map map, Point3D at, bool kit = false)
    {
        _map = map;
        _at = at;
        _kit = kit;
    }

    public override string Kind => Trade;

    public override bool Committed => true;

    public override bool Unpaid => true;

    public override bool Steadfast => true;

    public override bool Repeats(BotDeed other) => other is BotFetch;

    public override Map Map => _map;

    public override Point3D Where => _at;

    public override double Expects => Prior;

    public override double Minutes => 3.0;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override int Made => 0;

    public override string Stage => _kit ? "out to the band's chest for its kit" : "out to the band's chest for the goods";

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
            Runs++;
        }

        if (BotLair.Chest is not { } chest || chest.Map != _map)
        {
            Empty++;

            return BotDoing.Failed("the band has no chest");
        }

        _at = chest.GetWorldLocation();

        // Beside the chest, never onto it: a chest on the ground is impassable and a walk aimed at its own tile finds
        // no way in. See BotLair.Doorstep.
        if (!body.InRange(_at, 2))
        {
            var step = BotLair.Doorstep();

            return BotDoing.Walk(_map, step == Point3D.Zero ? _at : step, BotArrival.Within(1), "out to the band's chest");
        }

        var pack = body.Backpack;

        if (pack == null)
        {
            return BotDoing.Failed("no pack");
        }

        List<Item> taking = [];
        var kept = _kit ? BotUnload.Keeps(bot) : null;

        for (var i = 0; i < chest.Items.Count && taking.Count < Armful; i++)
        {
            // The coin stays where it is: it is the band's, and the fence is carrying goods to market, not a purse.
            if (chest.Items[i] is not { Deleted: false, Movable: true } item || item is Gold)
            {
                continue;
            }

            // A thief takes only what its trade asks for and only while it is short of it; the fence takes the rest.
            if (kept != null && (!kept.TryGetValue(item.GetType(), out var wants) || pack.GetAmount(item.GetType()) >= wants))
            {
                continue;
            }

            taking.Add(item);
        }

        // <b>The band pays for its own keeper's silence.</b> A witness that catches the fence with a thief can be
        // bought off on the spot, out of the pack — and the pack is exactly where a bot's money is not, because the
        // unload machinery banks everything above the float. The one moment the keeper stands over the band's own coin
        // is this one. Patrick's word of 17.09.2026: "they may buy the witness off, or kill it."
        var drew = 0;

        if (!_kit && BotFence.Is(body))
        {
            var short_ = Math.Max(0, BotFence.Most - pack.GetAmount(typeof(Gold)));
            var take = Math.Min(short_, chest.GetAmount(typeof(Gold)));

            if (take > 0 && chest.ConsumeTotal(typeof(Gold), take))
            {
                body.AddToBackpack(new Gold(take));
                BotFence.Drew(take);
                drew = take;
            }
        }

        var took = 0;

        for (var i = 0; i < taking.Count; i++)
        {
            if (pack.TryDropItem(body, taking[i], false))
            {
                took++;
            }
        }

        Carried += took;

        if (_kit)
        {
            Drawn += took;
        }

        if (took <= 0)
        {
            Empty++;

            return BotDoing.Done(drew > 0 ? $"{drew}gp for the witnesses" : "nothing in the chest worth carrying");
        }

        Done++;

        logger.Information(
            "{Name} took {Things} things out of the band's chest {Why}{Coin}",
            body.Name,
            took,
            _kit ? "for its own kit" : "to sell",
            drew > 0 ? $", and {drew}gp to keep witnesses quiet with" : ""
        );

        return BotDoing.Done($"{took} things out of the chest");
    }

    public static string Describe() =>
        $"{Runs} runs to the chest: {Done} carried {Carried} things away ({Drawn} of them a thief's own kit), {Empty} found nothing";

    public static void Forget()
    {
        Runs = 0;
        Done = 0;
        Carried = 0;
        Drawn = 0;
        Empty = 0;
    }
}
