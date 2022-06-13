/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ChampionSpawn.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Net;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Regions;

namespace Server.Engines.CannedEvil
{
    public class ChampionSpawn : Item
    {
        private bool m_Active;
        private ChampionSpawnType m_Type;
        private List<Mobile> m_Creatures;
        private List<Item> m_RedSkulls;
        private List<Item> m_WhiteSkulls;
        private ChampionPlatform m_Platform;
        private ChampionAltar m_Altar;
        private int m_Kills;
        private int m_MaxLevel;
        private int m_Level;

        //private int m_SpawnRange;
        private Rectangle2D m_SpawnArea;
        private ChampionSpawnRegion m_Region;

        //Goes back each level, below level 0 and it goes off!

        private TimerExecutionToken _timerToken;

        private IdolOfTheChampion m_Idol;
        private TimerExecutionToken _restartTimerToken;

        public virtual string BroadcastMessage => "The Champion has sensed your presence!  Beware its wrath!";
        public virtual bool ProximitySpawn => false;
        public virtual bool CanAdvanceByValor => true;
        public virtual bool CanActivateByValor => true;
        public virtual bool AlwaysActive => false;

        public override TimeSpan DecayTime => TimeSpan.FromSeconds(180.0);

        public virtual bool HasStarRoomGate => true;

        public Dictionary<Mobile, int> DamageEntries { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ConfinedRoaming { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasBeenAdvanced { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D EjectLocation { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map EjectMap { get; set; }

        public override int LabelNumber => 1041030; // Evil in a Can:  Don't delete me!

        [Constructible]
        public ChampionSpawn() : base(0xBD2)
        {
            Movable = false;
            Visible = false;

            m_Creatures = new List<Mobile>();
            m_RedSkulls = new List<Item>();
            m_WhiteSkulls = new List<Item>();

            m_Platform = new ChampionPlatform(this);
            m_Altar = new ChampionAltar(this);
            m_Idol = new IdolOfTheChampion(this);

            ExpireDelay = TimeSpan.FromMinutes(30.0);
            RestartDelay = TimeSpan.FromMinutes(30.0);
            DamageEntries = new Dictionary<Mobile, int>();

            Timer.StartTimer(TimeSpan.Zero, SetInitialSpawnArea);
        }

        public void SetInitialSpawnArea()
        {
            //Previous default used to be 24;
            SpawnArea = new Rectangle2D(new Point2D(X - 24, Y - 24), new Point2D(X + 24, Y + 24));
        }

        public virtual ChampionSpawnRegion GetRegion() => new(this);

        public void UpdateRegion()
        {
            m_Region?.Unregister();

            if (!Deleted && Map != Map.Internal)
            {
                m_Region = GetRegion();
                m_Region.Register();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RandomizeType { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Kills
        {
            get => m_Kills;
            set
            {
                m_Kills = value;

                double n = m_Kills / (double)MaxKills;
                int p = (int)(n * 100);

                if (p < 90)
                {
                    SetWhiteSkullCount(p / 20);
                }

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D SpawnArea
        {
            get => m_SpawnArea;
            set
            {
                m_SpawnArea = value;
                InvalidateProperties();
                UpdateRegion();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan RestartDelay { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime RestartTime { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan ExpireDelay { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime ExpireTime { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public ChampionSpawnType Type
        {
            get => m_Type;
            set
            {
                m_Type = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get => m_Active;
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

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ReadyToActivate { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ActivatedByValor { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ActivatedByProximity { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextProximityTime { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Champion { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level
        {
            get => m_Level;
            set
            {
                for (int i = m_RedSkulls.Count - 1; i >= value; --i)
                {
                    m_RedSkulls[i].Delete();
                    m_RedSkulls.RemoveAt(i);
                }

                for (int i = m_RedSkulls.Count; i < Math.Min(value, 16); ++i)
                {
                    Item skull = new Item(0x1854) { Hue = 0x26, Movable = false, Light = LightType.Circle150 };
                    skull.MoveToWorld(GetRedSkullLocation(i), Map);
                    m_RedSkulls.Add(skull);
                }

                m_Level = value;

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int MaxLevel{ get => m_MaxLevel;
            set => m_MaxLevel = Math.Max(Math.Min(value, 18), 0);
        }

        public bool IsChampionSpawn(Mobile m) => m_Creatures.Contains(m);

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int MaxKills
        {
            get
            {
                return Level switch
                {
                    >= 16 => 16,
                    >= 12 => 32,
                    >= 8  => 64,
                    >= 4  => 128,
                    _     => 256
                };
            }
        }

        public void SetWhiteSkullCount(int val)
        {
            for (int i = m_WhiteSkulls.Count - 1; i >= val; --i)
            {
                m_WhiteSkulls[i].Delete();
                m_WhiteSkulls.RemoveAt(i);
            }

            for (int i = m_WhiteSkulls.Count; i < val; ++i)
            {
                Item skull = new Item(0x1854);

                skull.Movable = false;
                skull.Light = LightType.Circle150;

                skull.MoveToWorld(GetWhiteSkullLocation(i), Map);

                m_WhiteSkulls.Add(skull);

                Effects.PlaySound(skull.Location, skull.Map, 0x29);
                Effects.SendLocationEffect(new Point3D(skull.X + 1, skull.Y + 1, skull.Z), skull.Map, 0x3728, 10);
            }
        }

        public void Start()
        {
            if (m_Active || Deleted)
            {
                return;
            }

            if (RandomizeType)
            {
                Type = Utility.Random(5) switch
                {
                    0 => ChampionSpawnType.VerminHorde,
                    1 => ChampionSpawnType.UnholyTerror,
                    2 => ChampionSpawnType.ColdBlood,
                    3 => ChampionSpawnType.Abyss,
                    4 => ChampionSpawnType.Arachnid,
                    _ => Type
                };
            }

            m_Active = true;
            ReadyToActivate = false;
            HasBeenAdvanced = false;
            m_MaxLevel = 16 + Utility.Random(3);

            _timerToken.Cancel();
            Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), OnSlice, out _timerToken);

            _restartTimerToken.Cancel();

            if (m_Altar != null)
            {
                m_Altar.Hue = Champion != null ? 0x26 : 0;
            }

            if (m_Platform != null)
            {
                m_Platform.Hue = 0x452;
            }

            ExpireTime = Core.Now + ExpireDelay;
        }

        public void Stop()
        {
            if (!m_Active || Deleted)
            {
                return;
            }

            m_Active = false;
            ActivatedByValor = false;
            HasBeenAdvanced = false;
            m_MaxLevel = 0;

            _timerToken.Cancel();
            _restartTimerToken.Cancel();

            if (m_Altar != null)
            {
                m_Altar.Hue = 0;
            }

            if (m_Platform != null)
            {
                m_Platform.Hue = 0x497;
            }

            if (AlwaysActive)
            {
                BeginRestart(RestartDelay);
            }
            else if (ActivatedByProximity)
            {
                ActivatedByProximity = false;
                NextProximityTime = Core.Now + TimeSpan.FromHours(6.0);
            }

            Timer.StartTimer(TimeSpan.FromMinutes(10.0), ExpireCreatures);
        }

        public void BeginRestart(TimeSpan ts)
        {
            RestartTime = Core.Now + ts;
            _restartTimerToken.Cancel();
            Timer.StartTimer(ts, EndRestart, out _restartTimerToken);
        }

        public void EndRestart()
        {
            HasBeenAdvanced = false;
            ReadyToActivate = true;
        }

        private ScrollofTranscendence CreateRandomTramSoT()
        {
            int level = Utility.Random(5) + 1;
            return ScrollofTranscendence.CreateRandom(level, level);
        }

        private ScrollofTranscendence CreateRandomFelSoT()
        {
            int level = Utility.Random(5) + 1;
            return ScrollofTranscendence.CreateRandom(level, level);
        }

        private static PowerScroll CreateRandomFelPS() => PowerScroll.CreateRandomNoCraft(5, 5);

        public static void GiveScrollOfTranscendenceFelTo (Mobile killer, ScrollofTranscendence SoTF)
        {
            if (SoTF == null || killer == null) //sanity
            {
                return;
            }

            killer.SendLocalizedMessage(1094936); // You have received a Scroll of Transcendence!

            if (killer.Alive)
            {
                killer.AddToBackpack(SoTF);
            }
            else
            {
                if (killer.Corpse is { Deleted: false })
                {
                    killer.Corpse.DropItem(SoTF);
                }
                else
                {
                    killer.AddToBackpack(SoTF);
                }
            }

            // Justice reward
            var pm = (PlayerMobile)killer;
            for (var j = 0; j < pm.JusticeProtectors.Count; ++j)
            {
                Mobile prot = pm.JusticeProtectors[j];
                if (prot.Map != killer.Map || prot.Kills >= 5 || prot.Criminal || !JusticeVirtue.CheckMapRegion(killer, prot))
                {
                    continue;
                }

                var chance = VirtueHelper.GetLevel(prot, VirtueName.Justice) switch
                {
                    VirtueLevel.Seeker   => 60,
                    VirtueLevel.Follower => 80,
                    VirtueLevel.Knight   => 100,
                    _                    => 0
                };

                if (chance > Utility.Random(100))
                {
                    prot.SendLocalizedMessage(1049368); // You have been rewarded for your dedication to Justice!
                    ScrollofTranscendence SoTFduplicate = new ScrollofTranscendence (SoTF.Skill, SoTF.Value);
                    prot.AddToBackpack(SoTFduplicate);
                }
            }
        }

        public static void GivePowerScrollFelTo (Mobile killer, PowerScroll PS)
        {
            if (PS == null || killer == null) //sanity
            {
                return;
            }

            killer.SendLocalizedMessage(1049524); // You have received a scroll of power!

            if (killer.Alive)
            {
                killer.AddToBackpack(PS);
            }
            else if (killer.Corpse is { Deleted: false })
            {
                killer.Corpse.DropItem(PS);
            }
            else
            {
                killer.AddToBackpack(PS);
            }

            // Justice reward
            var pm = (PlayerMobile)killer;
            for (var j = 0; j < pm.JusticeProtectors.Count; ++j)
            {
                Mobile prot = pm.JusticeProtectors[j];
                if (prot.Map != killer.Map || prot.Kills >= 5 || prot.Criminal || !JusticeVirtue.CheckMapRegion(killer, prot))
                {
                    continue;
                }

                var chance = VirtueHelper.GetLevel(prot, VirtueName.Justice) switch
                {
                    VirtueLevel.Seeker   => 60,
                    VirtueLevel.Follower => 80,
                    VirtueLevel.Knight   => 100,
                    _                    => 0
                };

                if (chance > Utility.Random(100))
                {
                    prot.SendLocalizedMessage(1049368); // You have been rewarded for your dedication to Justice!
                    //PowerScroll PSduplicate = new PowerScroll (PS.Skill, PS.Value);
                    prot.AddToBackpack(CreateRandomFelPS());
                }
            }
        }

        public void OnSlice()
        {
            if (!m_Active || Deleted)
            {
                return;
            }

            if (Champion != null)
            {
                if (Champion.Deleted)
                {
                    RegisterDamageTo(Champion);

                    //if (m_Champion is BaseChampion)
                    //	AwardArtifact(((BaseChampion)m_Champion).GetArtifact());

                    DamageEntries.Clear();

                    if (m_Platform != null)
                    {
                        m_Platform.Hue = 0x497;
                    }

                    if (m_Altar != null)
                    {
                        m_Altar.Hue = 0;

                        if (HasStarRoomGate && (!Core.ML || Map == Map.Felucca))
                        {
                            new StarRoomGate(m_Altar.Location, m_Altar.Map, true);
                        }
                    }

                    Champion = null;
                    Stop();
                }
            }
            else
            {
                int kills = m_Kills;

                for (var i = 0; i < m_Creatures.Count; ++i)
                {
                    Mobile m = m_Creatures[i];

                    if (m.Deleted)
                    {
                        if (m.Corpse is { Deleted: false })
                        {
                            ((Corpse)m.Corpse).BeginDecay(TimeSpan.FromMinutes(1));
                        }

                        m_Creatures.RemoveAt(i);
                        --i;
                        ++m_Kills;

                        Mobile killer = m.FindMostRecentDamager(false);

                        RegisterDamageTo(m);

                        if (killer is BaseCreature creature)
                        {
                            killer = creature.GetMaster();
                        }

                        if (killer is PlayerMobile pm)
                        {
                            if (Core.ML)
                            {
                                if (Map == Map.Felucca)
                                {
                                    if (Utility.RandomDouble() < 0.001)
                                    {
                                        double random = Utility.Random (49);

                                        if (random <= 24)
                                        {
                                            ScrollofTranscendence SoTF = CreateRandomFelSoT();
                                            GiveScrollOfTranscendenceFelTo (pm, SoTF);
                                        }
                                        else
                                        {
                                            PowerScroll PS = CreateRandomFelPS();
                                            GivePowerScrollFelTo (pm, PS);
                                        }
                                    }
                                }

                                if (Map == Map.Ilshenar || Map == Map.Tokuno)
                                {
                                    if (Utility.RandomDouble() < 0.0015)
                                    {
                                        pm.SendLocalizedMessage(1094936); // You have received a Scroll of Transcendence!
                                        ScrollofTranscendence SoTT = CreateRandomTramSoT();
                                        pm.AddToBackpack(SoTT);
                                    }
                                }
                            }

                            int mobSubLevel = GetSubLevelfor (m) + 1;

                            if (mobSubLevel >= 0)
                            {
                                bool gainedPath = false;

                                int pointsToGain = mobSubLevel * 40;

                                if (VirtueHelper.Award(pm, VirtueName.Valor, pointsToGain, ref gainedPath))
                                {
                                    if (gainedPath)
                                    {
                                        m.SendLocalizedMessage(1054032); // You have gained a path in Valor!
                                    }
                                    else
                                    {
                                        m.SendLocalizedMessage(1054030); // You have gained in Valor!
                                    }

                                    //No delay on Valor gains
                                }

                                ChampionTitleInfo info = pm.ChampionTitles;

                                info.Award(m_Type, mobSubLevel);
                            }
                        }
                    }
                }

                // Only really needed once.
                if (m_Kills > kills)
                {
                    InvalidateProperties();
                }

                double n = m_Kills / (double)MaxKills;
                int p = (int)(n * 100);

                if (p >= 99)
                {
                    AdvanceLevel();
                }
                else if (p > 0)
                {
                    SetWhiteSkullCount(p / 20);
                }

                if (Core.Now >= ExpireTime)
                {
                    Expire();
                }

                Respawn();
            }
        }

        public void AdvanceLevel()
        {
            ExpireTime = Core.Now + ExpireDelay;

            if (Level < m_MaxLevel)
            {
                m_Kills = 0;
                ++Level;
                InvalidateProperties();
                SetWhiteSkullCount(0);

                if (m_Altar != null)
                {
                    Effects.PlaySound(m_Altar.Location, m_Altar.Map, 0x29);
                    Effects.SendLocationEffect(new Point3D(m_Altar.X + 1, m_Altar.Y + 1, m_Altar.Z), m_Altar.Map, 0x3728, 10);
                }
            }
            else
            {
                SpawnChampion();
            }
        }

        public void SpawnChampion()
        {
            if (m_Altar != null)
            {
                m_Altar.Hue = 0x26;
            }

            if (m_Platform != null)
            {
                m_Platform.Hue = 0x452;
            }

            m_Kills = 0;
            Level = 0;
            InvalidateProperties();
            SetWhiteSkullCount(0);

            try
            {
                Champion = Activator.CreateInstance(ChampionSpawnInfo.GetInfo(m_Type).Champion) as Mobile;
            }
            catch (Exception e)
            { Console.WriteLine($"Exception creating champion {m_Type}: {e}"); }

            if (Champion != null)
            {
                Champion.MoveToWorld(new Point3D(X, Y, Z - 15), Map);

                if (Champion is BaseCreature bc)
                {
                    if (ConfinedRoaming)
                    {
                        bc.Home = Location;
                        bc.HomeMap = Map;
                        bc.RangeHome = Math.Min(m_SpawnArea.Width / 2, m_SpawnArea.Height / 2);
                    }
                    else
                    {
                        bc.Home = bc.Location;
                        bc.HomeMap = bc.Map;

                        Point2D xWall1 = new Point2D(m_SpawnArea.X, bc.Y);
                        Point2D xWall2 = new Point2D(m_SpawnArea.X + m_SpawnArea.Width, bc.Y);
                        Point2D yWall1 = new Point2D(bc.X, m_SpawnArea.Y);
                        Point2D yWall2 = new Point2D(bc.X, m_SpawnArea.Y + m_SpawnArea.Height);

                        double minXDist = Math.Min(bc.GetDistanceToSqrt(xWall1), bc.GetDistanceToSqrt(xWall2));
                        double minYDist = Math.Min(bc.GetDistanceToSqrt(yWall1), bc.GetDistanceToSqrt(yWall2));

                        bc.RangeHome = (int)Math.Min(minXDist, minYDist);
                    }
                }
                else
                {
                    throw new Exception("Champion Spawn is not inherited from BaseCreature");
                }
            }
        }

        public virtual int MaxSpawn => 250 - GetSubLevel() * 40;

        public void Respawn()
        {
            if (!m_Active || Deleted || Champion != null)
            {
                return;
            }

            while (m_Creatures.Count < MaxSpawn)
            {
                Mobile m = Spawn();

                if (m == null)
                {
                    return;
                }

                Point3D loc = GetSpawnLocation();

                // Allow creatures to turn into Paragons at Ilshenar champions.
                m.OnBeforeSpawn(loc, Map);

                m_Creatures.Add(m);
                m.MoveToWorld(loc, Map);

                if (m is BaseCreature bc)
                {
                    bc.Tamable = false;

                    if (ConfinedRoaming)
                    {
                        bc.Home = Location;
                        bc.HomeMap = Map;
                        bc.RangeHome = Math.Min(m_SpawnArea.Width / 2, m_SpawnArea.Height / 2);
                    }
                    else
                    {
                        bc.Home = bc.Location;
                        bc.HomeMap = bc.Map;

                        Point2D xWall1 = new Point2D(m_SpawnArea.X, bc.Y);
                        Point2D xWall2 = new Point2D(m_SpawnArea.X + m_SpawnArea.Width, bc.Y);
                        Point2D yWall1 = new Point2D(bc.X, m_SpawnArea.Y);
                        Point2D yWall2 = new Point2D(bc.X, m_SpawnArea.Y + m_SpawnArea.Height);

                        double minXDist = Math.Min(bc.GetDistanceToSqrt(xWall1), bc.GetDistanceToSqrt(xWall2));
                        double minYDist = Math.Min(bc.GetDistanceToSqrt(yWall1), bc.GetDistanceToSqrt(yWall2));

                        bc.RangeHome = (int)Math.Min(minXDist, minYDist);
                    }
                }
            }
        }

        public Point3D GetSpawnLocation()
        {
            Map map = Map;

            if (map == null)
            {
                return Location;
            }

            // Try 20 times to find a spawnable location.
            for (int i = 0; i < 20; i++)
            {
                int x = Utility.Random(m_SpawnArea.X, m_SpawnArea.Width);
                int y = Utility.Random(m_SpawnArea.Y, m_SpawnArea.Height);

                int z = Map.GetAverageZ(x, y);

                if (Map.CanSpawnMobile(new Point2D(x, y), z))
                {
                    return new Point3D(x, y, z);
                }
            }

            return Location;
        }

        public int Level1 => 4;
        public int Level2 => 8;
        public int Level3 => 12;

        public int GetSubLevel()
        {
            int level = Level;

            return level <= Level1 ? 0 : level <= Level2 ? 1 : level <= Level3 ? 2 : 3;
        }

        public int GetSubLevelfor (Mobile m)
        {
            Type[][] types = ChampionSpawnInfo.GetInfo(m_Type).SpawnTypes;
            Type t = m.GetType();

            for (int i = 0; i < types.GetLength(0); i++)
            {
                Type[] individualTypes = types[i];

                for (int j = 0; j < individualTypes.Length; j++)
                {
                    if (t == individualTypes[j])
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public Mobile Spawn()
        {
            Type[][] types = ChampionSpawnInfo.GetInfo(m_Type).SpawnTypes;

            int v = GetSubLevel();

            if (v >= 0 && v < types.Length)
            {
                return Spawn(types[v]);
            }

            return null;
        }

        public Mobile Spawn(params Type[] types)
        {
            try
            {
                return Activator.CreateInstance(types[Utility.Random(types.Length)]) as Mobile;
            }
            catch
            {
                return null;
            }
        }

        public void Expire()
        {
            m_Kills = 0;

            if (m_WhiteSkulls.Count == 0)
            {
                // They didn't even get 20%, go back a level

                if (Level > 0)
                {
                    --Level;
                }

                if (!AlwaysActive && Level == 0)
                {
                    Stop();
                }

                InvalidateProperties();
            }
            else
            {
                SetWhiteSkullCount(0);
            }

            ExpireTime = Core.Now + ExpireDelay;
        }

        public Point3D GetRedSkullLocation(int index)
        {
            int x, y;

            if (index < 5)
            {
                x = index - 2;
                y = -2;
            }
            else if (index < 9)
            {
                x = 2;
                y = index - 6;
            }
            else if (index < 13)
            {
                x = 10 - index;
                y = 2;
            }
            else
            {
                x = -2;
                y = 14 - index;
            }

            return new Point3D(X + x, Y + y, Z - 15);
        }

        public Point3D GetWhiteSkullLocation(int index)
        {
            int x, y;

            switch(index)
            {
                default: x = -1; y = -1; break;
                case 1:  x =  1; y = -1; break;
                case 2:  x =  1; y =  1; break;
                case 3:  x = -1; y =  1; break;
            }

            return new Point3D(X + x, Y + y, Z - 15);
        }

        public override void AddNameProperty(IPropertyList list)
        {
            list.Add("champion spawn");
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (m_Active)
            {
                list.Add(1060742);                         // active
                list.Add(1060658, $"{"Type"}\t{m_Type}"); // ~1_val~: ~2_val~
                list.Add(1060659, $"{"Level"}\t{Level}"); // ~1_val~: ~2_val~
                var killRatio = 100.0 * ((double)m_Kills / MaxKills);
                list.Add(1060660, $"{"Kills"}\t{m_Kills} of {MaxKills} ({killRatio:F1}%)"); // ~1_val~: ~2_val~
            }
            else
            {
                list.Add(1060743); // inactive
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_Active)
            {
                LabelTo(from, $"{m_Type} (Active; Level: {Level}; Kills: {m_Kills}/{MaxKills})");
            }
            else
            {
                LabelTo(from, $"{m_Type} (Inactive)");
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.SendGump(new PropertiesGump(from, this));
        }

        public override void OnLocationChange(Point3D oldLoc)
        {
            if (Deleted)
            {
                return;
            }

            if (m_Platform != null)
            {
                m_Platform.Location = new Point3D(X, Y, Z - 20);
            }

            if (m_Altar != null)
            {
                m_Altar.Location = new Point3D(X, Y, Z - 15);
            }

            if (m_Idol != null)
            {
                m_Idol.Location = new Point3D(X, Y, Z - 15);
            }

            if (m_RedSkulls != null)
            {
                for (var i = 0; i < Math.Min(m_RedSkulls.Count, 16); ++i)
                {
                    m_RedSkulls[i].Location = GetRedSkullLocation(i);
                }
            }

            if (m_WhiteSkulls != null)
            {
                for (int i = 0; i < m_WhiteSkulls.Count; ++i)
                {
                    m_WhiteSkulls[i].Location = GetWhiteSkullLocation(i);
                }
            }

            m_SpawnArea.X += Location.X - oldLoc.X;
            m_SpawnArea.Y += Location.Y - oldLoc.Y;

            UpdateRegion();
        }

        public override void OnMapChange()
        {
            if (Deleted)
            {
                return;
            }

            if (m_Platform != null)
            {
                m_Platform.Map = Map;
            }

            if (m_Altar != null)
            {
                m_Altar.Map = Map;
            }

            if (m_Idol != null)
            {
                m_Idol.Map = Map;
            }

            if (m_RedSkulls != null)
            {
                for (int i = 0; i < m_RedSkulls.Count; ++i)
                {
                    m_RedSkulls[i].Map = Map;
                }
            }

            if (m_WhiteSkulls != null)
            {
                for (int i = 0; i < m_WhiteSkulls.Count; ++i)
                {
                    m_WhiteSkulls[i].Map = Map;
                }
            }

            UpdateRegion();
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            m_Platform?.Delete();

            m_Altar?.Delete();

            m_Idol?.Delete();

            if (m_RedSkulls != null)
            {
                for (int i = 0; i < m_RedSkulls.Count; ++i)
                {
                    m_RedSkulls[i].Delete();
                }

                m_RedSkulls.Clear();
            }

            if (m_WhiteSkulls != null)
            {
                for (int i = 0; i < m_WhiteSkulls.Count; ++i)
                {
                    m_WhiteSkulls[i].Delete();
                }

                m_WhiteSkulls.Clear();
            }

            DeleteCreatures();

            if (Champion is { Player: false })
            {
                Champion.Delete();
            }

            Stop();

            UpdateRegion();
        }

        public void ExpireCreatures()
        {
            if (!m_Active && !ReadyToActivate && !AlwaysActive)
            {
                DeleteCreatures();
            }
        }

        public void DeleteCreatures()
        {
            if (m_Creatures != null)
            {
                for (int i = 0; i < m_Creatures.Count; ++i)
                {
                    Mobile mob = m_Creatures[i];

                    if (!mob.Player)
                    {
                        mob.Delete();
                    }
                }

                m_Creatures.Clear();
            }
        }

        public ChampionSpawn(Serial serial) : base(serial)
        {
        }

        public virtual void RegisterDamageTo(Mobile m)
        {
            if (m == null)
            {
                return;
            }

            foreach (DamageEntry de in m.DamageEntries)
            {
                if (de.HasExpired)
                {
                    continue;
                }

                Mobile damager = de.Damager;
                Mobile master = damager.GetDamageMaster(m);

                if (master != null)
                {
                    damager = master;
                }

                RegisterDamage(damager, de.DamageGiven);
            }
        }

        public void RegisterDamage(Mobile from, int amount)
        {
            if (from?.Player != true)
            {
                return;
            }

            if (DamageEntries.ContainsKey(from))
            {
                DamageEntries[from] += amount;
            }
            else
            {
                DamageEntries.Add(from, amount);
            }
        }

        public virtual void AwardArtifact(Item artifact)
        {
            if (artifact == null)
            {
                return;
            }

            if (DamageEntries.Count > 0)
            {
                int totalDamage = 0;

                Dictionary<Mobile, int> validEntries = new Dictionary<Mobile, int>();

                foreach (var (key, value) in DamageEntries)
                {
                    if (IsEligible(key, artifact))
                    {
                        validEntries.Add(key, value);
                        totalDamage += value;
                    }
                }

                bool artifactGiven = false;

                do
                {
                    int randomDamage = Utility.RandomMinMax(1, totalDamage);

                    int checkDamage = 0;

                    foreach (var (key, value) in validEntries)
                    {
                        checkDamage += value;

                        if (checkDamage > randomDamage)
                        {
                            if (GiveArtifact(key, artifact))
                            {
                                artifactGiven = true;
                            }
                            else
                            {
                                validEntries.Remove(key);
                            }

                            break;
                        }
                    }

                    if (validEntries.Count == 0) //EVERYONE has a full backpack?!@
                    {
                        artifact.Delete();
                        break;
                    }
                }
                while (!artifactGiven);
            }
            else
            {
                artifact.Delete();
            }
        }

        public bool GiveArtifact(Mobile to, Item artifact)
        {
            if (to == null || artifact == null)
            {
                return false;
            }

            Container pack = to.Backpack;

            if (pack?.TryDropItem(to, artifact, false) != true)
            {
                return false;
            }

            to.SendLocalizedMessage(1062317); // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
            return true;
        }

        public bool IsEligible(Mobile m, Item Artifact) =>
            m.Player && m.Alive && m.Region != null && m.Region == m_Region &&
            m.Backpack?.CheckHold(m, Artifact, false) == true;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(9); // version

            writer.Write(m_Level);

            writer.Write(ActivatedByProximity);
            writer.WriteDeltaTime(NextProximityTime);

            writer.Write(m_MaxLevel); //This can change, based on how you use the champion spawn

            writer.Write(ActivatedByValor);

            writer.Write(DamageEntries.Count);
            foreach (KeyValuePair<Mobile, int> kvp in DamageEntries)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }

            writer.Write(ConfinedRoaming);
            writer.Write(m_Idol);
            writer.Write(HasBeenAdvanced);
            writer.Write(m_SpawnArea);

            writer.Write(RandomizeType);

            writer.Write(m_Kills);

            writer.Write(m_Active);
            writer.Write((int)m_Type);
            writer.Write(m_Creatures);
            writer.Write(m_RedSkulls);
            writer.Write(m_WhiteSkulls);
            writer.Write(m_Platform);
            writer.Write(m_Altar);
            writer.Write(ExpireDelay);
            writer.WriteDeltaTime(ExpireTime);
            writer.Write(Champion);
            writer.Write(RestartDelay);

            if (_restartTimerToken.Running)
            {
                writer.Write(true);
                writer.WriteDeltaTime(RestartTime);
            }
            else
            {
                writer.Write(false);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            DamageEntries = new Dictionary<Mobile, int>();

            int version = reader.ReadInt();

            switch(version)
            {
                case 9:
                    {
                        m_Level = reader.ReadInt();

                        goto case 8;
                    }
                case 8:
                    {
                        ActivatedByProximity = reader.ReadBool();
                        NextProximityTime = reader.ReadDeltaTime();
                        goto case 7;
                    }
                case 7:
                    {
                        m_MaxLevel = reader.ReadInt();
                        goto case 6;
                    }
                case 6:
                    {
                        if (version < 7)
                        {
                            m_MaxLevel = 16 + Utility.Random(3); //full levels
                        }

                        ActivatedByValor = reader.ReadBool();
                        goto case 5;
                    }
                case 5:
                    {
                        int entries = reader.ReadInt();
                        Mobile m;
                        int damage;
                        for (int i = 0; i < entries; ++i)
                        {
                            m = reader.ReadEntity<Mobile>();
                            damage = reader.ReadInt();
                            if (m != null)
                            {
                                DamageEntries.Add(m, damage);
                            }
                        }
                        goto case 4;
                    }
                case 4:
                    {
                        ConfinedRoaming = reader.ReadBool();
                        m_Idol = reader.ReadEntity<IdolOfTheChampion>();
                        HasBeenAdvanced = reader.ReadBool();

                        goto case 3;
                    }
                case 3:
                    {
                        m_SpawnArea = reader.ReadRect2D();

                        goto case 2;
                    }
                case 2:
                    {
                        RandomizeType = reader.ReadBool();

                        goto case 1;
                    }
                case 1:
                    {
                        if (version < 3)
                        {
                            int oldRange = reader.ReadInt();

                            m_SpawnArea = new Rectangle2D(new Point2D(X - oldRange, Y - oldRange), new Point2D(X + oldRange, Y + oldRange));
                        }

                        m_Kills = reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 1)
                        {
                            m_SpawnArea = new Rectangle2D(new Point2D(X - 24, Y - 24), new Point2D(X + 24, Y + 24)); //Default was 24
                        }

                        bool active = reader.ReadBool();
                        m_Type = (ChampionSpawnType)reader.ReadInt();
                        m_Creatures = reader.ReadEntityList<Mobile>();
                        m_RedSkulls = reader.ReadEntityList<Item>();
                        m_WhiteSkulls = reader.ReadEntityList<Item>();
                        m_Platform = reader.ReadEntity<ChampionPlatform>();
                        m_Altar = reader.ReadEntity<ChampionAltar>();
                        ExpireDelay = reader.ReadTimeSpan();
                        ExpireTime = reader.ReadDeltaTime();
                        Champion = reader.ReadEntity<Mobile>();
                        RestartDelay = reader.ReadTimeSpan();

                        if (reader.ReadBool())
                        {
                            RestartTime = reader.ReadDeltaTime();
                            BeginRestart(RestartTime - Core.Now);
                        }

                        if (version < 4)
                        {
                            m_Idol = new IdolOfTheChampion(this);
                            m_Idol.MoveToWorld(new Point3D(X, Y, Z - 15), Map);
                        }

                        if (m_Platform == null || m_Altar == null || m_Idol == null)
                        {
                            Delete();
                        }
                        else if (active)
                        {
                            Start();
                        }
                        else if (AlwaysActive)
                        {
                            ReadyToActivate = true;
                        }

                        break;
                    }
            }

            Timer.StartTimer(TimeSpan.Zero, UpdateRegion);
        }
    }

    public class ChampionSpawnRegion : BaseRegion
    {
        public ChampionSpawn Spawn { get; }

        public ChampionSpawnRegion(ChampionSpawn spawn) :
            base(null, spawn.Map, Find(spawn.Location, spawn.Map), spawn.SpawnArea) => Spawn = spawn;

        public override bool AllowHousing(Mobile from, Point3D p) => false;

        public bool CanSpawn() => Spawn.EjectLocation != new Point3D(0, 0, 0) && Spawn.EjectMap != null;

        public override void AlterLightLevel(Mobile m, ref int global, ref int personal)
        {
            base.AlterLightLevel(m, ref global, ref personal);
            global = Math.Max(global, 1 + Spawn.Level);	//This is a guesstimate.  TODO: Verify & get exact values // OSI testing: at 2 red skulls, light = 0x3 ; 1 red = 0x3.; 3 = 8; 9 = 0xD 8 = 0xD 12 = 0x12 10 = 0xD
        }

        public override void OnEnter(Mobile m)
        {
            if (m.Player && m.AccessLevel == AccessLevel.Player && !Spawn.Active)
            {
                Region parent = Parent ?? this;

                if (Spawn.ReadyToActivate)
                {
                    Spawn.Start();
                }
                else if (Spawn.ProximitySpawn && !Spawn.ActivatedByProximity && Core.Now >= Spawn.NextProximityTime)
                {
                    List<Mobile> players = parent.GetPlayers();
                    List<IPAddress> addresses = new List<IPAddress>();
                    for (var i = 0; i < players.Count; i++)
                    {
                        if (players[i].AccessLevel == AccessLevel.Player && players[i].NetState != null &&
                            !addresses.Contains(players[i].NetState.Address) && !((PlayerMobile)players[i]).Young)
                        {
                            addresses.Add(players[i].NetState.Address);
                        }
                    }

                    if (addresses.Count >= 15)
                    {
                        foreach (Mobile player in players)
                        {
                            player.SendMessage(0x20, Spawn.BroadcastMessage);
                        }

                        Spawn.ActivatedByProximity = true;
                        Spawn.BeginRestart(TimeSpan.FromMinutes(5.0));
                    }
                }
            }
        }

        public override bool OnMoveInto(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation)
        {
            if (base.OnMoveInto(m, d, newLocation, oldLocation))
            {
                if (m.Player)
                {
                    if (((PlayerMobile)m).Young)
                    {
                        m.SendMessage("You decide against going here because of the danger.");
                    }
                    else if (!m.Alive)
                    {
                        m.SendMessage("A magical force prevents ghosts from entering this region.");
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        public override bool OnBeforeDeath(Mobile m)
        {
            if (Parent?.OnBeforeDeath(m) == false)
            {
                return false;
            }

            if (m.Player) //Give them 5 minutes to resurrect, then they are booted.
            {
                m.SendMessage("A magical force encompasses you, attempting to force you out of the area.");
                new EjectTimer(m, this).Start();
            }

            return true;
        }

        private class EjectTimer : Timer
        {
            private readonly Mobile m_From;
            private readonly ChampionSpawnRegion m_Region;

            public EjectTimer(Mobile from, ChampionSpawnRegion region) : base(TimeSpan.FromMinutes(5.0))
            {
                m_From = from;
                m_Region = region;
            }

            protected override void OnTick()
            {
                //See if they are dead, or logged out!
                if (m_Region.Spawn != null && m_Region.CanSpawn() && !m_From.Alive)
                {
                    if (m_From.NetState != null)
                    {
                        if (m_From.Region.IsPartOf(m_Region))
                        {
                            m_From.MoveToWorld(m_Region.Spawn.EjectLocation, m_Region.Spawn.EjectMap);
                            m_From.SendMessage("A magical force forces you out of the area.");
                        }
                    }
                    else if (Find(m_From.LogoutLocation, m_From.LogoutMap).IsPartOf(m_Region))
                    {
                        m_From.LogoutLocation = m_Region.Spawn.EjectLocation;
                        m_From.LogoutMap = m_Region.Spawn.EjectMap;
                    }
                }
            }
        }
    }

    public class IdolOfTheChampion : Item
    {
        public ChampionSpawn Spawn { get; private set; }

        public override string DefaultName => "Idol of the Champion";

        public IdolOfTheChampion(ChampionSpawn spawn): base(0x1F18)
        {
            Spawn = spawn;
            Movable = false;
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            Spawn?.Delete();
        }

        public IdolOfTheChampion(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(Spawn);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Spawn = reader.ReadEntity<ChampionSpawn>();

                        if (Spawn == null)
                        {
                            Delete();
                        }

                        break;
                    }
            }
        }
    }
}
