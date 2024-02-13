using System.Collections.Generic;
using System.Text.Json.Serialization;
using ModernUO.Serialization;
using Server.Json;

namespace Server.Engines.Spawners;

[SerializationGenerator(1, false)]
public partial class SpawnerEntry
{
    [DirtyTrackingEntity]
    private BaseSpawner _parent;

    [SerializableField(0)]
    [SerializedJsonPropertyName("name")]
    private string _spawnedName;

    [SerializableField(1)]
    [SerializedJsonPropertyName("probability")]
    private int _spawnedProbability;

    [SerializableField(2)]
    [SerializedJsonPropertyName("maxCount")]
    private int _spawnedMaxCount;

    [SerializableField(3)]
    [SerializedJsonPropertyName("properties")]
    private string _properties;

    [SerializableField(4)]
    [SerializedJsonPropertyName("parameters")]
    private string _parameters;

    [SerializedJsonIgnore]
    [SerializableField(5)]
    private List<ISpawnable> _spawned;

    public SpawnerEntry(BaseSpawner parent)
    {
        _parent = parent;
        _spawned = new List<ISpawnable>();
    }

    public SpawnerEntry(
        BaseSpawner parent,
        string name,
        int probability,
        int maxcount,
        string properties = null,
        string parameters = null
    ) : this(parent)
    {
        SpawnedName = name;
        SpawnedProbability = probability;
        SpawnedMaxCount = maxcount;
        Properties = properties;
        Parameters = parameters;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
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
                e.Spawner = _parent;

                Spawned.Add(e);
                _parent.Spawned.TryAdd(e, this);
            }
        }
    }

    [JsonIgnore]
    public EntryFlags Valid { get; set; }

    [JsonIgnore]
    public bool IsFull => Spawned.Count >= SpawnedMaxCount;

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
