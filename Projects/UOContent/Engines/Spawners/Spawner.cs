using System;
using System.Collections.Generic;
using System.Text.Json;
using ModernUO.Serialization;
using Server.Json;

namespace Server.Engines.Spawners;

[SerializationGenerator(1)]
public partial class Spawner : BaseSpawner
{
    // Cached single-element list for GetAllSpawnBounds
    private Rectangle3D[] _boundsCache;

    /// <summary>
    /// When true, enables proactive spiral scanning to find valid spawn positions.
    /// Only relevant when SpawnPositionMode is Automatic or Enabled.
    /// </summary>
    [SerializableFieldSaveFlag(0)]
    private bool ShouldSerializeUseSpiralScan() => _useSpiralScan;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private bool _useSpiralScan;

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

    protected override IReadOnlyList<Rectangle3D> GetAllSpawnBounds()
    {
        var bounds = SpawnBounds;
        if (bounds == default)
        {
            return Array.Empty<Rectangle3D>();
        }

        // Cache the array to avoid allocation per spawn
        if (_boundsCache == null || _boundsCache[0] != bounds)
        {
            _boundsCache = [bounds];
        }

        return _boundsCache;
    }
}
