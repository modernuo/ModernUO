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

    [SerializedIgnoreDupe]
    [SerializableField(2, setter: "private")]
    private List<SpawnerEntry> _entries;

    [InvalidateProperties]
    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private int _walkingRange = -1;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private WayPoint _wayPoint;

    [InvalidateProperties]
    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private bool _group;

    [InvalidateProperties]
    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private TimeSpan _minDelay;

    [InvalidateProperties]
    [SerializableField(7)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private TimeSpan _maxDelay;

    [InvalidateProperties]
    [SerializableField(9)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private int _team;

    [InvalidateProperties]
    [SerializableField(10)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private Rectangle3D _spawnBounds;

    /// <summary>
    /// If true, the home location of the spawn is the location where it spawned
    /// If false, the home location of the spawn is the location of the spawner
    /// </summary>
    [InvalidateProperties]
    [SerializableField(12)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private bool _spawnLocationIsHome;

    [SerializableField(13)]
    [SerializedCommandProperty(AccessLevel.Developer)]
    private DateTime _end;

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
            if (_spawnBounds == default)
            {
                return 0;
            }

            // Distance from spawner location to nearest edge
            var distToMinX = Math.Abs(Location.X - _spawnBounds.Start.X);
            var distToMaxX = Math.Abs(_spawnBounds.End.X - Location.X);
            var distToMinY = Math.Abs(Location.Y - _spawnBounds.Start.Y);
            var distToMaxY = Math.Abs(_spawnBounds.End.Y - Location.Y);

            // Return smallest distance to any edge
            return Math.Min(Math.Min(distToMinX, distToMaxX), Math.Min(distToMinY, distToMaxY));
        }
        set
        {
            // Create square bounds centered on spawner with full Z range
            _spawnBounds = new Rectangle3D(
                Location.X - value,
                Location.Y - value,
                sbyte.MinValue,
                value * 2 + 1,
                value * 2 + 1,
                256 // Full Z range: -128 to 127
            );
            this.MarkDirty();
            InvalidateProperties();
        }
    }

    public BaseSpawner() : this(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10))
    {
    }

    public BaseSpawner(string spawnedName) : this(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), 0, default, spawnedName)
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
        json.GetProperty("minDelay", options, out TimeSpan minDelay);
        json.GetProperty("maxDelay", options, out TimeSpan maxDelay);
        json.GetProperty("team", options, out int team);
        json.GetProperty("homeRange", options, out int homeRange);
        json.GetProperty("walkingRange", options, out int walkingRange);
        _walkingRange = walkingRange;

        // Try new format first
        if (json.GetProperty("spawnBounds", options, out Rectangle3D spawnBounds))
        {
            _spawnBounds = spawnBounds;
        }
        // Fall back to homeRange with location for oldest format
        else if (homeRange > 0 && json.GetProperty("location", options, out Point3D location))
        {
            _spawnBounds = new Rectangle3D(
                location.X - homeRange,
                location.Y - homeRange,
                sbyte.MinValue,
                homeRange * 2 + 1,
                homeRange * 2 + 1,
                256
            );
        }

        json.GetProperty("spawnLocationIsHome", options, out bool spawnLocationIsHome);
        _spawnLocationIsHome = spawnLocationIsHome;

        InitSpawn(amount, minDelay, maxDelay, team, _spawnBounds);

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

    [SerializableProperty(11)]
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
        json.SetProperty("walkingRange", options, WalkingRange);
        json.SetProperty("entries", options, Entries);
        json.SetProperty("spawnBounds", options, SpawnBounds);
        json.SetProperty("spawnLocationIsHome", options, SpawnLocationIsHome);
    }

    public abstract Point3D GetSpawnPosition(ISpawnable spawned, Map map);

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

    public override void OnLocationChange(Point3D oldLocation)
    {
        base.OnLocationChange(oldLocation);

        // Only shift bounds if they represent a HomeRange-style square
        // (spawner was centered and bounds are square)
        if (_spawnBounds == default)
        {
            return;
        }

        var isSquare = _spawnBounds.Width == _spawnBounds.Height;
        if (!isSquare)
        {
            return;
        }

        // Check if spawner was at center of bounds
        var centerX = _spawnBounds.Start.X + _spawnBounds.Width / 2;
        var centerY = _spawnBounds.Start.Y + _spawnBounds.Height / 2;
        if (centerX != oldLocation.X || centerY != oldLocation.Y)
        {
            return;
        }

        // Shift bounds by the location delta
        var deltaX = Location.X - oldLocation.X;
        var deltaY = Location.Y - oldLocation.Y;

        _spawnBounds = new Rectangle3D(
            _spawnBounds.Start.X + deltaX,
            _spawnBounds.Start.Y + deltaY,
            _spawnBounds.Start.Z,
            _spawnBounds.Width,
            _spawnBounds.Height,
            _spawnBounds.Depth
        );
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
        _spawnBounds = spawnBounds;
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

                var spawnLocation = m is BaseVendor ? Location : GetSpawnPosition(m, map);

                m.OnBeforeSpawn(spawnLocation, map);
                m.MoveToWorld(spawnLocation, map);

                if (m is BaseCreature c)
                {
                    var walkrange = GetWalkingRange();

                    c.RangeHome = walkrange >= 0 ? walkrange : HomeRange;
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
                    Spawned.Remove(e);
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
            _spawnBounds = new Rectangle3D(
                Location.X - homeRange,
                Location.Y - homeRange,
                sbyte.MinValue,
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
