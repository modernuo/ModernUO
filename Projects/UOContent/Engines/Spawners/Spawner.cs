using System;
using System.Text.Json;
using ModernUO.Serialization;
using Server.Json;

namespace Server.Engines.Spawners;

[SerializationGenerator(0)]
public partial class Spawner : BaseSpawner
{
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

    public override Point3D GetSpawnPosition(ISpawnable spawned, Map map)
    {
        if (map == null || map == Map.Internal)
        {
            return Location;
        }

        bool canSwim, cantWalk;

        if (spawned is Mobile mob)
        {
            canSwim = mob.CanSwim;
            cantWalk = mob.CantWalk;
        }
        else
        {
            canSwim = false;
            cantWalk = false;
        }

        var bounds = SpawnBounds;
        var hasBounds = bounds != default;

        // Z range from SpawnBounds (supports multi-story buildings)
        var minZ = hasBounds ? bounds.Start.Z : sbyte.MinValue;
        var maxZ = hasBounds ? bounds.End.Z - 1 : sbyte.MaxValue;

        // Try 10 times to find a valid location.
        for (var i = 0; i < 10; i++)
        {
            int x, y;

            if (hasBounds)
            {
                // Use SpawnBounds for X/Y selection
                x = Utility.RandomMinMax(bounds.Start.X, bounds.End.X - 1);
                y = Utility.RandomMinMax(bounds.Start.Y, bounds.End.Y - 1);
            }
            else
            {
                // No bounds set - spawn at spawner location
                x = Location.X;
                y = Location.Y;
            }

            if (map.CanSpawnMobile(x, y, minZ, maxZ, canSwim, cantWalk, out var spawnZ))
            {
                return new Point3D(x, y, spawnZ);
            }
        }

        return Location;
    }
}
