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
    }

    /*
    public override bool OnDefragSpawn(ISpawnable spawned, bool remove)
    {
      // To despawn a mob that was lured 4x away from its spawner
      // TODO: Move this to a config
      if (spawned is BaseCreature c && c.Combatant == null && c.GetDistanceToSqrt( Location ) > c.RangeHome * 4)
      {
        c.Delete();
        remove = true;
      }

      return base.OnDefragSpawn(entry, spawned, remove);
    }
    */

    public override Region Region => Region.Find(Location, Map);

    protected override bool SupportsSpiralScan => _useSpiralScan;

    protected override Rectangle3D GetBoundsForSpawnAttempt() => SpawnBounds;

    protected override ReadOnlySpan<Rectangle3D> GetAllSpawnBounds() => new(ref _spawnBounds);
}
