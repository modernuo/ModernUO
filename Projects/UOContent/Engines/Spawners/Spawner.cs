using System;
using System.Text.Json;
using Server.Json;

namespace Server.Engines.Spawners
{
    public class Spawner : BaseSpawner
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
            int amount, int minDelay, int maxDelay, int team, int homeRange,
            params string[] spawnedNames
        ) : this(
            amount,
            TimeSpan.FromMinutes(minDelay),
            TimeSpan.FromMinutes(maxDelay),
            team,
            homeRange,
            spawnedNames
        )
        {
        }

        [Constructible(AccessLevel.Developer)]
        public Spawner(
            int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange,
            params string[] spawnedNames
        ) : base(amount, minDelay, maxDelay, team, homeRange, spawnedNames)
        {
        }

        public Spawner(DynamicJson json, JsonSerializerOptions options) : base(json, options)
        {
        }

        public Spawner(Serial serial) : base(serial)
        {
        }

        public static bool IsValidWater(Map map, int x, int y, int z)
        {
            if (!Region.Find(new Point3D(x, y, z), map).AllowSpawn() || !map.CanFit(x, y, z, 16, false, true, false))
            {
                return false;
            }

            var landTile = map.Tiles.GetLandTile(x, y);

            if (landTile.Z == z && (TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags & TileFlag.Wet) != 0)
            {
                return true;
            }

            foreach (var staticTile in map.Tiles.GetStaticAndMultiTiles(x, y))
            {
                if (staticTile.Z == z && TileData.ItemTable[staticTile.ID & TileData.MaxItemValue].Wet)
                {
                    return true;
                }
            }

            return false;
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

        public override Point3D GetSpawnPosition(ISpawnable spawned, Map map)
        {
            if (map == null || map == Map.Internal)
            {
                return Location;
            }

            bool waterMob, waterOnlyMob;

            if (spawned is Mobile mob)
            {
                waterMob = mob.CanSwim;
                waterOnlyMob = mob.CanSwim && mob.CantWalk;
            }
            else
            {
                waterMob = false;
                waterOnlyMob = false;
            }

            // Try 10 times to find a valid location.
            for (var i = 0; i < 10; i++)
            {
                var x = Location.X + (Utility.Random(HomeRange * 2 + 1) - HomeRange);
                var y = Location.Y + (Utility.Random(HomeRange * 2 + 1) - HomeRange);

                var mapZ = map.GetAverageZ(x, y);

                if (waterMob)
                {
                    if (IsValidWater(map, x, y, Z))
                    {
                        return new Point3D(x, y, Z);
                    }

                    if (IsValidWater(map, x, y, mapZ))
                    {
                        return new Point3D(x, y, mapZ);
                    }
                }

                if (!waterOnlyMob)
                {
                    if (map.CanSpawnMobile(x, y, Z))
                    {
                        return new Point3D(x, y, Z);
                    }

                    if (map.CanSpawnMobile(x, y, mapZ))
                    {
                        return new Point3D(x, y, mapZ);
                    }
                }
            }

            return HomeLocation;
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }
    }
}
