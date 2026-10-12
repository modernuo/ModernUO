using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Carrying the takings to the band's chest at the hideout.
///
/// <para>
/// <b>Patrick's order of 17.09.2026, night.</b> "They must keep what they have taken somewhere, since the towns are shut
/// to them and a catch takes everything off them." A thief's coin used to ride in its pack until a patrol took it: the
/// towns are barred to a red (build 111b), the counters are all in towns, and so a robbery's whole profit was a wager on
/// never being caught. Now a member with more than <see cref="Least"/> in its pack walks it out to the chest by the fire
/// and puts down everything it is not carrying for itself.
/// </para>
///
/// <para>
/// Unpaid and claimed at <see cref="Prior"/>, like the band's other work: it earns the thief nothing at the moment it
/// happens, and the auction would price a walk of three hundred tiles with no wage at the bottom of every list.
/// </para>
/// </summary>
public sealed class BotStash : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotStash));

    public const string Trade = "stash";

    /// <summary>The least coin in a member's pack worth the walk out to the hideout.</summary>
    public static int Least { get; set; } = 200;

    /// <summary>What the walk is held at against other work, a minute: the band's own priority, as robbery and practice are.</summary>
    public static double Prior { get; set; } = 350.0;

    /// <summary>How often a member is looked over for a pack worth stashing.</summary>
    public static int EveryMs { get; set; } = 120000;

    /// <summary>Walks begun.</summary>
    public static long Runs { get; private set; }

    /// <summary>Walks that put something down.</summary>
    public static long Done { get; private set; }

    /// <summary>Walks that arrived with nothing worth putting down after all.</summary>
    public static long Empty { get; private set; }

    /// <summary>Walks that found no chest to put anything in.</summary>
    public static long Chestless { get; private set; }

    /// <summary>Coin and things put down in all.</summary>
    public static long Gold { get; private set; }

    public static long Things { get; private set; }

    private readonly Map _map;

    private Point3D _at;

    private bool _counted;

    public BotStash(Map map, Point3D at)
    {
        _map = map;
        _at = at;
    }

    public override string Kind => Trade;

    public override bool Committed => true;

    public override bool Unpaid => true;

    /// <summary>The walk is long and the chest does not move; an interruption should give it back, not throw it away.</summary>
    public override bool Steadfast => true;

    public override bool Repeats(BotDeed other) => other is BotStash;

    /// <summary>The walk is the thief's own and ends at the fire, far outside every town; it goes on foot as a thief does.</summary>
    public override bool Hurries => true;

    public override Map Map => _map;

    public override Point3D Where => _at;

    public override double Expects => Prior;

    public override double Minutes => 3.0;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override int Made => 0;

    public override string Stage => "carrying the takings to the chest";

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
            Chestless++;

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

        var coin = 0;
        var things = 0;
        var keeps = BotPurse.Keeps(body);
        var spare = pack.GetAmount(typeof(Gold)) - keeps;

        if (spare > 0 && pack.ConsumeTotal(typeof(Gold), spare))
        {
            if (chest.TryDropItem(body, new Gold(spare), false))
            {
                coin = spare;
            }
            else
            {
                // The chest would not take it: the coin goes back where it was rather than out of the world.
                body.AddToBackpack(new Gold(spare));
            }
        }

        // <b>The fence's pack is the band's shop window.</b> It carries the chest's goods out to a shopkeeper
        // (BotFetch) and would otherwise put them straight back in on arrival, which is a bot walking a circle: the
        // fence puts down coin and nothing else.
        var kept = BotUnload.Keeps(bot);
        List<Item> spared = [];
        var goods = !BotFence.Is(body);

        for (var i = 0; goods && i < pack.Items.Count; i++)
        {
            var item = pack.Items[i];

            // What the bot carries for itself stays with it: its bound gear, its kit, anything its trade asks for.
            if (item is not { Deleted: false, Movable: true } || item is Gold || BotBinding.IsBound(item, bot.Bond)
                || kept.ContainsKey(item.GetType()))
            {
                continue;
            }

            spared.Add(item);
        }

        for (var i = 0; i < spared.Count; i++)
        {
            if (chest.TryDropItem(body, spared[i], false))
            {
                things++;
            }
        }

        // <b>The other half of the keeper.</b> The fence's pack is the band's shop window and the loop above puts
        // none of it down — but a thing the band's own kit asked for was bought for the chest, not for a counter. See
        // BotFence.Wants, and BotShopper.Wanting, which is where it was bought.
        var stocked = 0;

        if (BotFence.Is(body) && BotFence.Wants() is { Count: > 0 } asked)
        {
            // By kind and not by stack. Its own kit stays in its own pack — a keeper that tips its bandages into the
            // chest is short of bandages, is sent to buy more, and walks the same circle for ever — but the allowance
            // is for the kind, and a dyed bound pile will not merge with a bought one, so the bandages are in two
            // piles and neither is bigger than the allowance. See BotFence.Spare, which is where that cost a day.
            var owed = BotFence.Spare(body as BotMobile, asked);
            List<(Item Stack, int Give)> parcels = [];

            for (var i = 0; i < pack.Items.Count; i++)
            {
                if (pack.Items[i] is not { Deleted: false, Movable: true } item || item is Gold
                    || BotBinding.IsBound(item, bot.Bond) || !owed.TryGetValue(item.GetType(), out var left)
                    || left <= 0)
                {
                    continue;
                }

                var give = Math.Min(left, Math.Max(1, item.Amount));
                owed[item.GetType()] = left - give;

                if (give > 0)
                {
                    parcels.Add((item, give));
                }
            }

            for (var i = 0; i < parcels.Count; i++)
            {
                var (stack, give) = parcels[i];
                var held = Math.Max(1, stack.Amount);

                // LiftItemDupe leaves the original holding what it is passed and hands back the rest, so the fence
                // keeps its own and the band gets the surplus in one item.
                var parcel = give >= held ? stack : Mobile.LiftItemDupe(stack, held - give);

                if (parcel != null && chest.TryDropItem(body, parcel, false))
                {
                    stocked += give;
                    things++;
                }
            }

            BotFence.Stocked(stocked);

            // <b>Named, because twice now this has arrived and put nothing down.</b> "finished stash: nothing worth
            // putting down" says only that the sum came to nought; which of the four steps it came to nought at — the
            // band asking for nothing, the pack holding nothing spare, the stacks refusing to split, the chest
            // refusing to take — is what this line answers. See DECISIONS 145.
            logger.Information(
                "{Name} at the chest: the band asked for {Kinds} kinds, it held {Owed} of them spare, made up {Parcels} parcels and put down {Stocked}",
                body.Name,
                asked.Count,
                owed.Count,
                parcels.Count,
                stocked
            );
        }

        Gold += coin;
        Things += things;

        if (coin <= 0 && things <= 0)
        {
            Empty++;

            return BotDoing.Done("nothing worth putting down");
        }

        Done++;
        BotLair.Took(things, coin);

        logger.Information(
            "{Name} put {Gold}gp and {Things} things in the band's chest at ({X}, {Y}){Order}",
            body.Name,
            coin,
            things,
            _at.X,
            _at.Y,
            stocked > 0 ? $", {stocked} of them the band's order" : ""
        );

        return BotDoing.Done($"{coin}gp and {things} things into the chest");
    }

    public static string Describe() =>
        $"{Runs} walks out to the band's chest: {Done} put something down ({Gold}gp and {Things} things), {Empty} arrived with nothing, {Chestless} found no chest";

    public static void Forget()
    {
        Runs = 0;
        Done = 0;
        Empty = 0;
        Chestless = 0;
        Gold = 0;
        Things = 0;
    }
}
