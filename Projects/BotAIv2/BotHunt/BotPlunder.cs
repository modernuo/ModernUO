using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// Going through a chest, a crate or a barrel standing out in the world.
///
/// <para>
/// <b>Ordered by Patrick on 08.09.2026, and the world already provides the case.</b> An <c>OrcCamp</c>
/// spawns three orcs, a captain, a prisoner, an unlocked chest filled the way a treasure-map chest is
/// filled, and a locked crate holding a hundred to four hundred gold. Until now the population could kill
/// everything standing in that camp and walk away from the chest, because nothing on this shard had any
/// notion of a container that was not a corpse, a stall or its own pack.
/// </para>
///
/// <para>
/// <b>What it takes and at what price is not decided here.</b> That is <see cref="BotSlay.Rifle"/>, which
/// already knows to keep an archer's own arrows, to hold back a cook's meat, to price what it lists off a
/// shopkeeper's own offer rather than a flat gold, and to stop when the pack is full. A second copy of
/// those rules written for chests would have disagreed with the first inside a week — this file has the
/// scars to prove it.
/// </para>
///
/// <para>
/// <b>Two things it will not touch, each with its own bucket.</b> A chest whose camp is still garrisoned —
/// by Patrick's order of 08.09.2026 the chest is what the fight is for, so it opens when the last hostile
/// is dead and not before — and anything standing inside a town, which is the same rule the woodsman and
/// the miner keep and what stops a population from emptying the crates in Britain's shops and calling it
/// work. Locks and traps are counted but no longer refuse: the garrison is the lock.
/// </para>
/// </summary>
public sealed class BotPlunder : BotDeed
{
    /// <summary>The ledger key.</summary>
    public const string Trade = "plunder";

    /// <summary>
    /// What going through a chest is reckoned at per minute before experience corrects it.
    ///
    /// <para>
    /// Above <c>BotPickings</c>'s ninety, because a camp chest holds several hundred gold against a rat's
    /// purse of six, and well below a fight, which is priced in the hundreds: a bot standing over an
    /// unopened chest with an orc captain swinging at it should be fighting. Measured like everything else
    /// — see <c>BotCommons.Corrected</c> — so this is an opening bid and not a claim.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 160.0;

    public static double WorkMinutes { get; set; } = 1.0;

    /// <summary>How far around itself a bot notices a chest worth opening.</summary>
    public static int Reach { get; set; } = 48;

    /// <summary>How near a container a bot has to be to reach into it. The engine's own arm's length.</summary>
    public static int Touch { get; set; } = 2;

    /// <summary>How far a living enemy may be before a bot will stop emptying a chest and deal with it.</summary>
    public static int Danger { get; set; } = 8;

    /// <summary>How long a container that has been emptied is left alone before anybody looks again.</summary>
    public static int EmptiedMs { get; set; } = 600000;

    /// <summary>Containers taken that were locked. Counted, not refused: see Nearest.</summary>
    public static long Locked { get; private set; }

    /// <summary>Containers taken that were trapped. Counted, not refused.</summary>
    public static long Trapped { get; private set; }

    /// <summary>Containers passed over because the camp around them is still held. See <see cref="Guarded"/>.</summary>
    public static long Held { get; private set; }

    /// <summary>How far around a chest its garrison counts. A camp is about a dozen tiles across.</summary>
    public static int GuardReach { get; set; } = 12;

    /// <summary>Containers passed over for standing inside a town, which makes them somebody's property.</summary>
    public static long Townbound { get; private set; }

    /// <summary>Chests passed over for being The Shadow's own, by a bot that is not of it.</summary>
    public static long Theirs { get; private set; }

    /// <summary>Containers walked to that turned out to hold nothing worth carrying.</summary>
    public static long Bare { get; private set; }

    /// <summary>Containers emptied.</summary>
    public static long Emptied { get; private set; }

    /// <summary>Things carried away out of them.</summary>
    public static long Taken { get; private set; }

    /// <summary>Coin carried away out of them.</summary>
    public static long Coins { get; private set; }

    /// <summary>Errands ended early because something living came within <see cref="Danger"/> tiles.</summary>
    public static long Interrupted { get; private set; }

    /// <summary>
    /// Containers already gone through, and when.
    ///
    /// <para>
    /// Shared, because an empty chest is empty for everybody — the same reasoning <c>BotQuarry.Shun</c>
    /// keeps about a creature nobody can reach. Kept by serial rather than by place: a crate is one object
    /// and there may be two of them on one tile.
    /// </para>
    /// </summary>
    private static readonly Dictionary<Serial, long> _emptied = [];

    private readonly Map _map;

    private readonly Container _box;

    private readonly Point3D _where;

    private int _taken;

    private int _coins;

    private int _made;

    public BotPlunder(Map map, Container box)
    {
        _map = map;
        _box = box;
        _where = box?.GetWorldLocation() ?? Point3D.Zero;
    }

    public override string Kind => Trade;

    /// <summary>Goes where the fighting is on purpose, and weighs the danger itself. See <see cref="BotDeed.Braves"/>.</summary>
    public override bool Braves => true;

    public override Map Map => _map;

    public override Point3D Where => _where;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    /// <summary>Nothing. Reaching into a box teaches a bot nothing at all.</summary>
    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override int Made => _made + _coins;

    public override string Stage =>
        _taken > 0 || _coins > 0
            ? $"took {_taken} things and {_coins}gp out of a {_box?.GetType().Name}"
            : $"at a {_box?.GetType().Name}";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        if (_box == null || _box.Deleted || _box.Map != _map)
        {
            return BotDoing.Failed("the chest is gone");
        }

        if (!body.InRange(_where, Touch))
        {
            return BotDoing.Walk(_map, _where, BotArrival.Within(Touch), $"to a {_box.GetType().Name}");
        }

        // <b>A chest is not worth dying over, and a bot with its hands in one is not fighting.</b> The camp
        // this was written for has four orcs standing in it, so arriving is very often arriving in a fight.
        // Ended rather than held: the hunt prices a fight in the hundreds and will take the bot straight
        // back to it, and the chest is still there afterwards.
        if (Threatened(body))
        {
            Interrupted++;

            return _taken > 0 || _coins > 0
                ? BotDoing.Done($"broke off with {_taken} things and {_coins}gp")
                : BotDoing.Failed("something living is standing over it");
        }

        var (taken, coins, made) = BotSlay.Rifle(bot, body, _box, null);

        _taken += taken;
        _coins += coins;
        _made += made;

        Taken += taken;
        Coins += coins;

        // Nothing came out this beat. Either it was empty to begin with or the pack is full; both mean this
        // errand is over, and both are told apart by what the bot is carrying rather than by a second look.
        if (taken == 0 && coins == 0)
        {
            Forget(_box);

            if (_taken == 0 && _coins == 0)
            {
                Bare++;

                return BotDoing.Done("nothing in it worth carrying");
            }

            Emptied++;

            return BotDoing.Done($"{_taken} things and {_coins}gp out of a {_box.GetType().Name}");
        }

        return BotDoing.Work("going through it");
    }

    /// <summary>
    /// Whether anything hostile is still standing over this chest — that is, whether the camp still holds.
    ///
    /// <para>
    /// The rule Patrick asked for: the chest is what the fight is for, so it opens when the garrison is
    /// dead and not before. Asked of the ground around the chest, so it is a fact about the camp rather than
    /// about whichever bot happens to be looking.
    /// </para>
    /// </summary>
    public static bool Guarded(Map map, Point3D where)
    {
        foreach (var near in map.GetMobilesInRange<BaseCreature>(where, GuardReach))
        {
            if (!near.Deleted && near.Alive && !near.Controlled && near.Karma < 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether something that would fight this bot is standing close enough to matter.</summary>
    private bool Threatened(Mobile body)
    {
        foreach (var near in _map.GetMobilesInRange<BaseCreature>(body.Location, Danger))
        {
            if (!near.Deleted && near.Alive && !near.Controlled && near.Karma < 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>This one has been gone through. Nobody looks at it again for a while.</summary>
    private static void Forget(Container box)
    {
        if (box != null)
        {
            _emptied[box.Serial] = Core.TickCount;
        }
    }

    /// <summary>Whether this container was emptied recently enough to be worth leaving alone.</summary>
    public static bool Spent(Container box) =>
        box != null
        && _emptied.TryGetValue(box.Serial, out var when)
        && Core.TickCount - when < EmptiedMs;

    /// <summary>
    /// The nearest container standing in the world that this bot is allowed to go through, or null.
    ///
    /// <para>
    /// <c>Parent == null</c> is the whole of "standing in the world": a pack, a bank box, a stall's holding
    /// and a corpse's contents all have a parent, and only what was placed on the ground is loose. A corpse
    /// is excluded by name as well, because that is <c>BotPickings</c>'s business and it asks the engine a
    /// permission question this does not.
    /// </para>
    /// </summary>
    public static Container Nearest(Mobile body, int range)
    {
        var map = body?.Map;

        if (map == null || map == Map.Internal)
        {
            return null;
        }

        Container best = null;
        var bestAway = double.MaxValue;

        foreach (var item in map.GetItemsInRange(body.Location, range))
        {
            if (item.Deleted || item.Parent != null || item is Corpse or BankBox || item is not Container box)
            {
                continue;
            }

            if (Spent(box) || Empty(box))
            {
                continue;
            }

            // <b>The band's chest is the band's.</b> Patrick's order of 17.09.2026: a chest at the hideout that only
            // bandits reach. Nothing on this shard opens a chest except this errand, so the whole of that rule is the
            // one question asked here; there is no lock, because locks have not refused a bot since 08.09.2026.
            if (BotLair.Theirs(box) && !BotUnderworld.Member(body))
            {
                Theirs++;

                continue;
            }

            // <b>Locked and trapped no longer refuse a bot — clearing the camp does.</b> Patrick's order of
            // 08.09.2026: the chest is the reward for the fight, so what stands between a bot and it is the
            // garrison, not the lock. Kept as counters because the two facts are still worth seeing: a shard
            // where everything worth taking is locked is a shard whose population should be learning
            // Lockpicking.
            if (box is LockableContainer { Locked: true })
            {
                Locked++;
            }

            if (box is TrappableContainer { TrapType: not TrapType.None })
            {
                Trapped++;
            }

            // The garrison. Anything living and hostile standing over the chest means the camp is not taken
            // yet, and taking it is the hunt's business — this errand waits for that to be done. Measured
            // around the chest rather than around the bot, because the question is whether the camp is
            // cleared, not whether this particular bot is safe where it stands.
            if (Guarded(map, box.GetWorldLocation()))
            {
                Held++;

                continue;
            }

            // Somebody's property. The same ruling the miner and the woodsman keep about rock and trees
            // inside the walls, and for the same reason: a population that empties the crates in Britain's
            // shops has not found work, it has found a crime the engine happens not to punish.
            if (Region.Find(box.GetWorldLocation(), map)?.IsPartOf<GuardedRegion>() == true)
            {
                Townbound++;

                continue;
            }

            var away = body.GetDistanceToSqrt(box.GetWorldLocation());

            if (away >= bestAway)
            {
                continue;
            }

            best = box;
            bestAway = away;
        }

        return best;
    }

    /// <summary>Whether there is nothing in this container anybody could carry off.</summary>
    private static bool Empty(Container box)
    {
        var held = box.Items;

        for (var i = 0; i < held.Count; i++)
        {
            if (held[i]?.Deleted == false && held[i].Movable)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>A world reload is a different world.</summary>
    public static void Forget()
    {
        Theirs = 0;
        _emptied.Clear();
        Locked = 0;
        Trapped = 0;
        Held = 0;
        Townbound = 0;
        Bare = 0;
        Emptied = 0;
        Taken = 0;
        Coins = 0;
        Interrupted = 0;
    }

    /// <summary>One line for the shard's own summary.</summary>
    public static string Describe() =>
        $"{Emptied} chests emptied of {Taken} things and {Coins}gp, {Bare} held nothing worth carrying, "
        + $"{Interrupted} broken off for something living standing over them; {Locked} of them were locked and "
        + $"{Trapped} trapped; passed over: {Held} still held by a garrison, {Townbound} standing inside a town and {Theirs} belonging to The Shadow";
}

/// <summary>
/// Offers a bot the job of going through a chest standing near it.
///
/// <para>
/// The sweep is a spatial query over <see cref="BotPlunder.Reach"/> tiles and it is rationed by
/// <c>BotNeeds</c> to once a minute per bot, for the reason every sweep on this shard is rationed: the
/// auction asks every proposer on every free review, and a query that looks at forty-eight tiles of world
/// per bot per review is a query the population runs thousands of times a minute for nothing.
/// </para>
/// </summary>
public sealed class BotPlunderer : IBotProposer
{
    public string Name => "Plunderer";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>Bots asked. For the denominator.</summary>
    public static long Asked { get; private set; }

    /// <summary>Asks answered with nothing because the minute was not up.</summary>
    public static long Soon { get; private set; }

    /// <summary>Asks answered with nothing because there was no chest in reach.</summary>
    public static long None { get; private set; }

    /// <summary>Errands offered.</summary>
    public static long Offered { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        Asked++;

        if (!BotNeeds.Due(body, "plunder"))
        {
            Soon++;

            return null;
        }

        var box = BotPlunder.Nearest(body, BotPlunder.Reach);

        if (box == null)
        {
            None++;

            return null;
        }

        Offered++;

        return new BotPlunder(map, box);
    }

    public static string Describe() =>
        $"{Asked} asked to go through a chest: {Offered} sent to one, {None} had none in reach, {Soon} had looked too recently";

    public static void Reset()
    {
        Asked = 0;
        Soon = 0;
        None = 0;
        Offered = 0;
    }
}
