using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Server.BotAI.V2;

/// <summary>One minute of the shard, for the page's charts.</summary>
public readonly record struct BotWebSample(
    DateTime At,
    int Alive,
    double WorkShare,
    double PathShare,
    long Taken,
    long Finished,
    long Failed,
    long Dropped,
    long Deaths,
    double LoopMs,
    int Stuck,
    int Searches,
    int Reached
);

/// <summary>
/// The shard a minute at a time since it started, so that a number on the page can be read as a trend
/// rather than a value. Every counter it reads is cumulative; the loop cost is the one figure taken as a
/// difference, because a total of milliseconds since boot says nothing about now.
/// </summary>
public static class BotWebHistory
{
    public static int EveryMs { get; set; } = 60000;

    public static int Keep { get; set; } = 1440;

    private static readonly object _lock = new();

    private static readonly Queue<BotWebSample> _samples = new();

    private static long _lastTick;

    private static double _lastSpent;

    private static long _lastSearches;

    private static long _lastReached;

    private static bool _first;

    public static int Count
    {
        get
        {
            lock (_lock)
            {
                return _samples.Count;
            }
        }
    }

    /// <summary>Takes a sample when one is due. Game loop only.</summary>
    public static void Sample(long now, int alive, int stuck)
    {
        if (!_first)
        {
            _first = true;
            _lastTick = now;
            _lastSpent = BotWill.SpentMs + BotWalk.SpentMs;
            _lastSearches = BotWalk.WalkerSearches;
            _lastReached = BotWalk.WalkerReached;

            return;
        }

        if (now - _lastTick < EveryMs)
        {
            return;
        }

        var seconds = Math.Max(1.0, (now - _lastTick) / 1000.0);
        var spent = BotWill.SpentMs + BotWalk.SpentMs;
        var loopMs = (spent - _lastSpent) / seconds;
        // The walkers' own count, beside counting as arriving (build 386, see BotWalk.WalkerSearches).
        var searches = BotWalk.WalkerSearches - _lastSearches;
        var reached = BotWalk.WalkerReached - _lastReached;

        _lastTick = now;
        _lastSpent = spent;
        _lastSearches = BotWalk.WalkerSearches;
        _lastReached = BotWalk.WalkerReached;

        var ended = BotWill.Finished + BotWill.Failed + BotWill.Dropped;
        var sample = new BotWebSample(
            DateTime.Now,
            alive,
            ended <= 0 ? 0.0 : (double)BotWill.Finished / ended,
            BotWalk.WalkerSearches <= 0 ? 0.0 : (double)BotWalk.WalkerReached / BotWalk.WalkerSearches,
            BotWill.Taken,
            BotWill.Finished,
            BotWill.Failed,
            BotWill.Dropped,
            BotWill.Deaths,
            Math.Round(loopMs, 2),
            stuck,
            (int)searches,
            (int)reached
        );

        lock (_lock)
        {
            _samples.Enqueue(sample);

            while (_samples.Count > Keep)
            {
                _samples.Dequeue();
            }
        }
    }

    /// <summary>The whole series as JSON. Any thread.</summary>
    public static string Json()
    {
        BotWebSample[] samples;

        lock (_lock)
        {
            samples = [.. _samples];
        }

        return BotJson.Object(
            w =>
            {
                w.WriteNumber("everyMs", EveryMs);
                w.WritePropertyName("samples");
                w.WriteStartArray();

                foreach (var s in samples)
                {
                    w.WriteStartObject();
                    w.WriteString("at", s.At.ToString("HH:mm"));
                    w.WriteString("day", s.At.ToString("yyyy-MM-dd"));
                    w.WriteNumber("alive", s.Alive);
                    w.WriteNumber("workShare", Math.Round(s.WorkShare, 4));
                    w.WriteNumber("pathShare", Math.Round(s.PathShare, 4));
                    w.WriteNumber("taken", s.Taken);
                    w.WriteNumber("finished", s.Finished);
                    w.WriteNumber("failed", s.Failed);
                    w.WriteNumber("dropped", s.Dropped);
                    w.WriteNumber("deaths", s.Deaths);
                    w.WriteNumber("loopMs", s.LoopMs);
                    w.WriteNumber("stuck", s.Stuck);
                    w.WriteNumber("searches", s.Searches);
                    w.WriteNumber("reached", s.Reached);
                    w.WriteEndObject();
                }

                w.WriteEndArray();
            }
        );
    }

    public static void Reset()
    {
        lock (_lock)
        {
            _samples.Clear();
        }

        _first = false;
    }
}
