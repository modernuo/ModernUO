/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DeltaSaves.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using ModernUO.Serialization;
using Server.Logging;

namespace Server;

public enum DeltaSaveMode
{
    /// <summary>Every entity serializes on every save. Placements are still maintained.</summary>
    Off,

    /// <summary>
    /// Every entity serializes; clean entities of trusted types are compared byte for byte
    /// with their previous record by the writer, and every mismatch is reported. No freeze
    /// savings; this is how a type's dirty tracking is qualified before <see cref="On" />.
    /// </summary>
    Verify,

    /// <summary>
    /// Clean entities of trusted types are not serialized: the writer copies their previous
    /// record. A fresh random sample of them is serialized and compared each save.
    /// </summary>
    On
}

/// <summary>
/// The decision a save runs under, computed before the workers wake and immutable until the
/// save finishes. <see cref="SerializeAll" /> is what a full save means: nothing is copied and
/// every placement is rewritten, so the serial-reuse guard can be cleared once it commits.
/// </summary>
public sealed class SavePlan
{
    public static readonly SavePlan Full = new(true, false, 0, 0, "default");

    public bool SerializeAll { get; }
    public bool Verify { get; }
    public uint SampleThreshold { get; }
    public int SaveIndex { get; }
    public string Reason { get; }

    /// <summary>The full-save request version this plan satisfies; see <see cref="DeltaSaves.RequestFullSave" />.</summary>
    public int FullRequestVersion { get; init; }

    public bool IsFull => SerializeAll;

    public SavePlan(bool serializeAll, bool verify, uint sampleThreshold, int saveIndex, string reason)
    {
        SerializeAll = serializeAll;
        Verify = verify;
        SampleThreshold = sampleThreshold;
        SaveIndex = saveIndex;
        Reason = reason;
    }
}

/// <summary>
/// Counters and verification findings for one save. The writer thread fills it during
/// <see cref="WorldState.WritingSave" />; the loop consumes it in <c>FinishWorldSave</c>.
/// </summary>
public sealed class SaveReport
{
    public const int SerialsPerType = 8;

    public long Serialized { get; internal set; }
    public long Verified { get; internal set; }
    public long Copied { get; internal set; }
    public long Skipped { get; internal set; }
    public long SerializedBytes { get; internal set; }
    public long CopiedBytes { get; internal set; }
    public TimeSpan FreezeDuration { get; internal set; }

    private Dictionary<Type, List<Serial>> _violations;
    private Dictionary<Type, int> _violationCounts;

    public bool HasViolations => _violationCounts != null;

    public IReadOnlyDictionary<Type, int> ViolationCounts => _violationCounts;

    public IReadOnlyList<Serial> ViolationSerials(Type type) =>
        _violations != null && _violations.TryGetValue(type, out var list) ? list : [];

    internal void AddViolation(Type type, Serial serial)
    {
        _violationCounts ??= new Dictionary<Type, int>();
        _violations ??= new Dictionary<Type, List<Serial>>();

        ref var count = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(
            _violationCounts, type, out _
        );
        count++;

        ref var serials = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(
            _violations, type, out _
        );
        serials ??= [];

        if (serials.Count < SerialsPerType)
        {
            serials.Add(serial);
        }
    }
}

/// <summary>
/// Delta world saves: the world freeze serializes only entities that changed, and the snapshot
/// writer copies everything else from the previous save file, producing the same complete
/// files as a full save. This class owns the mode, the per-save plan, type trust, the
/// denylist of types caught with a missed <see cref="ISerializableExtensions.MarkDirty" />,
/// and the failure protocol (any failed save forces the next one full).
/// </summary>
public static class DeltaSaves
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(DeltaSaves));

    private const string ModeKey = "world.delta.mode";
    private const string SampleRateKey = "world.delta.sampleRate";
    private const string FullSaveEveryKey = "world.delta.fullSaveEvery";
    private const string DenylistKey = "world.delta.denylist";

    private static readonly Dictionary<Type, bool> _eligibility = new();
    private static readonly HashSet<string> _denylist = new(StringComparer.Ordinal);
    private static readonly Dictionary<Type, int> _violationsSinceBoot = new();

    private static volatile bool _forceFullNext = true;
    private static string _forceFullReason = "first save after boot";

    // Each request bumps this; a committed full save clears only the requests its plan covered.
    private static volatile int _fullRequestVersion;

    public static DeltaSaveMode Mode { get; private set; }

    /// <summary>Fraction of clean trusted entities re-serialized for verification on a delta save.</summary>
    public static double SampleRate { get; private set; } = 0.01;

    /// <summary>Every Nth save serializes everything; 0 disables the periodic full save.</summary>
    public static int FullSaveEvery { get; private set; } = 24;

    /// <summary>Committed saves since boot.</summary>
    public static int SaveCount { get; private set; }

    /// <summary>Set by any failure, the boot, or <c>[DeltaSave full</c>; cleared when a full save commits.</summary>
    public static bool ForceFullNext => _forceFullNext;

    public static string ForceFullReason => _forceFullReason;

    /// <summary>The plan of the save in flight, or of the last one. Never null.</summary>
    public static SavePlan Plan { get; private set; } = SavePlan.Full;

    /// <summary>The report of the save in flight, or of the last one.</summary>
    public static SaveReport Report { get; private set; } = new();

    public static SaveReport LastReport { get; private set; }

    public static IReadOnlyCollection<string> Denylist => _denylist;

    public static IReadOnlyDictionary<Type, int> ViolationsSinceBoot => _violationsSinceBoot;

    public static void Configure()
    {
        // Parsed here rather than through the enum helper so a hand-edited "on" works.
        Mode = Enum.TryParse<DeltaSaveMode>(ServerConfiguration.GetOrUpdateSetting(ModeKey, "off"), true, out var mode)
            ? mode
            : DeltaSaveMode.Off;
        SampleRate = Math.Clamp(ServerConfiguration.GetOrUpdateSetting(SampleRateKey, 0.01), 0.0, 1.0);
        FullSaveEvery = Math.Max(0, ServerConfiguration.GetOrUpdateSetting(FullSaveEveryKey, 24));

        _denylist.Clear();
        foreach (var name in ServerConfiguration.GetOrUpdateSetting(DenylistKey, "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            _denylist.Add(name);
        }
    }

    public static void SetMode(DeltaSaveMode mode)
    {
        if (Mode == mode)
        {
            return;
        }

        Mode = mode;
        ServerConfiguration.SetSetting(ModeKey, mode.ToString().ToLowerInvariant());
        logger.Information("Delta save mode set to {Mode}", mode);
    }

    public static void SetSampleRate(double rate)
    {
        SampleRate = Math.Clamp(rate, 0.0, 1.0);
        ServerConfiguration.SetSetting(SampleRateKey, SampleRate);
    }

    public static void SetFullSaveEvery(int saves)
    {
        FullSaveEvery = Math.Max(0, saves);
        ServerConfiguration.SetSetting(FullSaveEveryKey, FullSaveEvery);
    }

    public static void RequestFullSave(string reason)
    {
        _forceFullReason = reason;
        _fullRequestVersion++;
        _forceFullNext = true;
    }

    // ---- Trust -----------------------------------------------------------------------------

    /// <summary>
    /// True when every class from <paramref name="type" /> up to the root that implements
    /// <see cref="ISerializable" /> is generated or audited, and none is volatile. Cached per
    /// type; attributes are read with inheritance disabled because each class must qualify
    /// on its own.
    /// </summary>
    public static bool IsEligible(Type type)
    {
        // Loop-only: persistences resolve trust into per-type tables when a type registers or
        // the denylist changes; save workers read those tables, never this cache.
        if (_eligibility.TryGetValue(type, out var eligible))
        {
            return eligible;
        }

        eligible = ComputeEligible(type);
        _eligibility[type] = eligible;
        return eligible;
    }

    private static bool ComputeEligible(Type type)
    {
        for (var t = type; t != null && t != typeof(object); t = t.BaseType)
        {
            if (t.GetCustomAttribute<VolatileSerializedStateAttribute>(false) != null)
            {
                return false;
            }

            if (t.GetCustomAttribute<SerializationGeneratorAttribute>(false) == null &&
                t.GetCustomAttribute<AuditedDirtyTrackingAttribute>(false) == null)
            {
                return false;
            }

            // The root is the last class in the chain that is still an ISerializable.
            if (t.BaseType == null || !typeof(ISerializable).IsAssignableFrom(t.BaseType))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsDenied(Type type) => _denylist.Count > 0 && _denylist.Contains(type.FullName);

    public static bool IsTrusted(Type type) => !IsDenied(type) && IsEligible(type);

    /// <summary>Stops skipping a type; returns false when it was already denied.</summary>
    public static bool Deny(Type type)
    {
        if (!_denylist.Add(type.FullName))
        {
            return false;
        }

        PersistDenylist();
        RefreshTrust();
        return true;
    }

    /// <summary>Removes a type from the denylist; returns false when it was not there.</summary>
    public static bool Trust(string typeName)
    {
        if (!_denylist.Remove(typeName))
        {
            return false;
        }

        PersistDenylist();
        RefreshTrust();
        return true;
    }

    private static void PersistDenylist()
    {
        var names = new List<string>(_denylist);
        names.Sort(StringComparer.Ordinal);
        ServerConfiguration.SetSetting(DenylistKey, string.Join(',', names));
    }

    private static void RefreshTrust() => Persistence.RefreshTrustAll();

    // ---- Save lifecycle --------------------------------------------------------------------

    /// <summary>
    /// Off-loop, before the workers wake: validates the copy sources and decides the plan. The
    /// loop reads <see cref="Plan" /> after the snapshot request it posts, so publication is
    /// ordered by that handoff.
    /// </summary>
    internal static SavePlan Prepare()
    {
        if (Mode != DeltaSaveMode.Off && !Persistence.ValidateCopySources())
        {
            RequestFullSave("copy source changed");
        }

        var index = SaveCount;
        string reason;
        bool serializeAll;

        if (_forceFullNext)
        {
            serializeAll = true;
            reason = _forceFullReason;
        }
        else if (Mode != DeltaSaveMode.On)
        {
            serializeAll = true;
            reason = Mode == DeltaSaveMode.Off ? "mode off" : "mode verify";
        }
        else if (FullSaveEvery > 0 && index % FullSaveEvery == 0)
        {
            serializeAll = true;
            reason = "periodic";
        }
        else
        {
            serializeAll = false;
            reason = "delta";
        }

        var verify = Mode != DeltaSaveMode.Off;
        var threshold = Mode == DeltaSaveMode.On ? SaveSampler.Threshold(SampleRate) : 0;

        Plan = new SavePlan(serializeAll, verify, threshold, index, reason) { FullRequestVersion = _fullRequestVersion };
        Report = new SaveReport();
        return Plan;
    }

    /// <summary>
    /// The per-entity decision, shared by the slot-range and buffer-chunk worker paths. Returns
    /// the <see cref="SlotStatus" /> to log; serializes into <paramref name="writer" /> when the
    /// status carries a length.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int SerializeEntity(
        ISerializable entity, Persistence owner, SerializationThreadWorker worker, BufferWriter writer, SavePlan plan
    )
    {
        if (entity.SkipSerialization)
        {
            worker.EntitiesSkipped++;
            return SlotStatus.Skipped;
        }

        // Trust is only consulted when it can change the outcome; dirty entities never pay for it.
        var canCopy = false;
        if (!plan.SerializeAll || plan.Verify)
        {
            canCopy = !entity.SaveDirty && entity.SavePlacement != SavePlacement.None &&
                      owner.IsTrustedType(entity.GetType());
        }

        if (canCopy && !plan.SerializeAll && !(plan.Verify && worker.Sampler.Hit(plan.SampleThreshold)))
        {
            worker.EntitiesCopied++;
            return SlotStatus.Copied;
        }

        entity.SaveDirty = false;

        var start = writer.Position;
        entity.Serialize(writer);
        var length = (int)(writer.Position - start);

        worker.EntitiesSerialized++;

        if (canCopy && plan.Verify && length > 0)
        {
            worker.EntitiesVerified++;
            return SlotStatus.Verify(length);
        }

        return SlotStatus.Emitted(length);
    }

    /// <summary>Loop, after the write phase: commits or rolls back the save's bookkeeping.</summary>
    internal static void OnSaveFinished(bool success, SerializationThreadWorker[] workers)
    {
        var plan = Plan;
        var report = Report;

        // Copied records (and their bytes) are counted by the writer, which is the only place
        // that knows whether the copy happened; the workers count the rest.
        for (var i = 0; i < workers.Length; i++)
        {
            var worker = workers[i];
            report.Serialized += worker.EntitiesSerialized;
            report.Verified += worker.EntitiesVerified;
            report.Skipped += worker.EntitiesSkipped;
            report.SerializedBytes += worker.BytesSerialized;
        }

        if (!success)
        {
            RequestFullSave("previous save failed");
            Persistence.OnSaveFailedAll();
            LastReport = report;
            return;
        }

        SaveCount++;
        Persistence.CommitSaveAll(plan.IsFull);

        if (plan.IsFull && plan.FullRequestVersion == _fullRequestVersion)
        {
            _forceFullNext = false;
        }

        LastReport = report;

        if (report.HasViolations)
        {
            try
            {
                HandleViolations(plan, report);
            }
            catch (Exception ex)
            {
                // Denylisting persists to the configuration file; if that fails the types are
                // still denied in memory and every save is a fresh chance to detect them.
                logger.Error(ex, "Handling delta save verification violations failed");
            }
        }

        logger.Information(
            "Delta save ({Mode}, {Reason}): serialized {Serialized} ({SerializedBytes} bytes), copied {Copied} ({CopiedBytes} bytes), verified {Verified}, skipped {Skipped}, violations {Violations}",
            Mode,
            plan.Reason,
            report.Serialized,
            report.SerializedBytes,
            report.Copied,
            report.CopiedBytes,
            report.Verified,
            report.Skipped,
            report.HasViolations ? report.ViolationCounts.Count : 0
        );
    }

    private static void HandleViolations(SavePlan plan, SaveReport report)
    {
        var denied = 0;

        {
            foreach (var (type, count) in report.ViolationCounts)
            {
                ref var total = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(
                    _violationsSinceBoot, type, out _
                );
                total += count;

                if (Deny(type))
                {
                    denied++;
                }

                logger.Error(
                    "Delta save verification: {Type} changed without MarkDirty on {Count} entities (e.g. {Serials}); the type is denylisted",
                    type.FullName,
                    count,
                    string.Join(", ", report.ViolationSerials(type))
                );
            }

            World.BroadcastStaff(
                $"Delta save verification found {report.ViolationCounts.Count} type(s) with missed dirty tracking. Check the logs and [DeltaSave report."
            );

            // Copied instances of a newly denied type may be stale in the file just written;
            // the next save serializes all of them, so bring it forward.
            if (denied > 0 && !plan.IsFull)
            {
                Timer.DelayCall(TimeSpan.FromSeconds(60), World.Save);
            }
        }
    }

    /// <summary>Test support: installs a plan without touching configuration or copy sources.</summary>
    internal static void SetPlanForTest(SavePlan plan)
    {
        Plan = plan;
        Report = new SaveReport();
    }

    /// <summary>Test support: resets the process-wide state.</summary>
    internal static void ResetForTest(DeltaSaveMode mode = DeltaSaveMode.Off)
    {
        Mode = mode;
        SampleRate = 0.01;
        FullSaveEvery = 24;
        SaveCount = 0;
        _forceFullNext = true;
        _forceFullReason = "first save after boot";
        Plan = SavePlan.Full;
        Report = new SaveReport();
        LastReport = null;
        _denylist.Clear();
        _violationsSinceBoot.Clear();
        _eligibility.Clear();
    }
}
