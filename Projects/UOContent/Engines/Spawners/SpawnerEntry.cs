using System.Collections.Generic;
using System.Text.Json.Serialization;
using Server.Mobiles;

namespace Server.Engines.Spawners
{
    public class SpawnerEntry
    {
        public SpawnerEntry() => Spawned = new List<ISpawnable>();

        public SpawnerEntry(
            string name,
            int probability,
            int maxcount,
            string properties = null,
            string parameters = null
        ) : this()
        {
            SpawnedName = name;
            SpawnedProbability = probability;
            SpawnedMaxCount = maxcount;
            Properties = properties;
            Parameters = parameters;
        }

        public SpawnerEntry(BaseSpawner parent, IGenericReader reader)
        {
            var version = reader.ReadInt();

            SpawnedName = reader.ReadString();
            SpawnedProbability = reader.ReadInt();
            SpawnedMaxCount = reader.ReadInt();

            Properties = reader.ReadString();
            Parameters = reader.ReadString();

            var count = reader.ReadInt();

            Spawned = new List<ISpawnable>(count);

            for (var i = 0; i < count; ++i)
            {
                var e = reader.ReadEntity<ISpawnable>();

                if (e != null)
                {
                    e.Spawner = parent;

                    if (e is BaseCreature creature)
                    {
                        creature.RemoveIfUntamed = true;
                    }

                    Spawned.Add(e);
                    parent.Spawned.TryAdd(e, this);
                }
            }
        }

        [JsonPropertyName("name")]
        public string SpawnedName { get; set; }

        [JsonPropertyName("parameters")]
        public string Parameters { get; set; }

        [JsonPropertyName("properties")]
        public string Properties { get; set; }

        [JsonPropertyName("maxCount")]
        public int SpawnedMaxCount { get; set; }

        [JsonPropertyName("probability")]
        public int SpawnedProbability { get; set; }

        [JsonIgnore]
        public EntryFlags Valid { get; set; }

        [JsonIgnore]
        public List<ISpawnable> Spawned { get; }

        [JsonIgnore]
        public bool IsFull => Spawned.Count >= SpawnedMaxCount;

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(0); // version

            writer.Write(SpawnedName);
            writer.Write(SpawnedProbability);
            writer.Write(SpawnedMaxCount);

            writer.Write(Properties);
            writer.Write(Parameters);

            writer.Write(Spawned.Count);

            for (var i = 0; i < Spawned.Count; ++i)
            {
                object o = Spawned[i];

                if (o is Item item)
                {
                    writer.Write(item);
                }
                else if (o is Mobile mobile)
                {
                    writer.Write(mobile);
                }
                else
                {
                    writer.Write(Serial.MinusOne);
                }
            }
        }

        public void Defrag(BaseSpawner parent)
        {
            for (var i = 0; i < Spawned.Count; ++i)
            {
                var spawned = Spawned[i];

                if (parent.OnDefragSpawn(spawned, false))
                {
                    Spawned.RemoveAt(i--);
                }
            }
        }
    }
}
