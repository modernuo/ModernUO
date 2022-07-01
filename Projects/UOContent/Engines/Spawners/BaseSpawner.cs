using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Server.Commands;
using Server.Items;
using Server.Json;
using Server.Mobiles;
using Server.Utilities;
using CPA = Server.CommandPropertyAttribute;

using static Server.Attributes;

namespace Server.Engines.Spawners;

public abstract class BaseSpawner : Item, ISpawner
{
    private static WarnTimer m_WarnTimer;

    private Guid _guid;
    private int m_Count;
    private bool m_Group;
    private int m_HomeRange;
    private TimeSpan m_MaxDelay;
    private TimeSpan m_MinDelay;
    private bool m_Running;
    private int m_Team;

    private InternalTimer m_Timer;
    private int m_WalkingRange = -1;

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

    public BaseSpawner(
        int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange,
        params string[] spawnedNames
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
        m_WalkingRange = walkingRange;

        InitSpawn(amount, minDelay, maxDelay, team, homeRange);

        json.GetProperty("entries", options, out List<SpawnerEntry> entries);

        foreach (var entry in entries)
        {
            AddEntry(entry.SpawnedName, entry.SpawnedProbability, entry.SpawnedMaxCount, false);
        }
    }

    public BaseSpawner(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "Spawner";
    public bool IsFull => Spawned?.Count >= m_Count;
    public bool IsEmpty => Spawned?.Count == 0;
    public DateTime End { get; set; }

    public List<SpawnerEntry> Entries { get; private set; }

    public Dictionary<ISpawnable, SpawnerEntry> Spawned { get; private set; }

    [CommandProperty(AccessLevel.Developer)]
    public Guid Guid
    {
        get => _guid;
        set => _guid = value;
    }

    [CommandProperty(AccessLevel.Developer)]
    public int Count
    {
        get => m_Count;
        set
        {
            m_Count = value;

            if (IsFull)
            {
                m_Timer?.Stop();
            }
            else if (m_Timer?.Running != true)
            {
                DoTimer();
            }

            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.Developer)]
    public virtual WayPoint WayPoint { get; set; }

    [CommandProperty(AccessLevel.Developer)]
    public bool Running
    {
        get => m_Running;
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
        }
    }

    [CommandProperty(AccessLevel.Developer)]
    public int WalkingRange
    {
        get => m_WalkingRange;
        set
        {
            m_WalkingRange = value;
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.Developer)]
    public int Team
    {
        get => m_Team;
        set
        {
            m_Team = value;
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.Developer)]
    public TimeSpan MinDelay
    {
        get => m_MinDelay;
        set
        {
            m_MinDelay = value;
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.Developer)]
    public TimeSpan MaxDelay
    {
        get => m_MaxDelay;
        set
        {
            m_MaxDelay = value;
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.Developer)]
    public TimeSpan NextSpawn
    {
        get => m_Running && m_Timer?.Running == true ? End - Core.Now : TimeSpan.Zero;
        set
        {
            Start();
            DoTimer(value);
        }
    }

    [CommandProperty(AccessLevel.Developer)]
    public bool Group
    {
        get => m_Group;
        set
        {
            m_Group = value;
            InvalidateProperties();
        }
    }

    [CommandProperty(AccessLevel.Developer)]
    public bool ReturnOnDeactivate { get; set; }

    public virtual Point3D HomeLocation => Location;
    public bool UnlinkOnTaming => true;

    [CommandProperty(AccessLevel.Developer)]
    public int HomeRange
    {
        get => m_HomeRange;
        set
        {
            m_HomeRange = value;
            InvalidateProperties();
        }
    }

    Region ISpawner.Region => Region.Find(Location, Map);

    public void UpdateEntries(List<SpawnerEntry> entries)
    {
        Entries = entries;
    }

    public void Remove(ISpawnable spawn)
    {
        Defrag();

        if (spawn != null)
        {
            Spawned.TryGetValue(spawn, out var entry);

            entry?.Spawned.Remove(spawn);

            Spawned.Remove(spawn);
        }

        if (m_Running && !IsFull && m_Timer?.Running == false)
        {
            DoTimer();
        }
    }

    public virtual void Respawn()
    {
        RemoveSpawns();

        for (var i = 0; i < m_Count; i++)
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
            for (var i = 0; i < Entries.Count; i++)
            {
                newSpawner.AddEntry(
                    Entries[i].SpawnedName,
                    Entries[i].SpawnedProbability,
                    Entries[i].SpawnedMaxCount,
                    false
                );
            }
        }
    }

    public SpawnerEntry AddEntry(string creaturename, int probability = 100, int amount = 1, bool dotimer = true)
    {
        var entry = new SpawnerEntry(creaturename, probability, amount);
        Entries.Add(entry);
        if (dotimer)
        {
            DoTimer(TimeSpan.FromSeconds(1));
        }

        return entry;
    }

    public void InitSpawn(int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange)
    {
        Visible = false;
        Movable = false;
        m_Running = true;
        m_Group = false;
        m_MinDelay = minDelay;
        m_MaxDelay = maxDelay;
        m_Count = amount;
        m_Team = team;
        m_HomeRange = homeRange;
        Entries = new List<SpawnerEntry>();
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

        if (m_Running)
        {
            list.Add(1060742); // active

            list.Add(1060656, m_Count);                                     // amount to make: ~1_val~
            list.Add(1061169, m_HomeRange);                                 // range ~1_val~
            list.Add(1050039, $"{"walking range:"}\t{m_WalkingRange}");     // ~1_NUMBER~ ~2_ITEMNAME~
            list.Add(1053099, $"{"group:"}\t{m_Group}");                    // ~1_oretype~: ~2_armortype~
            list.Add(1060847, $"{"team:"}\t{m_Team}");                      // ~1_val~ ~2_val~
            list.Add(1063483, $"{"delay:"}\t{m_MinDelay} to {m_MaxDelay}"); // ~1_MATERIAL~: ~2_ITEMNAME~

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

        if (m_Running)
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
        if (!m_Running)
        {
            if (Entries.Count > 0)
            {
                m_Running = true;
                DoTimer();
            }
        }
    }

    public void Stop()
    {
        m_Timer?.Stop();
        m_Running = false;
    }

    public void Defrag()
    {
        Entries ??= new List<SpawnerEntry>();

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

        if (remove)
        {
            Spawned.Remove(spawned);
        }

        return remove;
    }

    public void OnTick()
    {
        if (m_Group)
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
                ? Array.Empty<string>()
                : CommandSystem.Split(entry.Properties.Trim());

            var props = FormatProperties(propargs);

            var realProps = GetTypeProperties(type, props);

            if (realProps == null)
            {
                flags = EntryFlags.InvalidProps;
                return false;
            }

            var paramargs = string.IsNullOrEmpty(entry.Parameters)
                ? Array.Empty<string>()
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
                entry.Spawned.Add(m);

                var loc = m is BaseVendor ? Location : GetSpawnPosition(m, map);

                m.OnBeforeSpawn(loc, map);
                m.MoveToWorld(loc, map);

                if (m is BaseCreature c)
                {
                    var walkrange = GetWalkingRange();

                    c.RangeHome = walkrange >= 0 ? walkrange : m_HomeRange;
                    c.CurrentWayPoint = WayPoint;

                    if (m_Team > 0)
                    {
                        c.Team = m_Team;
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
                entry.Spawned.Add(item);

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

    public virtual int GetWalkingRange() => m_WalkingRange;

    public virtual Map GetSpawnMap() => Map;

    public void DoTimer()
    {
        if (!m_Running)
        {
            return;
        }

        var minSeconds = (int)m_MinDelay.TotalSeconds;
        var maxSeconds = (int)m_MaxDelay.TotalSeconds;

        var delay = TimeSpan.FromSeconds(Utility.RandomMinMax(minSeconds, maxSeconds));
        DoTimer(delay);
    }

    public virtual void DoTimer(TimeSpan delay)
    {
        if (!m_Running)
        {
            return;
        }

        End = Core.Now + delay;

        if (m_Timer == null)
        {
            m_Timer = new InternalTimer(this, delay);
        }
        else
        {
            m_Timer.Stop();
            m_Timer.Delay = delay;
        }

        if (!IsFull)
        {
            m_Timer.Start();
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

        if (m_Running && !IsFull && m_Timer?.Running == false)
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

        if (m_Running && !IsFull && m_Timer?.Running == false)
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

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);

        writer.Write(9); // version

        writer.Write(_guid);

        writer.Write(ReturnOnDeactivate);

        writer.Write(Entries.Count);

        for (var i = 0; i < Entries.Count; ++i)
        {
            Entries[i].Serialize(writer);
        }

        writer.Write(m_WalkingRange);

        writer.Write(WayPoint);

        writer.Write(m_Group);

        writer.Write(m_MinDelay);
        writer.Write(m_MaxDelay);
        writer.Write(m_Count);
        writer.Write(m_Team);
        writer.Write(m_HomeRange);
        writer.Write(m_Running);

        if (m_Running)
        {
            writer.WriteDeltaTime(End);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        var version = reader.ReadInt();

        Spawned = new Dictionary<ISpawnable, SpawnerEntry>();

        if (version < 7)
        {
            Entries = new List<SpawnerEntry>();
        }

        switch (version)
        {
            case 9:
                {
                    _guid = reader.ReadGuid();
                    goto case 8;
                }
            case 8:
                {
                    ReturnOnDeactivate = reader.ReadBool();
                    goto case 7;
                }
            case 7:
                {
                    var size = reader.ReadInt();

                    Entries = new List<SpawnerEntry>(size);

                    for (var i = 0; i < size; ++i)
                    {
                        Entries.Add(new SpawnerEntry(this, reader));
                    }

                    goto case 4; // Skip the other crap
                }
            case 6:
                {
                    var size = reader.ReadInt();

                    var addentries = Entries.Count == 0;

                    for (var i = 0; i < size; ++i)
                    {
                        if (addentries)
                        {
                            Entries.Add(new SpawnerEntry(string.Empty, 100, reader.ReadInt()));
                        }
                        else
                        {
                            Entries[i].SpawnedMaxCount = reader.ReadInt();
                        }
                    }

                    goto case 5;
                }
            case 5:
                {
                    var size = reader.ReadInt();

                    var addentries = Entries.Count == 0;

                    for (var i = 0; i < size; ++i)
                    {
                        if (addentries)
                        {
                            Entries.Add(new SpawnerEntry(string.Empty, reader.ReadInt(), 1));
                        }
                        else
                        {
                            Entries[i].SpawnedProbability = reader.ReadInt();
                        }
                    }

                    goto case 4;
                }
            case 4:
                {
                    m_WalkingRange = reader.ReadInt();

                    goto case 3;
                }
            case 3:
            case 2:
                {
                    WayPoint = reader.ReadEntity<WayPoint>();

                    goto case 1;
                }

            case 1:
                {
                    m_Group = reader.ReadBool();

                    goto case 0;
                }

            case 0:
                {
                    m_MinDelay = reader.ReadTimeSpan();
                    m_MaxDelay = reader.ReadTimeSpan();
                    m_Count = reader.ReadInt();
                    m_Team = reader.ReadInt();
                    m_HomeRange = reader.ReadInt();
                    m_Running = reader.ReadBool();

                    var ts = m_Running ? reader.ReadDeltaTime() - Core.Now : TimeSpan.Zero;

                    if (version < 7)
                    {
                        var size = reader.ReadInt();

                        var addentries = Entries.Count == 0;

                        for (var i = 0; i < size; ++i)
                        {
                            var typeName = reader.ReadString();

                            if (addentries)
                            {
                                Entries.Add(new SpawnerEntry(typeName, 100, 1));
                            }
                            else
                            {
                                Entries[i].SpawnedName = typeName;
                            }

                            if (AssemblyHandler.FindTypeByName(typeName) == null)
                            {
                                m_WarnTimer ??= new WarnTimer();

                                m_WarnTimer.Add(Location, Map, typeName);
                            }
                        }

                        var count = reader.ReadInt();

                        for (var i = 0; i < count; ++i)
                        {
                            var e = reader.ReadEntity<ISpawnable>();

                            if (e == null)
                            {
                                continue;
                            }

                            if (e is BaseCreature creature)
                            {
                                creature.RemoveIfUntamed = true;
                            }

                            e.Spawner = this;

                            for (var j = 0; j < Entries.Count; j++)
                            {
                                if (AssemblyHandler.FindTypeByName(Entries[j].SpawnedName) == e.GetType())
                                {
                                    Entries[j].Spawned.Add(e);
                                    Spawned.Add(e, Entries[j]);
                                    break;
                                }
                            }
                        }
                    }

                    DoTimer(ts);

                    break;
                }
        }

        if (version < 4)
        {
            m_WalkingRange = m_HomeRange;
        }
    }

    private class InternalTimer : Timer
    {
        private readonly BaseSpawner m_Spawner;

        public InternalTimer(BaseSpawner spawner, TimeSpan delay) : base(delay) => m_Spawner = spawner;

        protected override void OnTick()
        {
            if (m_Spawner?.Deleted == false)
            {
                m_Spawner.OnTick();
            }
        }
    }

    private class WarnTimer : Timer
    {
        private readonly List<WarnEntry> m_List;

        public WarnTimer() : base(TimeSpan.FromSeconds(1.0))
        {
            m_List = new List<WarnEntry>();
            Start();
        }

        public void Add(Point3D p, Map map, string name)
        {
            m_List.Add(new WarnEntry(p, map, name));
        }

        protected override void OnTick()
        {
            try
            {
                Console.WriteLine("Warning: {0} bad spawns detected, logged: 'badspawn.log'", m_List.Count);

                using var op = new StreamWriter("badspawn.log", true);
                op.WriteLine("# Bad spawns : {0}", DateTime.Now);
                op.WriteLine("# Format: X Y Z F Name");
                op.WriteLine();

                foreach (var e in m_List)
                {
                    op.WriteLine(
                        "{0}\t{1}\t{2}\t{3}\t{4}",
                        e.m_Point.X,
                        e.m_Point.Y,
                        e.m_Point.Z,
                        e.m_Map,
                        e.m_Name
                    );
                }

                op.WriteLine();
                op.WriteLine();
            }
            catch
            {
                // ignored
            }
        }

        private class WarnEntry
        {
            public readonly Map m_Map;
            public readonly string m_Name;
            public Point3D m_Point;

            public WarnEntry(Point3D p, Map map, string name)
            {
                m_Point = p;
                m_Map = map;
                m_Name = name;
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
