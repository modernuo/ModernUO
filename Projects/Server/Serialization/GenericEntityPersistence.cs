/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GenericEntityPersistence.cs                                     *
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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Server.Logging;

namespace Server;

public interface IGenericEntityPersistence
{
    string Name { get; }

    int EntityCount { get; }

    /// <summary>Concrete entity types registered since boot.</summary>
    int TypeCount { get; }

    /// <summary>Registered types whose clean entities a delta save may copy (eligible and not denylisted).</summary>
    int TrustedTypeCount { get; }

    void DeserializeIndexes(string savePath, Dictionary<ulong, string> typesDb);

    /// <summary>
    /// Enumerates the live entities. Diagnostics only: the dictionary must not be mutated while
    /// enumerating, so callers snapshot the sequence before doing anything that can add or delete.
    /// </summary>
    IEnumerable<ISerializable> EnumerateEntities();
}

public class GenericEntityPersistence<T> : GenericPersistence, IGenericEntityPersistence, ISlotRangeSource
    where T : class, ISerializable
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(GenericEntityPersistence<T>));

    // Layout-validated direct access to EntitiesBySerial's entries array, letting workers
    // iterate the dictionary in parallel during saves. Null when unsupported on this runtime.
    private static readonly FieldInfo _entriesField =
        ShadowDictionaryEntries.Supported ? ShadowDictionaryEntries.GetEntriesField<T>() : null;

    // The entries array captured at freeze time. The dictionary cannot mutate while saving
    // (adds/removes divert to the pending queues), so the array is stable until released.
    private object _entriesSnapshot;

    // Support legacy split file serialization
    private static Dictionary<int, List<EntitySpan<T>>> _entities;

    private readonly Serial _minSerial;
    private readonly Serial _maxSerial;
    private Serial _lastEntitySerial;
    private readonly Dictionary<Serial, T> _pendingAdd = new();
    private readonly Dictionary<Serial, T> _pendingDelete = new();

    // Insertion-ordered table of every entity type added since boot. idx v4 records
    // reference types by table index, so entries are never removed — a type whose
    // entities were all deleted keeps its slot until restart. Only mutated on the game
    // thread (AddEntity, deserialize); only read on the background writer thread during
    // WritingSave, when AddEntity diverts to the pending queues.
    private readonly Dictionary<Type, ushort> _typeIndexes = new();
    private readonly List<Type> _typeTable = [];

    // Delta saves: whether entities of each registered type may be copied from the previous
    // save, indexed like _typeTable. Rebuilt on the loop when a type registers or the denylist
    // changes; read by the save workers during the freeze.
    private bool[] _trustedByTypeIndex = [];

    // Serials freed since the last committed full save. A clean referrer's copied record can
    // still name one of them, so they are not reissued until every referrer has been rewritten.
    // A full save rotates the live set aside at its freeze (serials freed after the freeze
    // belong to the next save) and drops it when the save commits, or merges it back if not.
    private HashSet<Serial> _unreusableSerials;
    private HashSet<Serial> _unreusableSerialsClearedOnCommit;

    // Identity of the committed .bin every placement refers to (-1 = none), and the identity
    // of the file the writer just produced, committed once the save is published.
    private long _committedBinLength = -1;
    private DateTime _committedBinWriteTime;
    private long _committedAnchorTicks;
    private long _stagedBinLength = -1;
    private DateTime _stagedBinWriteTime;
    private long _stagedAnchorTicks;
    private bool _copySourceValid;

    internal IReadOnlyList<Type> TypeTable => _typeTable;

    public int TypeCount => _typeTable.Count;

    public int TrustedTypeCount
    {
        get
        {
            var trusted = 0;
            for (var i = 0; i < _typeTable.Count; i++)
            {
                trusted += _trustedByTypeIndex[i] ? 1 : 0;
            }

            return trusted;
        }
    }

    /// <summary>The file placements refer to. Overridable so tests can point at their own directory.</summary>
    protected virtual string CommittedBinPath => Path.Combine(World.SavePath, Name, $"{Name}.bin");

    internal bool HasCommittedCopySource => _committedBinLength >= 0;

    internal int UnreusableSerialCount =>
        (_unreusableSerials?.Count ?? 0) + (_unreusableSerialsClearedOnCommit?.Count ?? 0);

    internal bool TryGetTypeIndex(Type type, out ushort index) => _typeIndexes.TryGetValue(type, out index);

    internal void RegisterType(Type type)
    {
        ref var index = ref CollectionsMarshal.GetValueRefOrAddDefault(_typeIndexes, type, out var exists);
        if (!exists)
        {
            if (_typeTable.Count > ushort.MaxValue)
            {
                throw new InvalidOperationException(
                    $"{Name} exceeded {ushort.MaxValue + 1} distinct entity types."
                );
            }

            index = (ushort)_typeTable.Count;
            _typeTable.Add(type);

            if (_trustedByTypeIndex.Length < _typeTable.Count)
            {
                Array.Resize(ref _trustedByTypeIndex, Math.Max(_typeTable.Count, _trustedByTypeIndex.Length * 2));
            }

            _trustedByTypeIndex[index] = DeltaSaves.IsTrusted(type);
        }
    }

    internal override bool IsTrustedType(Type type) =>
        _typeIndexes.TryGetValue(type, out var index) && _trustedByTypeIndex[index];

    internal override void RefreshTrust()
    {
        for (var i = 0; i < _typeTable.Count; i++)
        {
            _trustedByTypeIndex[i] = DeltaSaves.IsTrusted(_typeTable[i]);
        }
    }

    internal override bool ValidateCopySource()
    {
        // No committed file: the first save after boot, or the save after a failure.
        if (_committedBinLength < 0)
        {
            _copySourceValid = false;
            return false;
        }

        // Length and write time catch replacements; the idx anchor (the save-start time, unique
        // per save) catches a different save that happens to match both.
        var file = new FileInfo(CommittedBinPath);
        _copySourceValid = file.Exists && file.Length == _committedBinLength &&
                           file.LastWriteTimeUtc == _committedBinWriteTime &&
                           ReadIndexAnchor(Path.ChangeExtension(CommittedBinPath, ".idx")) == _committedAnchorTicks;

        if (!_copySourceValid)
        {
            logger.Warning(
                "{Name}: the previous save file changed since it was written; the next save serializes everything.",
                Name
            );
        }

        return _copySourceValid;
    }

    private static long ReadIndexAnchor(string idxPath)
    {
        try
        {
            using var fs = new FileStream(idxPath, FileMode.Open, FileAccess.Read, FileShare.Read, 16);
            Span<byte> header = stackalloc byte[12];
            if (fs.Read(header) != header.Length || BinaryPrimitives.ReadInt32LittleEndian(header) < 5)
            {
                return -1;
            }

            return BinaryPrimitives.ReadInt64LittleEndian(header[4..]);
        }
        catch (IOException)
        {
            return -1;
        }
        catch (UnauthorizedAccessException)
        {
            return -1;
        }
    }

    internal override void CommitSave(bool full)
    {
        _committedBinLength = _stagedBinLength;
        _committedBinWriteTime = _stagedBinWriteTime;
        _committedAnchorTicks = _stagedAnchorTicks;
        _stagedBinLength = -1;

        if (full)
        {
            // Every referrer was rewritten by this save; serials freed after its freeze are
            // still in the live set.
            _unreusableSerialsClearedOnCommit = null;
        }
    }

    internal override void OnSaveFailed()
    {
        // Placements may already point into the file that was never published. Forget the
        // committed file so nothing is copied or compared until a full save commits and
        // rewrites every placement.
        _committedBinLength = -1;
        _stagedBinLength = -1;
        _copySourceValid = false;

        if (_unreusableSerialsClearedOnCommit != null)
        {
            if (_unreusableSerials == null)
            {
                _unreusableSerials = _unreusableSerialsClearedOnCommit;
            }
            else
            {
                _unreusableSerials.UnionWith(_unreusableSerialsClearedOnCommit);
            }

            _unreusableSerialsClearedOnCommit = null;
        }
    }

    public Dictionary<Serial, T> EntitiesBySerial { get; } = new();

    public int EntityCount => EntitiesBySerial.Count;

    public IEnumerable<ISerializable> EnumerateEntities()
    {
        foreach (var entity in EntitiesBySerial.Values)
        {
            yield return entity;
        }
    }

    public GenericEntityPersistence(string name, int priority, uint minSerial, uint maxSerial) : this(
        name,
        priority,
        (Serial)minSerial,
        (Serial)maxSerial
    )
    {
    }

    public GenericEntityPersistence(string name, int priority, Serial minSerial, Serial maxSerial) : base(name, priority)
    {
        _minSerial = minSerial;
        _maxSerial = maxSerial;
        _lastEntitySerial = minSerial - 1;
        typeof(T).RegisterFindEntity(Find);
    }

    public override void WriteSnapshot(string savePath)
    {
        var dir = Path.Combine(savePath, Name);
        PathUtility.EnsureDirectory(dir);

        var threads = World._threadWorkers;
        var plan = DeltaSaves.Plan;
        var report = DeltaSaves.Report;
        var binPath = Path.Combine(dir, $"{Name}.bin");

        // The previous save file backs copied records and the verification of re-serialized
        // ones. It is opened only when the plan can need it and it still is the file the
        // placements refer to (an empty persistence has no file to open and nothing to copy);
        // a copied record that finds no source aborts the save.
        using var source = plan.SerializeAll && !plan.Verify || !_copySourceValid
            ? null
            : CopySource.TryOpen(CommittedBinPath, _committedBinLength, _committedBinWriteTime);

        long binPosition;

        // 1MB buffer: segments are written as large spans, but idx entries and skip splits
        // still benefit on the snapshot thread.
        using (var binFs = new FileStream(binPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024))
        {
            // v4 records are fixed-width 26 bytes; the v5 header carries the save-start anchor
            // and the type table (name lengths vary — 64 bytes per entry is a staging hint, not
            // a contract).
            var expectedIdxSize = 20 + 26L * EntitiesBySerial.Count + 64L * _typeTable.Count;
            using var idx = new FileBufferWriter(Path.Combine(dir, $"{Name}.idx"), expectedIdxSize);

            binPosition = 0L;

            // Support for non-entity generic serialization.
            if (_selfLength > 0)
            {
                binFs.Write(threads[_selfThread].GetHeap(_selfPosition, _selfLength));
                binPosition += _selfLength;
            }

            idx.Write(5); // Version

            // One anchor for the whole save: the world is frozen from the moment it is stamped.
            idx.Write(World.SaveStartTime.Ticks);

            // The type table is fully known at freeze (AddEntity diverts to the pending
            // queues while saving) and is written before the records so the loader can
            // resolve constructors before reading them.
            idx.Write(_typeTable.Count);
            for (var i = 0; i < _typeTable.Count; i++)
            {
                idx.WriteRaw(_typeTable[i].FullName);
            }

            var countPosition = idx.Position;
            idx.Write(0);

            // The bin is written in worker-heap order, not dictionary order: each worker logged
            // (segment, slot statuses) as it drained, so records pair with their bytes by
            // re-walking the same slots in the same order. Copied records come from the
            // previous file instead of a heap. The idx stores absolute positions, so the loader
            // never cares about record order.
            var entityCount = 0;

            for (var t = 0; t < threads.Length; t++)
            {
                var worker = threads[t];
                var segments = worker.Segments;

                for (var i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];
                    if (!ReferenceEquals(segment.Owner, this))
                    {
                        continue;
                    }

                    try
                    {
                        binPosition = WriteSegmentRecords(
                            worker, in segment, idx, binFs, source, report, binPosition, ref entityCount
                        );
                    }
                    catch (Exception error)
                    {
                        // Never publish a partial snapshot: entities missing from the idx are deleted on load.
                        logger.Error(
                            error,
                            "Error writing segment: (Thread: {Thread} - {Start}, {Records} records)",
                            t,
                            segment.HeapStart,
                            segment.RecordCount
                        );
                        throw;
                    }
                }
            }

            var currentPosition = idx.Position;
            idx.Seek(countPosition, SeekOrigin.Begin);
            idx.Write(entityCount);
            idx.Seek(currentPosition, SeekOrigin.Begin);
        }

        // Stage the identity of the file just written; it becomes the copy source when the
        // save is published (CommitSave on the loop). A rename keeps the last write time.
        _stagedBinLength = binPosition;
        _stagedBinWriteTime = File.GetLastWriteTimeUtc(binPath);
        _stagedAnchorTicks = World.SaveStartTime.Ticks;
    }

    /// <summary>
    /// Streams one worker segment into the bin/idx: heap spans for serialized records, spans
    /// of the previous file for copied ones (coalesced when adjacent), and a byte comparison
    /// with the previous record for verify ones. Stores each written record's new placement
    /// on its entity.
    /// </summary>
    private long WriteSegmentRecords(
        SerializationThreadWorker worker, in SerializedSegment segment, FileBufferWriter idx, FileStream binFs,
        CopySource source, SaveReport report, long binPosition, ref int entityCount
    )
    {
        var state = new RecordWriter(worker, idx, binFs, source, report, (int)segment.HeapStart, binPosition);
        var statuses = worker.Statuses;
        var statusIndex = segment.StatusStart;

        if (segment.SlotOffset >= 0)
        {
            // Re-walk the same slots the worker drained; occupancy cannot have changed
            // because dictionary mutations divert to the pending queues until PostWorldSave.
            var entries = Unsafe.As<ShadowEntry<T>[]>(_entriesSnapshot);
            ref var entry = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(entries), segment.SlotOffset);

            for (var i = 0; i < segment.SlotCount; i++, entry = ref Unsafe.Add(ref entry, 1))
            {
                var entity = entry.Value;
                if (entity == null)
                {
                    continue;
                }

                state.Write(this, entity, statuses[statusIndex++]);
            }
        }
        else
        {
            var bufferEntities = worker.BufferEntities;

            for (var i = 0; i < segment.RecordCount; i++)
            {
                var entity = (T)bufferEntities[segment.EntitiesStart + i];
                state.Write(this, entity, statuses[statusIndex++]);
            }
        }

        state.Finish();
        entityCount += state.Records;
        return state.BinPosition;
    }

    /// <summary>
    /// Sequencing state for one segment: at most one pending heap span or one pending copy run
    /// at a time, flushed before a record of the other kind, so the bin order always matches
    /// the idx order.
    /// </summary>
    private ref struct RecordWriter
    {
        private readonly SerializationThreadWorker _worker;
        private readonly FileBufferWriter _idx;
        private readonly FileStream _bin;
        private readonly CopySource _source;
        private readonly SaveReport _report;

        private int _heapPos;
        private int _spanStart;
        private long _copyStart;
        private long _copyEnd;

        public long BinPosition;
        public int Records;

        public RecordWriter(
            SerializationThreadWorker worker, FileBufferWriter idx, FileStream bin, CopySource source,
            SaveReport report, int heapStart, long binPosition
        )
        {
            _worker = worker;
            _idx = idx;
            _bin = bin;
            _source = source;
            _report = report;
            _heapPos = heapStart;
            _spanStart = heapStart;
            _copyStart = -1;
            _copyEnd = -1;
            BinPosition = binPosition;
        }

        public void Write(GenericEntityPersistence<T> owner, T entity, int status)
        {
            if (status == SlotStatus.Skipped)
            {
                // No record: whatever bytes the previous save held for it are not ours anymore.
                entity.SavePlacement = SavePlacement.None;
                return;
            }

            if (status == SlotStatus.Copied)
            {
                var placement = entity.SavePlacement;

                if (_source == null || SavePlacement.IsNone(placement))
                {
                    throw new InvalidOperationException(
                        $"{entity.GetType()} ({entity.Serial}) was marked for copying but has no previous record."
                    );
                }

                var position = SavePlacement.Position(placement);
                var length = SavePlacement.Length(placement);

                FlushHeapSpan();

                if (_copyEnd == position)
                {
                    _copyEnd += length;
                }
                else
                {
                    FlushCopyRun();
                    _copyStart = position;
                    _copyEnd = position + length;
                }

                WriteIndexRecord(owner, entity, length);
                _report.Copied++;
                _report.CopiedBytes += length;
                return;
            }

            var recordLength = SlotStatus.Length(status);

            FlushCopyRun();

            if (SlotStatus.IsVerify(status) && _source != null)
            {
                var previous = entity.SavePlacement;
                var previousLength = SavePlacement.Length(previous);

                if (SavePlacement.IsNone(previous) || previousLength != recordLength ||
                    !_source.Slice(SavePlacement.Position(previous), previousLength)
                        .SequenceEqual(_worker.GetHeap(_heapPos, recordLength)))
                {
                    _report.AddViolation(entity.GetType(), entity.Serial);
                }
            }

            WriteIndexRecord(owner, entity, recordLength);
            _heapPos += recordLength;
        }

        private void WriteIndexRecord(GenericEntityPersistence<T> owner, T entity, int length)
        {
            _idx.Write(owner.GetTypeIndex(entity));
            _idx.Write(entity.Serial);
            _idx.Write(entity.Created.Ticks);
            _idx.Write(BinPosition);
            _idx.Write(length);

            // The writer thread owns this field during WritingSave (see ISerializable).
            entity.SavePlacement = SavePlacement.Encode(BinPosition, length);

            BinPosition += length;
            Records++;
        }

        private void FlushHeapSpan()
        {
            if (_heapPos > _spanStart)
            {
                _bin.Write(_worker.GetHeap(_spanStart, _heapPos - _spanStart));
            }

            _spanStart = _heapPos;
        }

        private void FlushCopyRun()
        {
            if (_copyEnd > _copyStart)
            {
                _source.CopyTo(_bin, _copyStart, _copyEnd - _copyStart);
            }

            _copyStart = -1;
            _copyEnd = -1;
        }

        public void Finish()
        {
            FlushHeapSpan();
            FlushCopyRun();
        }
    }

    private ushort GetTypeIndex(T entity)
    {
        // Every path into EntitiesBySerial registers the type first, so this cannot fire.
        // If it does, the save fails; treat it as a bug in an insertion path, not a bad entity.
        if (!_typeIndexes.TryGetValue(entity.GetType(), out var typeIndex))
        {
            throw new InvalidOperationException(
                $"{entity.GetType()} was serialized but never registered; entities must enter {Name} through AddEntity."
            );
        }

        return typeIndex;
    }

    public override void Serialize()
    {
        RotateUnreusableSerials();

        // Self-payload first so a large one overlaps the entity stream instead of ending it.
        World.PushSingleToCache(this);

        // Fast path: publish slot ranges of the dictionary's entries array so the workers
        // iterate it directly in parallel — the main thread never touches the entities.
        if (TrySnapshotEntries(out var slotCount))
        {
            World.PushSlotRangesToCache(this, slotCount);
            return;
        }

        // Fallback: enumerate and hand off every entity from the main thread. Kept branch-free:
        // a bare loop is ~2.3x faster than one carrying per-entity logic, and multi-megabyte
        // entities are rare enough that riding inside a shared chunk is an acceptable tail.
        foreach (var entity in EntitiesBySerial.Values)
        {
            World.PushToCache(entity);
        }
    }

    /// <summary>
    /// At the freeze of a full save the live guard set is put aside: this save rewrites every
    /// referrer of the serials in it, so it can be dropped once the save commits, while serials
    /// freed after this moment start a new set for the next save.
    /// </summary>
    internal void RotateUnreusableSerials()
    {
        if (!DeltaSaves.Plan.IsFull || _unreusableSerials == null)
        {
            return;
        }

        if (_unreusableSerialsClearedOnCommit == null)
        {
            _unreusableSerialsClearedOnCommit = _unreusableSerials;
        }
        else
        {
            _unreusableSerialsClearedOnCommit.UnionWith(_unreusableSerials);
        }

        _unreusableSerials = null;
    }

    internal bool TrySnapshotEntries(out int slotCount)
    {
        if (_entriesField != null && EntitiesBySerial.Count > 0 &&
            _entriesField.GetValue(EntitiesBySerial) is Array entries)
        {
            _entriesSnapshot = entries;
            slotCount = entries.Length;
            return true;
        }

        slotCount = 0;
        return false;
    }

    int ISlotRangeSource.SerializeRange(SerializationThreadWorker worker, BufferWriter writer, int offset, int count)
    {
        // Layout proven at startup by ShadowDictionaryEntries.Supported; ranges are produced
        // from the same array's length, so every read is in-bounds.
        var entries = Unsafe.As<ShadowEntry<T>[]>(_entriesSnapshot);
        var statuses = worker.Statuses;
        var plan = DeltaSaves.Plan;
        var occupied = 0;

        ref var entry = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(entries), offset);

        for (var i = 0; i < count; i++, entry = ref Unsafe.Add(ref entry, 1))
        {
            // Occupied slots are exactly the non-null values: Dictionary clears reference
            // values on remove, and never-used capacity is zero-initialized.
            var entity = entry.Value;
            if (entity != null)
            {
                statuses.Add(DeltaSaves.SerializeEntity(entity, this, worker, writer, plan));
                occupied++;
            }
        }

        return occupied;
    }

    private static ConstructorInfo GetConstructorFor(string typeName, Type t, Type[] constructorTypes)
    {
        if (t?.IsAbstract != false)
        {
            Console.WriteLine("failed");

            var issue = t?.IsAbstract == true ? "marked abstract" : "not found";

            Console.Write($"Error: Type '{typeName}' was {issue}. Delete all of those types? (y/n): ");

            if (ConsoleInputHandler.ReadLine().InsensitiveEquals("y"))
            {
                Console.WriteLine("Loading...");
                return null;
            }

            Console.WriteLine("Types will not be deleted. An exception will be thrown.");

            throw new Exception($"Bad type '{typeName}'");
        }

        var ctor = t.GetConstructor(constructorTypes);

        if (ctor == null)
        {
            throw new Exception($"Type '{t}' does not have a serialization constructor");
        }

        return ctor;
    }

    /**
     * Legacy ReadTypes for backward compatibility with old saves that still have a tdb file
     */
    private unsafe Dictionary<ulong, ConstructorInfo> ReadTypes(string savePath)
    {
        var typesPath = Path.Combine(savePath, Name, $"{Name}.tdb");
        if (!File.Exists(typesPath))
        {
            return [];
        }

        Type[] ctorArguments = [typeof(Serial)];

        using var mmf = MemoryMappedFile.CreateFromFile(typesPath, FileMode.Open);
        using var accessor = mmf.CreateViewStream();

        byte* ptr = null;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        var dataReader = new UnmanagedDataReader(ptr, accessor.Length);

        var count = dataReader.ReadInt();
        var types = new Dictionary<ulong, ConstructorInfo>(count);

        for (var i = 0; i < count; ++i)
        {
            // Legacy didn't have the null flag check
            var typeName = dataReader.ReadStringRaw();
            var type = AssemblyHandler.FindTypeByName(typeName);
            var ctor = GetConstructorFor(typeName, type, ctorArguments);

            if (ctor != null)
            {
                // Keep the type table complete so the next (v4) save can index it.
                RegisterType(type);
            }

            types.Add((ulong)i, ctor);
        }

        accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        return types;
    }

    public virtual void DeserializeIndexes(string savePath, Dictionary<ulong, string> typesDb)
    {
        var indexPath = Path.Combine(savePath, Name, $"{Name}.idx");

        _entities ??= [];

        // Support for legacy MUO Serialization that used split files
        if (!File.Exists(indexPath))
        {
            TryDeserializeSplitFileIndexes(savePath, typesDb);
            return;
        }

        InternalDeserializeIndexes(indexPath, typesDb, _entities[0] = []);
    }

    private void TryDeserializeSplitFileIndexes(string savePath, Dictionary<ulong, string> typesDb)
    {
        var index = 0;
        while (true)
        {
            var path = Path.Combine(savePath, Name, $"{Name}_{index}.idx");
            var fi = new FileInfo(path);
            if (!fi.Exists)
            {
                break;
            }

            if (fi.Length == 0)
            {
                continue;
            }

            InternalDeserializeIndexes(path, typesDb, _entities[index] = []);
            index++;
        }
    }

    private unsafe void InternalDeserializeIndexes(
        string filePath, Dictionary<ulong, string> typesDb, List<EntitySpan<T>> entities
    )
    {
        using var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);
        using var accessor = mmf.CreateViewStream();

        byte* ptr = null;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        var dataReader = new UnmanagedDataReader(ptr, accessor.Length);

        var version = dataReader.ReadInt();

        if (version >= 5)
        {
            // Re-base anchored timestamps by the elapsed time since the save started.
            var anchor = new DateTime(dataReader.ReadLong(), DateTimeKind.Utc);
            var shift = Core.Now - anchor;
            _anchoredTimeShift = anchor.Ticks > 0 && shift > TimeSpan.Zero ? shift : TimeSpan.Zero;

            // The whole save shares one anchor. Publish it so payloads without their own
            // (GenericPersistence bins) can shift too; indexes load before any of them.
            World.LoadTimeShift = _anchoredTimeShift;
        }

        if (version >= 4)
        {
            DeserializeIndexesV4(dataReader, entities);
        }
        else
        {
            var ctors = version < 2 ? ReadTypes(Path.GetDirectoryName(filePath)) : [];

            if (typesDb == null && ctors.Count == 0)
            {
                accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                return;
            }

            var now = DateTime.UtcNow;
            var ctorArgs = new object[1];
            Type[] ctorArguments = [typeof(Serial)];

            var count = dataReader.ReadInt();

            for (var i = 0; i < count; ++i)
            {
                ulong hash;
                // Version 2 & 3 with SerializedTypes.db
                if (version >= 2)
                {
                    var flag = dataReader.ReadByte();
                    if (flag != 2)
                    {
                        throw new Exception($"Invalid type flag, expected 2 but received {flag}.");
                    }

                    hash = dataReader.ReadULong();
                }
                else
                {
                    hash = (ulong)dataReader.ReadInt(); // Legacy RunUO tdb index
                }

                if (!ctors.TryGetValue(hash, out var ctor) && typesDb?.TryGetValue(hash, out var typeName) == true)
                {
                    var type = AssemblyHandler.FindTypeByHash(hash);
                    ctors[hash] = ctor = GetConstructorFor(typeName, type, ctorArguments);

                    if (ctor != null)
                    {
                        // Keep the type table complete so the next (v4) save can index it.
                        RegisterType(type);
                    }
                }

                var serial = (Serial)dataReader.ReadUInt();
                var created = version == 0 ? now : new DateTime(dataReader.ReadLong(), DateTimeKind.Utc);
                if (version is > 0 and < 3)
                {
                    dataReader.ReadLong(); // LastSerialized
                }

                var pos = dataReader.ReadLong();
                var length = dataReader.ReadInt();

                if (ctor == null)
                {
                    continue;
                }

                ctorArgs[0] = serial;

                if (ctor.Invoke(ctorArgs) is T entity)
                {
                    entity.Created = created;
                    entities.Add(new EntitySpan<T>(entity, pos, length));
                    EntitiesBySerial[serial] = entity;
                }
            }
        }

        accessor.SafeMemoryMappedViewHandle.ReleasePointer();

        if (EntitiesBySerial.Count > 0)
        {
            _lastEntitySerial = EntitiesBySerial.Keys.Max();
        }
    }

    private void DeserializeIndexesV4(UnmanagedDataReader dataReader, List<EntitySpan<T>> entities)
    {
        Type[] ctorArguments = [typeof(Serial)];

        var typeCount = dataReader.ReadInt();
        var ctors = new ConstructorInfo[typeCount];

        for (var i = 0; i < typeCount; i++)
        {
            var typeName = dataReader.ReadStringRaw();
            var type = AssemblyHandler.FindTypeByHash(HashUtility.ComputeHash64(typeName));
            var ctor = GetConstructorFor(typeName, type, ctorArguments);

            if (ctor != null)
            {
                // Keep the type table complete so the next save can index it.
                RegisterType(type);
            }

            ctors[i] = ctor;
        }

        var ctorArgs = new object[1];
        var count = dataReader.ReadInt();

        for (var i = 0; i < count; ++i)
        {
            var ctor = ctors[dataReader.ReadUShort()];
            var serial = (Serial)dataReader.ReadUInt();
            var created = new DateTime(dataReader.ReadLong(), DateTimeKind.Utc);
            var pos = dataReader.ReadLong();
            var length = dataReader.ReadInt();

            if (ctor == null)
            {
                continue;
            }

            ctorArgs[0] = serial;

            if (ctor.Invoke(ctorArgs) is T entity)
            {
                entity.Created = created;
                entities.Add(new EntitySpan<T>(entity, pos, length));
                EntitiesBySerial[serial] = entity;
            }
        }
    }

    public override void Deserialize(string savePath, Dictionary<ulong, string> typesDb)
    {
        var dataPath = Path.Combine(savePath, Name, $"{Name}.bin");
        var fi = new FileInfo(dataPath);

        if (!fi.Exists)
        {
            TryDeserializeMultithread(savePath, typesDb);
        }
        else if (fi.Length > 0)
        {
            InternalDeserialize(dataPath, 0, typesDb);
        }

        _entities.Clear();
        _entities.TrimExcess();
        _entities = null;

        if (_toDelete != null)
        {
            foreach (var t in _toDelete)
            {
                t.Delete();
            }

            _toDelete.Clear();
            _toDelete = null;
        }
    }

    private static List<T> _toDelete;

    // From the loaded idx (v5+); zero when the save predates the anchor.
    private TimeSpan _anchoredTimeShift;

    private unsafe void InternalDeserialize(string filePath, int index, Dictionary<ulong, string> typesDb)
    {
        using var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);
        using var accessor = mmf.CreateViewStream();

        byte* ptr = null;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        var dataReader = new UnmanagedDataReader(ptr, accessor.Length, typesDb)
        {
            AnchoredTimeShift = _anchoredTimeShift
        };

        Deserialize(dataReader);

        var deleteAllFailures = false;

        foreach (var entry in _entities[index])
        {
            var t = entry.Entity;

            if (entry.Length == 0)
            {
                t?.Delete();
                continue;
            }

            // Skip this entry
            if (t == null)
            {
                continue;
            }

            string error;

            try
            {
                dataReader.Seek(entry.Position, SeekOrigin.Begin);
                var pos = entry.Position;

                t.Deserialize(dataReader);
                var lengthDeserialized = dataReader.Position - pos;

                error = lengthDeserialized != entry.Length
                    ? $"Serialized object was {entry.Length} bytes, but {lengthDeserialized} bytes deserialized"
                    : null;
            }
            catch (Exception e)
            {
                error = e.ToString();
            }

            if (error != null)
            {
                Console.WriteLine($"***** Bad deserialize of {t.GetType()} ({t.Serial}) *****");
                Console.WriteLine(error);

                if (!deleteAllFailures)
                {
                    Console.Write("Delete the object and continue? (y/n/a): ");
                    var pressedKey = ConsoleInputHandler.ReadLine();

                    if (pressedKey.InsensitiveEquals("a"))
                    {
                        deleteAllFailures = true;
                    }
                    else if (!pressedKey.InsensitiveEquals("y"))
                    {
                        throw new Exception("Deserialization failed.");
                    }
                }

                _toDelete ??= [];
                _toDelete.Add(t);
            }
        }

        accessor.SafeMemoryMappedViewHandle.ReleasePointer();
    }

    private void TryDeserializeMultithread(string savePath, Dictionary<ulong, string> typesDb)
    {
        if (_entities == null)
        {
            return;
        }

        var folderPath = Path.Combine(savePath, Name);

        foreach (var i in _entities.Keys)
        {
            var path = Path.Combine(folderPath, $"{Name}_{i}.bin");
            InternalDeserialize(path, i, typesDb);
        }
    }

    // Override for non-entity serialization
    public override void Serialize(IGenericWriter writer)
    {
    }

    // Override for non-entity deserialization
    public override void Deserialize(IGenericReader reader)
    {
    }

    public override void PostWorldSave()
    {
        // Release the snapshot so a between-saves resize doesn't pin the old array.
        _entriesSnapshot = null;
        ProcessSafetyQueues();
    }

    public override void PostDeserialize()
    {
        ProcessSafetyQueues();
    }

    public Serial NewEntity
    {
        get
        {
#if THREADGUARD
            if (Thread.CurrentThread != Core.Thread)
            {
                logger.Error(
                    "Attempted to get a new entity serial from the wrong thread!\n{StackTrace}",
                    new StackTrace()
                );
            }
#endif
            var last = (uint)_lastEntitySerial;
            var min = (uint)_minSerial;
            var max = (uint)_maxSerial;

            for (uint i = 0; i < max; i++)
            {
                last++;

                if (last > max)
                {
                    last = min;
                }

                if (FindEntity<T>((Serial)last) == null && !IsUnreusable((Serial)last))
                {
                    return _lastEntitySerial = (Serial)last;
                }
            }

            OutOfMemory($"No serials left to allocate for {Name}");
            return Serial.MinusOne;
        }
    }

    private bool IsUnreusable(Serial serial) =>
        _unreusableSerials?.Contains(serial) == true || _unreusableSerialsClearedOnCommit?.Contains(serial) == true;

    public void AddEntity(T entity)
    {
        var worldState = World.WorldState;
        switch (worldState)
        {
            default: // Not Running
                {
                    throw new Exception($"Added {entity.GetType().Name} before world load.");
                }
            case WorldState.Saving:
                {
                    AppendSafetyLog("add", entity);
                    goto case WorldState.Loading;
                }
            case WorldState.Loading:
            case WorldState.WritingSave:
                {
                    if (_pendingDelete.Remove(entity.Serial))
                    {
                        logger.Warning(
                            "Deleted then added {Entity} during {WorldState} state.",
                            entity.GetType().Name,
                            worldState.ToString()
                        );
                    }

                    _pendingAdd[entity.Serial] = entity;
                    break;
                }
            case WorldState.PendingSave:
            case WorldState.Running:
                {
                    // New to the world since the last save: it has no record to copy.
                    entity.SaveDirty = true;

                    RegisterType(entity.GetType());
                    ref var entityEntry = ref CollectionsMarshal.GetValueRefOrAddDefault(EntitiesBySerial, entity.Serial, out var exists);
                    if (exists)
                    {
                        if (entityEntry == entity)
                        {
                            logger.Error(
                                $"Attempted to add '{{Entity}}' ({{Serial}}) but it already exists in the collection.{Environment.NewLine}{{StackTrace}}",
                                entity.GetType().FullName,
                                entity.Serial,
                                new StackTrace()
                            );
                        }
                        else
                        {
                            logger.Error(
                                $"Attempted to add '{{Entity}}' ({{Serial}}) but found '{{ExistingEntity}}' ({{ExistingSerial}}).{Environment.NewLine}{{StackTrace}}",
                                entity.GetType().FullName,
                                entity.Serial,
                                entityEntry.GetType().FullName,
                                entityEntry.Serial,
                                new StackTrace()
                            );
                        }
                    }
                    else
                    {
                        entityEntry = entity;
                    }
                    break;
                }
        }
    }

    public void RemoveEntity(T entity)
    {
        var worldState = World.WorldState;
        switch (worldState)
        {
            default: // Not Running
                {
                    throw new Exception($"Removed {entity.GetType().Name} before world load.");
                }
            case WorldState.Saving:
                {
                    AppendSafetyLog("delete", entity);
                    goto case WorldState.Loading;
                }
            case WorldState.Loading:
            case WorldState.WritingSave:
                {
                    _pendingAdd.Remove(entity.Serial);
                    _pendingDelete[entity.Serial] = entity;
                    break;
                }
            case WorldState.PendingSave:
            case WorldState.Running:
                {
                    if (EntitiesBySerial.Remove(entity.Serial))
                    {
                        (_unreusableSerials ??= []).Add(entity.Serial);
                    }

                    break;
                }
        }
    }

    private void ProcessSafetyQueues()
    {
        foreach (var entity in _pendingAdd.Values)
        {
            AddEntity(entity);
        }

        foreach (var entity in _pendingDelete.Values)
        {
            if (_pendingAdd.ContainsKey(entity.Serial))
            {
                logger.Warning("Entity {Entity} was both pending deletion and addition after save", entity);
            }

            RemoveEntity(entity);
        }

        _pendingAdd.Clear();
        _pendingDelete.Clear();
    }

    private static void AppendSafetyLog(string action, ISerializable entity)
    {
        var message =
            $"Warning: Attempted to {{Action}} {{Entity}} during world save.{Environment.NewLine}This action could cause inconsistent state.{Environment.NewLine}It is strongly advised that the offending scripts be corrected.";

        logger.Information(message, action, entity);

        try
        {
            using var op = new StreamWriter("world-save-errors.log", true);
            op.WriteLine($"{DateTime.UtcNow}\t{message}");
            op.WriteLine(new StackTrace(2).ToString());
            op.WriteLine();
        }
        catch
        {
            // ignored
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Find(Serial serial, bool returnDeleted = false) => FindEntity<T>(serial, returnDeleted);

    public R FindEntity<R>(Serial serial, bool returnDeleted = false) where R : class, T
    {
        switch (World.WorldState)
        {
            default:
                {
                    return null;
                }
            case WorldState.Loading:
            case WorldState.Saving:
            case WorldState.WritingSave:
                {
                    if (returnDeleted && _pendingDelete.TryGetValue(serial, out var entity))
                    {
                        return entity as R;
                    }

                    if (_pendingAdd.TryGetValue(serial, out entity) || EntitiesBySerial.TryGetValue(serial, out entity))
                    {
                        return entity as R;
                    }

                    return null;
                }
            case WorldState.PendingSave:
            case WorldState.Running:
                {
                    return EntitiesBySerial.TryGetValue(serial, out var entity) ? entity as R : null;
                }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void OutOfMemory(string message) => throw new OutOfMemoryException(message);
}
