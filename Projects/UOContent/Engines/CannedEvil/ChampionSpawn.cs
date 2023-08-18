/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
using System.Runtime.InteropServices;
using ModernUO.Serialization;
using Server.Engines.Virtues;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Regions;
using Server.Logging;
using Server.Utilities;

namespace Server.Engines.CannedEvil;

[SerializationGenerator(10, false)]
public partial class ChampionSpawn : Item
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ChampionSpawn));

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _activatedByProximity;

    [DeltaDateTime]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextProximityTime;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _activatedByValor;

    [SerializableField(5)]
    private Dictionary<Mobile, int>_damageEntries;

    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _confinedRoaming;

    [SerializableField(7)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private IdolOfTheChampion _idol;

    [SerializableField(8)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _hasBeenAdvanced;

    [SerializableField(10)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _randomizeType;

    [InvalidateProperties]
    [SerializableField(13)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ChampionSpawnType _type;

    [SerializableField(14)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private List<Mobile> _creatures;

    [SerializableField(15)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private List<Item> _redSkulls;

    [SerializableField(16)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private List<Item> _whiteSkulls;

    [SerializableField(17)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ChampionPlatform _platform;

    [SerializableField(18)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ChampionAltar _altar;

    [SerializableField(19)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _expireDelay;

    [DeltaDateTime]
    [SerializableField(20)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _expireTime;

    [SerializableField(21)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _champion;

    [SerializableField(22)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _restartDelay;

    [DeltaDateTime]
    [SerializableField(23, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _restartTime;

    private ChampionSpawnRegion m_Region;

    //Goes back each level, below level 0 and it goes off!

    private TimerExecutionToken _timerToken;
    private TimerExecutionToken _restartTimerToken;

    public virtual string BroadcastMessage => "The Champion has sensed your presence!  Beware its wrath!";
    public virtual bool ProximitySpawn => false;
    public virtual bool AlwaysActive => false;

    public override TimeSpan DecayTime => TimeSpan.FromSeconds(180.0);

    public virtual bool HasStarRoomGate => true;

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

        _creatures = new List<Mobile>();
        _redSkulls = new List<Item>();
        _whiteSkulls = new List<Item>();

        _platform = new ChampionPlatform(this);
        _altar = new ChampionAltar(this);
        _idol = new IdolOfTheChampion(this);

        ExpireDelay = TimeSpan.FromMinutes(30.0);
        RestartDelay = TimeSpan.FromMinutes(30.0);
        _damageEntries = new Dictionary<Mobile, int>();

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

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Level
    {
        get => _level;
        set
        {
            for (int i = _redSkulls.Count - 1; i >= value; --i)
            {
                _redSkulls[i].Delete();
                _redSkulls.RemoveAt(i);
            }

            for (int i = _redSkulls.Count; i < Math.Min(value, 16); ++i)
            {
                Item skull = new Item(0x1854) { Hue = 0x26, Movable = false, Light = LightType.Circle150 };
                skull.MoveToWorld(GetRedSkullLocation(i), Map);
                _redSkulls.Add(skull);
            }

            _level = value;
            this.MarkDirty();
            InvalidateProperties();
        }
    }

    [SerializableProperty(3)]
    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public int MaxLevel
    {
        get => _maxLevel;
        set => _maxLevel = Math.Clamp(value, 0, 18);
    }

    [SerializableProperty(9)]
    [CommandProperty(AccessLevel.GameMaster)]
    public Rectangle2D SpawnArea
    {
        get => _spawnArea;
        set
        {
            _spawnArea = value;
            this.MarkDirty();
            InvalidateProperties();
            UpdateRegion();
        }
    }

    [SerializableProperty(11)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Kills
    {
        get => _kills;
        set
        {
            _kills = value;
            this.MarkDirty();

            double n = _kills / (double)MaxKills;
            int p = (int)(n * 100);

            if (p < 90)
            {
                SetWhiteSkullCount(p / 20);
            }

            InvalidateProperties();
        }
    }

    [SerializableProperty(12)]
    [CommandProperty(AccessLevel.GameMaster)]
    public bool Active
    {
        get => _active;
        set
        {
            this.MarkDirty();

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

    public bool IsChampionSpawn(Mobile m) => _creatures.Contains(m);

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
        for (int i = _whiteSkulls.Count - 1; i >= val; --i)
        {
            _whiteSkulls[i].Delete();
            _whiteSkulls.RemoveAt(i);
        }

        for (int i = _whiteSkulls.Count; i < val; ++i)
        {
            Item skull = new Item(0x1854)
            {
                Movable = false,
                Light = LightType.Circle150
            };

            skull.MoveToWorld(GetWhiteSkullLocation(i), Map);

            _whiteSkulls.Add(skull);

            Effects.PlaySound(skull.Location, skull.Map, 0x29);
            Effects.SendLocationEffect(new Point3D(skull.X + 1, skull.Y + 1, skull.Z), skull.Map, 0x3728, 10);
        }

        this.MarkDirty();
    }

    public void Start()
    {
        if (_active || Deleted)
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

        _active = true;
        ReadyToActivate = false;
        HasBeenAdvanced = false;
        MaxLevel = 16 + Utility.Random(3);

        _timerToken.Cancel();
        Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), OnSlice, out _timerToken);

        _restartTimerToken.Cancel();

        if (_altar != null)
        {
            _altar.Hue = Champion != null ? 0x26 : 0;
        }

        if (_platform != null)
        {
            _platform.Hue = 0x452;
        }

        ExpireTime = Core.Now + ExpireDelay;
    }

    public void Stop()
    {
        if (!_active || Deleted)
        {
            return;
        }

        _active = false;
        ActivatedByValor = false;
        HasBeenAdvanced = false;
        MaxLevel = 0;

        _timerToken.Cancel();
        _restartTimerToken.Cancel();

        if (_altar != null)
        {
            _altar.Hue = 0;
        }

        if (_platform != null)
        {
            _platform.Hue = 0x497;
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

    public static void GiveScrollOfTranscendenceFelTo(Mobile killer, ScrollofTranscendence SoTF)
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
        else if (killer.Corpse is { Deleted: false })
        {
            killer.Corpse.DropItem(SoTF);
        }
        else
        {
            killer.AddToBackpack(SoTF);
        }

        // Justice reward
        var pm = (PlayerMobile)killer;
        var prot = JusticeVirtue.GetProtector(pm);
        if (prot == null || prot.Map != killer.Map || prot.Kills >= 5 || prot.Criminal ||
            !JusticeVirtue.CheckMapRegion(killer, prot))
        {
            return;
        }

        var chance = VirtueSystem.GetLevel(prot, VirtueName.Justice) switch
        {
            VirtueLevel.Seeker   => 60,
            VirtueLevel.Follower => 80,
            VirtueLevel.Knight   => 100,
            _                    => 0
        };

        if (chance > 0 && chance > Utility.Random(100))
        {
            prot.SendLocalizedMessage(1049368); // You have been rewarded for your dedication to Justice!
            ScrollofTranscendence SoTFduplicate = new ScrollofTranscendence (SoTF.Skill, SoTF.Value);
            prot.AddToBackpack(SoTFduplicate);
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
        var prot = JusticeVirtue.GetProtector(pm);
        if (prot == null || prot.Map != killer.Map || prot.Kills >= 5 || prot.Criminal ||
            !JusticeVirtue.CheckMapRegion(killer, prot))
        {
            return;
        }

        var chance = VirtueSystem.GetLevel(prot, VirtueName.Justice) switch
        {
            VirtueLevel.Seeker   => 60,
            VirtueLevel.Follower => 80,
            VirtueLevel.Knight   => 100,
            _                    => 0
        };

        if (chance > 0 && chance > Utility.Random(100))
        {
            prot.SendLocalizedMessage(1049368); // You have been rewarded for your dedication to Justice!
            prot.AddToBackpack(CreateRandomFelPS());
        }
    }

    public void OnSlice()
    {
        if (!_active || Deleted)
        {
            return;
        }

        if (Champion != null)
        {
            if (Champion.Deleted)
            {
                RegisterDamageTo(Champion);

                if (Core.ML)
                {
                    AwardArtifact((Champion as BaseChampion)?.GetArtifact());
                }

                this.Clear(_damageEntries);

                if (_platform != null)
                {
                    _platform.Hue = 0x497;
                }

                if (_altar != null)
                {
                    _altar.Hue = 0;

                    if (HasStarRoomGate && (!Core.ML || Map == Map.Felucca))
                    {
                        new StarRoomGate(_altar.Location, _altar.Map, true);
                    }
                }

                Champion = null;
                Stop();
            }
        }
        else
        {
            int kills = _kills;

            for (var i = 0; i < _creatures.Count; ++i)
            {
                Mobile m = _creatures[i];

                if (m.Deleted)
                {
                    if (m.Corpse is { Deleted: false })
                    {
                        ((Corpse)m.Corpse).BeginDecay(TimeSpan.FromMinutes(1));
                    }

                    this.RemoveAt(_creatures, i);
                    --i;
                    ++_kills;

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
                                // 1 in 1000 you get either a scroll of transcendence or a powerscroll
                                var random = Utility.Random(2000);
                                if (random == 0)
                                {
                                    GiveScrollOfTranscendenceFelTo(pm, CreateRandomFelSoT());
                                }
                                else if (random == 1)
                                {
                                    GivePowerScrollFelTo(pm, CreateRandomFelPS());
                                }
                            }

                            if ((Map == Map.Ilshenar || Map == Map.Tokuno) && Utility.Random(10000) < 15)
                            {
                                pm.SendLocalizedMessage(1094936); // You have received a Scroll of Transcendence!
                                pm.AddToBackpack(CreateRandomTramSoT());
                            }
                        }

                        int mobSubLevel = GetSubLevelfor(m) + 1;

                        if (mobSubLevel >= 0)
                        {
                            bool gainedPath = false;

                            int pointsToGain = mobSubLevel * 40;

                            if (VirtueSystem.Award(pm, VirtueName.Valor, pointsToGain, ref gainedPath))
                            {
                                if (gainedPath)
                                {
                                    pm.SendLocalizedMessage(1054032); // You have gained a path in Valor!
                                }
                                else
                                {
                                    pm.SendLocalizedMessage(1054030); // You have gained in Valor!
                                }

                                // No delay on Valor gains
                            }

                            pm.ChampionTitles.Award(_type, mobSubLevel);
                        }
                    }
                }
            }

            // Only really needed once.
            if (_kills > kills)
            {
                InvalidateProperties();
            }

            double n = _kills / (double)MaxKills;
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

        if (Level < _maxLevel)
        {
            _kills = 0;
            ++Level;
            InvalidateProperties();
            SetWhiteSkullCount(0);

            if (_altar != null)
            {
                Effects.PlaySound(_altar.Location, _altar.Map, 0x29);
                Effects.SendLocationEffect(new Point3D(_altar.X + 1, _altar.Y + 1, _altar.Z), _altar.Map, 0x3728, 10);
            }
        }
        else
        {
            SpawnChampion();
        }
    }

    public void SpawnChampion()
    {
        if (_altar != null)
        {
            _altar.Hue = 0x26;
        }

        if (_platform != null)
        {
            _platform.Hue = 0x452;
        }

        _kills = 0;
        Level = 0;
        InvalidateProperties();
        SetWhiteSkullCount(0);

        try
        {
            Champion = ChampionSpawnInfo.GetInfo(_type).Champion.CreateInstance<Mobile>();
        }
        catch (Exception e)
        {
            logger.Error(
                e,
                "Failed to spawn champion \"{MobileType}\" at {Location} ({Map}).",
                _type,
                Location,
                Map
            );
        }

        if (Champion != null)
        {
            Champion.MoveToWorld(new Point3D(X, Y, Z - 15), Map);

            if (Champion is BaseCreature bc)
            {
                if (ConfinedRoaming)
                {
                    bc.Home = Location;
                    bc.HomeMap = Map;
                    bc.RangeHome = Math.Min(_spawnArea.Width / 2, _spawnArea.Height / 2);
                }
                else
                {
                    bc.Home = bc.Location;
                    bc.HomeMap = bc.Map;

                    Point2D xWall1 = new Point2D(_spawnArea.X, bc.Y);
                    Point2D xWall2 = new Point2D(_spawnArea.X + _spawnArea.Width, bc.Y);
                    Point2D yWall1 = new Point2D(bc.X, _spawnArea.Y);
                    Point2D yWall2 = new Point2D(bc.X, _spawnArea.Y + _spawnArea.Height);

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
        if (!_active || Deleted || Champion != null)
        {
            return;
        }

        while (_creatures.Count < MaxSpawn)
        {
            Mobile m = Spawn();

            if (m == null)
            {
                return;
            }

            Point3D loc = GetSpawnLocation();

            // Allow creatures to turn into Paragons at Ilshenar champions.
            m.OnBeforeSpawn(loc, Map);

            this.Add(_creatures, m);
            m.MoveToWorld(loc, Map);

            if (m is BaseCreature bc)
            {
                bc.Tamable = false;

                if (ConfinedRoaming)
                {
                    bc.Home = Location;
                    bc.HomeMap = Map;
                    bc.RangeHome = Math.Min(_spawnArea.Width / 2, _spawnArea.Height / 2);
                }
                else
                {
                    bc.Home = bc.Location;
                    bc.HomeMap = bc.Map;

                    Point2D xWall1 = new Point2D(_spawnArea.X, bc.Y);
                    Point2D xWall2 = new Point2D(_spawnArea.X + _spawnArea.Width, bc.Y);
                    Point2D yWall1 = new Point2D(bc.X, _spawnArea.Y);
                    Point2D yWall2 = new Point2D(bc.X, _spawnArea.Y + _spawnArea.Height);

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
            int x = Utility.Random(_spawnArea.X, _spawnArea.Width);
            int y = Utility.Random(_spawnArea.Y, _spawnArea.Height);

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
        Type[][] types = ChampionSpawnInfo.GetInfo(_type).SpawnTypes;
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
        Type[][] types = ChampionSpawnInfo.GetInfo(_type).SpawnTypes;

        int v = GetSubLevel();

        if (v >= 0 && v < types.Length)
        {
            return Spawn(types[v]);
        }

        return null;
    }

    public Mobile Spawn(params Type[] types)
    {
        var type = types[Utility.Random(types.Length)];
        try
        {
            return type.CreateInstance<Mobile>();
        }
        catch (Exception e)
        {
            logger.Error(
                e,
                "Failed to spawn minion \"{Type}\" for champion {ChampionSpawnType} at {Location} ({Map}).",
                type,
                _type,
                Location,
                Map
            );
            return null;
        }
    }

    public void Expire()
    {
        _kills = 0;

        if (_whiteSkulls.Count == 0)
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

        if (_active)
        {
            list.Add(1060742);                        // active
            list.Add(1060658, $"{"Type"}\t{_type}"); // ~1_val~: ~2_val~
            list.Add(1060659, $"{"Level"}\t{Level}"); // ~1_val~: ~2_val~
            var killRatio = 100.0 * ((double)_kills / MaxKills);
            list.Add(1060660, $"{"Kills"}\t{_kills} of {MaxKills} ({killRatio:F1}%)"); // ~1_val~: ~2_val~
        }
        else
        {
            list.Add(1060743); // inactive
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        if (_active)
        {
            LabelTo(from, $"{_type} (Active; Level: {Level}; Kills: {_kills}/{MaxKills})");
        }
        else
        {
            LabelTo(from, $"{_type} (Inactive)");
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

        if (_platform != null)
        {
            _platform.Location = new Point3D(X, Y, Z - 20);
        }

        if (_altar != null)
        {
            _altar.Location = new Point3D(X, Y, Z - 15);
        }

        if (_idol != null)
        {
            _idol.Location = new Point3D(X, Y, Z - 15);
        }

        if (_redSkulls != null)
        {
            for (var i = 0; i < Math.Min(_redSkulls.Count, 16); ++i)
            {
                _redSkulls[i].Location = GetRedSkullLocation(i);
            }
        }

        if (_whiteSkulls != null)
        {
            for (int i = 0; i < _whiteSkulls.Count; ++i)
            {
                _whiteSkulls[i].Location = GetWhiteSkullLocation(i);
            }
        }

        _spawnArea.X += Location.X - oldLoc.X;
        _spawnArea.Y += Location.Y - oldLoc.Y;

        UpdateRegion();
    }

    public override void OnMapChange()
    {
        if (Deleted)
        {
            return;
        }

        if (_platform != null)
        {
            _platform.Map = Map;
        }

        if (_altar != null)
        {
            _altar.Map = Map;
        }

        if (_idol != null)
        {
            _idol.Map = Map;
        }

        if (_redSkulls != null)
        {
            for (int i = 0; i < _redSkulls.Count; ++i)
            {
                _redSkulls[i].Map = Map;
            }
        }

        if (_whiteSkulls != null)
        {
            for (int i = 0; i < _whiteSkulls.Count; ++i)
            {
                _whiteSkulls[i].Map = Map;
            }
        }

        UpdateRegion();
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _platform?.Delete();

        _altar?.Delete();

        _idol?.Delete();

        if (_redSkulls != null)
        {
            for (int i = 0; i < _redSkulls.Count; ++i)
            {
                _redSkulls[i].Delete();
            }

            this.Clear(_redSkulls);
        }

        if (_whiteSkulls != null)
        {
            for (int i = 0; i < _whiteSkulls.Count; ++i)
            {
                _whiteSkulls[i].Delete();
            }

            this.Clear(_whiteSkulls);
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
        if (!_active && !ReadyToActivate && !AlwaysActive)
        {
            DeleteCreatures();
        }
    }

    public void DeleteCreatures()
    {
        if (_creatures != null)
        {
            for (int i = 0; i < _creatures.Count; ++i)
            {
                Mobile mob = _creatures[i];

                if (!mob.Player)
                {
                    mob.Delete();
                }
            }

            this.Clear(_creatures);
        }
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

        ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(_damageEntries, from, out _);
        value += amount;

        this.MarkDirty();
    }

    public virtual void AwardArtifact(Item artifact)
    {
        if (artifact == null)
        {
            return;
        }

        if (DamageEntries.Count <= 0)
        {
            artifact.Delete();
            return;
        }

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

            if (validEntries.Count == 0) // EVERYONE has a full backpack?!@
            {
                artifact.Delete();
                break;
            }
        } while (!artifactGiven);
    }

    public static bool GiveArtifact(Mobile to, Item artifact)
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

    private void Deserialize(IGenericReader reader, int version)
    {
        _damageEntries = new Dictionary<Mobile, int>();

        _level = reader.ReadInt();
        _activatedByProximity = reader.ReadBool();
        _nextProximityTime = reader.ReadDeltaTime();
        _maxLevel = reader.ReadInt();
        _activatedByValor = reader.ReadBool();

        int entries = reader.ReadInt();
        for (int i = 0; i < entries; ++i)
        {
            var m = reader.ReadEntity<Mobile>();
            var damage = reader.ReadInt();
            if (m != null)
            {
                _damageEntries.Add(m, damage);
            }
        }

        _confinedRoaming = reader.ReadBool();
        _idol = reader.ReadEntity<IdolOfTheChampion>();
        _hasBeenAdvanced = reader.ReadBool();
        _spawnArea = reader.ReadRect2D();
        _randomizeType = reader.ReadBool();
        _kills = reader.ReadInt();

        _active = reader.ReadBool();
        _type = (ChampionSpawnType)reader.ReadInt();
        _creatures = reader.ReadEntityList<Mobile>();
        _redSkulls = reader.ReadEntityList<Item>();
        _whiteSkulls = reader.ReadEntityList<Item>();
        _platform = reader.ReadEntity<ChampionPlatform>();
        _altar = reader.ReadEntity<ChampionAltar>();
        _expireDelay = reader.ReadTimeSpan();
        _expireTime = reader.ReadDeltaTime();
        _champion = reader.ReadEntity<Mobile>();
        _restartDelay = reader.ReadTimeSpan();

        if (reader.ReadBool())
        {
            _restartTime = reader.ReadDeltaTime();
        }
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (_restartTime < Core.Now)
        {
            BeginRestart(_restartTime - Core.Now);
        }

        if (_platform == null || _altar == null || _idol == null)
        {
            Delete();
        }
        else if (_active)
        {
            _active = false; // Set back to false so we can properly start
            Start();
        }
        else if (AlwaysActive)
        {
            ReadyToActivate = true;
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

[SerializationGenerator(0, false)]
public partial class IdolOfTheChampion : Item
{
    [SerializableField(0)]
    private ChampionSpawn _spawn;

    public override string DefaultName => "Idol of the Champion";

    public IdolOfTheChampion(ChampionSpawn spawn): base(0x1F18)
    {
        _spawn = spawn;
        Movable = false;
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        _spawn?.Delete();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (_spawn == null)
        {
            Delete();
        }
    }
}
