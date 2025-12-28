using System;
using System.Text.Json;
using ModernUO.Serialization;
using Server.Json;

namespace Server.Engines.Spawners;

[SerializationGenerator(1)]
public partial class Spawner : BaseSpawner
{
    /// <summary>
    /// When true, enables proactive spiral scanning to find valid spawn positions.
    /// Only relevant when SpawnPositionMode is Automatic or Enabled.
    /// </summary>
    [SerializableFieldSaveFlag(0)]
    private bool ShouldSerializeUseSpiralScan() => _useSpiralScan;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private bool _useSpiralScan;

    [SerializableFieldSaveFlag(1)]
    private bool ShouldSerializeSpawnBounds() => _spawnBounds != default;

    [SerializableProperty(1)]
    public override Rectangle3D SpawnBounds
    {
        get => _spawnBounds;
        set
        {
            _spawnBounds = value;
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    [Constructible(AccessLevel.Developer)]
    public Spawner()
    {
    }

    [Constructible(AccessLevel.Developer)]
    public Spawner(string spawnedName) : base(spawnedName)
    {
    }

    [Constructible(AccessLevel.Developer)]
    public Spawner(
        int amount,
        TimeSpan minDelay,
        TimeSpan maxDelay,
        int team = 0,
        Rectangle3D spawnBounds = default,
        params ReadOnlySpan<string> spawnedNames
    ) : base(amount, minDelay, maxDelay, team, spawnBounds, spawnedNames)
    {
    }

    public Spawner(DynamicJson json, JsonSerializerOptions options) : base(json, options)
    {
        // Read spawnBounds (not in BaseSpawner to allow RegionSpawner to skip it)
        if (json.GetProperty("spawnBounds", options, out Rectangle3D spawnBounds))
        {
            SpawnBounds = spawnBounds;
        }
    }

    public override void ToJson(DynamicJson json, JsonSerializerOptions options)
    {
        base.ToJson(json, options);

        if (SpawnBounds != default)
        {
            json.SetProperty("spawnBounds", options, SpawnBounds);
        }
    }

    public override Region Region => Region.Find(Location, Map);

    protected override bool SupportsSpiralScan => _useSpiralScan;

    protected override Rectangle3D GetBoundsForSpawnAttempt() => SpawnBounds;

    protected override ReadOnlySpan<Rectangle3D> GetAllSpawnBounds() => new(ref _spawnBounds);

    private void MigrateFrom(V0Content content)
    {
        // V0 had no fields in Spawner, new v1 field _useSpiralScan defaults to false
    }
}
