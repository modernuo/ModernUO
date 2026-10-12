using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;
using Server.Network;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// Wakes the ground round the bots the way the engine wakes it round a player.
///
/// <para>
/// <b>The engine keeps a creature's mind switched off wherever no client is.</b> A map sector wakes only when a mobile with
/// a network connection walks into it (<c>Map.Sector.OnEnter</c>), and a creature in a sleeping sector has its AI timer
/// stopped (<c>AITimer.ShouldStop</c>, <c>BaseCreature.OnSectorDeactivate</c>): it does not walk, look for enemies, cast,
/// breathe, heal itself or run. It still swings at whatever it is fighting when that stands next to it, because the swing is
/// the engine's combat timer and whoever struck it first became its combatant (<c>Mobile.AggressiveAction</c>). A bot holds
/// no connection (<see cref="BotMobile"/>), so with Patrick off the shard every creature a bot has met was asleep: an orcish
/// mage that never cast, a deer that never ran, a wolf that never came at anybody on a road. Found on 26.09.2026 through the
/// proving ground, whose creatures never touched a double that did not stand next to them (build 249).
/// </para>
///
/// <para>
/// <b>What this does when it runs.</b> Every <see cref="EveryMs"/>, every living bot in the world that qualifies — with
/// <see cref="DeepsOnly"/>, only one inside a dungeon — wakes the sectors round it exactly as a player's client would
/// (<c>Map.ActivateSectors</c>, the same five by five). A sector no bot has needed for <see cref="GraceMs"/> is put back to
/// sleep, unless a client is near enough to keep it awake by the engine's own rule. <b>Off by default:</b> the world's
/// balance — every hunt, every road, every delve — was struck against sleeping creatures, and waking it is Patrick's call.
/// </para>
/// </summary>
public static class BotWake
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWake));

    /// <summary>Whether bots wake the ground round them at all. Off: the world sleeps wherever no client is, as it always has.</summary>
    public static bool Running { get; set; }

    /// <summary>Only a bot inside a dungeon wakes the ground: the delve meets what the proving ground measured, and a hunt in the open is as it was.</summary>
    public static bool DeepsOnly { get; set; } = true;

    /// <summary>How often the bots' ground is looked over.</summary>
    public static int EveryMs { get; set; } = 2000;

    /// <summary>How long a sector stays awake after the last bot that needed it has gone.</summary>
    public static int GraceMs { get; set; } = 60000;

    /// <summary>How far round a bot the door's census looks for creatures.</summary>
    public static int CensusReach { get; set; } = 18;

    /// <summary>Sectors this woke (each counted once per waking), and sectors it put back to sleep.</summary>
    public static long Woken { get; private set; }

    public static long PutBack { get; private set; }

    /// <summary>Sectors past their grace that were left awake because a client was near them.</summary>
    public static long LeftToClients { get; private set; }

    /// <summary>Sectors past their grace left awake because the debuggers' tour of the zones holds them (<see cref="BotZoneTour"/>).</summary>
    public static long LeftToTour { get; private set; }

    /// <summary>Whether a bot needs this sector awake now. Asked by the zones' tour before it puts one of its own back to sleep.</summary>
    public static bool Needed(Map map, int x, int y) => _needed.ContainsKey((map, x, y));

    /// <summary>Every sector a bot has needed lately, and when it last did.</summary>
    private static readonly Dictionary<(Map Map, int X, int Y), long> _needed = [];

    private static readonly List<(Map Map, int X, int Y)> _expired = [];

    private static Timer _timer;

    public static void Start()
    {
        _timer?.Stop();
        _timer = new WakeTimer(TimeSpan.FromMilliseconds(Math.Max(250, EveryMs)));
        _timer.Start();
    }

    public static void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    /// <summary>One look over the bots' ground: wake what they stand in, put back what they left. Asleep, it only lets go.</summary>
    public static void Beat()
    {
        var now = Core.TickCount;

        if (Running)
        {
            var bots = BotPopulation.Bots;

            for (var i = 0; i < bots.Count; i++)
            {
                var bot = bots[i];

                if (bot is not { Deleted: false, Alive: true } || bot.Map is not { } map || map == Map.Internal)
                {
                    continue;
                }

                if (DeepsOnly && bot.Region?.IsPartOf<DungeonRegion>() != true)
                {
                    continue;
                }

                Hold(map, bot.X >> Map.SectorShift, bot.Y >> Map.SectorShift, now);
            }
        }

        LetGo(now);
    }

    /// <summary>Wakes the five by five round one sector, as a client walking into it would, and marks each as needed now.</summary>
    private static void Hold(Map map, int cx, int cy, long now)
    {
        var width = map.Width >> Map.SectorShift;
        var height = map.Height >> Map.SectorShift;

        for (var x = cx - Map.SectorActiveRange; x <= cx + Map.SectorActiveRange; x++)
        {
            for (var y = cy - Map.SectorActiveRange; y <= cy + Map.SectorActiveRange; y++)
            {
                if (x < 0 || y < 0 || x >= width || y >= height)
                {
                    continue;
                }

                var sector = map.GetRealSector(x, y);

                if (!sector.Active)
                {
                    sector.Activate();
                    Woken++;
                }

                _needed[(map, x, y)] = now;
            }
        }
    }

    /// <summary>Puts back to sleep every sector no bot has needed for the grace, unless a client keeps it awake.</summary>
    private static void LetGo(long now)
    {
        if (_needed.Count == 0)
        {
            return;
        }

        _expired.Clear();

        foreach (var (key, last) in _needed)
        {
            if (Running && now - last < GraceMs)
            {
                continue;
            }

            _expired.Add(key);
        }

        for (var i = 0; i < _expired.Count; i++)
        {
            var (map, x, y) = _expired[i];

            _needed.Remove(_expired[i]);

            if (ClientNear(map, x, y))
            {
                LeftToClients++;

                continue;
            }

            // The debuggers' tour is holding it to watch a patrol: handed over, and the tour puts it back to sleep on its own grace.
            if (BotZoneTour.Adopt(map, x, y))
            {
                LeftToTour++;

                continue;
            }

            var sector = map.GetRealSector(x, y);

            if (sector.Active)
            {
                sector.Deactivate();
                PutBack++;
            }
        }

        _expired.Clear();
    }

    /// <summary>The engine's own rule for keeping a sector awake: a client within the active range of it.</summary>
    public static bool ClientNear(Map map, int x, int y)
    {
        foreach (var state in NetState.Instances)
        {
            if (state?.Mobile is not { Deleted: false } m || m.Map != map)
            {
                continue;
            }

            if (Math.Abs((m.X >> Map.SectorShift) - x) <= Map.SectorActiveRange
                && Math.Abs((m.Y >> Map.SectorShift) - y) <= Map.SectorActiveRange)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// For the door: of the creatures within <see cref="CensusReach"/> of any bot, how many have their minds switched on — all
    /// of them, those inside dungeons, and those that would fight — and how many clients are on the shard to wake anything.
    /// </summary>
    public static string Census()
    {
        var seen = new HashSet<Serial>();
        int near = 0, awake = 0, deepNear = 0, deepAwake = 0, hostile = 0, hostileAwake = 0, clients = 0;

        foreach (var state in NetState.Instances)
        {
            if (state?.Mobile != null)
            {
                clients++;
            }
        }

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false } || bot.Map is not { } map || map == Map.Internal)
            {
                continue;
            }

            var deep = bot.Region?.IsPartOf<DungeonRegion>() == true;

            foreach (var beast in map.GetMobilesInRange<BaseCreature>(bot.Location, CensusReach))
            {
                if (beast is not { Deleted: false, Alive: true, Controlled: false } || !seen.Add(beast.Serial))
                {
                    continue;
                }

                var on = beast.AIObject?.AITimer.Running == true;

                near++;

                if (on)
                {
                    awake++;
                }

                if (deep)
                {
                    deepNear++;

                    if (on)
                    {
                        deepAwake++;
                    }
                }

                if (beast is not BaseVendor && beast.FightMode != FightMode.None && beast.Karma < 0)
                {
                    hostile++;

                    if (on)
                    {
                        hostileAwake++;
                    }
                }
            }
        }

        return $"of the {near} creatures within {CensusReach} tiles of a bot, {awake} have their minds on; of the {hostile} that would fight, {hostileAwake}; "
            + $"inside dungeons {deepAwake} of {deepNear}; {clients} clients on the shard. The wake {Describe()}";
    }

    public static string Describe() =>
        (Running ? DeepsOnly ? "is on inside dungeons" : "is on everywhere a bot goes" : "is off (the world sleeps wherever no client is)")
        + $": {_needed.Count} sectors held awake now, {Woken} woken and {PutBack} put back to sleep since the boot"
        + (LeftToClients > 0 ? $", {LeftToClients} left awake for a client" : "")
        + (LeftToTour > 0 ? $", {LeftToTour} left awake for the zones' tour" : "");

    /// <summary>After a hot reload or reset: nothing is held. What was woken is let go on the next look, by the grace.</summary>
    public static void Forget()
    {
        Woken = 0;
        PutBack = 0;
        LeftToClients = 0;
        LeftToTour = 0;
    }

    private sealed class WakeTimer : Timer
    {
        public WakeTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick()
        {
            try
            {
                Beat();
            }
            catch (Exception e)
            {
                logger.Error(e, "The wake's look over the bots' ground threw");
            }
        }
    }
}
