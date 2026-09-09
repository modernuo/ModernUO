using System.Collections.Generic;
using System.Text.Json.Serialization;
using ModernUO.Serialization;
using Server.Json;

namespace Server.Engines.Spawners;

[SerializationGenerator(2, false)]
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

    [Tidy]
    [SerializedJsonIgnore]
    [SerializableField(5)]
    private List<ISpawnable> _spawned;

    private bool ShouldSerializeDisabled() => _disabled;

    /// <summary>
    /// Locked entries are skipped by weighted selection; live spawns are untouched.
    /// Stored inverted so the common (enabled) case writes nothing.
    /// </summary>
    [SaveFlag(nameof(ShouldSerializeDisabled))]
    [SerializableField(6)]
    [SerializedJsonPropertyName("disabled")]
    private bool _disabled;

    [JsonIgnore]
    public bool Enabled
    {
        get => !_disabled;
        set => Disabled = !value;
    }

    /// <summary>
    /// The spawner that owns this entry. Derived entry types declare it as their
    /// <c>[DirtyTrackingEntity]</c>; the generator only inspects the type it is generating.
    /// </summary>
    protected BaseSpawner Parent => _parent;

    internal void SetParent(BaseSpawner parent)
    {
        _parent = parent;
        _spawned ??= [];
    }

    private void MigrateFrom(V1Content content)
    {
        _spawnedName = content.SpawnedName;
        _spawnedProbability = content.SpawnedProbability;
        _spawnedMaxCount = content.SpawnedMaxCount;
        _properties = content.Properties;
        _parameters = content.Parameters;
        _spawned = content.Spawned ?? [];
        _disabled = false;
    }

    public SpawnerEntry(BaseSpawner parent)
    {
        _parent = parent;
        _spawned = [];
    }

    [JsonConstructor]
    public SpawnerEntry(
        string spawnedName,
        int spawnedProbability = 100,
        int spawnedMaxCount = 1,
        string properties = null,
        string parameters = null
    ) : this(null, spawnedName, spawnedProbability, spawnedMaxCount, properties, parameters)
    {
    }

    public SpawnerEntry(
        BaseSpawner parent,
        string name,
        int probability = 100,
        int maxCount = 1,
        string properties = null,
        string parameters = null
    ) : this(parent)
    {
        SpawnedName = name;
        SpawnedProbability = probability;
        SpawnedMaxCount = maxCount;
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
                Spawned.Add(e);
            }
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        // This wasn't tidy in the original code, but it is after generating the code.
        for (var i = Spawned.Count - 1; i >= 0; i--)
        {
            var e = Spawned[i];
            if (e == null)
            {
                Spawned.RemoveAt(i);
            }
            else
            {
                e.Spawner = _parent;
            }
        }

        Spawned.TrimExcess();
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
                _parent.MarkDirty();
            }
        }
    }
}
