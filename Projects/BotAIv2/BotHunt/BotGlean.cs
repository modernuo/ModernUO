using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// Picking spent ammunition up off the ground.
///
/// <para>
/// <b>Where an arrow goes when it misses is the whole reason this exists.</b> On this era's rules a miss puts
/// the arrow back into the world — <c>Ammo.MoveToWorld</c>, a tile or two from whatever was being shot at —
/// with a four in ten chance, and a hit drops it into the target instead, which is to say into the corpse. The
/// corpse half is already collected by whoever loots it. The ground half was collected by nobody at all, so an
/// archer's quiver drained one way: out.
/// </para>
///
/// <para>
/// <b>It is priced as the errand it is.</b> Arrows are worth a few coppers each, so this wins only when there
/// is nothing better to do — which is exactly what was asked for, and exactly where it belongs: a bot with a
/// paying trade in front of it should not be crouching in a field over three arrows. What makes it worth
/// having at all is that ammunition is otherwise bought, and an archer that has to buy every arrow it fires
/// spends its takings on being able to earn them.
/// </para>
/// </summary>
public sealed class BotGlean : BotDeed
{
    /// <summary>The ledger key.</summary>
    public const string Trade = "glean";

    /// <summary>What gathering is reckoned at per minute before experience corrects it. Deliberately small.</summary>
    public static double Prior { get; set; } = 14.0;

    public static double WorkMinutes { get; set; } = 1.0;

    /// <summary>How far around itself an archer looks for what it has shot away.</summary>
    public static int Reach { get; set; } = 20;

    /// <summary>How near a bot has to be to pick something up off the floor.</summary>
    public static int Touch { get; set; } = 2;

    private readonly Type _kind;

    private readonly Map _map;

    private readonly Point3D _where;

    private int _gathered;

    public BotGlean(Type kind, Map map, Point3D where)
    {
        _kind = kind;
        _map = map;
        _where = where;
    }

    public override string Kind => Trade;

    /// <summary>A corpse is still there after a flight. See <see cref="BotDeed.Resumes"/>.</summary>
    public override bool Resumes => true;

    public override Map Map => _map;

    public override Point3D Where => _where;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    /// <summary>Nothing. Bending down teaches a bot nothing at all.</summary>
    public override SkillName? Trains => null;

    public override int Outlay => 0;

    /// <summary>No coin changes hands; what comes back is goods, and goods it would otherwise have bought.</summary>
    public override double Coin => 0.0;

    /// <summary>
    /// The road was refused, so the arrow's tile is written down. The other half of the guard in
    /// <c>BotGleaner.Propose</c>, and without it that guard has nothing to read.
    ///
    /// See <c>BotPickings.Bend</c> for the whole argument: the reach ledger is about ground a bot stands
    /// in, an arrow through the floor is not a surveyed pocket, and a mark nobody reads is this project's
    /// most repeated defect. False always — there is no second arrow to try on this errand.
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        if (_where != Point3D.Zero)
        {
            bot?.Resolve?.Ledger?.Beware(Trade, _map, _where);
        }

        return false;
    }

    /// <summary>What was picked up, at whatever the shard reckons an arrow is worth.</summary>
    public override int Made => _gathered * BotMarket.Worth(_kind, 1);

    public override string Stage =>
        _gathered > 0 ? $"gathered {_gathered} {_kind?.Name}" : $"after spent {_kind?.Name}";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal || _kind == null)
        {
            return BotDoing.Failed("no body");
        }

        var lying = Nearest(body, _kind, Reach);

        if (lying == null)
        {
            // Nothing left within reach. Finished rather than failed: the ground was worth a look, and what
            // was there is now in the quiver.
            return _gathered > 0
                ? BotDoing.Done($"{_gathered} {_kind.Name} back off the ground")
                : BotDoing.Done("nothing left lying about");
        }

        if (!body.InRange(lying.GetWorldLocation(), Touch))
        {
            return BotDoing.Walk(_map, lying.GetWorldLocation(), BotArrival.Within(Touch), $"after spent {_kind.Name}");
        }

        var pack = body.Backpack;

        if (pack == null)
        {
            return BotDoing.Failed("nowhere to put it");
        }

        _gathered += Math.Max(1, lying.Amount);

        // <b>Onto the quiver's own pile, never beside it (12.10.2026).</b> A missed arrow lies as a pile of one, and
        // <c>DropItem</c> does not stack, so every arrow picked up became a pile of its own that the engine weighs at
        // ceil(0.1 x 1) = a whole stone: Baern the Tamer gleaned 162 arrows at 09:28 on 10.10.2026 and was 162 stones
        // heavier, over his ceiling for 35 hours with arrows the porter keeps without limit. See BotUnload.IntoPack.
        BotUnload.IntoPack(body, pack, lying);

        return BotDoing.Work("gathering");
    }

    /// <summary>
    /// The nearest of this kind lying loose on the ground, or null.
    ///
    /// <para>
    /// On the ground specifically — <c>Parent == null</c> — because ammunition inside a container belongs to
    /// somebody: a corpse being looted, a bot's own pack, a shopkeeper's stock. Only what was dropped in the
    /// world is free to pick up.
    /// </para>
    /// </summary>
    public static Item Nearest(Mobile bot, Type kind, int range)
    {
        var map = bot?.Map;

        if (map == null || map == Map.Internal || kind == null)
        {
            return null;
        }

        Item best = null;
        var bestAway = double.MaxValue;

        foreach (var item in map.GetItemsInRange(bot.Location, range))
        {
            if (item.Deleted || item.Parent != null || !item.Movable || item.GetType() != kind)
            {
                continue;
            }

            var away = bot.GetDistanceToSqrt(item.Location);

            if (away >= bestAway)
            {
                continue;
            }

            best = item;
            bestAway = away;
        }

        return best;
    }
}

/// <summary>
/// Offers an archer the job of picking its arrows back up, when it is short of them and some are lying about.
///
/// <para>
/// Only a bot that shoots, and only one that is actually short: the quiver it was born with is the measure,
/// so a full archer walks past its own spent arrows rather than tidying the field for its own sake.
/// </para>
/// </summary>
    /// <summary>
    /// The road was refused, so the place is written down — and this is the half that was missing.
    ///
    /// <para>
    /// <b>Asking the reach ledger was not enough and the log said so within the hour.</b>
    /// <c>BotReach.Ask</c> answers out of pockets that enclosure searches have proved closed: it is about
    /// the ground a bot is <em>standing in</em>. A corpse fifteen tiles below the floor is not a pocket
    /// anybody has surveyed, so the answer is Unknown and the offer goes through. Corwin took the picking errand
    /// 533 times in five minutes on 10.09.2026 at one such corpse and took the whole shard's band to 33%,
    /// an hour after the reach guard went in and four hours after the identical loop on a spent arrow.
    /// </para>
    ///
    /// <para>
    /// So the failure marks the place, the way <c>BotUnload.Bend</c> has always marked a counter it could
    /// not reach, and the proposer reads the mark. Both halves are needed: a mark nobody reads is this
    /// project's oldest and most repeated defect, and the appraisal's own caution factor is not a substitute
    /// — a fifth root turns 0.15 into 0.68, which does not stop anything.
    /// </para>
    ///
    /// <para>
    /// False, always: there is no second place to try. The errand ends and the mark stops it coming back.
    /// </para>
    /// </summary>
    // (on BotGlean, for the arrow that started it: (1457, 1461, -15), 355 attempts in five minutes.)

public sealed class BotGleaner : IBotProposer
{
    /// <summary>
    /// Spent arrows passed over because the ground they lie on has already been proved unreachable.
    ///
    /// <para>
    /// <b>Written after this proposer took the whole shard's completion band down with it.</b> On
    /// 10.09.2026 at 07:23 the band read 28% against a steady 76%, and 406 of the window's 440 failures
    /// were three archers — Brannoc 235, Aric 120, Bertram 51 — walking at one arrow lying at
    /// (1457, 1461, <b>-15</b>): seventeen tiles from home and fifteen below it, inside the ground. Three
    /// hundred and fifty-five attempts in five minutes at a single tile.
    /// </para>
    ///
    /// <para>
    /// <c>Nearest</c> answers "what is the closest one" and nothing asked whether it could be walked to, so
    /// every failure put the same arrow straight back on offer. The cure is the one <c>BotStudent</c>
    /// already uses and it costs a dictionary lookup: the reach ledger is asked, and it answers only from
    /// pockets that real searches have already proved closed. A proposal declined costs nothing; an errand
    /// taken and failed costs a place in the band.
    /// </para>
    /// </summary>
    public static long Sealed { get; private set; }

    /// <summary>Arrows passed over because this bot's own road to them was refused before.</summary>
    public static long Baulked { get; private set; }

    /// <summary>Forgotten with the rest of the counters on a world reload.</summary>
    public static void Forget()
    {
        Sealed = 0;
        Baulked = 0;
    }

    public string Name => "Gleaner";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;
        var bond = bot?.Bond;

        if (map == null || map == Map.Internal || bond == null || !body.Alive)
        {
            return null;
        }

        var kind = bond.Weapon?.Ammunition;

        if (kind == null)
        {
            return null;
        }

        // Short of what it was issued. Bound ammunition is a ceiling, so this is the honest measure of "should
        // be carrying more".
        var granted = BotBinding.BoundCount(kind, bond);
        var carried = body.Backpack?.GetAmount(kind) ?? 0;

        if (granted <= 0 || carried >= granted)
        {
            return null;
        }

        var lying = BotGlean.Nearest(body, kind, BotGlean.Reach);

        if (lying == null)
        {
            return null;
        }

        var where = lying.GetWorldLocation();

        if (bot.Resolve?.Ledger?.Cautious(BotGlean.Trade, map, where) == true)
        {
            Baulked++;

            return null;
        }

        // Asked of the reach ledger, which already knows. See Sealed: an arrow that fell through the floor
        // is still the nearest arrow, for ever, and nothing here had ever asked whether it could be reached.
        if (BotReach.Ask(map, body.Location, where, BotArrival.Within(BotGlean.Touch)) == BotReachVerdict.Sealed)
        {
            Sealed++;

            return null;
        }

        return new BotGlean(kind, map, where);
    }
}
