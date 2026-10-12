using System;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The Shadow's camp at its hideout: a fire at the middle of it and a chest beside the fire that only the band opens.
///
/// <para>
/// <b>Patrick's order of 17.09.2026, night.</b> "They must keep what they have taken somewhere, since the towns are shut
/// to them and a catch takes everything off them. Bandit camps: say a campfire for the middle of it and a chest that
/// only bandits reach. If somebody comes near the camp and there are bandits in it, they go into stealth." Before this
/// the hideout was a point on the map and nothing else: the members rose there and practised there, and a robbery's
/// takings rode in the robber's pack until a patrol took them.
/// </para>
///
/// <para>
/// <b>Two engine items, no new class.</b> The fire is a bare <c>Item</c> with the campfire's own graphic rather than
/// <c>Campfire</c>, which is the camping skill's: that one skips serialization and puts itself out on a timer, so a
/// hideout built of it would be dark and gone at the next boot. The chest is a <c>WoodenChest</c> made immovable. What
/// keeps the island's hands out of it is not a lock — <c>BotPlunder</c> has counted locks and walked through them since
/// 08.09.2026 — but that every path to a chest on this shard asks here first.
/// </para>
/// </summary>
public static class BotLair
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotLair));

    /// <summary>Whether the camp is built at all. False leaves the hideout a bare point on the map.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>The campfire's graphic, the same the camping skill lights.</summary>
    public static int FireId { get; set; } = 0xDE3;

    /// <summary>How far a stranger may come to the fire before the band that is there goes to ground.</summary>
    public static int HideWithin { get; set; } = 12;

    /// <summary>How often the camp is looked over for strangers.</summary>
    public static int WatchMs { get; set; } = 3000;

    /// <summary>Camps pitched.</summary>
    public static long Pitched { get; private set; }

    /// <summary>Camps that could find no standing ground for the chest beside the fire.</summary>
    public static long Unpitched { get; private set; }

    /// <summary>Camps burned after a raid.</summary>
    public static long Burned { get; private set; }

    /// <summary>Times a stranger came near enough to send the band to ground.</summary>
    public static long Startled { get; private set; }

    /// <summary>Members that hid because of it.</summary>
    public static long WentToGround { get; private set; }

    /// <summary>Things stashed in the chest, and the coin among them.</summary>
    public static long Stashed { get; private set; }

    public static long StashedGold { get; private set; }

    private static Item _fire;

    private static Container _chest;

    private static long _watchedTick;

    /// <summary>The fire at the middle of the camp, or null while there is no camp.</summary>
    public static Item Fire => _fire is { Deleted: false } ? _fire : null;

    /// <summary>The band's chest, or null while there is no camp.</summary>
    public static Container Chest => _chest is { Deleted: false } ? _chest : null;

    /// <summary>
    /// A tile beside the chest that a bot can stand on.
    ///
    /// <para>
    /// <b>The chest's own tile is not one of them.</b> A chest on the ground is impassable, so a walk aimed at it looks
    /// for a way onto a square nothing can enter: at 05:26:40 on 18.09.2026 the first thief ever to carry its takings
    /// out — Joss Ashdown — "dropped (1878, 1466) because no way round it was found in 150ms (out to the band's
    /// chest)", and the stash that Patrick's second order is built on had never once been completed. The walk is aimed
    /// beside it and the reach does the rest.
    /// </para>
    /// </summary>
    public static Point3D Doorstep()
    {
        if (Chest is not { } chest || chest.Map is not { } map || map == Map.Internal)
        {
            return Point3D.Zero;
        }

        var at = chest.GetWorldLocation();

        for (var i = 0; i < 8; i++)
        {
            var x = at.X + (i is 0 or 1 or 7 ? 1 : i is 3 or 4 or 5 ? -1 : 0);
            var y = at.Y + (i is 1 or 2 or 3 ? 1 : i is 5 or 6 or 7 ? -1 : 0);

            if (Fire is { } fire && fire.X == x && fire.Y == y)
            {
                continue;
            }

            if (BotStep.Settle(map, x, y, out var z))
            {
                return new Point3D(x, y, z);
            }
        }

        return at;
    }

    /// <summary>Whether this container is the band's own, which only the band may go into.</summary>
    public static bool Theirs(Item box) => box != null && _chest is { Deleted: false } && ReferenceEquals(box, _chest);

    /// <summary>
    /// Takes up again a camp this build did not pitch: the fire and the chest are ordinary items and are in the world
    /// save, so after a boot they are standing where they were and only this file has forgotten them. Found by what
    /// they are and where, which needs no shape in the store and cannot lose the chest's contents to a shape change.
    /// </summary>
    public static bool Rebind(Map map, Point3D at)
    {
        if (!Running || map == null || map == Map.Internal || at == Point3D.Zero)
        {
            return false;
        }

        foreach (var item in map.GetItemsInRange(at, 3))
        {
            if (item is not { Deleted: false })
            {
                continue;
            }

            if (_fire == null && item.ItemID == FireId && !item.Movable)
            {
                _fire = item;
            }
            else if (_chest == null && item is WoodenChest { Movable: false } box)
            {
                _chest = box;

                // A chest pitched before this was known is scenery until it is told otherwise. See Pitch.
                box.LiftOverride = true;
            }
        }

        if (_fire == null && _chest == null)
        {
            return false;
        }

        logger.Information(
            "The Shadow's camp at ({X}, {Y}) was taken up again: {Fire}, {Chest}",
            at.X,
            at.Y,
            _fire == null ? "no fire" : "the fire still lit",
            _chest == null ? "no chest" : $"a chest holding {_chest.Items.Count} things"
        );

        return true;
    }

    /// <summary>
    /// Builds the camp at the hideout: the fire on the spot itself, the chest on the first standing tile beside it.
    /// Called when The Shadow chooses its ground, and again after a raid has burned the last one.
    /// </summary>
    public static void Pitch(Map map, Point3D at)
    {
        if (!Running || map == null || map == Map.Internal || at == Point3D.Zero)
        {
            return;
        }

        Burn(false);

        _fire = new Item(FireId)
        {
            Movable = false,
            Light = LightType.Circle300,
            Name = "a campfire"
        };

        _fire.MoveToWorld(at, map);

        // Beside the fire, not under it: the first of the eight tiles round it that something can stand on.
        for (var i = 0; i < 8; i++)
        {
            var x = at.X + (i is 0 or 1 or 7 ? 1 : i is 3 or 4 or 5 ? -1 : 0);
            var y = at.Y + (i is 1 or 2 or 3 ? 1 : i is 5 or 6 or 7 ? -1 : 0);

            if (!BotStep.Settle(map, x, y, out var z))
            {
                continue;
            }

            // <b>LiftOverride, or the chest is scenery and nothing can ever be put in it.</b> The engine calls an
            // immovable container lying in the world with no owner a decoration — Container.IsDecoContainer, which is
            // !Movable && !IsLockedDown && !IsSecure && Parent == null && !LiftOverride — and CheckHold refuses every
            // drop into a decoration, silently, for everybody. The band's chest was made immovable on the night of
            // 17.09.2026 so nobody could carry it off, and from that moment it could not accept a single thing: every
            // "nothing worth putting down" of 18.09, the thief's takings and the keeper's supplies alike, is this
            // line. Immovable it stays; a decoration it is not.
            _chest = new WoodenChest { Movable = false, Name = "the band's chest", LiftOverride = true };
            _chest.MoveToWorld(new Point3D(x, y, z), map);

            Pitched++;

            logger.Information(
                "The Shadow pitched its camp at ({X}, {Y}): a fire and a chest beside it",
                at.X,
                at.Y
            );

            return;
        }

        Unpitched++;

        logger.Information("The Shadow's fire at ({X}, {Y}) stands alone: no tile beside it would hold a chest", at.X, at.Y);
    }

    /// <summary>
    /// Burns the camp down. The chest's contents go with it, which is the price of being found: a raid that reaches the
    /// hideout takes the band's takings as surely as a catch takes a thief's pack.
    /// </summary>
    public static void Burn(bool counted = true)
    {
        var had = _fire != null || _chest != null;

        if (_chest is { Deleted: false })
        {
            var things = _chest.Items.Count;

            _chest.Delete();

            if (counted && things > 0)
            {
                logger.Information("The Shadow's chest burned with {Things} things in it", things);
            }
        }

        if (_fire is { Deleted: false })
        {
            _fire.Delete();
        }

        _fire = null;
        _chest = null;

        if (counted && had)
        {
            Burned++;
        }
    }

    /// <summary>
    /// One look round the fire: a bot that is not of the band within <see cref="HideWithin"/> sends every member standing
    /// there into hiding. Called from the underworld's own beat.
    /// </summary>
    public static void Watch(long now)
    {
        if (!Running || now - _watchedTick < WatchMs)
        {
            return;
        }

        _watchedTick = now;

        if (Fire is not { } fire || fire.Map is not { } map || map == Map.Internal)
        {
            return;
        }

        var seen = false;
        var here = fire.GetWorldLocation();

        foreach (var m in map.GetMobilesInRange<Mobile>(here, HideWithin))
        {
            // A hidden watcher is nobody: the debugger walks the island invisible, and a hidden thief is not a stranger.
            if (m is { Deleted: false, Alive: true } && !m.Hidden && !BotUnderworld.Member(m))
            {
                seen = true;

                break;
            }
        }

        if (!seen)
        {
            return;
        }

        var hid = 0;

        foreach (var m in map.GetMobilesInRange<BotMobile>(here, HideWithin))
        {
            // Not one that is already fighting: hiding puts up its weapon, and a member set on at its own fire has
            // been seen already. It hides when the fight is over, like anybody else.
            if (m is { Deleted: false, Alive: true, Hidden: false, Combatant: null } member && BotUnderworld.Member(member)
                && member.Resolve?.Deed is not (BotBrawl or BotRob or BotManhunt)
                && BotShadow.Ready(member) && BotShadow.Hide(member))
            {
                hid++;
            }
        }

        if (hid <= 0)
        {
            return;
        }

        Startled++;
        WentToGround += hid;

        logger.Information("Somebody came within {Tiles} of The Shadow's fire and {Hid} of the band went to ground", HideWithin, hid);
    }

    /// <summary>Books what a member put in the chest, for the summary line.</summary>
    public static void Took(int things, int gold)
    {
        Stashed += Math.Max(0, things);
        StashedGold += Math.Max(0, gold);
    }

    public static string Describe() =>
        Fire == null
            ? "The Shadow has no camp"
            : $"The Shadow's camp stands at ({Fire.X}, {Fire.Y}) with {Chest?.Items.Count ?? 0} things in the chest ({Pitched} pitched, {Unpitched} without a chest, {Burned} burned); {Stashed} things and {StashedGold}gp stashed; {Startled} strangers sent {WentToGround} of the band to ground";

    public static void Forget()
    {
        Pitched = 0;
        Unpitched = 0;
        Burned = 0;
        Startled = 0;
        WentToGround = 0;
        Stashed = 0;
        StashedGold = 0;
    }
}
