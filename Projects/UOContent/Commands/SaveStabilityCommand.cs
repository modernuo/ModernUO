using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Server.Logging;

namespace Server.Commands;

/// <summary>
/// Measures whether entities serialize to the same bytes over time when nothing changed them.
/// Hashes every entity of every registered entity persistence, waits, hashes again, and reports
/// per type how many records changed. Both passes are chunked across ticks so the loop never
/// stalls, and the wait spans real time so anything derived from <see cref="Core.Now" /> (which
/// is frozen within a tick) shows up. On an idle shard the only legitimate churn is NPC movement
/// and regeneration; a static type with a high changed fraction is a serialization bug.
/// </summary>
public static class SaveStability
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SaveStability));

    private const int DefaultDelaySeconds = 60;
    private static readonly TimeSpan TickBudget = TimeSpan.FromMilliseconds(20);

    private static Run _current;

    public sealed class TypeStats
    {
        public int Total;
        public int Changed;
    }

    public sealed record SaveStabilityReport(
        int Checked,
        int Changed,
        Dictionary<Type, TypeStats> ByType
    );

    public static void Configure()
    {
        CommandSystem.Register("SaveStability", AccessLevel.Administrator, SaveStability_OnCommand);
    }

    [Usage("SaveStability [delaySeconds=60] [sampleStride=1] | SaveStability cancel")]
    [Description("Hashes every entity, waits, hashes again, and reports the types whose serialized bytes changed.")]
    private static void SaveStability_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Length > 0 && e.GetString(0).InsensitiveEquals("cancel"))
        {
            if (_current != null)
            {
                _current.Cancel();
                _current = null;
                from.SendMessage("Save stability check cancelled.");
            }
            else
            {
                from.SendMessage("No save stability check is running.");
            }

            return;
        }

        if (_current != null)
        {
            from.SendMessage("A save stability check is already running. Use [SaveStability cancel to stop it.");
            return;
        }

        var delaySeconds = e.Length > 0 ? e.GetInt32(0) : DefaultDelaySeconds;
        var stride = e.Length > 1 ? e.GetInt32(1) : 1;

        if (delaySeconds < 1 || stride < 1)
        {
            from.SendMessage("Usage: [SaveStability [delaySeconds] [sampleStride]");
            return;
        }

        _current = new Run(from, TimeSpan.FromSeconds(delaySeconds), stride);
        _current.Start();
    }

    /// <summary>Snapshots every entity of every registered entity persistence, taking every Nth.</summary>
    public static List<ISerializable> SnapshotEntities(int stride = 1)
    {
        var list = new List<ISerializable>();
        var i = 0;

        foreach (var persistence in Persistence.EntityPersistences)
        {
            foreach (var entity in persistence.EnumerateEntities())
            {
                if (i++ % stride == 0)
                {
                    list.Add(entity);
                }
            }
        }

        return list;
    }

    /// <summary>Hashes the serialized bytes of every entity in order; deleted entities hash to 0.</summary>
    public static ulong[] CaptureHashes(List<ISerializable> entities)
    {
        var hashes = new ulong[entities.Count];
        var writer = new BufferWriter(true);

        for (var i = 0; i < entities.Count; i++)
        {
            hashes[i] = Hash(entities[i], writer);
        }

        writer.Close();
        return hashes;
    }

    /// <summary>Re-hashes every entity and reports, per type, how many differ from the captured hashes.</summary>
    public static SaveStabilityReport Compare(List<ISerializable> entities, ulong[] captured)
    {
        var writer = new BufferWriter(true);
        var report = new SaveStabilityReport(0, 0, new Dictionary<Type, TypeStats>());
        var checkedCount = 0;
        var changed = 0;

        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (entity.Deleted || captured[i] == 0)
            {
                continue;
            }

            checkedCount++;
            var type = entity.GetType();

            if (!report.ByType.TryGetValue(type, out var stats))
            {
                report.ByType[type] = stats = new TypeStats();
            }

            stats.Total++;

            if (Hash(entity, writer) != captured[i])
            {
                stats.Changed++;
                changed++;
            }
        }

        writer.Close();
        return report with { Checked = checkedCount, Changed = changed };
    }

    private static ulong Hash(ISerializable entity, BufferWriter writer)
    {
        if (entity.Deleted)
        {
            return 0;
        }

        writer.Seek(0, SeekOrigin.Begin);
        entity.Serialize(writer);
        return HashUtility.ComputeHash64(writer.Buffer.AsSpan(0, (int)writer.Position));
    }

    // Drives capture -> wait -> compare across ticks under a fixed time budget per tick.
    private sealed class Run
    {
        private readonly Mobile _from;
        private readonly TimeSpan _delay;
        private readonly int _stride;
        private readonly BufferWriter _writer = new(true);

        private List<ISerializable> _entities;
        private ulong[] _hashes;
        private SaveStabilityReport _report;
        private int _index;
        private bool _comparing;
        private Timer _timer;

        public Run(Mobile from, TimeSpan delay, int stride)
        {
            _from = from;
            _delay = delay;
            _stride = stride;
        }

        public void Start()
        {
            _entities = SnapshotEntities(_stride);
            _hashes = new ulong[_entities.Count];

            _from.SendMessage($"Save stability: hashing {_entities.Count} entities across ticks...");
            _timer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.Zero, Step);
        }

        public void Cancel()
        {
            _timer?.Stop();
            _timer = null;
            _writer.Close();
        }

        private void Step()
        {
            var started = Stopwatch.GetTimestamp();

            while (_index < _entities.Count)
            {
                var entity = _entities[_index];

                if (_comparing)
                {
                    CompareOne(entity, _index);
                }
                else
                {
                    _hashes[_index] = Hash(entity, _writer);
                }

                _index++;

                if (Stopwatch.GetElapsedTime(started) >= TickBudget)
                {
                    return;
                }
            }

            _timer.Stop();
            _timer = null;

            if (!_comparing)
            {
                _from.SendMessage($"Save stability: captured. Comparing again in {_delay.TotalSeconds:F0}s.");
                _comparing = true;
                _index = 0;
                _report = new SaveStabilityReport(0, 0, new Dictionary<Type, TypeStats>());
                _timer = Timer.DelayCall(_delay, TimeSpan.Zero, Step);
                return;
            }

            Finish();
        }

        private void CompareOne(ISerializable entity, int index)
        {
            if (entity.Deleted || _hashes[index] == 0)
            {
                return;
            }

            var type = entity.GetType();

            if (!_report.ByType.TryGetValue(type, out var stats))
            {
                _report.ByType[type] = stats = new TypeStats();
            }

            stats.Total++;

            if (Hash(entity, _writer) != _hashes[index])
            {
                stats.Changed++;
            }
        }

        private void Finish()
        {
            _writer.Close();
            _current = null;

            var checkedCount = 0;
            var changed = 0;
            var ranked = new List<KeyValuePair<Type, TypeStats>>();

            foreach (var pair in _report.ByType)
            {
                checkedCount += pair.Value.Total;
                changed += pair.Value.Changed;

                if (pair.Value.Changed > 0)
                {
                    ranked.Add(pair);
                }
            }

            ranked.Sort(static (a, b) => b.Value.Changed.CompareTo(a.Value.Changed));

            _from.SendMessage($"Save stability: {changed} of {checkedCount} entities changed over {_delay.TotalSeconds:F0}s.");
            logger.Information(
                "Save stability: {Changed} of {Checked} entities changed over {Delay}s ({Types} types)",
                changed,
                checkedCount,
                _delay.TotalSeconds,
                ranked.Count
            );

            var shown = 0;
            foreach (var (type, stats) in ranked)
            {
                var percent = stats.Changed * 100.0 / stats.Total;
                logger.Information("  {Type}: {Changed}/{Total} ({Percent:F1}%)", type.FullName, stats.Changed, stats.Total, percent);

                if (shown++ < 25)
                {
                    _from.SendMessage($"  {type.Name}: {stats.Changed}/{stats.Total} ({percent:F1}%)");
                }
            }

            if (ranked.Count > 25)
            {
                _from.SendMessage($"  ...{ranked.Count - 25} more types in the log.");
            }
        }
    }
}
