using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// A bot's bank box kept in order, and where the surplus of its capped kinds went: the box, a shopkeeper, another bot, the ground,
/// or nowhere because the box had no place for it (06.10.2026, box16).
///
/// <para>
/// <b>Fifty of 210 bank boxes were full, and they were full of ingot stacks lying apart.</b> A miner's trip put its bars in the
/// box with the engine's <c>DropItem</c> (<c>BotDig.Store</c>), which neither joins an existing stack nor asks the box's hundred
/// and twenty-five: every trip a new stack of every metal. At 05:36 on 06.10.2026 thirty-five Gatherers' boxes held 132 to 558
/// things each (Valindra 558, Durgan 499, Malachi 388), and the full boxes' places were Bronze 2044, Copper 1927, Dull Copper
/// 1916, Shadow Iron 1720, Gold 1636, Agapite 941: ten thousand places, where one stack a metal a box would take under three
/// hundred (2,452 trips "putting the metal away" in the 3.5 hours of build 405, each a new stack a metal). A full box refuses
/// every lot that does not join a stack, so the porter (<see cref="BotUnload.Sellable(IBotWilful, Mobile)"/>) counted nothing worth a trip and
/// the surplus draughts and bandages rode in the pack: Malachi took no unload in three and a half hours with 66 units over.
/// </para>
///
/// <para>
/// <see cref="Tidy"/> joins the stacks of one kind lying apart in a box, as a player does by dropping one pile on another:
/// nothing made, nothing lost, the engine's own <c>StackWith</c>. <see cref="Join"/> is the miner's way in now: onto a stack
/// of its kind first, a new place only for a metal the box has none of.
/// </para>
/// </summary>
public static class BotSurplus
{
    // ---- The box kept in order ----------------------------------------------------------------------------------------------

    /// <summary>How long one bot's box is left before it is tidied again. A full box is looked at once in this time.</summary>
    public static int TidyMs { get; set; } = 600000;

    /// <summary>Boxes in which stacks lying apart were joined, and the places that freed.</summary>
    public static long Tidied { get; private set; }

    public static long Joined { get; private set; }

    /// <summary>Lots a miner put in a box at or over its count because the box held no stack of the metal to join (one place a metal).</summary>
    public static long Overfilled { get; private set; }

    private static readonly Dictionary<Serial, long> _tidiedAt = [];

    /// <summary>
    /// Joins the stacks of one kind lying apart in this bot's bank box, when the box is at its count of things and was not tidied
    /// within <see cref="TidyMs"/>. Returns the places freed.
    /// </summary>
    public static int TidyIfFull(Mobile body)
    {
        var box = body?.FindBankNoCreate();

        if (box == null || box.Deleted || box.MaxItems <= 0 || box.TotalItems < box.MaxItems)
        {
            return 0;
        }

        var now = Core.TickCount;

        if (_tidiedAt.TryGetValue(body.Serial, out var at) && now - at < TidyMs)
        {
            return 0;
        }

        if (_tidiedAt.Count > 4096)
        {
            _tidiedAt.Clear();
        }

        _tidiedAt[body.Serial] = now;

        return Tidy(body, box);
    }

    /// <summary>
    /// Joins every stack in <paramref name="box"/> (top level only) onto the first stack of its kind, look and colour, by the
    /// engine's <c>StackWith</c>, which adds the amount and deletes the joined pile. Nothing bound, no container. Returns the places freed.
    /// </summary>
    public static int Tidy(Mobile body, Container box)
    {
        if (box == null || box.Deleted || box.Items.Count < 2)
        {
            return 0;
        }

        var bond = (body as BotMobile)?.Bond;
        Dictionary<(Type, int, int), Item> first = [];
        List<Item> items = [.. box.Items];
        var joined = 0;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (item == null || item.Deleted || !item.Stackable || item is Container || BotBinding.IsBound(item, bond))
            {
                continue;
            }

            var key = (item.GetType(), item.ItemID, item.Hue);

            if (!first.TryGetValue(key, out var pile) || pile.Deleted || pile.Parent != box)
            {
                first[key] = item;

                continue;
            }

            if (pile.StackWith(body, item, false))
            {
                joined++;

                continue;
            }

            // The first pile is at the engine's sixty thousand, or named apart: this one becomes the pile to join.
            first[key] = item;
        }

        if (joined > 0)
        {
            Tidied++;
            Joined += joined;
        }

        return joined;
    }

    /// <summary>
    /// Puts <paramref name="lot"/> in the box onto a stack of its kind if the box holds one (a full box takes it: the engine's
    /// count is of things, and a joined pile is no new thing). Returns whether it joined; the lot is untouched when it did not.
    /// </summary>
    public static bool Join(Mobile body, Container box, Item lot)
    {
        if (box == null || box.Deleted || lot == null || lot.Deleted || !lot.Stackable)
        {
            return false;
        }

        var items = box.Items;

        for (var i = 0; i < items.Count; i++)
        {
            var pile = items[i];

            if (pile is { Deleted: false } && pile is not Container && pile.StackWith(body, lot, false))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The miner's way into the box: onto a stack of the metal, else a new place, counted when the box was already full.</summary>
    public static void Put(Mobile body, Container box, Item lot)
    {
        if (Join(body, box, lot))
        {
            return;
        }

        if (box.MaxItems > 0 && box.TotalItems + 1 > box.MaxItems)
        {
            Overfilled++;
        }

        box.DropItem(lot);
    }

    /// <summary>
    /// The kinds this box holds stacks of, by kind, look and colour: a lot of one of them joins a stack and needs no place. Read
    /// once per walk of a pack, and only for a full box.
    /// </summary>
    public static HashSet<(Type, int, int)> Piles(Container box)
    {
        HashSet<(Type, int, int)> piles = [];

        if (box == null || box.Deleted)
        {
            return piles;
        }

        var items = box.Items;

        for (var i = 0; i < items.Count; i++)
        {
            var pile = items[i];

            if (pile is { Deleted: false, Stackable: true } && pile is not Container && pile.Amount < 60000)
            {
                piles.Add((pile.GetType(), pile.ItemID, pile.Hue));
            }
        }

        return piles;
    }

    // ---- Where the surplus of capped kinds went, over the last hour -------------------------------------------------------

    /// <summary>Routes of a capped kind's surplus, and the two ways it went nowhere.</summary>
    public const int Box = 0;

    public const int Shop = 1;

    public const int ToBot = 2;

    public const int Ground = 3;

    /// <summary>Units the box refused at the porter's store (it had no place and no stack to join).</summary>
    public const int Refused = 4;

    /// <summary>Units over the allowance of a kind the box has no place for, that no shopkeeper in reach buys (the peddler's look).</summary>
    public const int Stranded = 5;

    private const int Routes = 6;

    private static readonly string[] _named =
        ["box", "shopkeeper", "a bot", "the ground", "refused by a full box", "no box and no buyer (counted once a bot and kind in five minutes)"];

    /// <summary>The hour is kept in twelve slots of five minutes, the summary's own beat.</summary>
    public static int SlotMs { get; set; } = 300000;

    private const int Slots = 12;

    private static readonly Dictionary<Type, int>[,] _hour = new Dictionary<Type, int>[Routes, Slots];

    private static readonly long[] _slotOf = new long[Slots];

    private static readonly long[] _total = new long[Routes];

    /// <summary>Counts <paramref name="units"/> of a capped kind's surplus going by <paramref name="route"/>.</summary>
    public static void Count(int route, Type kind, int units)
    {
        if (kind == null || units <= 0 || route < 0 || route >= Routes)
        {
            return;
        }

        var slot = Slot();
        var into = _hour[route, slot] ??= [];

        into[kind] = into.GetValueOrDefault(kind) + units;
        _total[route] += units;
    }

    private static readonly Dictionary<(Serial, Type), long> _countedIn = [];

    /// <summary>
    /// Counts a standing state (not a movement) at most once a bot and kind in each five-minute slot: the peddler looks at a pack
    /// every minute, and a surplus nobody takes would otherwise be counted at every look.
    /// </summary>
    public static void CountOnce(int route, Mobile body, Type kind, int units)
    {
        if (body == null || kind == null || units <= 0)
        {
            return;
        }

        var n = Core.TickCount / Math.Max(1000, SlotMs);
        var key = (body.Serial, kind);

        if (_countedIn.TryGetValue(key, out var last) && last == n)
        {
            return;
        }

        if (_countedIn.Count > 8192)
        {
            _countedIn.Clear();
        }

        _countedIn[key] = n;
        Count(route, kind, units);
    }

    /// <summary>Counts what was moved of <paramref name="kind"/> only if it is a capped kind for this bot (a number kept, not all and not none).</summary>
    public static void CountIfCapped(IBotWilful bot, int route, Type kind, int units)
    {
        if (bot == null || kind == null || units <= 0)
        {
            return;
        }

        if (BotUnload.Keeps(bot).TryGetValue(kind, out var cap) && cap > 0 && cap < int.MaxValue)
        {
            Count(route, kind, units);
        }
    }

    private static int Slot()
    {
        var n = Core.TickCount / Math.Max(1000, SlotMs);
        var i = (int)(n % Slots);

        if (_slotOf[i] != n)
        {
            _slotOf[i] = n;

            for (var r = 0; r < Routes; r++)
            {
                _hour[r, i]?.Clear();
            }
        }

        return i;
    }

    private static Dictionary<Type, int> Hour(int route)
    {
        var now = Core.TickCount / Math.Max(1000, SlotMs);
        Dictionary<Type, int> sum = [];

        for (var i = 0; i < Slots; i++)
        {
            var slot = _hour[route, i];

            if (slot == null || now - _slotOf[i] >= Slots)
            {
                continue;
            }

            foreach (var (kind, units) in slot)
            {
                sum[kind] = sum.GetValueOrDefault(kind) + units;
            }
        }

        return sum;
    }

    /// <summary>The `Packs:` clause: boxes tidied, and each route over the last hour by kind (the four largest), with the total since the load.</summary>
    public static string Describe()
    {
        List<string> parts = [];

        for (var r = 0; r < Routes; r++)
        {
            var hour = Hour(r);
            var units = 0;
            List<(Type Kind, int Units)> kinds = [];

            foreach (var (kind, many) in hour)
            {
                units += many;
                kinds.Add((kind, many));
            }

            kinds.Sort((a, b) => b.Units.CompareTo(a.Units));

            List<string> named = [];

            for (var i = 0; i < kinds.Count && i < 4; i++)
            {
                named.Add($"{kinds[i].Kind.Name} {kinds[i].Units}");
            }

            parts.Add(named.Count == 0 ? $"{_named[r]} 0 ({_total[r]})" : $"{_named[r]} {units} ({_total[r]}; {string.Join(", ", named)})");
        }

        return $"capped surplus over the last hour (since the load): {string.Join(", ", parts)}; {Tidied} bank boxes tidied, {Joined} places freed by joining stacks lying apart, {Overfilled} lots a miner put in a box at its count for want of a stack to join";
    }

    /// <summary>Whether this bot's bank box is at its count of things: for /api/state.</summary>
    public static bool BoxFull(Mobile body)
    {
        var box = body?.FindBankNoCreate();

        return box is { Deleted: false, MaxItems: > 0 } && box.TotalItems >= box.MaxItems;
    }
}
