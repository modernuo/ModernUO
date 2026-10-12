using System;
using System.Diagnostics;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// The sweep: every living creature of the home map passed once every few seconds, a window of sectors at a time, within a
/// budget of loop time per beat.
///
/// <para>
/// <b>Spatial questions only, never the world's list.</b> One window is <see cref="WindowSectors"/> sectors to a side
/// (128 tiles at eight), asked of the map with <c>GetMobilesInBounds</c>, which walks the sectors of the window and nothing
/// else; an empty window — the sea, the black between the facets — costs the walk of its sector heads. The beat takes
/// windows until <see cref="BudgetMs"/> is spent or <see cref="MostWindows"/> are done, and the time is measured and printed,
/// because a budget nobody measures is a hope.
/// </para>
///
/// <para>
/// <b>And the tour's ground more often.</b> While a debugger holds a zone awake, its box is passed every
/// <see cref="BotZoneTour.FocusMs"/> (<see cref="Focus"/>) rather than once a sweep: the whole point of standing there is to
/// see the patrol move, and a sighting every fifteen seconds would see a minute of it four times.
/// </para>
/// </summary>
public static class BotZoneScan
{
    /// <summary>Sectors to a side of one window. Eight: 128 tiles.</summary>
    public static int WindowSectors { get; set; } = 8;

    /// <summary>Loop milliseconds the sweep may spend in one beat.</summary>
    public static double BudgetMs { get; set; } = 0.6;

    /// <summary>Windows at most in one beat, whatever the clock says.</summary>
    public static int MostWindows { get; set; } = 48;

    /// <summary>Whole sweeps finished, and windows passed, since the start.</summary>
    public static long Sweeps { get; private set; }

    public static long Windows { get; private set; }

    /// <summary>Loop milliseconds the sweep has spent, all told, and on the tour's focus.</summary>
    public static double SpentMs { get; private set; }

    public static double FocusSpentMs { get; private set; }

    public static long Focused { get; private set; }

    /// <summary>The last whole sweep: seconds it took, creatures passed, attackers among them, attackers awake, sea creatures, the towns' and the tame.</summary>
    public static double SweepSeconds { get; private set; }

    public static int Creatures { get; private set; }

    public static int Hostile { get; private set; }

    public static int HostileAwake { get; private set; }

    public static int Passive { get; private set; }

    public static int PassiveAwake { get; private set; }

    public static int Sea { get; private set; }

    public static int Ignored { get; private set; }

    private static Map _map;

    private static int _across;

    private static int _down;

    private static int _cursor;

    private static long _sweepBegan;

    private static int _creatures;

    private static int _hostile;

    private static int _hostileAwake;

    private static int _passive;

    private static int _passiveAwake;

    private static int _sea;

    private static int _ignored;

    public static void Open(Map map, long now)
    {
        Forget();
        _map = map;

        var side = Math.Max(1, WindowSectors) << Map.SectorShift;

        _across = (map.Width + side - 1) / side;
        _down = (map.Height + side - 1) / side;
        _sweepBegan = now;
    }

    /// <summary>Windows until the budget is spent. True when it passed any.</summary>
    public static bool Step(long now)
    {
        var map = _map;

        if (map == null || _across == 0)
        {
            return false;
        }

        var began = Stopwatch.GetTimestamp();
        var budget = (long)(BudgetMs * Stopwatch.Frequency / 1000.0);
        var side = Math.Max(1, WindowSectors) << Map.SectorShift;
        var done = 0;

        while (done < MostWindows)
        {
            var wx = _cursor % _across;
            var wy = _cursor / _across;

            Window(map, new Rectangle2D(wx * side, wy * side, side, side), now, false);
            done++;
            Windows++;

            if (++_cursor >= _across * _down)
            {
                _cursor = 0;
                Sweeps++;
                SweepSeconds = (now - _sweepBegan) / 1000.0;
                _sweepBegan = now;
                Creatures = _creatures;
                Hostile = _hostile;
                HostileAwake = _hostileAwake;
                Passive = _passive;
                PassiveAwake = _passiveAwake;
                Sea = _sea;
                Ignored = _ignored;
                _creatures = _hostile = _hostileAwake = _passive = _passiveAwake = _sea = _ignored = 0;
            }

            if (Stopwatch.GetTimestamp() - began >= budget)
            {
                break;
            }
        }

        SpentMs += Stopwatch.GetElapsedTime(began).TotalMilliseconds;

        return done > 0;
    }

    /// <summary>One box passed now, for the tour's held ground. Counted apart from the sweep.</summary>
    public static void Focus(Map map, Rectangle2D box, long now)
    {
        if (map == null || map != _map)
        {
            return;
        }

        var began = Stopwatch.GetTimestamp();

        Window(map, box, now, true);
        Focused++;
        FocusSpentMs += Stopwatch.GetElapsedTime(began).TotalMilliseconds;
    }

    private static void Window(Map map, Rectangle2D box, long now, bool focus)
    {
        foreach (var c in map.GetMobilesInBounds<BaseCreature>(box))
        {
            if (c is not { Deleted: false, Alive: true } || c.Controlled || c.Summoned || c.Blessed || c.IsDeadBondedPet)
            {
                if (!focus)
                {
                    _ignored++;
                }

                continue;
            }

            var awake = c.AIObject?.AITimer.Running == true;
            var kind = BotHaunts.Note(c, awake, now);

            if (kind == null)
            {
                if (!focus)
                {
                    _ignored++;
                }

                continue;
            }

            var water = c.CantWalk && c.CanSwim;
            var attacks = BotHaunts.AttacksFirst(c.FightMode);

            // A sea creature's patrol is not the land's: it is never written into the raster the land zones are drawn from. Nor is
            // a creature in a fight (02.10.2026): one chasing a bot is where the bot led it, not where it patrols — the Sonnet
            // analyst of 388 found 676 sighting cells inside Britain that matched no spawner, chasers recorded as patrols.
            if (attacks && awake && !water && c.Combatant == null)
            {
                BotZoneRaster.Record(c.X, c.Y, now);
            }

            if (focus)
            {
                continue;
            }

            _creatures++;

            if (water)
            {
                _sea++;
            }

            if (attacks)
            {
                _hostile++;

                if (awake)
                {
                    _hostileAwake++;
                }
            }
            else
            {
                _passive++;

                if (awake)
                {
                    _passiveAwake++;
                }
            }
        }
    }

    public static string Describe() =>
        _map == null
            ? "the sweep is not running"
            : $"{Sweeps} sweeps of {_across * _down} windows, the last in {SweepSeconds:F0}s: {Creatures} creatures, {Hostile} that attack on sight ({HostileAwake} awake, {(Hostile == 0 ? 0 : HostileAwake * 100 / Hostile)}%), {Passive} that do not ({PassiveAwake} awake), {Sea} of the sea, {Ignored} tame, summoned, blessed or the towns'; {SpentMs:F0}ms of the loop in all ({(Windows == 0 ? 0 : SpentMs / Windows):F3}ms a window), {Focused} focus passes for the tour in {FocusSpentMs:F0}ms";

    public static void Forget()
    {
        _map = null;
        _across = 0;
        _down = 0;
        _cursor = 0;
        Sweeps = 0;
        Windows = 0;
        SpentMs = 0.0;
        FocusSpentMs = 0.0;
        Focused = 0;
        SweepSeconds = 0.0;
        Creatures = Hostile = HostileAwake = Passive = PassiveAwake = Sea = Ignored = 0;
        _creatures = _hostile = _hostileAwake = _passive = _passiveAwake = _sea = _ignored = 0;
    }
}
