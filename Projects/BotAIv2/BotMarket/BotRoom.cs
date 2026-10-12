using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// A buyer whose pack refused a hand-over: marked "no room", passed over by every carrier while it stays so, and pressed to make
/// room (06.10.2026, deliver16).
///
/// <para>
/// <b>Measured on build 407 (<c>session-2026-10-06_06-12.log</c>, 06:12–09:08, an-deliver16).</b> 267 of the 444 failed
/// deliveries (60 %) ended "&lt;buyer&gt;'s pack would not take &lt;kind&gt;", from only 14 buyers (Brokk 72, Hrolf 46, Dorin 43, Greta
/// 35): a pack at the engine's 125 things (Adela, live at 09:20: "at 125 of 125 things", 352 of 390 stones) refuses every lot that
/// needs a place of its own. The refusal was counted (<see cref="BotHandover.PackFull"/>) and nothing read it: the want stayed
/// open, and the next carrier took the same walk 22 seconds later (the median gap between two refusals at one buyer; 202 of 253
/// gaps under two minutes). 160 of the 267 were followed within 20 minutes by an accepted hand-over of the same kind to the same
/// buyer, so the state passes: a mark that lasts <see cref="MarkMs"/>, not an exile.
/// </para>
///
/// <para>
/// <b>What a mark does.</b> The courier passes the buyer over while it is marked and its pack still shows no room
/// (<see cref="Shut"/>; <see cref="BotPass.Room"/> on the want, so a line that fails names it). The buyer is pressed to make room:
/// its peddler weighs the pack at once, lets go of what was held for a shift at the shop, most places first
/// (<c>BotPeddler</c>), and its porter takes the trip to the bank when the box can take anything (<c>BotPorter</c>). The mark
/// clears the moment the pack shows <see cref="Places"/> free places and room for the refused lot's weight
/// (<see cref="MadeRoom"/>), or when <see cref="MarkMs"/> runs out (<see cref="Lapsed"/>). The first refusal of each episode is
/// still a failed delivery; nothing here changes what counts.
/// </para>
/// </summary>
public static class BotRoom
{
    /// <summary>Whether a refused hand-over marks its buyer at all. Off, carriers walk to a full pack as before.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How long a mark stands at most without the pack making room. Ten minutes.</summary>
    public static int MarkMs { get; set; } = 600000;

    /// <summary>How many free places a marked pack must show to read as having room again. Three: the lot, and a coin's pile.</summary>
    public static int Places { get; set; } = 3;

    private sealed class Mark
    {
        public long Tick;

        public int Weight;
    }

    private static readonly Dictionary<Serial, Mark> _marks = [];

    private static readonly List<Serial> _gone = [];

    // ---- Counted ------------------------------------------------------------------------------------------------------------

    /// <summary>Hand-overs that marked their buyer (a new mark, or one renewed).</summary>
    public static long Marks { get; private set; }

    /// <summary>Carrier looks that passed a want over for its buyer's marked pack.</summary>
    public static long HeldOff { get; private set; }

    /// <summary>Marks cleared because the pack showed room again; marks that ran out with none; marks dropped with their buyer dead or gone.</summary>
    public static long MadeRoom { get; private set; }

    public static long Lapsed { get; private set; }

    public static long Fell { get; private set; }

    /// <summary>Loads a marked buyer's peddler offered, and trips to the bank its porter offered, to make room.</summary>
    public static long Peddles { get; private set; }

    public static long Trips { get; private set; }

    /// <summary>Buyers marked now.</summary>
    public static int Standing => _marks.Count;

    /// <summary>
    /// The hand-over in <see cref="BotHandover.Exchange"/> found the buyer's pack would not take a lot of this weight (in stones):
    /// the buyer is marked from now. Called whether anything went over before it or not: either way the pack is full now.
    /// </summary>
    internal static void Refused(Mobile buyer, int weight)
    {
        if (!Running || buyer == null)
        {
            return;
        }

        if (_marks.Count > 512)
        {
            Sweep(Core.TickCount);
        }

        if (!_marks.TryGetValue(buyer.Serial, out var mark))
        {
            mark = new Mark();
            _marks[buyer.Serial] = mark;
        }

        mark.Tick = Core.TickCount;
        mark.Weight = Math.Max(mark.Weight, Math.Max(1, weight));
        Marks++;
    }

    /// <summary>
    /// Whether a carrier passes this buyer over: marked, the mark fresh, and the pack still without room. A pack that shows room
    /// again, or a mark past <see cref="MarkMs"/>, is cleared here and counted. A dictionary lookup for an unmarked buyer.
    /// </summary>
    public static bool Shut(Mobile buyer)
    {
        if (!Running || buyer == null || _marks.Count == 0 || !_marks.TryGetValue(buyer.Serial, out var mark))
        {
            return false;
        }

        return !Clear(buyer, mark, Core.TickCount);
    }

    /// <summary>A carrier's look passed a want over for its buyer's marked pack. See <see cref="BotCourier"/>.</summary>
    internal static void Held() => HeldOff++;

    /// <summary>Whether this bot is marked and has not made room yet: its peddler and porter are pressed. See <see cref="Shut"/>.</summary>
    public static bool Pressing(Mobile bot) => Shut(bot);

    internal static void Peddled() => Peddles++;

    internal static void Tripped() => Trips++;

    /// <summary>Clears the mark if its time is up, the pack has room, or the buyer is gone; returns whether it was cleared.</summary>
    private static bool Clear(Mobile buyer, Mark mark, long now)
    {
        if (buyer.Deleted || !buyer.Alive)
        {
            _marks.Remove(buyer.Serial);
            Fell++;

            return true;
        }

        if (HasRoom(buyer, mark.Weight))
        {
            _marks.Remove(buyer.Serial);
            MadeRoom++;

            return true;
        }

        if (now - mark.Tick >= MarkMs)
        {
            _marks.Remove(buyer.Serial);
            Lapsed++;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Whether the pack has <see cref="Places"/> free places of the engine's count and room for <paramref name="weight"/> stones
    /// of its own: the two questions <c>Container.CheckHold</c> asks of a lot dropped into it.
    /// </summary>
    public static bool HasRoom(Mobile buyer, int weight)
    {
        var pack = buyer?.Backpack;

        if (pack == null)
        {
            return false;
        }

        var most = pack.MaxItems;

        if (most > 0 && pack.TotalItems + Math.Max(1, Places) > most)
        {
            return false;
        }

        var heaviest = pack.MaxWeight;

        return heaviest <= 0 || pack.TotalWeight + Math.Max(0, weight) <= heaviest;
    }

    // ---- A pack at its count, read before any walk (06.10.2026, work17) ------------------------------------------------------
    //
    // <b>The mark was read by the carriers only, and only after a refusal.</b> On build 410 (an-work17) all 31 failed hauls ended
    // at a buyer's full pack, every one of them within ten minutes of that buyer's previous refusal: BotHauler never asked
    // Shut. The buyers were 11 of the 18 Crafters, their packs at 125 of 125 things (Argus 12:52: Hrolf LeatherCap x100, Varek
    // x70, Jorunna x41, Adela x33, spec pieces that do not stack), who also said wants for more material (restock 32 failures
    // "no room in the pack", meet 26 "gave the delivery up" after the carrier's refusal). A pack at its count takes nothing
    // that needs a place of its own; the questions below are asked before a want is said, a haul or a restock offered, a
    // meeting called, or a piece made on spec.

    /// <summary>
    /// Free places a pack must keep for its bot to make a piece nobody ordered: a stint's pieces each take one (a cap, a dagger,
    /// a potion do not stack), and the rest is for what carriers bring. Twenty.
    /// </summary>
    public static int SpecPlaces { get; set; } = 20;

    /// <summary>
    /// How full a pack (of the engine's count of things) must be before its peddler lifts the shop-shift hold and sells the kinds
    /// holding most places first, as for a marked pack. Four fifths: 100 of 125.
    /// </summary>
    public static double PackedShare { get; set; } = 0.8;

    /// <summary>Bots marked by a look that found the pack could not take a lot (no refusal yet): its peddler and porter are pressed.</summary>
    public static long Seen { get; private set; }

    /// <summary>Wants not said because the asker's pack could not take the thing. See <see cref="BotMarket.Ask"/>.</summary>
    public static long Unasked { get; private set; }

    /// <summary>Restocks not offered because the pack could not take the purchase. See <c>BotShopper</c>.</summary>
    public static long RestockHeld { get; private set; }

    /// <summary>Spec stints (a piece nobody ordered) not offered for a pack with fewer than <see cref="SpecPlaces"/> free places.</summary>
    public static long SpecHeld { get; private set; }

    /// <summary>Trader looks that passed a want over for its buyer's pack (marked, or at its count). See <c>BotHauler</c>.</summary>
    public static long HaulHeld { get; private set; }

    /// <summary>Haul orders struck before the purchase or the walk for the buyer's pack. See <c>BotHaul.Prune</c>.</summary>
    public static long HaulStruck { get; private set; }

    /// <summary>Carrier looks that passed a buyer over whose pack was at its count before any refusal. See <see cref="BotCourier"/>.</summary>
    public static long FirstHeld { get; private set; }

    /// <summary>Meetings not called with a buyer whose pack could not take the goods. See <see cref="BotRendezvous.Call"/>.</summary>
    public static long MeetHeld { get; private set; }

    /// <summary>Peddler loads out of a pack at <see cref="PackedShare"/> of its places, the shop-shift hold lifted.</summary>
    public static long PackedLoads { get; private set; }

    /// <summary>Free places in the pack by the engine's count; int.MaxValue for a pack with no count.</summary>
    public static int Free(Mobile body)
    {
        var pack = body?.Backpack;

        if (pack == null)
        {
            return 0;
        }

        return pack.MaxItems <= 0 ? int.MaxValue : Math.Max(0, pack.MaxItems - pack.TotalItems);
    }

    /// <summary>
    /// Whether this bot's pack would take a lot of this kind: a free place, or a stack of the kind already in it that a stackable
    /// lot joins (the engine joins before it counts). With <paramref name="coin"/>, also a place for the coin drawn to pay with
    /// unless a pile of gold is there to join it ("the pack would not hold the 5gp it drew to pay with"). Walks the pack only when
    /// it is at its count.
    /// </summary>
    public static bool Receives(Mobile body, Type kind, bool coin = false)
    {
        var pack = body?.Backpack;

        if (pack == null)
        {
            return false;
        }

        var free = Free(body);

        if (free >= (coin ? 2 : 1))
        {
            return true;
        }

        var joins = false;
        var piled = false;
        var items = pack.Items;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (item == null || item.Deleted || !item.Stackable)
            {
                continue;
            }

            if (kind != null && item.GetType() == kind && item.Amount < 60000)
            {
                joins = true;
            }
            else if (item is Gold && item.Amount < 60000)
            {
                piled = true;
            }
        }

        var needed = (joins ? 0 : 1) + (coin && !piled ? 1 : 0);

        return free >= needed;
    }

    /// <summary>
    /// A look found this bot's pack could not take what it was about to ask for or be brought: it is marked as a refusal would
    /// mark it, so carriers pass it over and its peddler and porter are pressed, and cleared by the same pack count. Counted
    /// apart from <see cref="Marks"/>.
    /// </summary>
    public static void Note(Mobile body)
    {
        if (!Running || body == null)
        {
            return;
        }

        if (_marks.Count > 512)
        {
            Sweep(Core.TickCount);
        }

        if (_marks.ContainsKey(body.Serial))
        {
            return;
        }

        _marks[body.Serial] = new Mark { Tick = Core.TickCount, Weight = 1 };
        Seen++;
    }

    /// <summary>Whether a pack is crowded enough for its peddler to sell as a marked pack does. See <see cref="PackedShare"/>.</summary>
    public static bool Packed(Mobile body)
    {
        var pack = body?.Backpack;

        return pack != null && pack.MaxItems > 0 && pack.TotalItems >= pack.MaxItems * PackedShare;
    }

    /// <summary>
    /// Whether a piece nobody ordered may be made: the pack keeps <see cref="SpecPlaces"/> free places. Refusals counted
    /// (<see cref="SpecHeld"/>), and the bot noted when its pack is at its count.
    /// </summary>
    public static bool SpecShut(Mobile body)
    {
        if (!Running || SpecPlaces <= 0)
        {
            return false;
        }

        var free = Free(body);

        if (free >= SpecPlaces)
        {
            return false;
        }

        SpecHeld++;

        if (free <= 0)
        {
            Note(body);
        }

        return true;
    }

    internal static void NotAsked() => Unasked++;

    internal static void RestockNotOffered() => RestockHeld++;

    internal static void HaulPassed() => HaulHeld++;

    internal static void HaulOrderStruck() => HaulStruck++;

    internal static void FirstPassed() => FirstHeld++;

    internal static void MeetNotCalled() => MeetHeld++;

    internal static void PackedLoad() => PackedLoads++;

    /// <summary>Drops the marks of the gone, the dead and the stale. Called when the table grows.</summary>
    private static void Sweep(long now)
    {
        _gone.Clear();

        foreach (var (serial, mark) in _marks)
        {
            if (now - mark.Tick >= MarkMs || World.FindMobile(serial) is not { Deleted: false, Alive: true })
            {
                _gone.Add(serial);
            }
        }

        for (var i = 0; i < _gone.Count; i++)
        {
            _marks.Remove(_gone[i]);
            Lapsed++;
        }
    }

    /// <summary>The clause of <c>The market:</c>.</summary>
    public static string Describe() =>
        !Running
            ? "full packs are not marked (off)"
            : $"full packs: {Marks} refused hand-overs marked their buyer for up to {MarkMs / 60000} min ({Standing} marked now), {HeldOff} carrier looks held off a marked buyer, "
              + $"{Peddles} loads its peddler and {Trips} trips its porter offered to make room, {MadeRoom} marks cleared when the pack showed {Places} free places, {Lapsed} ran out with no room made, {Fell} dropped with the buyer dead or gone; "
              + $"packs at their count read before the walk: {Seen} bots marked by a look, {FirstHeld} carrier looks and {HaulHeld} trader looks passed such a buyer over, {HaulStruck} haul orders struck before the purchase or the walk, {MeetHeld} meeting calls refused, "
              + $"{Unasked} wants not said and {RestockHeld} restocks not offered for a pack that could not take the thing, {SpecHeld} spec stints held for a pack under {SpecPlaces} free places, {PackedLoads} peddler loads out of a pack at {PackedShare:P0} of its places";

    public static void Forget()
    {
        _marks.Clear();
        Marks = 0;
        HeldOff = 0;
        MadeRoom = 0;
        Lapsed = 0;
        Fell = 0;
        Peddles = 0;
        Trips = 0;
        Seen = 0;
        Unasked = 0;
        RestockHeld = 0;
        SpecHeld = 0;
        HaulHeld = 0;
        HaulStruck = 0;
        FirstHeld = 0;
        MeetHeld = 0;
        PackedLoads = 0;
    }
}
