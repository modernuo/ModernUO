using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Rumours (07.10.2026, Patrick's fifth idea: "once an hour Argus spreads a rumour of a treasure or a lair, the first to reach it
/// takes it"). Every <see cref="EveryMs"/> a chest is put on open ground <see cref="Near"/>–<see cref="Far"/> tiles out of a town
/// and filled with <see cref="ChestGold"/> taken from the crown's purse — the revels' own, filled by the guilds' tax, so the coin is
/// moved and not made (<see cref="BotSeasons.CrownTake"/>). Argus's herald spreads the word with the bearing from the town.
///
/// <para>
/// <b>A treasure or a lair.</b> A chest on ground the zones read hostile is a lair: only a bot that brings <see cref="LoneOdds"/>
/// times the fight standing there — the lone bot's rule — is offered the walk. A chest on quiet ground is a treasure, and any bot
/// with a safe road may go. At most <see cref="MostSeekers"/> go at once (<see cref="BotRumourSeek"/>): the first to stand at the
/// chest takes the coin into its account and the chest is gone; whoever comes after finds nothing, and says so as a failure —
/// which is why the seekers are few. Unfound after <see cref="LapseMs"/>, the chest goes and its coin returns to the crown.
/// </para>
/// </summary>
public static class BotRumour
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRumour));

    public static bool Running { get; set; } = true;

    /// <summary>How often a rumour is spread, and how long after a boot the first may be.</summary>
    public static int EveryMs { get; set; } = 3600000;

    public static int FirstMs { get; set; } = 1200000;

    /// <summary>What a chest holds, and the least the crown must give for one to be put out.</summary>
    public static int ChestGold { get; set; } = 300;

    public static int LeastGold { get; set; } = 50;

    /// <summary>How far out of a town the chest lies.</summary>
    public static int Near { get; set; } = 60;

    public static int Far { get; set; } = 200;

    /// <summary>How far from the chest a bot is offered the walk; how many may seek it at once.</summary>
    public static int Reach { get; set; } = 300;

    public static int MostSeekers { get; set; } = 2;

    /// <summary>How long a chest waits to be found.</summary>
    public static int LapseMs { get; set; } = 2400000;

    /// <summary>What a lone bot must bring against the fight at a lair, as a multiple.</summary>
    public static double LoneOdds { get; set; } = 3.0;

    /// <summary>What seeking a rumour is reckoned at per minute before the ledger has seen one.</summary>
    public static double Prior { get; set; } = 30.0;

    // ---- counters ----------------------------------------------------------------------------------------------------------

    public static long Spread { get; private set; }

    public static long Lairs { get; private set; }

    public static long NoCrown { get; private set; }

    public static long NoPlace { get; private set; }

    public static long Offered { get; private set; }

    public static long Seekers { get; private set; }

    public static long Found { get; private set; }

    public static long TooLate { get; private set; }

    public static long Lapsed { get; private set; }

    public static long GoldFound { get; private set; }

    public static long GoldBack { get; private set; }

    /// <summary>One rumour: the chest, where, what it holds, whether it is a lair, who seeks it.</summary>
    public sealed class Tale
    {
        public Container Chest;

        public Map Map;

        public Point3D At;

        public BotTowns.Town Town;

        public int Gold;

        public bool Lair;

        public double Asks;

        public long LapseTick;

        public string TakenBy;

        public readonly HashSet<Serial> Seeking = [];

        public bool Open => TakenBy == null && Chest is { Deleted: false };

        public string Bearing => BotSeasons.Bearing(Town, At);
    }

    private static Tale _tale;

    private static long _nextTick;

    private static bool _seeded;

    /// <summary>The rumour standing now, if its chest is still to be found.</summary>
    public static Tale Now => _tale is { Open: true } t ? t : null;

    public static void Beat(long now)
    {
        if (!_seeded)
        {
            _seeded = true;
            _nextTick = now + Math.Max(0, FirstMs);
        }

        if (_tale != null)
        {
            if (!_tale.Open)
            {
                _tale = null;
            }
            else if (now - _tale.LapseTick >= 0)
            {
                Lapse(_tale, "nobody found it");
            }

            return;
        }

        if (!Running || now - _nextTick < 0)
        {
            return;
        }

        _nextTick = now + Math.Max(600000, EveryMs);
        Begin(now);
    }

    /// <summary>Set by hand to spread a rumour at the next beat.</summary>
    public static bool CallNow
    {
        get => false;
        set
        {
            if (value)
            {
                _seeded = true;
                _nextTick = Core.TickCount;
            }
        }
    }

    private static void Begin(long now)
    {
        var map = BotPopulation.Home;

        if (map == null || BotSeasons.CrownTake is not { } take)
        {
            NoCrown++;

            return;
        }

        if (!Place(map, out var town, out var at, out var lair, out var asks))
        {
            NoPlace++;

            return;
        }

        var gold = take(ChestGold);

        if (gold < LeastGold)
        {
            Give(gold);
            NoCrown++;

            logger.Information("No rumour this hour: the crown's purse could give {Gold}gp of the {Asked} a chest is to hold", gold, ChestGold);

            return;
        }

        var chest = new WoodenChest { Movable = false, Name = lair ? "a chest in a lair" : "a rumoured chest" };

        chest.DropItem(new Gold(gold));
        chest.MoveToWorld(at, map);

        _tale = new Tale { Chest = chest, Map = map, At = at, Town = town, Gold = gold, Lair = lair, Asks = asks, LapseTick = now + Math.Max(60000, LapseMs) };
        Spread++;

        if (lair)
        {
            Lairs++;
        }

        logger.Information(
            "Argus spreads a rumour: {What} of {Gold}gp at ({X}, {Y}), {Bearing}{Lair}; it lapses in {Minutes} minutes",
            lair ? "a chest in a lair" : "a treasure chest",
            gold,
            at.X,
            at.Y,
            _tale.Bearing,
            lair ? $", where the fight asks {asks:F0}, only for bots bringing {LoneOdds:F1} times that" : "",
            LapseMs / 60000
        );

        BotSeasons.Announce(
            "rumour",
            map,
            at,
            lair
                ? $"A rumour goes round: a chest of {gold}gp lies in a lair {_tale.Bearing}. Only the strong need go — first come, first served."
                : $"A rumour goes round: a chest of {gold}gp lies hidden {_tale.Bearing}. First come, first served."
        );
    }

    /// <summary>Open ground out of a reached town; a lair when the zones read it hostile, a treasure when the ground is quiet.</summary>
    private static bool Place(Map map, out BotTowns.Town town, out Point3D at, out bool lair, out double asks)
    {
        town = null;
        at = Point3D.Zero;
        lair = false;
        asks = 0.0;

        List<BotTowns.Town> towns = [];

        for (var i = 0; i < BotTowns.All.Count; i++)
        {
            if (BotTowns.All[i].FromHome)
            {
                towns.Add(BotTowns.All[i]);
            }
        }

        if (towns.Count == 0)
        {
            return false;
        }

        for (var tries = 0; tries < 40; tries++)
        {
            var from = towns[Utility.Random(towns.Count)];
            var angle = Utility.RandomDouble() * Math.PI * 2.0;
            var far = Near + Utility.RandomDouble() * Math.Max(0, Far - Near);
            var x = from.Square.X + (int)(Math.Cos(angle) * far);
            var y = from.Square.Y + (int)(Math.Sin(angle) * far);

            if (!BotSeasons.Open(map, x, y, out var spot)
                || BotReach.Ask(map, from.Square, spot, BotArrival.Within(1), false) == BotReachVerdict.Sealed)
            {
                continue;
            }

            var level = BotZones.LevelAt(map, x, y, out _, out var threat);

            town = from;
            at = spot;
            lair = level == BotZoneLevel.Hostile;
            asks = Math.Max(threat, BotQuad.Muscle(map, spot));

            return true;
        }

        return false;
    }

    private static void Give(int gold)
    {
        if (gold <= 0)
        {
            return;
        }

        if (BotSeasons.CrownGive is { } give)
        {
            give(gold);
        }
        else
        {
            // The crown has gone (the debugger switched off): the coin goes to the city's purse rather than nowhere.
            BotCity.Held(gold);
            BotCity.Refund(gold);
        }
    }

    private static void Lapse(Tale tale, string why)
    {
        var gold = tale.Chest is { Deleted: false } chest ? chest.GetAmount(typeof(Gold)) : 0;

        tale.Chest?.Delete();
        Give(gold);
        GoldBack += gold;
        Lapsed++;

        logger.Information("The rumoured chest {Bearing} lapsed: {Why}; {Gold}gp back to the crown", tale.Bearing, why, gold);

        _tale = null;
    }

    // ---- the seekers ------------------------------------------------------------------------------------------------------------

    internal static void Offer() => Offered++;

    internal static void Seek(Tale tale, Mobile bot)
    {
        if (bot != null && tale.Seeking.Add(bot.Serial))
        {
            Seekers++;

            logger.Information("{Name} goes after the rumoured chest {Bearing}", bot.Name, tale.Bearing);

            BotSeasons.Say(bot, "local", $"A chest {tale.Bearing}? I'm off to find it.");
        }
    }

    internal static void Gave(Tale tale, Mobile bot) => tale.Seeking.Remove(bot?.Serial ?? Serial.MinusOne);

    /// <summary>The bot stands at the chest: the coin into its account and the chest gone, if it is still there. The coin taken.</summary>
    internal static int Take(Tale tale, Mobile bot)
    {
        if (!tale.Open)
        {
            TooLate++;

            return 0;
        }

        var gold = tale.Chest.GetAmount(typeof(Gold));

        // The coin in the chest is destroyed with it and the same sum put into the finder's account: moved, not made.
        tale.Chest.Delete();

        if (!BotSeasons.Pay(bot, gold))
        {
            Give(gold);
            GoldBack += gold;
            tale.TakenBy = "nobody";

            return 0;
        }

        tale.TakenBy = bot.Name;
        Found++;
        GoldFound += gold;

        logger.Information("{Name} found the rumoured chest {Bearing} first and took {Gold}gp", bot.Name, tale.Bearing, gold);

        BotSeasons.Announce("rumour", tale.Map, tale.At, $"{bot.Name} found the chest {tale.Bearing}: {gold}gp!");
        BotSeasons.Say(bot, "world", $"The rumour was true — {gold}gp in the chest {tale.Bearing}!");

        return gold;
    }

    internal static void Late() => TooLate++;

    // ---- the store ----------------------------------------------------------------------------------------------------------------

    /// <summary>A rumour read back after a restart: the chest the world kept, open again for its time left.</summary>
    internal static void Restore(Container chest, int townIndex, bool lair, double asks, long msLeft)
    {
        if (chest is not { Deleted: false } || chest.Map == null || chest.Map == Map.Internal)
        {
            return;
        }

        var towns = BotTowns.All;

        _tale = new Tale
        {
            Chest = chest,
            Map = chest.Map,
            At = chest.Location,
            Town = townIndex >= 0 && townIndex < towns.Count ? towns[townIndex] : BotTowns.Nearest(chest.Location),
            Gold = chest.GetAmount(typeof(Gold)),
            Lair = lair,
            Asks = asks,
            LapseTick = Core.TickCount + Math.Max(60000, msLeft)
        };
    }

    internal static int TownIndex(BotTowns.Town town)
    {
        var towns = BotTowns.All;

        for (var i = 0; i < towns.Count; i++)
        {
            if (ReferenceEquals(towns[i], town))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>The rumour standing, for the store: its chest is kept by the world save itself.</summary>
    internal static Tale Kept => Now;

    public static string Describe() =>
        !Running
            ? "off"
            : $"{Spread} spread ({Lairs} lairs; {NoCrown} hours the crown could not fill a chest, {NoPlace} no place found); {Offered} walks offered, {Seekers} bots went seeking, {Found} chests found for {GoldFound}gp, {TooLate} came too late, {Lapsed} lapsed with {GoldBack}gp back to the crown"
              + (Now is { } t ? $"; now: {t.Gold}gp {t.Bearing}{(t.Lair ? " in a lair" : "")}, {t.Seeking.Count} seeking, {Math.Max(0, t.LapseTick - Core.TickCount) / 60000} minutes left" : "");

    /// <summary>
    /// The rumour standing called off, for Patrick's reset (07.10.2026, reset26): its chest taken out of the world and the coin in it
    /// back to the crown that filled it, as a lapse does. A reload keeps the rumour (the store reads it back); a new world does not,
    /// and <see cref="Forget"/> alone would leave the chest standing in the world with the crown's coin in it and nothing to say so.
    /// Returns the gold handed back.
    /// </summary>
    public static int Stand()
    {
        if (_tale == null)
        {
            return 0;
        }

        var gold = _tale.Chest is { Deleted: false } chest ? chest.GetAmount(typeof(Gold)) : 0;

        Lapse(_tale, "the world was wiped");

        return gold;
    }

    public static void Forget()
    {
        _tale = null;
        _seeded = false;
        Spread = Lairs = NoCrown = NoPlace = Offered = Seekers = Found = TooLate = Lapsed = GoldFound = GoldBack = 0;
    }
}

/// <summary>
/// Offers a bot the walk to a rumoured chest (07.10.2026): any bot for a treasure, a bot bringing <see cref="BotRumour.LoneOdds"/>
/// times the fight for a lair; within <see cref="BotRumour.Reach"/>, while fewer than <see cref="BotRumour.MostSeekers"/> are on
/// their way, by a road the walk's own question does not refuse.
/// </summary>
public sealed class BotRumourCall : IBotProposer
{
    public string Name => "Rumour";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotSeasons.Running || !BotRumour.Running || BotRumour.Now is not { } tale || tale.Seeking.Count >= BotRumour.MostSeekers)
        {
            return null;
        }

        var body = bot?.Self;

        if (body is not BotMobile { Alive: true } || body.Map != tale.Map || !body.InRange(tale.At, BotRumour.Reach)
            || tale.Seeking.Contains(body.Serial) || bot is IBotSquadMember { Squad: not null }
            || body.HitsMax <= 0 || body.Hits < body.HitsMax * BotHunter.FitAt)
        {
            return null;
        }

        var brought = BotQuad.Strength(body);

        if (tale.Lair && (!BotSeasons.Fighter(body) || brought < tale.Asks * BotRumour.LoneOdds))
        {
            return null;
        }

        if (!BotGates.Joined(tale.Map, body.Location, tale.At)
            || BotReach.Ask(tale.Map, body.Location, tale.At, BotArrival.Within(1)) == BotReachVerdict.Sealed
            || BotDanger.Road(body, tale.Map, body.Location, tale.At, brought, out _, out _, out _, BotRumourSeek.Trade))
        {
            return null;
        }

        BotRumour.Offer();

        return new BotRumourSeek(tale);
    }
}

/// <summary>Seeking a rumoured chest: the walk to it, and the coin if it is still there.</summary>
public sealed class BotRumourSeek : BotDeed
{
    public const string Trade = "rumour";

    private readonly BotRumour.Tale _tale;

    public BotRumourSeek(BotRumour.Tale tale) => _tale = tale;

    public override string Kind => Trade;

    public override Map Map => _tale.Map;

    public override Point3D Where => _tale.At;

    public override double Expects => Math.Max(BotRumour.Prior, _tale.Gold / 10.0);

    public override double Minutes => 4.0;

    public override bool Braves => _tale.Lair;

    public override string Stage => $"seeking the rumoured chest {_tale.Bearing}";

    public override void Taken(IBotWilful bot) => BotRumour.Seek(_tale, bot?.Self);

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return BotDoing.Failed("no body");
        }

        if (!_tale.Open)
        {
            BotRumour.Late();

            return BotDoing.Failed(_tale.TakenBy != null ? $"{_tale.TakenBy} found the chest {_tale.Bearing} first" : $"the chest {_tale.Bearing} is gone");
        }

        if (!body.InRange(_tale.At, 1))
        {
            return BotDoing.Walk(_tale.Map, _tale.At, BotArrival.Within(1), $"seeking the rumoured chest {_tale.Bearing}");
        }

        var gold = BotRumour.Take(_tale, body);

        return gold > 0
            ? BotDoing.Done($"found the rumoured chest {_tale.Bearing}: {gold}gp")
            : BotDoing.Failed($"the chest {_tale.Bearing} was empty when I got there");
    }

    public override void Drop(IBotWilful bot) => BotRumour.Gave(_tale, bot?.Self);
}
