using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Server.BotAI.V2;

/// <summary>
/// A course over the open cells of the chart: A* from one cell to another, sliced across ticks, one search at a time.
///
/// <para>
/// <b>A search across the sea is too big for one tick and too cheap to thread.</b> The worst pair on map0 — Britain's dock
/// to Skara Brae's, round the south of the continent — expands 113,061 cells, and a pair that cannot be joined expands the
/// whole open sea, 214,506 (the Python pass of 29.09.2026). That is tens of milliseconds, so a search keeps its heap and its
/// marks between slices and stops at the slice's deadline. The marks are stamped rather than cleared: the arrays are the
/// size of the chart and allocated once, and a new search is a new stamp.
/// </para>
///
/// <para>
/// <b>What a course is.</b> Eight directions, ten a straight step and fourteen a diagonal one, a diagonal only past two open
/// corners (<see cref="BotSeaChart.Passable"/>), and a cell shut for a while by a ship that found it blocked is not entered
/// (<see cref="Shut"/>). The cells come back as the turns only: a straight run of cells is one leg, sailed straight from one
/// cell's middle to another's, which is a line of cells the chart has already proved open.
/// </para>
/// </summary>
public static class BotSeaCourse
{
    /// <summary>One course asked for. Filled in when <see cref="Done"/>.</summary>
    public sealed class Ask
    {
        public int From;

        public int To;

        public string For;

        public bool Done;

        public bool Found;

        /// <summary>The turns of the course, first cell to last, as cell indices.</summary>
        public List<int> Turns;

        public int Tiles;

        public long Expanded;
    }

    /// <summary>How long a cell a ship found blocked stays shut to new courses. Ten minutes: another ship moves on well before.</summary>
    public static int ShutMs { get; set; } = 600000;

    private static readonly Queue<Ask> _asks = new();

    private static readonly Dictionary<int, long> _shut = [];

    private static Ask _current;

    private static int[] _g;

    private static int[] _came;

    private static int[] _seenStamp;

    private static int[] _closedStamp;

    private static int _stamp;

    private static readonly PriorityQueue<int, int> _heap = new();

    public static long Searches { get; private set; }

    public static long Found { get; private set; }

    public static long NotFound { get; private set; }

    public static long Expanded { get; private set; }

    public static long Worst { get; private set; }

    public static int Waiting => _asks.Count + (_current != null ? 1 : 0);

    public static void Enqueue(Ask ask)
    {
        if (ask != null)
        {
            _asks.Enqueue(ask);
        }
    }

    /// <summary>Shuts a cell to new courses for <see cref="ShutMs"/>. Courses already drawn are not redrawn by this.</summary>
    public static void Shut(int cell)
    {
        if (cell >= 0)
        {
            _shut[cell] = Core.TickCount;
        }
    }

    private static bool IsShut(int cell)
    {
        if (_shut.Count == 0 || !_shut.TryGetValue(cell, out var when))
        {
            return false;
        }

        if (Core.TickCount - when < ShutMs)
        {
            return true;
        }

        _shut.Remove(cell);

        return false;
    }

    /// <summary>Works on the courses asked for until the deadline (a <see cref="Stopwatch"/> timestamp).</summary>
    public static void Step(long deadline)
    {
        if (!BotSeaChart.Ready)
        {
            return;
        }

        while (true)
        {
            if (_current == null)
            {
                if (_asks.Count == 0)
                {
                    return;
                }

                Begin(_asks.Dequeue());
            }

            if (!Search(deadline))
            {
                return;
            }
        }
    }

    private static void Begin(Ask ask)
    {
        var size = BotSeaChart.Width * BotSeaChart.Height;

        if (_g == null || _g.Length != size)
        {
            _g = new int[size];
            _came = new int[size];
            _seenStamp = new int[size];
            _closedStamp = new int[size];
            _stamp = 0;
        }

        _current = ask;
        _stamp++;
        _heap.Clear();
        Searches++;

        if (!BotSeaChart.IsOpen(ask.From) || !BotSeaChart.IsOpen(ask.To))
        {
            Finish(false);

            return;
        }

        _g[ask.From] = 0;
        _came[ask.From] = -1;
        _seenStamp[ask.From] = _stamp;
        _heap.Enqueue(ask.From, Heuristic(ask.From, ask.To));
    }

    /// <summary>True when the search in hand ended, false when it ran out of slice.</summary>
    private static bool Search(long deadline)
    {
        var ask = _current;

        if (ask == null)
        {
            return true;
        }

        var h = BotSeaChart.Height;
        var count = 0;

        while (_heap.TryDequeue(out var cell, out _))
        {
            if (_closedStamp[cell] == _stamp)
            {
                continue;
            }

            if (cell == ask.To)
            {
                Finish(true);

                return true;
            }

            _closedStamp[cell] = _stamp;
            ask.Expanded++;
            Expanded++;

            var cx = cell / h;
            var cy = cell % h;
            var g = _g[cell];

            for (var d = 0; d < 8; d++)
            {
                if (!BotSeaChart.Passable(cx, cy, d, out var next) || _closedStamp[next] == _stamp || IsShut(next))
                {
                    continue;
                }

                var cost = g + ((d & 1) == 1 ? 14 : 10);

                if (_seenStamp[next] == _stamp && _g[next] <= cost)
                {
                    continue;
                }

                _seenStamp[next] = _stamp;
                _g[next] = cost;
                _came[next] = cell;
                _heap.Enqueue(next, cost + Heuristic(next, ask.To));
            }

            if ((++count & 255) == 0 && Stopwatch.GetTimestamp() >= deadline)
            {
                return false;
            }
        }

        Finish(false);

        return true;
    }

    /// <summary>Octile distance in tenths of a cell, a hair over so that straight courses win ties.</summary>
    private static int Heuristic(int a, int b)
    {
        var h = BotSeaChart.Height;
        var dx = Math.Abs(a / h - b / h);
        var dy = Math.Abs(a % h - b % h);

        return (10 * (dx + dy) - 6 * Math.Min(dx, dy)) * 1001 / 1000;
    }

    private static void Finish(bool found)
    {
        var ask = _current;

        _current = null;
        _heap.Clear();

        if (ask == null)
        {
            return;
        }

        ask.Done = true;
        ask.Found = found;

        if (ask.Expanded > Worst)
        {
            Worst = ask.Expanded;
        }

        if (!found)
        {
            NotFound++;

            return;
        }

        Found++;

        // Walked back from the goal, then turned round; only the cells where the heading changes are kept.
        List<int> cells = [];

        for (var c = ask.To; c >= 0; c = _came[c])
        {
            cells.Add(c);

            if (c == ask.From)
            {
                break;
            }
        }

        cells.Reverse();

        ask.Tiles = _g[ask.To] * 8 / 10;
        ask.Turns = Turns(cells);
    }

    /// <summary>The first cell, every cell where the heading changes, and the last.</summary>
    public static List<int> Turns(List<int> cells)
    {
        List<int> turns = [];

        if (cells == null || cells.Count == 0)
        {
            return turns;
        }

        turns.Add(cells[0]);

        var h = BotSeaChart.Height;
        var heading = int.MinValue;

        for (var i = 1; i < cells.Count; i++)
        {
            var step = (cells[i] / h - cells[i - 1] / h) * 3 + (cells[i] % h - cells[i - 1] % h);

            if (heading != int.MinValue && step != heading)
            {
                turns.Add(cells[i - 1]);
            }

            heading = step;
        }

        if (cells.Count > 1)
        {
            turns.Add(cells[^1]);
        }

        return turns;
    }

    public static string Describe() =>
        Searches == 0
            ? "no course asked for"
            : $"{Searches} courses searched, {Found} found, {NotFound} with no way, {Expanded} cells expanded (the most in one {Worst}), {_shut.Count} cells shut by blocked ships";

    public static void Forget()
    {
        _asks.Clear();
        _shut.Clear();
        _current = null;
        _heap.Clear();
        _g = null;
        _came = null;
        _seenStamp = null;
        _closedStamp = null;
        _stamp = 0;
        Searches = 0;
        Found = 0;
        NotFound = 0;
        Expanded = 0;
        Worst = 0;
    }
}
