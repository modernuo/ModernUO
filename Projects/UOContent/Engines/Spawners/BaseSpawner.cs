using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using ModernUO.Serialization;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Json;
using Server.Logging;
using Server.Mobiles;
using static Server.Attributes;

namespace Server.Engines.Spawners;

/// <summary>
/// Controls how a spawner handles spawn position optimization.
/// </summary>
public enum SpawnPositionMode : byte
{
    /// <summary>
    /// Auto-detect if optimization is needed based on failure patterns.
    /// Only engages lazy caching after non-transient spawn failures.
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Force optimization on. Always cache successful spawn positions.
    /// </summary>
    Enabled = 1,

    /// <summary>
    /// Force optimization off. Use only random position attempts.
    /// </summary>
    Disabled = 2,

    /// <summary>
    /// Spawner has given up due to 100% failure rate.
    /// Skips all spawn position logic and returns spawner location.
    /// Admin can reset via [props.
    /// </summary>
    Abandoned = 3
}

[SerializationGenerator(12, false)]
public abstract partial class BaseSpawner : Item, ISpawner
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BaseSpawner));

    // Default values for serialization optimization
    private static readonly TimeSpan DefaultMinDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultMaxDelay = TimeSpan.FromMinutes(10);

    [SerializedIgnoreDupe]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private Guid _guid;

    [SerializableFieldSaveFlag(1)]
    private bool ShouldSerializeReturnOnDeactivate() => _returnOnDeactivate;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private bool _returnOnDeactivate;

    [SerializedIgnoreDupe]
    [SerializableField(2, setter: "private")]
    private List<SpawnerEntry> _entries;

    private int _walkingRange = -1;

    [SerializableFieldSaveFlag(4)]
    private bool ShouldSerializeWayPoint() => _wayPoint != null;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private WayPoint _wayPoint;

    [SerializableFieldSaveFlag(5)]
    private bool ShouldSerializeGroup() => _group;

    [InvalidateProperties]
    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private bool _group;

    [SerializableFieldSaveFlag(6)]
    private bool ShouldSerializeMinDelay() => _minDelay != DefaultMinDelay;

    [SerializableFieldDefault(6)]
    private TimeSpan MinDelayDefault() => DefaultMinDelay;

    [InvalidateProperties]
    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private TimeSpan _minDelay;

    [SerializableFieldSaveFlag(7)]
    private bool ShouldSerializeMaxDelay() => _maxDelay != DefaultMaxDelay;

    [SerializableFieldDefault(7)]
    private TimeSpan MaxDelayDefault() => DefaultMaxDelay;

    [InvalidateProperties]
    [SerializableField(7)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private TimeSpan _maxDelay;

    [SerializableFieldSaveFlag(9)]
    private bool ShouldSerializeTeam() => _team != 0;

    [InvalidateProperties]
    [SerializableField(9)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private int _team;

    /// <summary>
    /// The spawn bounds for this spawner. Abstract to allow derived classes to manage their own storage.
    /// </summary>
    [CommandProperty(AccessLevel.Developer)]
    public abstract Rectangle3D SpawnBounds { get; set; }

    /// <summary>
    /// If true, the home location of the spawn is the location where it spawned
    /// If false, the home location of the spawn is the location of the spawner
    /// </summary>
    [SerializableFieldSaveFlag(11)]
    private bool ShouldSerializeSpawnLocationIsHome() => _spawnLocationIsHome;

    [InvalidateProperties]
    [SerializableField(11)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private bool _spawnLocationIsHome;

    [SerializableFieldSaveFlag(12)]
    private bool ShouldSerializeEnd() => _end != default;

    [SerializableField(12)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private DateTime _end;

    /// <summary>
    /// Controls how spawn position optimization is handled.
    /// </summary>
    [SerializableFieldSaveFlag(13)]
    private bool ShouldSerializeSpawnPositionMode() =>
        _spawnPositionMode is not SpawnPositionMode.Automatic and not SpawnPositionMode.Abandoned;

    [SerializableField(13)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private SpawnPositionMode _spawnPositionMode;

    private const int DefaultMaxSpawnAttempts = 10;

    /// <summary>
    /// Maximum number of random position attempts before engaging optimization.
    /// </summary>
    [SerializableFieldSaveFlag(14)]
    private bool ShouldSerializeMaxSpawnAttempts() => _maxSpawnAttempts != DefaultMaxSpawnAttempts;

    [SerializableFieldDefault(14)]
    private int MaxSpawnAttemptsDefault() => DefaultMaxSpawnAttempts;

    [SerializableField(14)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private int _maxSpawnAttempts;

    // Non-serialized: Runtime state for spawn position optimization
    private SpawnPositionState _spawnPositionState;

    private InternalTimer _timer;

    /// <summary>
    /// Gets the distance from the spawner to the nearest edge of the spawn bounds.
    /// Setting this creates a square spawn area centered on the spawner location.
    /// </summary>
    [CommandProperty(AccessLevel.Developer)]
    public virtual int HomeRange
    {
        get
        {
            if (SpawnBounds == default)
            {
                return 0;
            }

            // Distance from spawner location to nearest edge
            var distToMinX = Math.Abs(Location.X - SpawnBounds.Start.X);
            var distToMaxX = Math.Abs(SpawnBounds.End.X - Location.X);
            var distToMinY = Math.Abs(Location.Y - SpawnBounds.Start.Y);
            var distToMaxY = Math.Abs(SpawnBounds.End.Y - Location.Y);

            // Return smallest distance to any edge
            return Math.Min(Math.Min(distToMinX, distToMaxX), Math.Min(distToMinY, distToMaxY));
        }
        set
        {
            int z;
            int depth;
            if (SpawnBounds != default)
            {
                z = SpawnBounds.Start.Z;
                depth = SpawnBounds.Depth;
            }
            else if (value == 0)
            {
                z = Location.Z;
                depth = 0;
            }
            else
            {
                z = -128;
                depth = 256;
            }

            // Create square bounds centered on spawner, Z range from surface to surface + 16
            SpawnBounds = new Rectangle3D(
                Location.X - value,
                Location.Y - value,
                z,
                value * 2 + 1,
                value * 2 + 1,
                depth
            );
            this.MarkDirty();
            InvalidateProperties();
        }
    }

    /// <summary>
    /// Returns true if SpawnBounds represents a HomeRange-style square centered on the spawner.
    /// </summary>
    public bool IsHomeRangeStyle => IsHomeRangeStyleAt(Location);

    /// <summary>
    /// Returns true if SpawnBounds represents a HomeRange-style square centered on the given location.
    /// </summary>
    public bool IsHomeRangeStyleAt(Point3D location)
    {
        if (SpawnBounds == default)
        {
            return true; // No bounds = default HomeRange behavior
        }

        // Must be square
        if (SpawnBounds.Width != SpawnBounds.Height)
        {
            return false;
        }

        // Given location must be at center
        var centerX = SpawnBounds.Start.X + SpawnBounds.Width / 2;
        var centerY = SpawnBounds.Start.Y + SpawnBounds.Height / 2;

        return centerX == location.X && centerY == location.Y;
    }

    /// <summary>
    /// Checks if the given location is within the spawn bounds.
    /// Virtual to allow RegionSpawner to override with region-based logic.
    /// </summary>
    public virtual bool IsInSpawnBounds(Point3D location)
    {
        return SpawnBounds == default || SpawnBounds.Contains(location);
    }

    public BaseSpawner() : this(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10))
    {
    }

    public BaseSpawner(string spawnedName) : this(
        1,
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(10),
        spawnedNames: spawnedName
    )
    {
    }

    public BaseSpawner(
        int amount,
        TimeSpan minDelay,
        TimeSpan maxDelay,
        int team = 0,
        Rectangle3D spawnBounds = default,
        params ReadOnlySpan<string> spawnedNames
    ) : base(0x1f13)
    {
        _guid = Guid.NewGuid();
        InitSpawn(amount, minDelay, maxDelay, team, spawnBounds);
        for (var i = 0; i < spawnedNames.Length; i++)
        {
            AddEntry(spawnedNames[i], 100, amount, false);
        }
    }

    public BaseSpawner(DynamicJson json, JsonSerializerOptions options) : base(0x1f13)
    {
        if (!json.GetProperty("guid", options, out _guid))
        {
            _guid = Guid.NewGuid();
        }

        if (json.GetProperty("name", options, out string name))
        {
            Name = name;
        }

        json.GetProperty("count", options, out int amount);
        json.GetProperty("minDelay", options, DefaultMinDelay, out TimeSpan minDelay);
        json.GetProperty("maxDelay", options, DefaultMaxDelay, out TimeSpan maxDelay);
        json.GetProperty("team", options, out int team);
        json.GetProperty("homeRange", options, -1, out int homeRange);
        json.GetProperty("walkingRange", options, out _walkingRange);

        // Handle legacy homeRange format (new spawnBounds format handled by derived classes)
        if (homeRange >= 0 && json.GetProperty("location", options, out Point3D location))
        {
            int z;
            int depth;
            if (homeRange == 0)
            {
                z = location.Z;
                depth = 0;
            }
            else
            {
                z = -128;
                depth = 256;
            }

            // Fall back to homeRange with location for oldest format
            // Note: Map not available during JSON loading, so use location.Z directly
            SpawnBounds = new Rectangle3D(
                location.X - homeRange,
                location.Y - homeRange,
                z,
                homeRange * 2 + 1,
                homeRange * 2 + 1,
                depth
            );
        }

        json.GetProperty("spawnLocationIsHome", options, out _spawnLocationIsHome);
        json.GetProperty("spawnPositionMode", options, out _spawnPositionMode);
        json.GetProperty("maxSpawnAttempts", options, DefaultMaxSpawnAttempts, out _maxSpawnAttempts);

        InitSpawn(amount, minDelay, maxDelay, team, SpawnBounds);

        json.GetProperty("entries", options, out List<SpawnerEntry> entries);

        foreach (var entry in entries)
        {
            AddEntry(entry.SpawnedName, entry.SpawnedProbability, entry.SpawnedMaxCount, false, entry.Properties, entry.Parameters);
        }
    }

    public override string DefaultName => "Spawner";
    public bool IsFull => Spawned?.Count >= _count;
    public bool IsEmpty => Spawned?.Count == 0;

    [IgnoreDupe]
    public Dictionary<ISpawnable, SpawnerEntry> Spawned { get; private set; }

    [CommandProperty(AccessLevel.Developer)]
    [SerializableProperty(3, nameof(_walkingRange))]
    public int WalkingRange
    {
        get => _walkingRange > 0 ? _walkingRange : HomeRange;
        set
        {
            _walkingRange = value;
            InvalidateProperties();
        }
    }

    [SerializableProperty(8)]
    [CommandProperty(AccessLevel.Developer)]
    public int Count
    {
        get => _count;
        set
        {
            _count = value;

            if (IsFull)
            {
                _timer?.Stop();
            }
            else if (_timer?.Running != true)
            {
                DoTimer();
            }

            InvalidateProperties();
            this.MarkDirty();
        }
    }

    [SerializableProperty(10)]
    [CommandProperty(AccessLevel.Developer)]
    public bool Running
    {
        get => _running;
        set
        {
            if (value)
            {
                Start();
            }
            else
            {
                Stop();
            }

            InvalidateProperties();
            this.MarkDirty();
        }
    }

    [CommandProperty(AccessLevel.Developer)]
    public TimeSpan NextSpawn
    {
        get => _running && _timer?.Running == true ? End - Core.Now : TimeSpan.Zero;
        set
        {
            if (!_running && Entries.Count > 0)
            {
                _running = true;
                DoTimer(value);
            }
        }
    }

    public virtual Point3D HomeLocation => Location;
    public bool UnlinkOnTaming => true;

    public abstract Region Region { get; }

    /// <summary>
    /// Returns the bounds to use for a single spawn attempt.
    /// Called for each random attempt in Phase 1.
    /// Spawner: returns SpawnBounds
    /// RegionSpawner: picks a weighted random rectangle from the region
    /// </summary>
    protected abstract Rectangle3D GetBoundsForSpawnAttempt();

    /// <summary>
    /// Returns all possible spawn bounds for cache operations.
    /// Used in Phase 3 to search for cached positions.
    /// Spawner: returns single-element array with SpawnBounds
    /// RegionSpawner: returns all region rectangles
    /// </summary>
    protected abstract ReadOnlySpan<Rectangle3D> GetAllSpawnBounds();

    /// <summary>
    /// Whether this spawner supports spiral scanning.
    /// Only makes sense for contiguous bounds (Spawner).
    /// Disjoint rectangles (RegionSpawner) should return false.
    /// </summary>
    protected virtual bool SupportsSpiralScan => false;

    public void Remove(ISpawnable spawn)
    {
        Defrag();

        if (spawn != null && Spawned?.Remove(spawn, out var entry) == true)
        {
            entry?.RemoveFromSpawned(spawn);
        }

        if (_running && !IsFull && _timer?.Running != true)
        {
            DoTimer();
        }
    }

    public virtual void Respawn()
    {
        RemoveSpawns();

        for (var i = 0; i < _count; i++)
        {
            Spawn();
        }

        DoTimer(); // Turn off the timer!
    }

    public virtual void Reset()
    {
        Stop();
        RemoveSpawns();
    }

    public virtual void ToJson(DynamicJson json, JsonSerializerOptions options)
    {
        json.Type = GetType().Name;

        // Always required
        json.SetProperty("guid", options, _guid);
        json.SetProperty("location", options, Location);
        json.SetProperty("map", options, Map);
        json.SetProperty("count", options, Count);
        json.SetProperty("entries", options, Entries);

        // Only write if non-default
        if (!string.IsNullOrEmpty(Name))
        {
            json.SetProperty("name", options, Name);
        }

        if (_minDelay != DefaultMinDelay)
        {
            json.SetProperty("minDelay", options, MinDelay);
        }

        if (_maxDelay != DefaultMaxDelay)
        {
            json.SetProperty("maxDelay", options, MaxDelay);
        }

        if (_team != 0)
        {
            json.SetProperty("team", options, Team);
        }

        if (_walkingRange != 0)
        {
            json.SetProperty("walkingRange", options, WalkingRange);
        }

        if (_spawnLocationIsHome)
        {
            json.SetProperty("spawnLocationIsHome", options, SpawnLocationIsHome);
        }

        if (_spawnPositionMode is not SpawnPositionMode.Automatic and not SpawnPositionMode.Abandoned)
        {
            json.SetProperty("spawnPositionMode", options, _spawnPositionMode);
        }

        if (_maxSpawnAttempts != DefaultMaxSpawnAttempts)
        {
            json.SetProperty("maxSpawnAttempts", options, _maxSpawnAttempts);
        }
    }

    public virtual Point3D GetSpawnPosition(ISpawnable spawned, Map map)
    {
        // Abandoned spawners skip all work
        if (map == null || map == Map.Internal || _spawnPositionMode == SpawnPositionMode.Abandoned)
        {
            return Location;
        }

        // Ensure state is initialized
        _spawnPositionState ??= new SpawnPositionState();

        // Determine mob type for caching
        var isMobile = spawned is Mobile;
        var canSwim = isMobile && ((Mobile)spawned).CanSwim;
        var cantWalk = isMobile && ((Mobile)spawned).CantWalk;
        var isWaterMob = canSwim && cantWalk;
        var hasNonTransientFailure = false;

        // Phase 1: Random attempts (always first - maintains randomness)
        var maxAttempts = _maxSpawnAttempts > 0 ? _maxSpawnAttempts : DefaultMaxSpawnAttempts;
        for (var i = 0; i < maxAttempts; i++)
        {
            var rawBounds = GetBoundsForSpawnAttempt();

            // No bounds = spawn at spawner location
            if (rawBounds == default)
            {
                return Location;
            }

            // Normalize to ensure Start <= End in all dimensions
            var bounds = rawBounds.Normalized;

            var x = Utility.RandomMinMax(bounds.Start.X, bounds.End.X - 1);
            var y = Utility.RandomMinMax(bounds.Start.Y, bounds.End.Y - 1);
            var minZ = bounds.Start.Z;
            var maxZ = Math.Max(minZ, bounds.End.Z - 1);

            bool success;
            int spawnZ;
            SpawnFailureReason failureReason;

            if (isMobile)
            {
                success = map.CanSpawnMobile(x, y, minZ, maxZ, canSwim, cantWalk, out spawnZ, out failureReason);
            }
            else
            {
                success = map.CanSpawnItem(x, y, minZ, maxZ, out spawnZ);
                failureReason = success ? SpawnFailureReason.None : SpawnFailureReason.NonTransientBlocker;
            }

            if (success)
            {
                // Check if blocked by a private house (non-transient)
                if (isMobile && SectorSpawnCacheManager.IsBlockedByHouse(map, x, y, spawnZ))
                {
                    hasNonTransientFailure = true;
                }
                else
                {
                    // Lazy cache: add successful position to global sector cache
                    if (_spawnPositionState.ShouldCachePositions(_spawnPositionMode))
                    {
                        SectorSpawnCacheManager.SetValid(map, new Point3D(x, y, spawnZ), isWaterMob);
                    }

                    return new Point3D(x, y, spawnZ);
                }
            }
            else if ((failureReason & SpawnFailureReason.NonTransientBlocker) != 0)
            {
                hasNonTransientFailure = true;
            }
        }

        // Phase 2: Check if optimization should engage
        if (_spawnPositionMode == SpawnPositionMode.Disabled)
        {
            return Location;
        }

        var useOptimization = _spawnPositionMode == SpawnPositionMode.Enabled
                              || _spawnPositionMode == SpawnPositionMode.Automatic && hasNonTransientFailure;

        if (!useOptimization)
        {
            // Transient failure only - just use spawner location
            return Location;
        }

        _spawnPositionState.RecordNonTransientFailure();

        var allBounds = GetAllSpawnBounds();

        // Phase 3: Continue spiral scan to populate cache (before selecting from it)
        if (!SupportsSpiralScan)
        {
            // Mark spiral complete for non-spiral spawners so ShouldAbandon() can trigger
            _spawnPositionState.SpiralComplete = true;
        }
        else if (!_spawnPositionState.SpiralComplete)
        {
            // Use the first bounds for spiral center/range
            var rawPrimaryBounds = allBounds.Length > 0 ? allBounds[0] : default;
            if (rawPrimaryBounds != default)
            {
                var primaryBounds = rawPrimaryBounds.Normalized;
                var minZ = primaryBounds.Start.Z;
                var maxZ = Math.Max(minZ, primaryBounds.End.Z - 1);

                // Scan more rings initially (3), fewer once cache has positions (1)
                var ringsPerTick = _spawnPositionState.SpiralRing == 0 ? 3 : 1;

                _spawnPositionState.SpiralComplete = SectorSpawnCacheManager.ContinueSpiralScan(
                    map,
                    Location,
                    primaryBounds,
                    minZ,
                    maxZ,
                    canSwim,
                    cantWalk,
                    ref _spawnPositionState.SpiralRing,
                    ref _spawnPositionState.SpiralRingPosition,
                    ringsPerTick
                );
            }
        }

        // Phase 4: Try cached positions from global sector cache (uses deduplicated sectors)
        if (TryGetVerifiedCachedPosition(map, allBounds, isMobile, isWaterMob, canSwim, cantWalk, out var spawnPos))
        {
            // Check if cache returned a useful position (not just spawner's own location)
            if (spawnPos != Location)
            {
                _spawnPositionState.RecordUsefulCacheHit();
                return spawnPos;
            }
        }

        // Cache miss or returned Location only
        _spawnPositionState.RecordUselessResult();

        // Phase 5: Check for abandoned state (only auto-abandon from Automatic mode)
        if (_spawnPositionMode == SpawnPositionMode.Automatic && _spawnPositionState.ShouldAbandon())
        {
            SpawnPositionMode = SpawnPositionMode.Abandoned;
            _spawnPositionState = null;
            logger.Warning(
                "Spawner {Serial} at {Location} ({Map}) marked abandoned - no valid spawn positions found after spiral scan.",
                Serial,
                Location,
                map.Name ?? "null"
            );
        }

        return Location;
    }

    /// <summary>
    /// Attempts to get a verified spawn position from the sector cache across all bounds.
    /// Uses deduplicated sector lookup for uniform distribution.
    /// </summary>
    private static bool TryGetVerifiedCachedPosition(
        Map map,
        ReadOnlySpan<Rectangle3D> allBounds,
        bool isMobile,
        bool isWaterMob,
        bool canSwim,
        bool cantWalk,
        out Point3D spawnPos)
    {
        if (!SectorSpawnCacheManager.TryGetRandomPosition(
                map,
                allBounds,
                isWaterMob,
                out var cachedPos,
                out var containingBounds
            ))
        {
            spawnPos = default;
            return false;
        }

        // Re-verify in 3D using the bounds that contains this position
        var normalizedBounds = containingBounds.Normalized;
        var minZ = normalizedBounds.Start.Z;
        var maxZ = Math.Max(minZ, normalizedBounds.End.Z - 1);

        var verified = isMobile
            ? map.CanSpawnMobile(cachedPos.X, cachedPos.Y, minZ, maxZ, canSwim, cantWalk, out var verifiedZ)
            : map.CanSpawnItem(cachedPos.X, cachedPos.Y, minZ, maxZ, out verifiedZ);

        if (!verified)
        {
            spawnPos = default;
            return false;
        }

        // Skip positions inside private houses
        if (isMobile && SectorSpawnCacheManager.IsBlockedByHouse(map, cachedPos.X, cachedPos.Y, verifiedZ))
        {
            spawnPos = default;
            return false;
        }

        spawnPos = new Point3D(cachedPos.X, cachedPos.Y, verifiedZ);
        return true;
    }

    public override void OnAfterDuped(Item newItem)
    {
        if (newItem is BaseSpawner newSpawner)
        {
            newSpawner._guid = Guid.NewGuid();
            newSpawner.Spawned = new Dictionary<ISpawnable, SpawnerEntry>();
            newSpawner.Entries = [];

            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                newSpawner.AddEntry(
                    entry.SpawnedName,
                    entry.SpawnedProbability,
                    entry.SpawnedMaxCount,
                    false,
                    entry.Properties,
                    entry.Parameters
                );
            }
        }
    }

    public override void OnMapChange()
    {
        base.OnMapChange();

        if (IsHomeRangeStyleAt(Location))
        {
            ShowSpawnerBordersCommand.RemoveProjection(this);
        }
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        base.OnLocationChange(oldLocation);

        // Recalculate HomeRange-style bounds when spawner moves
        if (IsHomeRangeStyleAt(oldLocation))
        {
            HomeRange = SpawnBounds.Width / 2;
            ShowSpawnerBordersCommand.RemoveProjection(this);
        }

        // Reset spawn position optimization state when spawner moves
        ResetSpawnPositionState();
    }

    /// <summary>
    /// Resets the spawn position optimization state.
    /// Called when spawner moves or bounds change.
    /// </summary>
    protected void ResetSpawnPositionState()
    {
        _spawnPositionState?.Reset();

        // If we were abandoned, reset to automatic to give it another chance
        if (_spawnPositionMode == SpawnPositionMode.Abandoned)
        {
            _spawnPositionMode = SpawnPositionMode.Automatic;
        }
    }

    public SpawnerEntry AddEntry(
        string creaturename,
        int probability = 100,
        int amount = 1,
        bool dotimer = true,
        string properties = null,
        string parameters = null
    )
    {
        var entry = new SpawnerEntry(this, creaturename, probability, amount, properties, parameters);
        AddToEntries(entry);
        if (dotimer)
        {
            DoTimer(TimeSpan.FromSeconds(1));
        }

        return entry;
    }

    public void InitSpawn(int amount, TimeSpan minDelay, TimeSpan maxDelay, int team = 0, Rectangle3D spawnBounds = default)
    {
        Visible = false;
        Movable = false;
        _running = true;
        _group = false;
        _minDelay = minDelay;
        _maxDelay = maxDelay;
        _count = amount;
        _team = team;

        if (spawnBounds != default)
        {
            SpawnBounds = spawnBounds;
        }
        else
        {
            HomeRange = 4;
        }

        Entries = [];
        Spawned = new Dictionary<ISpawnable, SpawnerEntry>();

        DoTimer(TimeSpan.FromSeconds(1));
    }

    public override bool CanSeeStaffOnly(Mobile from) => from.AccessLevel >= AccessLevel.Developer;

    public override bool IsAccessibleTo(Mobile from) => from.AccessLevel >= AccessLevel.Developer;

    public override void OnDoubleClick(Mobile from)
    {
        from.SendGump(new SpawnerGump(this));
    }

    public virtual void GetSpawnerProperties(IPropertyList list)
    {
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_running)
        {
            list.Add(1060742); // active

            list.Add(1060656, _count);                                     // amount to make: ~1_val~
            if (SpawnBounds != default)
            {
                list.Add(1061169, SpawnBounds.ToString());                                  // range ~1_val~
            }
            list.Add(1050039, $"{"walking range:"}\t{_walkingRange}");     // ~1_NUMBER~ ~2_ITEMNAME~
            list.Add(1053099, $"{"group:"}\t{_group}");                    // ~1_oretype~: ~2_armortype~
            list.Add(1060847, $"{"team:"}\t{_team}");                      // ~1_val~ ~2_val~
            list.Add(1063483, $"{"delay:"}\t{_minDelay} to {_maxDelay}"); // ~1_MATERIAL~: ~2_ITEMNAME~

            GetSpawnerProperties(list);

            for (var i = 0; i < 6 && i < Entries.Count; ++i)
            {
                var entry = Entries[i];
                list.Add(1060658 + i, $"\t{entry.SpawnedName}\t{CountSpawns(entry)}");
            }
        }
        else
        {
            list.Add(1060743); // inactive
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        if (_running)
        {
            LabelTo(from, "[Running]");
        }
        else
        {
            LabelTo(from, "[Off]");
        }
    }

    public void Start()
    {
        if (!_running && Entries.Count > 0)
        {
            _running = true;
            DoTimer();
        }
    }

    public void Stop()
    {
        _timer?.Stop();
        _running = false;
    }

    public void Defrag()
    {
        Entries ??= [];

        for (var i = 0; i < Entries.Count; ++i)
        {
            Entries[i].Defrag(this);
        }
    }

    public virtual bool OnDefragSpawn(ISpawnable spawned, bool remove)
    {
        if (!remove) // Override could have set it to true already
        {
            remove = spawned.Deleted || spawned.Spawner == null || spawned switch
            {
                Item item => item.RootParent is Mobile || item.IsLockedDown || item.IsSecure,
                Mobile m  => m is BaseCreature c && (c.Controlled || c.IsStabled),
                _         => true
            };
        }

        return remove && Spawned.Remove(spawned);
    }

    public void OnTick()
    {
        if (_group)
        {
            Defrag();

            if (Spawned.Count > 0)
            {
                return;
            }

            Respawn();
        }
        else
        {
            Spawn();
        }

        DoTimer();
    }

    public virtual void Spawn()
    {
        Defrag();

        if (Entries.Count <= 0 || IsFull)
        {
            return;
        }

        var probsum = 0;

        foreach (var spawnerEntry in Entries)
        {
            if (!spawnerEntry.IsFull)
            {
                probsum += spawnerEntry.SpawnedProbability;
            }
        }

        if (probsum <= 0)
        {
            return;
        }

        var rand = Utility.RandomMinMax(1, probsum);

        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            if (entry.IsFull)
            {
                continue;
            }

            if (rand <= entry.SpawnedProbability)
            {
                Spawn(entry, out var flags);
                entry.Valid = flags;
                return;
            }

            rand -= entry.SpawnedProbability;
        }
    }

    private static string[,] FormatProperties(string[] args)
    {
        string[,] props;

        var remains = args.Length;

        if (remains >= 2)
        {
            props = new string[remains / 2, 2];

            remains /= 2;

            for (var j = 0; j < remains; ++j)
            {
                props[j, 0] = args[j * 2];
                props[j, 1] = args[j * 2 + 1];
            }
        }
        else
        {
            props = new string[0, 0];
        }

        return props;
    }

    private static PropertyInfo[] GetTypeProperties(Type type, string[,] props)
    {
        PropertyInfo[] realProps = null;

        if (props != null)
        {
            realProps = new PropertyInfo[props.GetLength(0)];

            var allProps =
                type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

            for (var i = 0; i < realProps.Length; ++i)
            {
                PropertyInfo thisProp = null;

                var propName = props[i, 0];

                for (var j = 0; thisProp == null && j < allProps.Length; ++j)
                {
                    if (propName.InsensitiveEquals(allProps[j].Name))
                    {
                        thisProp = allProps[j];
                    }
                }

                if (thisProp == null)
                {
                    return null;
                }

                var attr = GetCPA(thisProp);

                if (attr == null || attr.WriteLevel > AccessLevel.Developer || !thisProp.CanWrite || attr.ReadOnly)
                {
                    return null;
                }

                realProps[i] = thisProp;
            }
        }

        return realProps;
    }

    public bool Spawn(int index, out EntryFlags flags)
    {
        if (index >= 0 && index < Entries.Count)
        {
            return Spawn(Entries[index], out flags);
        }

        flags = EntryFlags.InvalidEntry;
        return false;
    }

    public bool Spawn(SpawnerEntry entry, out EntryFlags flags)
    {
        var map = GetSpawnMap();
        flags = EntryFlags.None;

        if (map == null || map == Map.Internal || Parent != null)
        {
            return false;
        }

        // Defrag taken care of in Spawn(), beforehand
        // Count check taken care of in Spawn(), beforehand

        var type = AssemblyHandler.FindTypeByName(entry.SpawnedName);

        if (type == null)
        {
            flags = EntryFlags.InvalidType;
            return false;
        }

        try
        {
            IEntity entity = null;

            var propargs = string.IsNullOrEmpty(entry.Properties)
                ? []
                : CommandSystem.Split(entry.Properties.Trim());

            var props = FormatProperties(propargs);

            var realProps = GetTypeProperties(type, props);

            if (realProps == null)
            {
                flags = EntryFlags.InvalidProps;
                return false;
            }

            var paramargs = string.IsNullOrEmpty(entry.Parameters)
                ? []
                : entry.Parameters.Trim().Split(' ');

            if (paramargs.Length == 0)
            {
                entity = type.CreateInstance<IEntity>(
                    ci => IsConstructible(ci, AccessLevel.Developer)
                );
            }
            else
            {
                var ctors = type.GetConstructors();

                for (var i = 0; i < ctors.Length; ++i)
                {
                    var ctor = ctors[i];

                    if (IsConstructible(ctor, AccessLevel.Developer))
                    {
                        var paramList = ctor.GetParameters();

                        if (paramargs.Length == paramList.Length)
                        {
                            var paramValues = Add.ParseValues(paramList, paramargs);

                            if (paramValues != null)
                            {
                                entity = ctor.Invoke(paramValues) as IEntity;
                                break;
                            }
                        }
                    }
                }
            }

            if (entity == null)
            {
                flags = EntryFlags.InvalidType | EntryFlags.InvalidParams;
                return false;
            }

            for (var i = 0; i < realProps.Length; i++)
            {
                if (realProps[i] != null)
                {
                    var result = Types.TryParse(
                        realProps[i].PropertyType,
                        props[i, 1],
                        out var toSet
                    );

                    if (result == null)
                    {
                        realProps[i].SetValue(entity, toSet, null);
                    }
                    else
                    {
                        flags = EntryFlags.InvalidProps;

                        (entity as ISpawnable)?.Delete();

                        return false;
                    }
                }
            }

            if (entity is Mobile m)
            {
                Spawned.Add(m, entry);
                entry.AddToSpawned(m);

                // var spawnLocation = m is BaseVendor ? Location : GetSpawnPosition(m, map);

                var spawnLocation = GetSpawnPosition(m, map);

                m.OnBeforeSpawn(spawnLocation, map);
                m.MoveToWorld(spawnLocation, map);

                if (m is BaseCreature c)
                {
                    c.RangeHome = WalkingRange;
                    c.CurrentWayPoint = WayPoint;

                    if (_team > 0)
                    {
                        c.Team = _team;
                    }

                    // If true, the home location of the mob is the location where it spawned
                    // If false, the home location of the mob is the location of the spawner
                    if (_spawnLocationIsHome)
                    {
                        c.Home = spawnLocation;
                        c.HomeMap = map;
                    }
                    else
                    {
                        c.Home = Location;
                        c.HomeMap = Map;
                    }
                }

                m.Spawner = this;
                m.OnAfterSpawn();
            }
            else if (entity is Item item)
            {
                Spawned.Add(item, entry);
                entry.AddToSpawned(item);

                var loc = GetSpawnPosition(item, map);

                item.OnBeforeSpawn(loc, map);

                item.MoveToWorld(loc, map);

                item.Spawner = this;
                item.OnAfterSpawn();
            }
            else
            {
                // Other IEntity types that might get created are simply not supported
                flags = EntryFlags.InvalidType | EntryFlags.InvalidParams;
                return false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"EXCEPTION CAUGHT: {Serial}");
            Console.WriteLine(e);
            return false;
        }

        ClearProperties();
        return true;
    }

    public virtual Map GetSpawnMap() => Map;

    public void DoTimer()
    {
        if (!_running)
        {
            return;
        }

        var min = (long)_minDelay.TotalMilliseconds;
        var max = (long)_maxDelay.TotalMilliseconds;

        var delay = TimeSpan.FromMilliseconds(Utility.RandomMinMax(min, max));
        DoTimer(delay);
    }

    public virtual void DoTimer(TimeSpan delay)
    {
        if (!_running)
        {
            return;
        }

        if (delay <= TimeSpan.Zero)
        {
            End = Core.Now;
        }
        else
        {
            End = Core.Now + delay;
        }

        if (_timer == null)
        {
            _timer = new InternalTimer(this, delay);
        }
        else
        {
            _timer.Stop();
            _timer.Delay = delay;
        }

        if (!IsFull)
        {
            _timer.Start();
        }
    }

    public int CountSpawns(SpawnerEntry entry)
    {
        Defrag();

        return entry.Spawned.Count;
    }

    public void RemoveEntry(SpawnerEntry entry)
    {
        Defrag();

        for (var i = entry.Spawned.Count - 1; i >= 0; i--)
        {
            var e = entry.Spawned[i];
            entry.Spawned.RemoveAt(i);
            e?.Delete();
        }

        Entries.Remove(entry);

        if (_running && !IsFull && _timer?.Running != true)
        {
            DoTimer();
        }

        InvalidateProperties();
    }

    public void RemoveSpawn(int index) // Entry
    {
        if (index >= 0 && index < Entries.Count)
        {
            RemoveSpawn(Entries[index]);
        }
    }

    public void RemoveSpawn(SpawnerEntry entry)
    {
        for (var i = entry.Spawned.Count - 1; i >= 0; i--)
        {
            var e = entry.Spawned[i];

            if (e != null)
            {
                entry.Spawned.RemoveAt(i);
                Spawned.Remove(e);
                e.Delete();
            }
        }
    }

    public void RemoveSpawns()
    {
        Defrag();

        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];

            for (var j = entry.Spawned.Count - 1; j >= 0; j--)
            {
                var e = entry.Spawned[j];

                if (e != null)
                {
                    Spawned?.Remove(e);
                    entry.Spawned.RemoveAt(j);
                    e.Delete();
                }
            }
        }

        if (_running && !IsFull && _timer?.Running != true)
        {
            DoTimer();
        }

        InvalidateProperties();
    }

    public void BringToHome()
    {
        Defrag();

        foreach (var e in Spawned.Keys)
        {
            e?.MoveToWorld(Location, Map);
        }
    }

    public override void OnDelete()
    {
        base.OnDelete();

        Stop();
        RemoveSpawns();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        // Handle v10 migration - convert HomeRange to SpawnBounds now that Location is available
        if (_pendingHomeRangeMigrations.Remove(this, out var homeRange))
        {
            SpawnBounds = new Rectangle3D(
                Location.X - homeRange,
                Location.Y - homeRange,
                -128,
                homeRange * 2 + 1,
                homeRange * 2 + 1,
                256
            );
        }

        Spawned = new Dictionary<ISpawnable, SpawnerEntry>();

        foreach (var entry in Entries)
        {
            foreach (var spawned in entry.Spawned)
            {
                Spawned.Add(spawned, entry);
            }
        }

        DoTimer(_end - Core.Now);
    }

    private class InternalTimer : Timer
    {
        private readonly BaseSpawner _spawner;

        public InternalTimer(BaseSpawner spawner, TimeSpan delay) : base(delay) => _spawner = spawner;

        protected override void OnTick()
        {
            if (_spawner?.Deleted == false)
            {
                _spawner.OnTick();
            }
        }
    }
}

[Flags]
public enum EntryFlags
{
    None = 0x000,
    InvalidType = 0x001,
    InvalidParams = 0x002,
    InvalidProps = 0x004,
    InvalidEntry = 0x008
}
