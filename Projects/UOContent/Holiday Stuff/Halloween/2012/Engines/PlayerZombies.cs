using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Events.Halloween;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Events
{
    public static class HalloweenHauntings
    {
        private static Timer _timer;
        private static Timer _clearTimer;

        private static int m_TotalZombieLimit;
        private static int m_DeathQueueLimit;
        private static int m_QueueDelaySeconds;
        private static int m_QueueClearIntervalSeconds;

        private static HashSet<PlayerMobile> _deathQueue;

        private static readonly Rectangle2D[] m_Cemetaries =
        {
            new(1272, 3712, 30, 20), // Jhelom
            new(1337, 1444, 48, 52), // Britain
            new(2424, 1098, 20, 28), // Trinsic
            new(2728, 840, 54, 54),  // Vesper
            new(4528, 1314, 20, 28), // Moonglow
            new(712, 1104, 30, 22),  // Yew
            new(5824, 1464, 22, 6),  // Fire Dungeon
            new(5224, 3655, 14, 5),  // T2A

            new(1272, 3712, 20, 30), // Jhelom
            new(1337, 1444, 52, 48), // Britain
            new(2424, 1098, 28, 20), // Trinsic
            new(2728, 840, 54, 54),  // Vesper
            new(4528, 1314, 28, 20), // Moonglow
            new(712, 1104, 22, 30),  // Yew
            new(5824, 1464, 6, 22),  // Fire Dungeon
            new(5224, 3655, 5, 14)   // T2A
        };

        internal static Dictionary<PlayerMobile, ZombieSkeleton> _reAnimated;

        public static void Initialize()
        {
            m_TotalZombieLimit = 200;
            m_DeathQueueLimit = 200;
            m_QueueDelaySeconds = 120;
            m_QueueClearIntervalSeconds = 1800;

            var today = Core.Now;
            var tick = TimeSpan.FromSeconds(m_QueueDelaySeconds);
            var clear = TimeSpan.FromSeconds(m_QueueClearIntervalSeconds);

            _reAnimated = new Dictionary<PlayerMobile, ZombieSkeleton>();
            _deathQueue = new HashSet<PlayerMobile>();

            if (today >= HolidaySettings.StartHalloween && today <= HolidaySettings.FinishHalloween)
            {
                _timer = Timer.DelayCall(tick, 0, Timer_Callback);
                _clearTimer = Timer.DelayCall(clear, 0, Clear_Callback);

                EventSink.PlayerDeath += EventSink_PlayerDeath;
            }
        }

        public static void EventSink_PlayerDeath(Mobile m)
        {
            if (m is PlayerMobile { Deleted: false } pm &&
                _timer.Running && !_deathQueue.Contains(pm) && _deathQueue.Count < m_DeathQueueLimit)
            {
                _deathQueue.Add(pm);
            }
        }

        private static void Clear_Callback()
        {
            if (Core.Now > HolidaySettings.FinishHalloween)
            {
                _clearTimer.Stop();
                _clearTimer = null;
                _reAnimated = null;
                _deathQueue = null;
                return;
            }

            _reAnimated.Clear();
            _deathQueue.Clear();
        }

        private static void Timer_Callback()
        {

            if (Core.Now > HolidaySettings.FinishHalloween)
            {
                _timer.Stop();
                _timer = null;
                return;
            }

            PlayerMobile player = null;

            foreach (var entry in _deathQueue)
            {
                if (!_reAnimated.ContainsKey(entry))
                {
                    player = entry;
                    break;
                }
            }

            if (player?.Deleted != false || _reAnimated.Count >= m_TotalZombieLimit)
            {
                return;
            }

            var map = Utility.RandomBool() ? Map.Trammel : Map.Felucca;
            var home = Utility.RandomPointIn(m_Cemetaries.RandomElement(), map);

            if (map.CanSpawnMobile(home))
            {
                var zombieskel = new ZombieSkeleton(player);

                _reAnimated.Add(player, zombieskel);
                zombieskel.Home = home;
                zombieskel.RangeHome = 10;

                zombieskel.MoveToWorld(home, map);

                _deathQueue.Remove(player);
            }
        }
    }

    [SerializationGenerator(0, false)]
    public partial class PlayerBones : BaseContainer
    {
        [Constructible]
        public PlayerBones(string name) : base(Utility.RandomMinMax(0x0ECA, 0x0ED2))
        {
            Name = $"{name}'s bones";

            Hue = Utility.Random(10) switch
            {
                0 => 0xa09,
                1 => 0xa93,
                2 => 0xa47,
                _ => Hue
            };
        }
    }

    [SerializationGenerator(0, false)]
    public partial class ZombieSkeleton : BaseCreature
    {
        [SerializableField(0, "private", "private")]
        private PlayerMobile _deadPlayer;

        public override string DefaultName => _deadPlayer != null ? $"{_deadPlayer.Name}'s Zombie Skeleton" : "Zombie Skeleton";

        public ZombieSkeleton(PlayerMobile player = null) : base(AIType.AI_Melee)
        {
            _deadPlayer = player;

            Body = 0x93;
            BaseSoundID = 0x1c3;

            SetStr(500);
            SetDex(500);
            SetInt(500);

            SetHits(2500);
            SetMana(500);
            SetStam(500);

            SetDamage(8, 18);

            SetDamageType(ResistanceType.Physical, 40);
            SetDamageType(ResistanceType.Cold, 60);

            SetResistance(ResistanceType.Fire, 50);
            SetResistance(ResistanceType.Energy, 50);
            SetResistance(ResistanceType.Physical, 50);
            SetResistance(ResistanceType.Cold, 50);
            SetResistance(ResistanceType.Poison, 50);

            SetSkill(SkillName.MagicResist, 65.1, 80.0);
            SetSkill(SkillName.Tactics, 95.1, 100);
            SetSkill(SkillName.Wrestling, 85.1, 95);

            Fame = 1000;
            Karma = -1000;

            VirtualArmor = 18;
        }

        public override string CorpseName => "a rotting corpse";

        public override bool BleedImmune => true;

        public override Poison PoisonImmune => Poison.Regular;

        public override void GenerateLoot()
        {
            var deadPlayerExists = _deadPlayer?.Deleted == false;

            PackItem(
                Utility.Random(deadPlayerExists ? 8 : 10) switch
                {
                    0 => new LeftArm(),
                    1 => new RightArm(),
                    2 => new Torso(),
                    3 => new Bone(),
                    4 => new RibCage(),
                    9 => deadPlayerExists ? new PlayerBones(_deadPlayer.Name) : null,
                    _ => null // 5-8, 10 (50%)
                }
            );

            AddLoot(LootPack.Meager);
        }

        public override void OnDelete()
        {
            if (_deadPlayer?.Deleted == false)
            {
                HalloweenHauntings._reAnimated?.Remove(_deadPlayer);
            }
        }
    }
}
