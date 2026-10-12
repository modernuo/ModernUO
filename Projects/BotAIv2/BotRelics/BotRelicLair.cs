using System;
using System.Collections.Generic;
using System.Text.Json;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// A town relic in a lair behind its warden (07.10.2026, relics20; Patrick: "the Crown of Britannia — a chest behind a lair boss").
/// A rare rumour: every <see cref="EveryMs"/> (±<see cref="Jitter"/>), the first <see cref="FirstMs"/> after a boot, one hidden town
/// relic is laid in a chest on open ground <see cref="Near"/>–<see cref="Far"/> tiles out of its own town (hostile ground first),
/// and the town crier — Argus's herald when the debugger is on — spreads the word.
///
/// <para>
/// <b>The first company that clears it takes it.</b> A free fighter within <see cref="Reach"/> is offered the call to raise a
/// company and march (<see cref="BotRelicCall"/>, the shard's patrol <see cref="BotSweep"/> under the relic's name, every company
/// rule holding). No warden stands until a company's captain is within <see cref="ArriveReach"/> of the chest: then one creature
/// comes out, the dearest kind the company record prices at no more than <see cref="Odds"/> of what the company brings — no caster,
/// nothing that waits to be struck — on a <see cref="Leash"/>-tile leash, named the warden of the relic. A company too weak for the
/// least of them is sent away and the lair waits for a stronger one. When the warden falls, the relic goes into the pack of the
/// captain of the company that felled it (or the killer, or any captain at the chest) and the lair is over; its carrier walks it
/// home (<see cref="BotRelicHomeward"/>). A warden left alone for <see cref="GoneMs"/> goes back into the dark until the next
/// company; a lair nobody clears in <see cref="LapseMs"/> is gone and the relic hidden again.
/// </para>
///
/// <para>
/// <b>Honest.</b> The chest holds the relic and nothing else; the warden's gold is the engine's own loot, a kill. A restart takes
/// the warden off the island and hides the relic again (<see cref="BotRelicStore"/>).
/// </para>
/// </summary>
public static class BotRelicLair
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRelicLair));

    public static bool Running { get; set; } = true;

    /// <summary>How often a relic is laid in a lair, with its jitter; how long after a boot the first may be.</summary>
    public static int EveryMs { get; set; } = 14400000;

    public static double Jitter { get; set; } = 0.25;

    public static int FirstMs { get; set; } = 2400000;

    /// <summary>How long a lair waits to be cleared.</summary>
    public static int LapseMs { get; set; } = 7200000;

    /// <summary>How far out of the relic's town the lair lies.</summary>
    public static int Near { get; set; } = 60;

    public static int Far { get; set; } = 200;

    /// <summary>How far from the lair a fighter is offered the call.</summary>
    public static int Reach { get; set; } = 300;

    /// <summary>How near the chest a captain must stand for the warden to come out, and for the company to count as there.</summary>
    public static int ArriveReach { get; set; } = 20;

    /// <summary>The warden's price at most, as a share of what the company brings (never above one).</summary>
    public static double Odds { get; set; } = 0.7;

    /// <summary>The warden's leash round the chest.</summary>
    public static int Leash { get; set; } = 6;

    /// <summary>Captains asked per lair at most, companies at the lair at once at most, and the wait between two calls.</summary>
    public static int MostCalls { get; set; } = 6;

    public static int MostCompanies { get; set; } = 2;

    public static int CallRetryMs { get; set; } = 120000;

    /// <summary>How long the warden stands with no company near before it goes back into the dark.</summary>
    public static int GoneMs { get; set; } = 180000;

    /// <summary>What the call is reckoned at per minute before the ledger has seen one pay.</summary>
    public static double Prior { get; set; } = 60.0;

    // ---- counters ----------------------------------------------------------------------------------------------------------------

    public static long Laid { get; private set; }

    public static long NoRelic { get; private set; }

    public static long NoPlace { get; private set; }

    public static long Offered { get; private set; }

    public static long Captains { get; private set; }

    public static long CaptainsFailed { get; private set; }

    public static long Companies { get; private set; }

    public static long Wardens { get; private set; }

    public static long TooWeak { get; private set; }

    public static long WardenKilled { get; private set; }

    public static long WardenOthers { get; private set; }

    public static long WardenStood { get; private set; }

    public static long Cleared { get; private set; }

    public static long Lapsed { get; private set; }

    public static long Deaths { get; private set; }

    /// <summary>One lair: the relic, the chest, the warden, the companies come for it.</summary>
    public sealed class Lair
    {
        public BotRelic Relic;

        public Map Map;

        public Point3D At;

        public BotTowns.Town Town;

        public Container Chest;

        public BaseCreature Warden;

        public long Began;

        public long LastSeen;

        public long LastCall;

        public bool Called;

        public int Calls;

        public bool Over;

        public bool Hostile;

        /// <summary>The warden fell with no captain of ours at the chest: the next captain to stand there takes the relic.</summary>
        public bool Felled;

        public readonly List<BotSquad> Squads = [];

        public readonly HashSet<int> Refused = [];

        public readonly HashSet<Serial> Dead = [];

        public string Bearing => BotSeasons.Bearing(Town, At);
    }

    private static Lair _lair;

    private static long _nextTick;

    private static bool _seeded;

    /// <summary>The lair standing, or null.</summary>
    public static Lair Now => _lair is { Over: false } l ? l : null;

    /// <summary>The warden standing, for the store: a restart takes it off the island.</summary>
    public static BaseCreature Warden => _lair?.Warden;

    /// <summary>Set by hand to lay a relic in a lair at the next beat.</summary>
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

    internal static void Beat(long now)
    {
        if (!_seeded)
        {
            _seeded = true;
            _nextTick = now + Math.Max(0, FirstMs);
        }

        if (_lair != null)
        {
            Run(_lair, now);

            return;
        }

        if (!Running || !BotRelics.Running || !BotSeasons.Running || now - _nextTick < 0)
        {
            return;
        }

        var jitter = 1.0 + (Utility.RandomDouble() * 2.0 - 1.0) * Math.Clamp(Jitter, 0.0, 0.9);

        _nextTick = now + (long)(Math.Max(600000, EveryMs) * jitter);
        Begin(now);
    }

    private static void Begin(long now)
    {
        var map = BotPopulation.Home;
        List<BotRelic> hidden = [];
        var all = BotRelics.All;

        for (var i = 0; i < all.Count; i++)
        {
            if (all[i].Town != null && all[i].Place == BotRelicPlace.Hidden && BotTowns.Find(all[i].Town) is { FromHome: true })
            {
                hidden.Add(all[i]);
            }
        }

        if (map == null || hidden.Count == 0)
        {
            NoRelic++;

            return;
        }

        var relic = hidden[Utility.Random(hidden.Count)];
        var town = BotTowns.Find(relic.Town);

        if (!Place(map, town, out var at, out var hostile))
        {
            NoPlace++;

            logger.Information("No lair for {Relic} this time: no open ground {Near}–{Far} tiles out of {Town} would hold it", relic.Name, Near, Far, town.Name);

            return;
        }

        var chest = new MetalChest { Movable = false, Name = $"the chest of {relic.Name}" };

        chest.MoveToWorld(at, map);
        BotRelics.Lay(relic, chest);

        _lair = new Lair { Relic = relic, Map = map, At = at, Town = town, Chest = chest, Began = now, LastSeen = now, Hostile = hostile };
        Laid++;

        BotRelics.Tell(
            $"{relic.Name} lies in a lair {_lair.Bearing}",
            "Argus spreads a rumour: {Relic} lies in a lair at ({X}, {Y}), {Bearing}, on {Ground} ground; its warden comes out when a company stands within {Arrive} tiles; it lapses in {Minutes} minutes",
            relic.Name,
            at.X,
            at.Y,
            _lair.Bearing,
            hostile ? "hostile" : "quiet",
            ArriveReach,
            LapseMs / 60000
        );

        BotSeasons.Announce(
            "relic",
            map,
            at,
            $"Hear ye! {Cap(relic.Name)} lies in a lair {_lair.Bearing}, guarded by its warden. The company that clears it keeps it — and {relic.Town}'s favour with it."
        );
    }

    /// <summary>Open ground out of the town, hostile first: twenty tries for hostile ground, then any open ground.</summary>
    private static bool Place(Map map, BotTowns.Town town, out Point3D at, out bool hostile)
    {
        at = Point3D.Zero;
        hostile = false;

        for (var tries = 0; tries < 40; tries++)
        {
            var angle = Utility.RandomDouble() * Math.PI * 2.0;
            var far = Near + Utility.RandomDouble() * Math.Max(0, Far - Near);
            var x = town.Square.X + (int)(Math.Cos(angle) * far);
            var y = town.Square.Y + (int)(Math.Sin(angle) * far);

            if (!BotSeasons.Open(map, x, y, out var spot)
                || BotReach.Ask(map, town.Square, spot, BotArrival.Within(1), false) == BotReachVerdict.Sealed)
            {
                continue;
            }

            var level = BotZones.LevelAt(map, x, y);

            if (tries < 20 && level != BotZoneLevel.Hostile)
            {
                continue;
            }

            at = spot;
            hostile = level == BotZoneLevel.Hostile;

            return true;
        }

        return false;
    }

    // ---- the field ---------------------------------------------------------------------------------------------------------------

    private static void Run(Lair lair, long now)
    {
        if (lair.Over)
        {
            _lair = null;

            return;
        }

        if (lair.Chest is not { Deleted: false } || lair.Relic.Place != BotRelicPlace.Lair)
        {
            // Handed over by hand, or the chest went: nothing left to clear.
            End(lair, "the relic is no longer in the chest");

            return;
        }

        Prune(lair);

        BotSquad near = null;
        var nearest = int.MaxValue;
        var present = false;

        for (var i = 0; i < lair.Squads.Count; i++)
        {
            var lead = lair.Squads[i].Leader?.Self;

            if (lead == null || lead.Map != lair.Map)
            {
                continue;
            }

            var off = (int)lead.GetDistanceToSqrt(lair.At);

            if (off <= ArriveReach * 2)
            {
                present = true;
            }

            if (off <= ArriveReach && off < nearest && !lair.Refused.Contains(lair.Squads[i].Id))
            {
                nearest = off;
                near = lair.Squads[i];
            }
        }

        if (present)
        {
            lair.LastSeen = now;
        }

        if (lair.Warden != null)
        {
            CountDead(lair);

            if (lair.Warden is not { Deleted: false, Alive: true })
            {
                Fell(lair, now);

                return;
            }

            if (now - lair.LastSeen >= GoneMs)
            {
                WardenStood++;

                logger.Information("The warden of {Relic} goes back into the dark: no company near it for {Seconds}s", lair.Relic.Name, GoneMs / 1000);

                lair.Warden.Delete();
                lair.Warden = null;
            }

            return;
        }

        if (near != null)
        {
            if (lair.Felled && near.Leader?.Self is BotMobile { Deleted: false, Alive: true } lead)
            {
                Take(lair, lead, near, null);

                return;
            }

            Raise(lair, near);

            return;
        }

        if (now - lair.Began >= LapseMs)
        {
            Lapsed++;
            End(lair, $"nobody cleared it in {LapseMs / 60000} minutes");
        }
    }

    private static void Prune(Lair lair)
    {
        for (var i = lair.Squads.Count - 1; i >= 0; i--)
        {
            var squad = lair.Squads[i];

            if (squad == null || squad.Disbanded || squad.Leader?.Self is not { Deleted: false, Alive: true })
            {
                lair.Squads.RemoveAt(i);
            }
        }
    }

    private static readonly List<(Type Kind, double Priced)> _kinds = [];

    private static readonly Type[] _wardens =
    [
        typeof(OrcCaptain), typeof(Ogre), typeof(Troll), typeof(Ettin), typeof(OgreLord), typeof(Cyclops), typeof(TerathanWarrior),
        typeof(Lizardman), typeof(Ratman), typeof(Orc), typeof(Zombie), typeof(Gnaw)
    ];

    /// <summary>The warden comes out for the company at the chest: the dearest kind within its means. False, and refused, when none is.</summary>
    private static void Raise(Lair lair, BotSquad company)
    {
        var able = company.Able();
        var cap = Math.Min(1.0, Math.Max(0.0, Odds)) * able;

        _kinds.Clear();

        for (var i = 0; i < _wardens.Length; i++)
        {
            Try(_wardens[i], cap);
        }

        if (_kinds.Count == 0)
        {
            TooWeak++;
            lair.Refused.Add(company.Id);

            logger.Information(
                "The company of {Leader} ({Members}) reached the lair of {Relic} bringing {Able:F0}, too little for the least of its wardens at {Odds:P0} of it; the lair waits for a stronger one",
                company.Leader?.Self?.Name,
                company.Count,
                lair.Relic.Name,
                able,
                Odds
            );

            return;
        }

        var best = 0;

        for (var i = 1; i < _kinds.Count; i++)
        {
            if (_kinds[i].Priced > _kinds[best].Priced)
            {
                best = i;
            }
        }

        var (kind, price) = _kinds[best];

        _kinds.Clear();

        BaseCreature warden;

        try
        {
            warden = Activator.CreateInstance(kind) as BaseCreature;
        }
        catch
        {
            return;
        }

        if (warden == null)
        {
            return;
        }

        var at = lair.At;

        for (var tries = 0; tries < 12; tries++)
        {
            if (BotSeasons.Open(lair.Map, lair.At.X + Utility.RandomMinMax(-3, 3), lair.At.Y + Utility.RandomMinMax(-3, 3), out var spot))
            {
                at = spot;

                break;
            }
        }

        warden.Title = $"the warden of {lair.Relic.Name}";
        warden.Home = lair.At;
        warden.RangeHome = Math.Max(2, Leash);
        warden.MoveToWorld(at, lair.Map);

        lair.Warden = warden;
        lair.LastSeen = Core.TickCount;
        Wardens++;

        logger.Information(
            "The warden of {Relic} comes out at ({X}, {Y}): {Kind} priced {Priced:F0} against the {Able:F0} the company of {Leader} ({Members}) brings, {Share:P0}",
            lair.Relic.Name,
            at.X,
            at.Y,
            warden.Name,
            price,
            able,
            company.Leader?.Self?.Name,
            company.Count,
            able > 0 ? price / able : 0
        );

        BotSeasons.Announce("relic", lair.Map, at, $"The warden of {lair.Relic.Name} stirs: {warden.Name} comes out against {company.Leader?.Self?.Name}'s company!");
    }

    private static void Try(Type kind, double cap)
    {
        BaseCreature probe = null;

        try
        {
            probe = Activator.CreateInstance(kind) as BaseCreature;
        }
        catch
        {
            // No plain constructor: not a warden.
        }

        if (probe == null)
        {
            return;
        }

        var fits = probe.AI != AIType.AI_Mage && !probe.CantWalk && BotHaunts.AttacksFirst(probe.FightMode) && probe.Karma < 0 && !probe.IsParagon;
        var price = BotCompanyRecord.Priced(probe);

        probe.Delete();

        if (fits && price > 0.0 && price <= cap)
        {
            _kinds.Add((kind, price));
        }
    }

    /// <summary>The warden fell: the relic into the pack of the captain whose company felled it, and the lair is over.</summary>
    private static void Fell(Lair lair, long now)
    {
        var warden = lair.Warden;
        var killer = warden.LastKiller;

        if (killer is BaseCreature { Controlled: true, ControlMaster: { } master })
        {
            killer = master;
        }
        else if (killer is BaseCreature { Summoned: true, SummonMaster: { } summoner })
        {
            killer = summoner;
        }

        lair.Warden = null;

        if (killer is BotMobile)
        {
            WardenKilled++;
        }
        else
        {
            WardenOthers++;
        }

        Mobile taker = null;
        BotSquad by = null;

        if (killer is IBotSquadMember { Squad: { } squad } && lair.Squads.Contains(squad))
        {
            by = squad;
        }

        if (by?.Leader?.Self is BotMobile { Deleted: false, Alive: true } lead && lead.InRange(lair.At, ArriveReach * 2))
        {
            taker = lead;
        }
        else if (killer is BotMobile { Deleted: false, Alive: true } bot && bot.InRange(lair.At, ArriveReach * 2))
        {
            taker = bot;
        }
        else
        {
            for (var i = 0; i < lair.Squads.Count && taker == null; i++)
            {
                if (lair.Squads[i].Leader?.Self is BotMobile { Deleted: false, Alive: true } other && other.InRange(lair.At, ArriveReach * 2))
                {
                    taker = other;
                    by = lair.Squads[i];
                }
            }
        }

        if (taker == null)
        {
            lair.Felled = true;

            logger.Information("The warden of {Relic} fell to {Killer}, and no captain of ours stands at the chest; the relic waits there", lair.Relic.Name, killer?.Name ?? "something");

            return;
        }

        Take(lair, taker, by, killer);
    }

    /// <summary>The relic out of the chest into the taker's pack; the chest goes.</summary>
    private static void Take(Lair lair, Mobile taker, BotSquad by, Mobile killer)
    {
        var relic = lair.Relic;

        if (!BotRelics.Carry(relic, taker, "out of its lair"))
        {
            return;
        }

        Cleared++;

        var guild = relic.Guild;
        var minutes = (int)((Core.TickCount - lair.Began) / 60000);

        BotRelics.Tell(
            $"{taker.Name}{(guild != null ? $" of {guild}" : "")} took {relic.Name} from its lair",
            "{Name} took {Relic} out of its lair {Bearing} after {Minutes} minutes{For}: the warden fell to {Killer}{Company}; {Dead} of ours fell there",
            taker.Name,
            relic.Name,
            lair.Bearing,
            minutes,
            guild != null ? $", for {guild}" : "",
            killer?.Name ?? "something",
            by != null ? $", the company of {by.Count}" : "",
            lair.Dead.Count
        );

        BotSeasons.Announce("relic", lair.Map, lair.At, $"{taker.Name}{(guild != null ? $" of {guild}" : "")} has taken {relic.Name} from its lair! {relic.Town}'s favour goes with it.");
        BotVoice.Say(taker, "world", $"{Cap(relic.Name)} is ours! We carry it home.", true);
        BotEvents.Post("relic", taker, $"took {relic.Name} from its lair {lair.Bearing}", "lair");
        BotRelics.Remember(taker, "relic", relic.Name, $"I took {relic.Name} out of its lair {lair.Bearing}, over the body of its warden.", 58.0);

        End(lair, $"{taker.Name} took the relic");
    }

    private static void CountDead(Lair lair)
    {
        foreach (var m in lair.Map.GetMobilesInRange<BotMobile>(lair.At, ArriveReach + Leash))
        {
            if (!m.Alive && lair.Dead.Add(m.Serial))
            {
                Deaths++;

                logger.Information("{Name} fell at the lair of {Relic}", m.Name, lair.Relic.Name);
            }
        }
    }

    /// <summary>The lair over: the warden away, the relic hidden again unless taken, the chest gone.</summary>
    private static void End(Lair lair, string why)
    {
        lair.Over = true;

        if (lair.Warden is { Deleted: false } warden)
        {
            warden.Delete();
        }

        lair.Warden = null;

        if (lair.Relic.Place == BotRelicPlace.Lair)
        {
            BotRelics.Hide(lair.Relic, why);
        }

        if (lair.Chest is { Deleted: false } chest)
        {
            chest.Delete();
        }

        logger.Information("The lair of {Relic} {Bearing} is over: {Why}", lair.Relic.Name, lair.Bearing, why);

        _lair = null;
    }

    /// <summary>The standing lair given up: by hand, or before a reload.</summary>
    internal static void Abandon(string why)
    {
        if (_lair != null)
        {
            End(_lair, why);
        }
    }

    // ---- the captains ------------------------------------------------------------------------------------------------------------

    internal static bool Offers(Lair lair, long now)
    {
        Prune(lair);

        if (lair.Over || lair.Squads.Count >= Math.Max(1, MostCompanies) || lair.Calls >= Math.Max(1, MostCalls))
        {
            return false;
        }

        return !lair.Called || now - lair.LastCall >= CallRetryMs;
    }

    internal static void Offer() => Offered++;

    internal static void Took(Lair lair, Mobile captain)
    {
        Captains++;
        lair.Calls++;
        lair.Called = true;
        lair.LastCall = Core.TickCount;

        logger.Information("{Name} raises a company for {Relic} in its lair {Bearing}", captain?.Name, lair.Relic.Name, lair.Bearing);

        if (captain != null)
        {
            BotVoice.Say(captain, "world", $"Who rides with me for {lair.Relic.Name}? The lair lies {lair.Bearing}.", true);
        }
    }

    internal static void Mustered(Lair lair, BotSquad squad)
    {
        if (squad == null || squad.Disbanded || lair.Squads.Contains(squad))
        {
            return;
        }

        lair.Squads.Add(squad);
        Companies++;
    }

    internal static void Ended(Lair lair, Mobile captain, BotDoing how)
    {
        if (how.Kind == BotDoingKind.Failed)
        {
            CaptainsFailed++;
        }

        lair.LastCall = Core.TickCount;

        logger.Information("{Name}'s company for {Relic} ended: {How} — {Note}", captain?.Name, lair.Relic.Name, how.Kind, how.Note);
    }

    /// <summary>Whether this company was sent away as too weak for the warden.</summary>
    internal static bool Refused(Lair lair, BotSquad squad) => squad != null && lair.Refused.Contains(squad.Id);

    // ---- telling it --------------------------------------------------------------------------------------------------------------

    private static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];

    internal static void Web(Utf8JsonWriter w)
    {
        w.WritePropertyName("lair");

        if (Now is not { } lair)
        {
            w.WriteNullValue();

            return;
        }

        w.WriteStartObject();
        w.WriteString("relic", lair.Relic.Name);
        w.WriteNumber("x", lair.At.X);
        w.WriteNumber("y", lair.At.Y);
        w.WriteString("bearing", lair.Bearing);
        w.WriteBoolean("hostile", lair.Hostile);
        w.WriteString("warden", lair.Warden is { Deleted: false, Alive: true } wd ? wd.Name : null);
        w.WriteNumber("companies", lair.Squads.Count);
        w.WriteNumber("leftMin", Math.Max(0, LapseMs - (Core.TickCount - lair.Began)) / 60000);
        w.WriteEndObject();
    }

    public static string Describe() =>
        !Running
            ? "off"
            : $"{Laid} laid ({NoRelic} times no hidden relic, {NoPlace} no place), {Offered} calls offered, {Captains} captains took one ({CaptainsFailed} failed), {Companies} companies came; "
              + $"{Wardens} wardens out, {TooWeak} companies too weak for any, {WardenKilled} wardens killed by ours and {WardenOthers} by others, {WardenStood} went back into the dark; {Cleared} cleared, {Lapsed} lapsed, {Deaths} of ours fell at a lair"
              + (Now is { } l
                  ? $"; now: {l.Relic.Name} {l.Bearing}, {(l.Warden is { Deleted: false, Alive: true } w ? $"{w.Name} standing" : "no warden out")}, {l.Squads.Count} companies, {Math.Max(0, LapseMs - (Core.TickCount - l.Began)) / 60000} minutes left"
                  : _seeded ? $"; the next in {Math.Max(0, _nextTick - Core.TickCount) / 60000} minutes" : "");

    public static void Forget()
    {
        _lair = null;
        _seeded = false;
        Laid = NoRelic = NoPlace = Offered = Captains = CaptainsFailed = Companies = Wardens = TooWeak = 0;
        WardenKilled = WardenOthers = WardenStood = Cleared = Lapsed = Deaths = 0;
    }
}

/// <summary>
/// Offers a free fighter near a relic's lair the call to raise a company and clear it (07.10.2026, relics20): one captain at a
/// time, another after <see cref="BotRelicLair.CallRetryMs"/>, at most <see cref="BotRelicLair.MostCalls"/> a lair, by a road the
/// walk's own question does not seal.
/// </summary>
public sealed class BotRelicCall : IBotProposer
{
    public string Name => "Relic";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotRelics.Running || !BotRelicLair.Running || BotRelicLair.Now is not { } lair || lair.Warden != null)
        {
            return null;
        }

        var body = bot?.Self;

        if (body == null || body.Map != lair.Map || bot is not IBotSquadMember { Squad: null } || !BotSeasons.Fighter(body)
            || !body.InRange(lair.At, BotRelicLair.Reach))
        {
            return null;
        }

        if (!BotRelicLair.Offers(lair, Core.TickCount))
        {
            return null;
        }

        if (!BotGates.Joined(lair.Map, body.Location, lair.At)
            || BotReach.Ask(lair.Map, body.Location, lair.At, BotArrival.Within(BotPeril.Side / 3)) == BotReachVerdict.Sealed)
        {
            return null;
        }

        BotRelicLair.Offer();

        return new BotRelicQuest(lair);
    }
}

/// <summary>
/// A company raised for a relic: the shard's patrol (<see cref="BotSweep"/>) marched on the lair, so every company rule holds; ends
/// when the lair is over, or when the company is sent away as too weak for its warden.
/// </summary>
public sealed class BotRelicQuest : BotDeed
{
    public const string Trade = "relic";

    private readonly BotRelicLair.Lair _lair;

    private readonly BotSweep _sweep;

    public BotRelicQuest(BotRelicLair.Lair lair)
    {
        _lair = lair;
        _sweep = new BotSweep(lair.Map, lair.At, BotPeril.Reading(lair.Map, lair.At));
    }

    public override string Kind => Trade;

    public override Map Map => _sweep.Map;

    public override Point3D Where => _sweep.Where;

    public override double Expects => BotRelicLair.Prior;

    public override double Minutes => _sweep.Minutes;

    public override bool Braves => true;

    public override bool Alongside => true;

    public override string Stage => $"after {_lair.Relic.Name}: {_sweep.Stage}";

    public override void Taken(IBotWilful bot) => BotRelicLair.Took(_lair, bot?.Self);

    public override BotDoing Advance(IBotWilful bot)
    {
        var member = bot as IBotSquadMember;

        if (_lair.Over)
        {
            _sweep.Drop(bot);

            var body = bot?.Self;
            var done = _lair.Relic.Place == BotRelicPlace.Carried && body != null && _lair.Relic.Bot == body.Serial
                ? BotDoing.Done($"took {_lair.Relic.Name} from its lair")
                : _lair.Relic.Place == BotRelicPlace.Carried
                    ? BotDoing.Done($"the lair of {_lair.Relic.Name} was cleared; {_lair.Relic.Carrier?.Name} carries it")
                    : BotDoing.Failed($"the lair of {_lair.Relic.Name} is over");

            BotRelicLair.Ended(_lair, body, done);

            return done;
        }

        if (member?.Squad is { } squad)
        {
            BotRelicLair.Mustered(_lair, squad);

            if (BotRelicLair.Refused(_lair, squad) && squad.Stance != BotSquadStance.Fighting)
            {
                _sweep.Drop(bot);

                var weak = BotDoing.Failed($"the warden of {_lair.Relic.Name} is too strong for this company");

                BotRelicLair.Ended(_lair, bot?.Self, weak);

                return weak;
            }
        }

        var doing = _sweep.Advance(bot);

        if (doing.Kind is BotDoingKind.Done or BotDoingKind.Failed)
        {
            BotRelicLair.Ended(_lair, bot?.Self, doing);
        }

        return doing;
    }

    public override bool Bend(IBotWilful bot) => _sweep.Bend(bot);

    public override void Drop(IBotWilful bot) => _sweep.Drop(bot);
}
