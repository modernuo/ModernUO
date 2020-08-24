using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Server.Commands;
using Server.Items;
using Server.Json;
using Server.Mobiles;
using Server.Utilities;
using CPA = Server.CommandPropertyAttribute;

namespace Server.Engines.Spawners
{
    public abstract class BaseSpawner : Item, ISpawner
    {
        private static WarnTimer m_WarnTimer;
        private int m_Count;
        private bool m_Group;
        private int m_HomeRange;
        private int m_Team;
        private TimeSpan m_MaxDelay;
        private TimeSpan m_MinDelay;
        private bool m_Running;

        private InternalTimer m_Timer;
        private int m_WalkingRange = -1;

        public BaseSpawner() : this(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), 0, 4)
        {
        }

        public BaseSpawner(string spawnedName) : this(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), 0, 4,
            spawnedName)
        {
        }

        public BaseSpawner(int amount, int minDelay, int maxDelay, int team, int homeRange,
            params string[] spawnedNames) : this(amount, TimeSpan.FromMinutes(minDelay), TimeSpan.FromMinutes(maxDelay),
            team, homeRange, spawnedNames)
        {
        }

        public BaseSpawner(int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange,
            params string[] spawnedNames) : base(0x1f13)
        {
            InitSpawn(amount, minDelay, maxDelay, team, homeRange);
            for (int i = 0; i < spawnedNames.Length; i++)
                AddEntry(spawnedNames[i], 100, amount, false);
        }

        public BaseSpawner(DynamicJson json, JsonSerializerOptions options) : base(0x1f13)
        {
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
                AddEntry(entry.SpawnedName, entry.SpawnedProbability, entry.SpawnedMaxCount, false);
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
        public bool ReturnOnDeactivate { get; set; }

        [CommandProperty(AccessLevel.Developer)]
        public int Count
        {
            get => m_Count;
            set
            {
                m_Count = value;

                if (m_Timer != null && (!IsFull && !m_Timer.Running || IsFull && m_Timer.Running))
                    DoTimer();

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
                    Start();
                else
                    Stop();

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
            get => m_Running && m_Timer?.Running == true ? End - DateTime.UtcNow : TimeSpan.FromSeconds(0);
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

        public void Remove(ISpawnable spawn)
        {
            Defrag();

            if (spawn != null)
            {
                Spawned.TryGetValue(spawn, out SpawnerEntry entry);

                entry?.Spawned.Remove(spawn);

                Spawned.Remove(spawn);
            }

            if (m_Running && !IsFull && m_Timer?.Running == false)
                DoTimer();
        }

        public override void OnAfterDuped(Item newItem)
        {
            if (newItem is BaseSpawner newSpawner)
                for (int i = 0; i < Entries.Count; i++)
                    newSpawner.AddEntry(Entries[i].SpawnedName, Entries[i].SpawnedProbability, Entries[i].SpawnedMaxCount,
                        false);
        }

        public SpawnerEntry AddEntry(string creaturename, int probability = 100, int amount = 1, bool dotimer = true)
        {
            SpawnerEntry entry = new SpawnerEntry(creaturename, probability, amount);
            Entries.Add(entry);
            if (dotimer)
                DoTimer(TimeSpan.FromSeconds(1));

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

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.Developer)
                from.SendGump(new SpawnerGump(this));
        }

        public virtual void GetSpawnerProperties(ObjectPropertyList list)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Running)
            {
                list.Add(1060742); // active

                list.Add(1060656, m_Count.ToString()); // amount to make: ~1_val~
                list.Add(1061169, m_HomeRange.ToString()); // range ~1_val~
                list.Add(1050039, "walking range:\t{0}", m_WalkingRange); // ~1_NUMBER~ ~2_ITEMNAME~

                list.Add(1053099, "group:\t{0}", m_Group); // ~1_oretype~: ~2_armortype~
                list.Add(1060847, "team:\t{0}", m_Team); // ~1_val~ ~2_val~
                list.Add(1063483, "delay:\t{0} to {1}", m_MinDelay, m_MaxDelay); // ~1_MATERIAL~: ~2_ITEMNAME~

                GetSpawnerProperties(list);

                for (int i = 0; i < 6 && i < Entries.Count; ++i)
                    list.Add(1060658 + i, "\t{0}\t{1}", Entries[i].SpawnedName, CountSpawns(Entries[i]));
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
                LabelTo(from, "[Running]");
            else
                LabelTo(from, "[Off]");
        }

        public void Start()
        {
            if (!m_Running)
                if (Entries.Count > 0)
                {
                    m_Running = true;
                    DoTimer();
                }
        }

        public void Stop()
        {
            if (m_Running)
            {
                m_Timer?.Stop();
                m_Running = false;
            }
        }

        public void Defrag()
        {
            Entries ??= new List<SpawnerEntry>();

            for (int i = 0; i < Entries.Count; ++i)
                Entries[i].Defrag(this);
        }

        public virtual bool OnDefragSpawn(ISpawnable spawned, bool remove)
        {
            if (!remove) // Override could have set it to true already
                remove = spawned.Deleted || spawned.Spawner == null || spawned switch
                {
                    Item item => item.RootParent is Mobile || item.IsLockedDown || item.IsSecure,
                    Mobile m => m is BaseCreature c && (c.Controlled || c.IsStabled),
                    _ => true
                };

            if (remove)
                Spawned.Remove(spawned);

            return remove;
        }

        public void OnTick()
        {
            if (m_Group)
            {
                Defrag();

                if (Spawned.Count > 0)
                    return;

                Respawn();
            }
            else
            {
                Spawn();
            }

            DoTimer();
        }

        public virtual void Respawn()
        {
            RemoveSpawns();

            for (int i = 0; i < m_Count; i++)
                Spawn();

            DoTimer(); // Turn off the timer!
        }

        public virtual void Spawn()
        {
            Defrag();

            if (Entries.Count <= 0 || IsFull)
                return;

            int probsum = Entries.Where(t => !t.IsFull).Sum(t => t.SpawnedProbability);

            if (probsum <= 0)
                return;

            int rand = Utility.RandomMinMax(1, probsum);

            for (int i = 0; i < Entries.Count; i++)
            {
                SpawnerEntry entry = Entries[i];
                if (entry.IsFull)
                    continue;

                if (rand <= entry.SpawnedProbability)
                {
                    Spawn(entry, out EntryFlags flags);
                    entry.Valid = flags;
                    return;
                }

                rand -= entry.SpawnedProbability;
            }
        }

        private static string[,] FormatProperties(string[] args)
        {
            string[,] props;

            int remains = args.Length;

            if (remains >= 2)
            {
                props = new string[remains / 2, 2];

                remains /= 2;

                for (int j = 0; j < remains; ++j)
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

                PropertyInfo[] allProps =
                    type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

                for (int i = 0; i < realProps.Length; ++i)
                {
                    PropertyInfo thisProp = null;

                    string propName = props[i, 0];

                    for (int j = 0; thisProp == null && j < allProps.Length; ++j)
                        if (Insensitive.Equals(propName, allProps[j].Name))
                            thisProp = allProps[j];

                    if (thisProp == null)
                        return null;
                    CPA attr = Properties.GetCPA(thisProp);

                    if (attr == null || attr.WriteLevel > AccessLevel.Developer || !thisProp.CanWrite || attr.ReadOnly)
                        return null;
                    realProps[i] = thisProp;
                }
            }

            return realProps;
        }

        public bool Spawn(int index, out EntryFlags flags)
        {
            if (index >= 0 && index < Entries.Count)
                return Spawn(Entries[index], out flags);
            flags = EntryFlags.InvalidEntry;
            return false;
        }

        public bool Spawn(SpawnerEntry entry, out EntryFlags flags)
        {
            Map map = GetSpawnMap();
            flags = EntryFlags.None;

            if (map == null || map == Map.Internal || Parent != null)
                return false;

            // Defrag taken care of in Spawn(), beforehand
            // Count check taken care of in Spawn(), beforehand

            Type type = AssemblyHandler.FindFirstTypeForName(entry.SpawnedName);

            if (type != null)
            {
                try
                {
                    object o = null;
                    string[] paramargs;
                    string[] propargs;

                    propargs = string.IsNullOrEmpty(entry.Properties)
                        ? Array.Empty<string>()
                        : CommandSystem.Split(entry.Properties.Trim());

                    string[,] props = FormatProperties(propargs);

                    PropertyInfo[] realProps = GetTypeProperties(type, props);

                    if (realProps == null)
                    {
                        flags = EntryFlags.InvalidProps;
                        return false;
                    }

                    paramargs = string.IsNullOrEmpty(entry.Parameters)
                        ? Array.Empty<string>()
                        : entry.Parameters.Trim().Split(' ');

                    if (paramargs.Length == 0)
                    {
                        o = ActivatorUtil.CreateInstance(type, ci => Add.IsConstructible(ci, AccessLevel.Developer));
                    }
                    else
                    {
                        ConstructorInfo[] ctors = type.GetConstructors();

                        for (int i = 0; i < ctors.Length; ++i)
                        {
                            ConstructorInfo ctor = ctors[i];

                            if (Add.IsConstructible(ctor, AccessLevel.Developer))
                            {
                                ParameterInfo[] paramList = ctor.GetParameters();

                                if (paramargs.Length == paramList.Length)
                                {
                                    object[] paramValues = Add.ParseValues(paramList, paramargs);

                                    if (paramValues != null)
                                    {
                                        o = ctor.Invoke(paramValues);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    for (int i = 0; i < realProps.Length; i++)
                        if (realProps[i] != null)
                        {
                            object toSet = null;
                            string result =
                                Properties.ConstructFromString(realProps[i].PropertyType, o, props[i, 1], ref toSet);

                            if (result == null)
                            {
                                realProps[i].SetValue(o, toSet, null);
                            }
                            else
                            {
                                flags = EntryFlags.InvalidProps;

                                (o as ISpawnable)?.Delete();

                                return false;
                            }
                        }

                    if (o is Mobile m)
                    {
                        Spawned.Add(m, entry);
                        entry.Spawned.Add(m);

                        Point3D loc = m is BaseVendor ? Location : GetSpawnPosition(m, map);

                        m.OnBeforeSpawn(loc, map);
                        InvalidateProperties();

                        m.MoveToWorld(loc, map);

                        if (m is BaseCreature c)
                        {
                            int walkrange = GetWalkingRange();

                            c.RangeHome = walkrange >= 0 ? walkrange : m_HomeRange;
                            c.CurrentWayPoint = WayPoint;

                            if (m_Team > 0)
                                c.Team = m_Team;

                            c.Home = Location;
                            c.HomeMap = Map;
                        }

                        m.Spawner = this;
                        m.OnAfterSpawn();
                    }
                    else if (o is Item item)
                    {
                        Spawned.Add(item, entry);
                        entry.Spawned.Add(item);

                        Point3D loc = GetSpawnPosition(item, map);

                        item.OnBeforeSpawn(loc, map);

                        item.MoveToWorld(loc, map);

                        item.Spawner = this;
                        item.OnAfterSpawn();
                    }
                    else
                    {
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

                InvalidateProperties();
                return true;
            }

            flags = EntryFlags.InvalidType;
            return false;
        }

        public virtual int GetWalkingRange() => m_WalkingRange;

        public abstract Point3D GetSpawnPosition(ISpawnable spawned, Map map);

        public virtual Map GetSpawnMap() => Map;

        public void DoTimer()
        {
            if (!m_Running)
                return;

            int minSeconds = (int)m_MinDelay.TotalSeconds;
            int maxSeconds = (int)m_MaxDelay.TotalSeconds;

            TimeSpan delay = TimeSpan.FromSeconds(Utility.RandomMinMax(minSeconds, maxSeconds));
            DoTimer(delay);
        }

        public virtual void DoTimer(TimeSpan delay)
        {
            if (!m_Running)
                return;

            End = DateTime.UtcNow + delay;

            m_Timer?.Stop();

            m_Timer = new InternalTimer(this, delay);
            if (!IsFull)
                m_Timer.Start();
        }

        public int CountSpawns(SpawnerEntry entry)
        {
            Defrag();

            return entry.Spawned.Count;
        }

        public void RemoveEntry(SpawnerEntry entry)
        {
            Defrag();

            for (int i = entry.Spawned.Count - 1; i >= 0; i--)
            {
                ISpawnable e = entry.Spawned[i];
                entry.Spawned.RemoveAt(i);
                e?.Delete();
            }

            Entries.Remove(entry);

            if (m_Running && !IsFull && m_Timer?.Running == false)
                DoTimer();

            InvalidateProperties();
        }

        public void RemoveSpawn(int index) // Entry
        {
            if (index >= 0 && index < Entries.Count)
                RemoveSpawn(Entries[index]);
        }

        public void RemoveSpawn(SpawnerEntry entry)
        {
            for (int i = entry.Spawned.Count - 1; i >= 0; i--)
            {
                ISpawnable e = entry.Spawned[i];

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

            for (int i = 0; i < Entries.Count; i++)
            {
                SpawnerEntry entry = Entries[i];

                for (int j = entry.Spawned.Count - 1; j >= 0; j--)
                {
                    ISpawnable e = entry.Spawned[j];

                    if (e != null)
                    {
                        Spawned.Remove(e);
                        entry.Spawned.RemoveAt(j);
                        e.Delete();
                    }
                }
            }

            if (m_Running && !IsFull && m_Timer?.Running == false)
                DoTimer();

            InvalidateProperties();
        }

        public void BringToHome()
        {
            Defrag();

            foreach (ISpawnable e in Spawned.Keys) e?.MoveToWorld(Location, Map);
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

            writer.Write(8); // version

            writer.Write(ReturnOnDeactivate);

            writer.Write(Entries.Count);

            for (int i = 0; i < Entries.Count; ++i)
                Entries[i].Serialize(writer);

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
                writer.WriteDeltaTime(End);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            Spawned = new Dictionary<ISpawnable, SpawnerEntry>();

            if (version < 7)
                Entries = new List<SpawnerEntry>();

            switch (version)
            {
                case 8:
                    {
                        ReturnOnDeactivate = reader.ReadBool();
                        goto case 7;
                    }
                case 7:
                    {
                        int size = reader.ReadInt();

                        Entries = new List<SpawnerEntry>(size);

                        for (int i = 0; i < size; ++i)
                            Entries.Add(new SpawnerEntry(this, reader));

                        goto case 4; // Skip the other crap
                    }
                case 6:
                    {
                        int size = reader.ReadInt();

                        bool addentries = Entries.Count == 0;

                        for (int i = 0; i < size; ++i)
                            if (addentries)
                                Entries.Add(new SpawnerEntry(string.Empty, 100, reader.ReadInt()));
                            else
                                Entries[i].SpawnedMaxCount = reader.ReadInt();

                        goto case 5;
                    }
                case 5:
                    {
                        int size = reader.ReadInt();

                        bool addentries = Entries.Count == 0;

                        for (int i = 0; i < size; ++i)
                            if (addentries)
                                Entries.Add(new SpawnerEntry(string.Empty, reader.ReadInt(), 1));
                            else
                                Entries[i].SpawnedProbability = reader.ReadInt();

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
                        WayPoint = reader.ReadItem() as WayPoint;

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

                        TimeSpan ts = TimeSpan.Zero;

                        if (m_Running)
                            ts = reader.ReadDeltaTime() - DateTime.UtcNow;

                        if (version < 7)
                        {
                            int size = reader.ReadInt();

                            bool addentries = Entries.Count == 0;

                            for (int i = 0; i < size; ++i)
                            {
                                string typeName = reader.ReadString();

                                if (addentries)
                                    Entries.Add(new SpawnerEntry(typeName, 100, 1));
                                else
                                    Entries[i].SpawnedName = typeName;

                                if (AssemblyHandler.FindFirstTypeForName(typeName) == null)
                                {
                                    m_WarnTimer ??= new WarnTimer();

                                    m_WarnTimer.Add(Location, Map, typeName);
                                }
                            }

                            int count = reader.ReadInt();

                            for (int i = 0; i < count; ++i)
                                if (reader.ReadEntity() is ISpawnable e)
                                {
                                    if (e is BaseCreature creature)
                                        creature.RemoveIfUntamed = true;

                                    e.Spawner = this;

                                    for (int j = 0; j < Entries.Count; j++)
                                        if (AssemblyHandler.FindFirstTypeForName(Entries[j].SpawnedName) == e.GetType())
                                        {
                                            Entries[j].Spawned.Add(e);
                                            Spawned.Add(e, Entries[j]);
                                            break;
                                        }
                                }
                        }

                        DoTimer(ts);

                        break;
                    }
            }

            if (version < 4)
                m_WalkingRange = m_HomeRange;
        }

        private class InternalTimer : Timer
        {
            private readonly BaseSpawner m_Spawner;

            public InternalTimer(BaseSpawner spawner, TimeSpan delay) : base(delay)
            {
                if (spawner.IsFull)
                    Priority = TimerPriority.FiveSeconds;
                else
                    Priority = TimerPriority.OneSecond;

                m_Spawner = spawner;
            }

            protected override void OnTick()
            {
                if (m_Spawner != null)
                    if (!m_Spawner.Deleted)
                        m_Spawner.OnTick();
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

                    using StreamWriter op = new StreamWriter("badspawn.log", true);
                    op.WriteLine("# Bad spawns : {0}", DateTime.Now);
                    op.WriteLine("# Format: X Y Z F Name");
                    op.WriteLine();

                    foreach (WarnEntry e in m_List)
                        op.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", e.m_Point.X, e.m_Point.Y, e.m_Point.Z, e.m_Map,
                            e.m_Name);

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
}
