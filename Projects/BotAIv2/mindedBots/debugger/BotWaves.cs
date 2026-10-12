using System;
using System.Collections.Generic;
using Server.BotAI.V2;
using Server.Logging;
using Server.Mobiles;
using Server.Regions;

namespace Server.BotAI.Mind;

/// <summary>
/// Something to fight, when the watcher has called a hunt and the island has nothing to offer.
///
/// <para>
/// <b>Patrick's order of 08.09.2026, and the shape of it is the whole design: waves that grow.</b> One weak
/// thing, then two, then three, then a strong one with two weak, and so on to two strong and five weak. Not
/// a horde dropped on the population at once — a shard where the answer to a revel is nine bots dead in a
/// field has taught them nothing and cost an evening of skills — and not one lone rat either, which is a
/// revel nobody notices.
/// </para>
///
/// <para>
/// <b>It spawns only when there is genuinely nothing to fight.</b> The island has its own spawns and they
/// are the right ones to use; this exists for the case the log has been shouting about since the first
/// evening — "nothing within 50 tiles of the bots is worth fighting, so no gold will enter the world". A
/// revel for hunting declared into an empty field is a price rise on nothing at all.
/// </para>
///
/// <para>
/// <b>Everything it makes, it takes away.</b> The wave is remembered by reference, the next one waits for
/// the last to be dead, and when the revel ends whatever is still standing is deleted. A shard that
/// accumulates ettins because a watcher had an idea an hour ago is a shard nobody can measure.
/// </para>
/// </summary>
public static class BotWaves
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWaves));

    /// <summary>Whether the watcher may put anything in the world at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>
    /// The revels this happens for. A hunt, a prowl and a plunder are the three that want something alive
    /// in front of them; nobody needs a monster to help them bake.
    /// </summary>
    private static readonly string[] _hunts = ["hunt", "prowl", "plunder"];

    /// <summary>
    /// The waves, in order: how many weak things and how many strong ones.
    ///
    /// Patrick's own list. It ends rather than repeating, and the last line is held for as long as the revel
    /// lasts: an escalation with no ceiling is a wipe with a delay in front of it.
    /// </summary>
    private static readonly (int Weak, int Strong)[] _waves =
    [
        (1, 0), (2, 0), (3, 0), (2, 1), (3, 1), (4, 1), (4, 2), (5, 2)
    ];

    /// <summary>
    /// What counts as weak: things a single bot beats on its own, and loses to only by bad luck.
    /// </summary>
    private static readonly Func<BaseCreature>[] _weak =
    [
        () => new Orc(),
        () => new Ratman(),
        () => new Skeleton(),
        () => new Zombie(),
        () => new HeadlessOne()
    ];

    /// <summary>
    /// And what counts as strong: things a company handles and a lone bot runs from. Nothing above an ettin
    /// — the population's own hunters already lose to trolls often enough to be honest about the ceiling.
    /// </summary>
    private static readonly Func<BaseCreature>[] _strong =
    [
        () => new OrcCaptain(),
        () => new Troll(),
        () => new Ettin(),
        () => new Ogre()
    ];

    /// <summary>How far around the revel counts as "there is already something to fight".</summary>
    public static int Reach { get; set; } = 40;

    /// <summary>How far from the middle of the revel a wave appears.</summary>
    public static int Spread { get; set; } = 8;

    /// <summary>How long the field stays quiet between waves.</summary>
    public static int RestMs { get; set; } = 12000;

    /// <summary>The most that may be standing at once, whatever the table says.</summary>
    public static int MostAlive { get; set; } = 8;

    /// <summary>Waves put up.</summary>
    public static long Raised { get; private set; }

    /// <summary>Weak things and strong things made.</summary>
    public static long Weaklings { get; private set; }

    public static long Champions { get; private set; }

    /// <summary>Things taken away again at the end of a revel, unkilled.</summary>
    public static long Cleared { get; private set; }

    /// <summary>Times a wave was not needed because the island had something to fight already.</summary>
    public static long Unneeded { get; private set; }

    /// <summary>What is standing now, by reference: a wave is over when every one of these is dead.</summary>
    private static readonly List<BaseCreature> _alive = [];

    private static int _stage;

    private static long _clearedTick;

    /// <summary>Which wave is standing, one-based, or nought between them.</summary>
    public static int Wave => _alive.Count > 0 ? _stage : 0;

    /// <summary>How many of it are still on their feet.</summary>
    public static int Standing => _alive.Count;

    /// <summary>
    /// Called on the watcher's own beat. Cheap when nothing is on, which is nearly always.
    /// </summary>
    public static void Muster()
    {
        if (!BotRevel.Running || !Running || !IsHunt(BotRevel.Kind))
        {
            // The revel is over, or was never one of ours. Anything still standing goes.
            Stand();

            return;
        }

        var map = BotRevel.Ground;
        var middle = BotRevel.Where;

        if (map == null || map == Map.Internal || middle == Point3D.Zero)
        {
            return;
        }

        Prune();

        if (_alive.Count > 0)
        {
            return;
        }

        var now = Core.TickCount;

        if (_stage > 0 && now - _clearedTick < RestMs)
        {
            return;
        }

        // The island first. Its own spawns are the ones worth fighting, and a wave dropped beside a troll
        // that is already there is two fights for a prize that pays once.
        if (Busy(map, middle))
        {
            Unneeded++;
            _clearedTick = now;

            return;
        }

        var wave = _waves[Math.Min(_stage, _waves.Length - 1)];

        var weak = Put(map, middle, _weak, wave.Weak);
        var strong = Put(map, middle, _strong, wave.Strong);

        if (weak + strong == 0)
        {
            // Nowhere to put anything: water, a town, a cliff. Said once and left alone until the next beat.
            return;
        }

        _stage++;
        Raised++;
        Weaklings += weak;
        Champions += strong;

        logger.Information(
            "Wave {Stage} of the {Kind} revel stands at {X},{Y}: {Weak} weak and {Strong} strong",
            _stage,
            BotRevel.Kind,
            middle.X,
            middle.Y,
            weak,
            strong
        );

        // Said out loud, because Patrick watches this shard from a client and a wave that arrives in silence
        // reads as the island misbehaving rather than as the watcher doing something.
        if (strong > 0)
        {
            BotVigil.Body?.Say($"Wave {_stage}: {weak} of them, and {strong} that will not go down easily.");
        }
        else
        {
            BotVigil.Body?.Say($"Wave {_stage}: {weak} of them.");
        }
    }

    /// <summary>Whether this trade is one a monster helps with. Asked by the revel too, whose waves want a field (<see cref="BotVenues"/>).</summary>
    public static bool IsHunt(string kind)
    {
        if (kind == null)
        {
            return false;
        }

        for (var i = 0; i < _hunts.Length; i++)
        {
            if (string.Equals(kind, _hunts[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the island already has something worth fighting near the revel.</summary>
    private static bool Busy(Map map, Point3D middle)
    {
        foreach (var creature in map.GetMobilesInRange<BaseCreature>(middle, Reach))
        {
            // Karma is the cheap reading of "would this fight anybody", and it is the right one here: a
            // hind and a bot's own packhorse both fail it, an orc and a troll both pass. Nothing tamed and
            // nothing summoned counts, so a bot's own mount standing beside it is not a reason to withhold
            // a wave.
            if (creature is { Deleted: false, Alive: true, Controlled: false, Summoned: false } &&
                creature.Karma < 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Puts <paramref name="many"/> things down round the revel, and says how many actually landed.</summary>
    private static int Put(Map map, Point3D middle, Func<BaseCreature>[] kinds, int many)
    {
        var made = 0;

        for (var i = 0; i < many && _alive.Count < MostAlive; i++)
        {
            if (!Somewhere(map, middle, out var at))
            {
                continue;
            }

            var creature = kinds[Utility.Random(kinds.Length)]();

            if (creature == null)
            {
                continue;
            }

            // Kept on a short lead. A wave that wanders off to Britain is a wave the revel cannot be judged
            // by, and a wave that walks into the guards is a wave the watch kills for us.
            creature.Home = middle;
            creature.RangeHome = Reach;

            creature.MoveToWorld(at, map);
            _alive.Add(creature);
            made++;
        }

        return made;
    }

    /// <summary>
    /// A tile near the middle that will hold a body, is out of town, and is not on top of anybody.
    ///
    /// The height comes from <see cref="BotStep.Settle"/> and never from arithmetic: a monster dropped at an
    /// invented Z is a monster standing inside a hill, which this project has paid to learn twice.
    /// </summary>
    private static bool Somewhere(Map map, Point3D middle, out Point3D at)
    {
        at = Point3D.Zero;

        for (var tries = 0; tries < 20; tries++)
        {
            var x = middle.X + Utility.RandomMinMax(-Spread, Spread);
            var y = middle.Y + Utility.RandomMinMax(-Spread, Spread);

            if (!BotStep.Settle(map, x, y, out var z))
            {
                continue;
            }

            var spot = new Point3D(x, y, z);

            // Never in a guarded town: the watch would kill the wave before a bot reached it, and a revel
            // judged on a fight the guards had is a measurement of nothing.
            if (Region.Find(spot, map)?.IsPartOf<GuardedRegion>() == true)
            {
                continue;
            }

            if (!map.CanFit(x, y, z, 16, false, false))
            {
                continue;
            }

            at = spot;

            return true;
        }

        return false;
    }

    /// <summary>Forgets whatever has died since the last look.</summary>
    private static void Prune()
    {
        for (var i = _alive.Count - 1; i >= 0; i--)
        {
            var creature = _alive[i];

            if (creature is not { Deleted: false, Alive: true })
            {
                _alive.RemoveAt(i);

                if (_alive.Count == 0)
                {
                    _clearedTick = Core.TickCount;
                }
            }
        }
    }

    /// <summary>
    /// The revel is over. Everything still standing goes back where it came from, which is nowhere.
    ///
    /// Deleted rather than left to wander: what this puts down is an argument the watcher was making, and an
    /// argument that outlives the conversation is just an ettin.
    /// </summary>
    public static void Stand()
    {
        if (_alive.Count == 0)
        {
            _stage = 0;

            return;
        }

        var gone = 0;

        for (var i = 0; i < _alive.Count; i++)
        {
            if (_alive[i] is { Deleted: false } creature)
            {
                creature.Delete();
                gone++;
            }
        }

        _alive.Clear();
        _stage = 0;
        Cleared += gone;

        if (gone > 0)
        {
            logger.Information("{Gone} things left over from the revel were taken off the island", gone);
        }
    }

    /// <summary>One clause for the notice and the summary.</summary>
    public static string Describe() =>
        Raised == 0
            ? "no waves have been called"
            : $"{Raised} waves called, {Weaklings} weak and {Champions} strong, {Cleared} taken away unkilled, {Unneeded} times the island had something to fight already"
            + (_alive.Count > 0 ? $"; wave {_stage} is standing, {_alive.Count} of it alive" : "");

    public static void Forget()
    {
        Stand();
        Raised = 0;
        Weaklings = 0;
        Champions = 0;
        Cleared = 0;
        Unneeded = 0;
        _stage = 0;
    }
}
