using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// A crafter takes the work asked of it before it goes mining: an order's piece, a delivery to a want, its own trade's material
/// for one, or that material out of its own bank box. Mining stays its work when nothing is asked.
///
/// <para>
/// <b>Patrick, 03.10.2026: "proposal 4 — a crafter makes the order first, then goes to mine. Do it."</b> Measured on build 396
/// (04:19–12:47, 210 bots, 18 crafters): crafters spent 404 of their 911 finished minutes an hour mining (44 %) and 65 crafting
/// (7 %); mining brought them 8gp an hour in coin and their ingots peddled 333gp of their 5,212gp an hour, while deliveries
/// brought 1,926 and peddled goods 1,885. Of their 1,281 mining takes, 441 came while a line stood open for a kind that crafter
/// had made that session (LeatherCap 153, PlateGorget 136, LeatherBustierArms 110, LeatherSkirt 107 takes …). The auction never
/// preferred mining to an order a crafter could make — a forge or a needle for an order opens at hundreds a minute — but it
/// preferred mining to everything around one: the runner-up of a crafter's mining take was a restock of its own trade's
/// material 111 times (feathers 73, leather 30, logs, cloth), a delivery of its own pieces to a buyer 25 times, and its smiths
/// passed an order over for want of iron 6,746 times while their own ingots lay in their bank boxes.
/// </para>
///
/// <para>
/// <b>An order, not a price (<see cref="BotWill"/>'s auction).</b> Restocks are priced to lose on purpose and a delivery is
/// priced by its walk; raising either to beat a dig would lift them over every hunt on the island too. So the auction keeps its
/// prices and, for a crafter (not a gatherer, whose living mining is), a winning dig or prospect gives way to the best work asked
/// of it that is worth anything at all (<see cref="Asked"/>); nothing else changes, and a crafter with nothing asked digs as
/// before. Work already in hand is not taken away: the next choice after the trip is the crafter's. Counted by what took the
/// dig's place (<see cref="Describe"/>, in <c>Roles:</c>).
/// </para>
///
/// <para>
/// <b>And its own material first (<see cref="Propose"/>).</b> A crafter that passed an order over for want of material
/// (<see cref="Short"/>: the smith's metal, the tailor's leather, the fletcher's feathers) and holds it in its own bank box is
/// offered the walk to the nearest bank to take it out (<see cref="BotDraw"/>).
/// </para>
/// </summary>
public sealed class BotCraftFirst : IBotProposer
{
    public string Name => "CraftFirst";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>Whether mining gives way to asked work for a crafter. Off: the auction as it was.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How long a crafter remembers the material an order it passed over lacked. Ten minutes, the smith's own memory.</summary>
    public static int ShortMs { get; set; } = 600000;

    /// <summary>How far a crafter walks to a bank for its own material.</summary>
    public static int DrawReach { get; set; } = 400;

    /// <summary>
    /// What the walk to the bank box is claimed at a minute. Above digging's prior (45) and a restock's (12), under a rescue (200):
    /// the errand that unblocks an order with money down.
    /// </summary>
    public static double DrawClaim { get; set; } = 120.0;

    private static readonly Dictionary<Serial, (Type Kind, int Need, long Tick)> _short = [];

    private static readonly Dictionary<string, long> _yielded = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Choices where a crafter's winning dig gave way to asked work, by the kind of work taken instead.</summary>
    public static long Yielded { get; private set; }

    /// <summary>Choices where a crafter's dig won with no asked work worth anything on offer.</summary>
    public static long Dug { get; private set; }

    /// <summary>Orders a crafter passed over for want of material it then remembered, by trade.</summary>
    public static long Remembered { get; private set; }

    /// <summary>
    /// Looks that offered the walk to the bank box (one a review while it stands unchosen), walks taken, looks that found none of
    /// the material in the box (the memory is then let go) or no bank in reach, and units taken out.
    /// </summary>
    public static long Draws { get; private set; }

    public static long DrawsTaken { get; private set; }

    public static long Boxless { get; private set; }

    public static long Bankless { get; private set; }

    public static long Drawn { get; private set; }

    /// <summary>Walks that reached the bank and took nothing out (the pack would not take it, or the box was emptied on the way).</summary>
    public static long DrawsEmpty { get; private set; }

    // ---- Who, and what ------------------------------------------------------------------------------------------------------

    /// <summary>A crafter: the Crafter and the Architect. Never a gatherer, whose living is the dig (half its gold).</summary>
    public static bool Crafter(IBotWilful bot) => bot?.Self is BotMobile { Class: BotCrafter or BotArchitect };

    /// <summary>Whether this work is the dig this rule puts behind asked work: mining, or prospecting for a seam.</summary>
    public static bool Mining(BotDeed deed) => deed is BotDig or BotProspect;

    /// <summary>
    /// Work somebody asked of this crafter: a piece for an order, a delivery to a want (or the meeting with a carrier bringing its
    /// own), its own trade's material bought for the trade, or out of its bank box for an order.
    /// </summary>
    public static bool Asked(BotDeed deed) =>
        deed switch
        {
            BotBrew brew => brew.Order != null,
            IBotWantCarrier => true,
            BotMeet => true,
            BotForge forge => forge.Order != null,
            BotSew sew => sew.Order != null,
            BotTinker tinker => tinker.Order != null,
            BotFletch fletch => fletch.Order != null,
            BotBowmake bow => bow.Order != null,
            BotDraw => true,
            BotRestock restock => Material(restock.Wanted),
            _ => false
        };

    /// <summary>What a crafter's trades are made of: metal, hide, cloth, feathers and wood.</summary>
    public static bool Material(Type kind) =>
        kind != null && (typeof(BaseIngot).IsAssignableFrom(kind) || typeof(BaseLeather).IsAssignableFrom(kind)
                         || typeof(BaseHides).IsAssignableFrom(kind) || kind == typeof(Cloth) || kind == typeof(UncutCloth)
                         || kind == typeof(BoltOfCloth) || kind == typeof(Feather) || kind == typeof(Shaft) || kind == typeof(Log)
                         || kind == typeof(Board));

    /// <summary>The auction put asked work before a crafter's dig. Counted by what was taken instead.</summary>
    internal static void Gave(BotDeed taken)
    {
        Yielded++;

        var kind = taken?.Kind ?? "?";

        _yielded[kind] = _yielded.GetValueOrDefault(kind) + 1;
    }

    /// <summary>A crafter's dig won with nothing asked of it worth anything.</summary>
    internal static void Kept() => Dug++;

    /// <summary>
    /// A craft's look passed an order over for want of <paramref name="need"/> of <paramref name="kind"/>: remembered for
    /// <see cref="ShortMs"/>, so the walk to the bank box can be offered. The smith's metal, the tailor's leather, the fletcher's
    /// feathers.
    /// </summary>
    public static void Short(Mobile body, Type kind, int need)
    {
        if (body == null || kind == null || need <= 0)
        {
            return;
        }

        if (_short.Count > 4096)
        {
            _short.Clear();
        }

        _short[body.Serial] = (kind, need, Core.TickCount);
        Remembered++;
    }

    /// <summary>
    /// The walk reached the bank: what it took out, and the memory let go either way, so a box the pack cannot take from is not
    /// walked to again on the next review (the review of this build). A walk that took nothing is counted (<see cref="DrawsEmpty"/>).
    /// </summary>
    internal static void Served(Mobile body, int units)
    {
        if (body != null)
        {
            _short.Remove(body.Serial);
        }

        if (units > 0)
        {
            Drawn += units;
        }
        else
        {
            DrawsEmpty++;
        }
    }

    internal static void Walked() => DrawsTaken++;

    // ---- The walk to the bank box ---------------------------------------------------------------------------------------------

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (!Running || map == null || map == Map.Internal || !body.Alive || !Crafter(bot)
            || !_short.TryGetValue(body.Serial, out var known))
        {
            return null;
        }

        if (Core.TickCount - known.Tick >= ShortMs)
        {
            _short.Remove(body.Serial);

            return null;
        }

        var lack = known.Need - (body.Backpack?.GetAmount(known.Kind) ?? 0);

        // Enough in the pack now, or standing at a bank, where the craft's own look takes it out itself.
        if (lack <= 0 || BotMarket.AtBank(body))
        {
            return null;
        }

        var box = body.FindBankNoCreate();
        var boxed = box?.GetAmount(known.Kind) ?? 0;

        if (boxed <= 0)
        {
            Boxless++;
            _short.Remove(body.Serial);

            return null;
        }

        var town = BotTowns.Nearest(body.Location);
        var hearth = town == null ? Point3D.Zero : BotSettle.Of(town)?.Hearth ?? town.Square;
        var bank = hearth == Point3D.Zero ? Point3D.Zero : BotShopkeep.BankNear(map, hearth);

        if (bank == Point3D.Zero || !Utility.InRange(body.Location, bank, DrawReach) || !BotGates.Joined(map, body.Location, bank)
            || bot.Resolve?.Ledger?.Cautious(BotDraw.Trade, map, bank) == true)
        {
            Bankless++;

            return null;
        }

        Draws++;

        return new BotDraw(map, bank, known.Kind, Math.Min(lack, boxed));
    }

    // ---- Said ------------------------------------------------------------------------------------------------------------------

    public static string Describe()
    {
        List<KeyValuePair<string, long>> ranked = [.. _yielded];

        ranked.Sort(static (a, b) => b.Value.CompareTo(a.Value));

        var by = ranked.Count == 0 ? "none" : string.Join(", ", ranked.ConvertAll(static kv => $"{kv.Key} {kv.Value}"));

        return $"a crafter's winning dig gave way to work asked of it {Yielded} times ({by}) and was kept {Dug} times with nothing asked worth anything; "
               + $"{Remembered} orders passed over for want of material remembered ({_short.Count} crafters remembering one now), {Draws} looks offered a walk to its own bank box for it, {DrawsTaken} walks taken ({Drawn} units taken out, {DrawsEmpty} walks that took nothing), {Boxless} looks found none of it in the box and {Bankless} no bank within {DrawReach} tiles";
    }

    public static void Forget()
    {
        _short.Clear();
        _yielded.Clear();
        Yielded = 0;
        Dug = 0;
        Remembered = 0;
        Draws = 0;
        DrawsTaken = 0;
        Boxless = 0;
        Bankless = 0;
        Drawn = 0;
        DrawsEmpty = 0;
    }
}
