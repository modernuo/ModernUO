using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using ModernUO.Serialization;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Json;
using Server.Mobiles;
using static Server.Attributes;

namespace Server.Engines.Spawners;

[SerializationGenerator(11, false)]
public abstract partial class BaseSpawner : Item, ISpawner
{
    [SerializedIgnoreDupe]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private Guid _guid;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private bool _returnOnDeactivate;

    // Note: _entries field removed - now owned by derived classes (Spawner, etc.)
    // See Entries abstract property below

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private int _walkingRange = -1;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private WayPoint _wayPoint;

    [InvalidateProperties]
    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private bool _group;

    [InvalidateProperties]
    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private TimeSpan _minDelay;

    [InvalidateProperties]
    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private TimeSpan _maxDelay;

    [InvalidateProperties]
    [SerializableField(8)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private int _team;

    [InvalidateProperties]
    [SerializableField(9)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private int _homeRange;

    [SerializableField(11)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private DateTime _end;

    private InternalTimer _timer;

    /// <summary>
    /// Gets the spawner entries. Each derived class owns its specific entry type.
    /// </summary>
    public abstract IReadOnlyList<ISpawnerEntry> Entries { get; }

    /// <summary>
    /// Gets the number of entries.
    /// </summary>
    public int EntryCount => Entries?.Count ?? 0;

    /// <summary>
    /// Gets whether the spawner timer is running.
    /// </summary>
    protected bool IsTimerRunning => _timer?.Running == true;

    public BaseSpawner() : this(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), 0, 4)
    {
    }

    public BaseSpawner(string spawnedName) : this(
        1,
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(10),
        0,
        4,
        spawnedName
    )
    {
    }

    public BaseSpawner(
        int amount, int minDelay, int maxDelay, int team, int homeRange,
        params ReadOnlySpan<string> spawnedNames
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

    public BaseSpawner(
        int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange,
        params ReadOnlySpan<string> spawnedNames
    ) : base(0x1f13)
    {
        _guid = Guid.NewGuid();
        InitSpawn(amount, minDelay, maxDelay, team, homeRange);
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
        json.GetProperty("minDelay", options, out TimeSpan minDelay);
        json.GetProperty("maxDelay", options, out TimeSpan maxDelay);
        json.GetProperty("team", options, out int team);
        json.GetProperty("homeRange", options, out int homeRange);
        json.GetProperty("walkingRange", options, out int walkingRange);
        _walkingRange = walkingRange;

        InitSpawn(amount, minDelay, maxDelay, team, homeRange);

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
    public Dictionary<ISpawnable, ISpawnerEntry> Spawned { get; protected set; }

    [SerializableProperty(7)]
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

    Region ISpawner.Region => Region.Find(Location, Map);

    public void Remove(ISpawnable spawn)
    {
        Defrag();

        if (spawn != null)
        {
            Spawned.Remove(spawn, out var entry);
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
        json.SetProperty("name", options, Name);
        json.SetProperty("guid", options, _guid);
        json.SetProperty("location", options, Location);
        json.SetProperty("map", options, Map);
        json.SetProperty("count", options, Count);
        json.SetProperty("minDelay", options, MinDelay);
        json.SetProperty("maxDelay", options, MaxDelay);
        json.SetProperty("team", options, Team);
        json.SetProperty("homeRange", options, HomeRange);
        json.SetProperty("walkingRange", options, WalkingRange);
        json.SetProperty("entries", options, Entries);
    }

    public abstract Point3D GetSpawnPosition(ISpawnable spawned, Map map);

    public override void OnAfterDuped(Item newItem)
    {
        if (newItem is BaseSpawner newSpawner)
        {
            newSpawner._guid = Guid.NewGuid();
            newSpawner.Spawned = new Dictionary<ISpawnable, ISpawnerEntry>();
            newSpawner.InitializeEntries();

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

    /// <summary>
    /// Initializes the entry list for this spawner. Called during construction and duplication.
    /// </summary>
    protected abstract void InitializeEntries();

    /// <summary>
    /// Adds an entry to this spawner.
    /// </summary>
    public abstract ISpawnerEntry AddEntry(
        string creaturename,
        int probability = 100,
        int amount = 1,
        bool dotimer = true,
        string properties = null,
        string parameters = null
    );

    public void InitSpawn(int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange)
    {
        Visible = false;
        Movable = false;
        _running = true;
        _group = false;
        _minDelay = minDelay;
        _maxDelay = maxDelay;
        _count = amount;
        _team = team;
        _homeRange = homeRange;
        InitializeEntries();
        Spawned = new Dictionary<ISpawnable, ISpawnerEntry>();

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
            list.Add(1061169, _homeRange);                                 // range ~1_val~
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
        if (Entries == null)
        {
            return;
        }

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

    public bool Spawn(ISpawnerEntry entry, out EntryFlags flags)
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

                var loc = m is BaseVendor ? Location : GetSpawnPosition(m, map);

                m.OnBeforeSpawn(loc, map);
                m.MoveToWorld(loc, map);

                if (m is BaseCreature c)
                {
                    var walkrange = GetWalkingRange();

                    c.RangeHome = walkrange >= 0 ? walkrange : _homeRange;
                    c.CurrentWayPoint = WayPoint;

                    if (_team > 0)
                    {
                        c.Team = _team;
                    }

                    c.Home = Location;
                    c.HomeMap = Map;
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

    public virtual int GetWalkingRange() => _walkingRange;

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

    public int CountSpawns(ISpawnerEntry entry)
    {
        Defrag();

        return entry.Spawned.Count;
    }

    /// <summary>
    /// Removes an entry and all its spawned entities from this spawner.
    /// </summary>
    public abstract void RemoveEntry(ISpawnerEntry entry);

    /// <summary>
    /// Clears all entries from this spawner.
    /// </summary>
    public abstract void ClearAllEntries();

    /// <summary>
    /// Helper method for derived classes to clean up spawned entities when removing an entry.
    /// </summary>
    protected void CleanupEntrySpawns(ISpawnerEntry entry)
    {
        Defrag();

        for (var i = entry.Spawned.Count - 1; i >= 0; i--)
        {
            var e = entry.Spawned[i];
            entry.RemoveFromSpawned(e);
            e?.Delete();
        }

        if (_running && !IsFull && !IsTimerRunning)
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

    public void RemoveSpawn(ISpawnerEntry entry)
    {
        for (var i = entry.Spawned.Count - 1; i >= 0; i--)
        {
            var e = entry.Spawned[i];

            if (e != null)
            {
                entry.RemoveFromSpawned(e);
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
                    Spawned.Remove(e);
                    entry.RemoveFromSpawned(e);
                    e.Delete();
                }
            }
        }

        if (_running && !IsFull && !IsTimerRunning)
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

    private void Deserialize(IGenericReader reader, int version)
    {
        _guid = reader.ReadGuid();
        _returnOnDeactivate = reader.ReadBool();

        // Version 10 and earlier: entries were serialized in BaseSpawner
        // Version 11+: entries are serialized by derived classes
        if (version <= 10)
        {
            var count = reader.ReadInt();
            var legacyEntries = new List<SpawnerEntry>(count);

            for (var i = 0; i < count; ++i)
            {
                var entry = new SpawnerEntry(this);
                entry.Deserialize(reader);
                legacyEntries.Add(entry);
            }

            // Let derived class handle these entries
            OnLegacyEntriesLoaded(legacyEntries);
        }

        _walkingRange = reader.ReadInt();
        _wayPoint = reader.ReadEntity<WayPoint>();
        _group = reader.ReadBool();
        _minDelay = reader.ReadTimeSpan();
        _maxDelay = reader.ReadTimeSpan();
        _count = reader.ReadInt();
        _team = reader.ReadInt();
        _homeRange = reader.ReadInt();
        _running = reader.ReadBool();

        _end = _running ? reader.ReadDeltaTime() : Core.Now;
    }

    /// <summary>
    /// Called during deserialization of legacy saves (version 10 and earlier) to pass entries to derived classes.
    /// </summary>
    protected virtual void OnLegacyEntriesLoaded(List<SpawnerEntry> legacyEntries)
    {
        // Derived classes should override this to handle legacy entries
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Spawned = new Dictionary<ISpawnable, ISpawnerEntry>();

        if (Entries != null)
        {
            foreach (var entry in Entries)
            {
                foreach (var spawned in entry.Spawned)
                {
                    Spawned.Add(spawned, entry);
                }
            }
        }

        DoTimer(_end - Core.Now);
    }

    private void MigrateFrom(V10Content content)
    {
        // Version 10 -> 11: Entries moved from BaseSpawner to derived classes
        // Migration is handled in Deserialize via OnLegacyEntriesLoaded
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
