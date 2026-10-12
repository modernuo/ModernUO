using System;
using System.Collections.Generic;
using System.IO;
using Server.Json;
using Server.Guilds;
using Server.Logging;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>What <c>Configuration/bot-townground.json</c> may say. Everything optional; no file, no record.</summary>
public sealed class BotTownGroundSettings
{
    public bool? Running { get; set; }

    public int? SweepEveryMs { get; set; }

    /// <summary>What a guild really paid for a square, read off the claim lines of the logs: refunded instead of the standing price.</summary>
    public List<BotTownGroundPaid> Paid { get; set; }
}

/// <summary>One square's real price: the guild, any tile of the square, and the gold its last winning claim cost.</summary>
public sealed class BotTownGroundPaid
{
    public string Guild { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public int Gold { get; set; }
}

/// <summary>
/// <b>No guild holds a square where the town's guards act</b> (Patrick, 07.10.2026 ~10:20: "guilds may not claim town squares
/// with guards. Remove such claims right now and give the guilds their money back if they paid for the claim").
///
/// <para>
/// <b>The test, exact and the engine's own.</b> A thirty-tile square (<see cref="BotQuad.Key(Map, Point3D)"/>) is town ground
/// when on any one of its 900 tiles the engine's answer to "would a guard come here" is yes: <c>Region.Find(tile, map)</c> at
/// z 0, then <c>GetRegion&lt;GuardedRegion&gt;()</c> not disabled — the same question <c>BotHaven.Guarded</c> and
/// <c>BotOutlaw.Guarded</c> ask of one point. So a town's <c>TownRegion</c> (Britain, Trinsic, Yew, …) and the moongates'
/// <c>GuardedRegion</c> count, Buccaneer's Den (guards disabled) does not, and a corner of sixteen tiles counts as much as
/// a square wholly inside the walls: strict, like the walls of a hall (<c>BotBounds</c>). Asked once a square and kept
/// (<see cref="Guarded(Map, ValueTuple{int, int, int}, out string, out int)"/>); a square whose sectors carry no guarded
/// region is answered without a single probe. The answers are forgotten at every sweep, so a town whose guards a GM turned
/// off or on is read again within the hour.
/// </para>
///
/// <para>
/// <b>What it refuses.</b> The staker (<c>BotHolder.Worth</c>) passes such a square over, open or held (a held one is
/// therefore never weighed for a seizure, <c>BotSeizure.Weigh</c>), and <c>BotClaim.Open</c> refuses it as the last wall.
/// <b>What it sweeps.</b> At the first look after the halls are adopted, and every <see cref="SweepEveryMs"/> after, every
/// held square on town ground is dropped (WRN, naming the guild, the square and the town) and the guild gets back what it
/// paid for it: the claim store records whether a square was paid for, not the price, so a bought square is refunded at
/// the standing price for its kind (<c>BotClaim.Price</c>, 5000gp — the same as <c>BotClaim.Ousting</c>) and a free one
/// with nothing. The coin comes out of the city's purse, which took every coin of the claims (<c>BotCity.Tax</c>,
/// build 408), into the guild's chest; a purse that cannot cover it still makes the chest whole, and the shortfall is
/// counted and logged. A claim still mustering on town ground is called off and its held price goes back to the chest.
/// </para>
///
/// <para>
/// <b>What it does not touch.</b> A guild hall's own square (its yard, <c>BotClaim.Seat</c>) stays its guild's even when it
/// reaches into a town, and a seat is a point, not a claim. A guild house in a town is bought from the town
/// (<c>BotGuildHouses</c>) and is a different thing altogether. A yard in a town cannot be seized, though: no claim of any
/// kind is opened on town ground.
/// </para>
/// </summary>
public static class BotTownGround
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotTownGround));

    /// <summary>Whether town ground is refused and swept. Off: claims are opened and held anywhere, as before 07.10.2026.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How often every held square is looked at again. An hour.</summary>
    public static int SweepEveryMs { get; set; } = 3600000;

    /// <summary>Staker looks at an open square passed over because it lies on town ground. Counts squares looked at.</summary>
    public static long OpenPassed { get; private set; }

    /// <summary>Staker looks at another guild's square passed over (never weighed for a seizure) because it lies on town ground.</summary>
    public static long HeldPassed { get; private set; }

    /// <summary>Claims refused at <c>BotClaim.Open</c> itself for town ground: the last wall.</summary>
    public static long Refused { get; private set; }

    /// <summary>Sweeps run, and held squares dropped by them; of those, how many had been paid for.</summary>
    public static long Sweeps { get; private set; }

    public static long Dropped { get; private set; }

    public static long DroppedBought { get; private set; }

    /// <summary>What the dropped squares' guilds got back, and how much of it the city's purse could not cover.</summary>
    public static long Refunded { get; private set; }

    public static long Short { get; private set; }

    /// <summary>Of the dropped squares, those whose guild was gone, so nothing was refunded.</summary>
    public static long Orphans { get; private set; }

    /// <summary>Claims still mustering on town ground called off by a sweep, and the held price handed back to their chests.</summary>
    public static long BidsCalled { get; private set; }

    public static long BidsReturned { get; private set; }

    /// <summary>Halls' yards on town ground, kept, at the last sweep. A gauge, not a count.</summary>
    public static int YardsKept { get; private set; }

    private static readonly Dictionary<(int Map, int X, int Y), (string Town, int Tiles)> _known = [];

    private const string ConfigPath = "Configuration/bot-townground.json";

    /// <summary>
    /// <b>The real prices, by the record</b> (Patrick, 07.10.2026: "give the guilds their money back if they paid for the
    /// claim"). The claim store keeps whether a square was bought, not for how much, and the ladder of prices
    /// (<c>BotClaim.Price</c> up to <c>Cheap</c>, then <c>Step</c> more each) made The Anchor's later squares cost 7000 to
    /// 16000. The prices the logs show are read from <see cref="ConfigPath"/> by guild and square; each is used once, and a
    /// square not in it is refunded at the standing price.
    /// </summary>
    private static readonly Dictionary<(string Guild, int X, int Y), int> _record = [];

    /// <summary>Squares refunded at the price the record shows, and at the standing price, with the gold of each.</summary>
    public static long ByRecord { get; private set; }

    public static long ByRecordGold { get; private set; }

    public static long AtStanding { get; private set; }

    public static long AtStandingGold { get; private set; }

    private static long _sweepShort;

    /// <summary>Reads the dials and the record of real prices. Called at the estate's start; no file is written if there is none.</summary>
    public static void Load()
    {
        var settings = JsonConfig.Deserialize<BotTownGroundSettings>(Path.Combine(Core.BaseDirectory, ConfigPath));

        if (settings == null)
        {
            return;
        }

        Running = settings.Running ?? Running;
        SweepEveryMs = settings.SweepEveryMs ?? SweepEveryMs;
        _record.Clear();

        var gold = 0L;

        for (var i = 0; settings.Paid != null && i < settings.Paid.Count; i++)
        {
            var paid = settings.Paid[i];

            if (paid == null || string.IsNullOrEmpty(paid.Guild) || paid.Gold <= 0)
            {
                continue;
            }

            _record[(paid.Guild, (int)Math.Floor(paid.X / (double)BotQuad.Side), (int)Math.Floor(paid.Y / (double)BotQuad.Side))] = paid.Gold;
            gold += paid.Gold;
        }

        logger.Information(
            "Town ground: {Count} squares' real prices on record ({Gold}gp), refunded instead of the standing price if they lie on town ground",
            _record.Count,
            gold
        );
    }

    private static long _swept;

    private static bool _sweptOnce;

    /// <summary>Whether a square lies on town ground, and the town and how many of its tiles. Asked once a square, then remembered.</summary>
    public static bool Guarded(Map map, (int Map, int X, int Y) key, out string town, out int tiles)
    {
        town = null;
        tiles = 0;

        if (!Running || map == null || map == Map.Internal)
        {
            return false;
        }

        if (!_known.TryGetValue(key, out var known))
        {
            known = Probe(map, key);
            _known[key] = known;
        }

        town = known.Town;
        tiles = known.Tiles;

        return tiles > 0;
    }

    /// <summary>Whether the square this point falls in lies on town ground.</summary>
    public static bool Guarded(Map map, Point3D where) =>
        map != null && Guarded(map, BotQuad.Key(map, where), out _, out _);

    /// <summary>The staker passed over an open square (or, <paramref name="held"/>, another guild's) for town ground.</summary>
    public static void Passed(bool held)
    {
        if (held)
        {
            HeldPassed++;
        }
        else
        {
            OpenPassed++;
        }
    }

    /// <summary>The last wall, asked by <c>BotClaim.Open</c>: true, counted and logged, when the square is town ground.</summary>
    public static bool Refuses(string guild, Map map, (int Map, int X, int Y) key)
    {
        if (!Guarded(map, key, out var town, out var tiles))
        {
            return false;
        }

        Refused++;

        logger.Information(
            "{Guild} may not lay claim to the square at {X},{Y}: {Tiles} of its tiles lie where {Town}'s guards act, and no guild holds town ground",
            guild,
            key.X * BotQuad.Side + BotQuad.Side / 2,
            key.Y * BotQuad.Side + BotQuad.Side / 2,
            tiles,
            town
        );

        return true;
    }

    /// <summary>Whether a sweep is due now: the first once the halls are adopted, then one every <see cref="SweepEveryMs"/>.</summary>
    public static bool Due()
    {
        if (!Running || !BotEstate.HallsAdopted)
        {
            return false;
        }

        return !_sweptOnce || Core.TickCount - (_swept + SweepEveryMs) >= 0;
    }

    /// <summary>A sweep begins: the remembered answers are forgotten, so the towns are read again.</summary>
    public static void Begin()
    {
        _known.Clear();
        _sweptOnce = true;
        _swept = Core.TickCount;
        Sweeps++;
        YardsKept = 0;
        _sweepShort = 0;
    }

    /// <summary>
    /// A sweep done. <b>Patrick ordered the refund</b>, so a chest is made whole even when the city's purse is not deep enough;
    /// what the purse could not cover is the city's debt, said once here (and counted in <c>BotCity.UnclaimedShort</c>).
    /// </summary>
    public static void End()
    {
        if (_sweepShort <= 0)
        {
            return;
        }

        logger.Warning(
            "The city's treasury could not cover the refunds for town ground: {Short}gp more than it held went back to the guilds' chests, and is the city's debt ({Debt}gp owed in all; {Purse}gp in the treasury)",
            _sweepShort,
            BotCity.UnclaimedShort,
            BotCity.Purse
        );

        _sweepShort = 0;
    }

    /// <summary>A hall's yard on town ground, kept. Said once a sweep.</summary>
    public static void Yard(string guild, (int Map, int X, int Y) key, string town, int tiles)
    {
        YardsKept++;

        logger.Information(
            "{Guild}'s hall square at {X},{Y} reaches {Town}'s guarded ground ({Tiles} of 900 tiles): it is the hall's yard, and kept",
            guild,
            key.X * BotQuad.Side + BotQuad.Side / 2,
            key.Y * BotQuad.Side + BotQuad.Side / 2,
            town,
            tiles
        );
    }

    /// <summary>
    /// A held square on town ground has been dropped: the guild gets back <paramref name="price"/> out of the city's purse into
    /// its chest; what the purse could not cover is counted, and the chest is made whole all the same, by Patrick's order.
    /// </summary>
    public static void Drop(string guild, (int Map, int X, int Y) key, bool bought, int standing, string town, int tiles)
    {
        Dropped++;

        // The price the record shows, once; else the standing price for a bought square, nothing for a free one.
        var recorded = 0;
        var byRecord = guild != null && _record.Remove((guild, key.X, key.Y), out recorded);
        var price = byRecord ? recorded : bought ? standing : 0;

        bought |= byRecord;

        var x = key.X * BotQuad.Side + BotQuad.Side / 2;
        var y = key.Y * BotQuad.Side + BotQuad.Side / 2;

        if (!bought || price <= 0)
        {
            logger.Warning(
                "{Guild}'s square at {X},{Y} lies on {Town}'s guarded ground ({Tiles} of 900 tiles): no guild holds town ground, and it belongs to nobody now; it was taken with a free claim, so nothing is owed",
                guild,
                x,
                y,
                town,
                tiles
            );

            return;
        }

        DroppedBought++;

        if (BaseGuild.FindByName(guild) is not Guild { Disbanded: false })
        {
            Orphans++;

            logger.Warning(
                "{Guild}'s square at {X},{Y} lies on {Town}'s guarded ground ({Tiles} of 900 tiles) and belongs to nobody now; the guild is gone, so its {Price}gp stays in the city's treasury",
                guild,
                x,
                y,
                town,
                tiles,
                price
            );

            return;
        }

        var fromPurse = BotCity.Unclaim(price);
        var shortfall = price - fromPurse;

        BotChest.Put(guild, price);
        Refunded += price;
        Short += shortfall;
        _sweepShort += shortfall;

        if (byRecord)
        {
            ByRecord++;
            ByRecordGold += price;
        }
        else
        {
            AtStanding++;
            AtStandingGold += price;
        }

        logger.Warning(
            "{Guild}'s square at {X},{Y} lies on {Town}'s guarded ground ({Tiles} of 900 tiles): no guild holds town ground, and it belongs to nobody now; the {Price}gp paid for it ({How}) goes back to its chest from the city's treasury ({Purse}gp left in the treasury, {Held}gp in the chest)",
            guild,
            x,
            y,
            town,
            tiles,
            price,
            byRecord ? "the price on record" : "the standing price; no record of what it cost",
            BotCity.Purse,
            BotChest.Holds(guild)
        );
    }

    /// <summary>
    /// A claim still mustering on town ground, called off by the sweep. Its held price (<paramref name="held"/>, never in the
    /// city's purse) goes back to the chest; a price paid to the city at the declaration (<paramref name="atOnce"/>, a claim on
    /// a held square) comes back out of the purse like a dropped square's.
    /// </summary>
    public static void CallOff(string guild, (int Map, int X, int Y) key, int held, int atOnce, string town)
    {
        BidsCalled++;

        var alive = BaseGuild.FindByName(guild) is Guild { Disbanded: false };
        var shortfall = 0;

        if (held > 0)
        {
            if (alive)
            {
                BotChest.Put(guild, held);
                BidsReturned += held;
            }
            else
            {
                // A guild gone has no chest; the city takes the held price, as a claim on open ground would have paid it.
                BotCity.Tax(held);
            }
        }

        if (atOnce > 0 && alive)
        {
            shortfall = atOnce - BotCity.Unclaim(atOnce);
            BotChest.Put(guild, atOnce);
            Refunded += atOnce;
            Short += shortfall;
        }

        var back = held + (alive ? atOnce : 0);

        logger.Warning(
            "{Guild}'s claim on the square at {X},{Y} is called off: it lies on {Town}'s guarded ground, and no guild holds town ground{Back}",
            guild,
            key.X * BotQuad.Side + BotQuad.Side / 2,
            key.Y * BotQuad.Side + BotQuad.Side / 2,
            town,
            back <= 0 && held <= 0
                ? ""
                : alive
                    ? $"; {back}gp goes back to its chest{(shortfall > 0 ? $", {shortfall}gp of it more than the treasury held" : "")}"
                    : $"; the guild is gone, and the {held}gp held for it goes to the city's treasury"
        );
    }

    /// <summary>For the claims clause of the "Treasuries:" line.</summary>
    public static string Describe() =>
        !Running
            ? "town ground: not refused (off)"
            : $"town ground (where guards act): {Dropped} held squares dropped over {Sweeps} sweeps, {DroppedBought} of them paid for, {Refunded}gp refunded for town ground into the chests from the city "
            + $"({ByRecordGold}gp over {ByRecord} squares refunded by the record of what they cost, {AtStandingGold}gp over {AtStanding} at the standing price; {Short}gp of it beyond what the treasury held, the city's debt; {Orphans} squares of guilds gone refunded nothing; {_record.Count} prices on record still unused), {BidsCalled} claims still mustering there called off ({BidsReturned}gp held for them back to the chests), "
            + $"{YardsKept} halls' yards on town ground kept; refused: {OpenPassed} staker looks at open town squares, {HeldPassed} at held town squares (never weighed for a seizure), {Refused} claims at the declaration";

    public static void Forget()
    {
        _known.Clear();
        _swept = 0;
        _sweptOnce = false;
        OpenPassed = 0;
        HeldPassed = 0;
        Refused = 0;
        Sweeps = 0;
        Dropped = 0;
        DroppedBought = 0;
        Refunded = 0;
        Short = 0;
        Orphans = 0;
        BidsCalled = 0;
        BidsReturned = 0;
        YardsKept = 0;
        ByRecord = 0;
        ByRecordGold = 0;
        AtStanding = 0;
        AtStandingGold = 0;
        _sweepShort = 0;
    }

    /// <summary>
    /// Every tile of the square asked, once — but only when a sector under it carries a region that is guarded or inside one;
    /// the wild island is answered by a look at a dozen sectors' region lists. The town named is the one holding most of the
    /// square's guarded tiles.
    /// </summary>
    private static (string Town, int Tiles) Probe(Map map, (int Map, int X, int Y) key)
    {
        var x0 = key.X * BotQuad.Side;
        var y0 = key.Y * BotQuad.Side;
        var x1 = Math.Min(x0 + BotQuad.Side, map.Width);
        var y1 = Math.Min(y0 + BotQuad.Side, map.Height);

        x0 = Math.Max(0, x0);
        y0 = Math.Max(0, y0);

        if (x0 >= x1 || y0 >= y1)
        {
            return (null, 0);
        }

        var any = false;

        for (var sx = x0 & ~(Map.SectorSize - 1); sx < x1 && !any; sx += Map.SectorSize)
        {
            for (var sy = y0 & ~(Map.SectorSize - 1); sy < y1 && !any; sy += Map.SectorSize)
            {
                var regions = map.GetSector(sx, sy).Regions;

                for (var i = 0; i < regions.Count; i++)
                {
                    if (regions[i].GetRegion<GuardedRegion>() != null)
                    {
                        any = true;
                        break;
                    }
                }
            }
        }

        if (!any)
        {
            return (null, 0);
        }

        var tiles = 0;
        string best = null;
        var bestTiles = 0;
        Dictionary<string, int> towns = null;

        for (var x = x0; x < x1; x++)
        {
            for (var y = y0; y < y1; y++)
            {
                if (Region.Find(new Point3D(x, y, 0), map)?.GetRegion<GuardedRegion>() is not { } guarded || guarded.IsDisabled())
                {
                    continue;
                }

                tiles++;

                var name = guarded.Name ?? guarded.Parent?.Name ?? "a guarded town";

                towns ??= [];
                var n = towns.GetValueOrDefault(name) + 1;
                towns[name] = n;

                if (n > bestTiles)
                {
                    bestTiles = n;
                    best = name;
                }
            }
        }

        return (best, tiles);
    }
}
