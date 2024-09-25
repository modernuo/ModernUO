using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
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

        private const int TotalZombieLimit = 200;
        private const int DeathQueueLimit = 200;
        private const int QueueDelaySeconds = 120;
        private const int QueueClearIntervalSeconds = 1800;

        private static HashSet<PlayerMobile> _deathQueue;

        private static readonly Rectangle2D[] _cemetaries =
        [
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
        ];

        internal static Dictionary<PlayerMobile, ZombieSkeleton> _reAnimated;

        [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
        public static void OnPlayerDeathEvent(PlayerMobile pm)
        {
            var now = Core.Now;

            if (now < HolidaySettings.StartHalloween || now > HolidaySettings.FinishHalloween)
            {
                return;
            }

            _timer ??= Timer.DelayCall(TimeSpan.FromSeconds(QueueDelaySeconds), 0, Timer_Callback);
            _clearTimer ??= Timer.DelayCall(TimeSpan.FromSeconds(QueueClearIntervalSeconds), 0, Clear_Callback);

            if (_timer.Running)
            {
                _deathQueue ??= [];

                if (_deathQueue.Count < DeathQueueLimit)
                {
                    _deathQueue.Add(pm);
                }
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

            _reAnimated?.Clear();
            _deathQueue?.Clear();
        }

        private static void Timer_Callback()
        {
            if (Core.Now > HolidaySettings.FinishHalloween)
            {
                _timer.Stop();
                _timer = null;
                return;
            }

            if (_deathQueue == null)
            {
                return;
            }

            PlayerMobile player = null;
            foreach (var entry in _deathQueue)
            {
                if (_reAnimated?.ContainsKey(entry) != true)
                {
                    player = entry;
                    break;
                }
            }

            if (player?.Deleted != false || _reAnimated?.Count >= TotalZombieLimit)
            {
                return;
            }

            var map = Utility.RandomBool() ? Map.Trammel : Map.Felucca;
            var home = Utility.RandomPointIn(_cemetaries.RandomElement(), map);

            if (map.CanSpawnMobile(home))
            {
                var zombieskel = new ZombieSkeleton(player);

                _reAnimated ??= [];
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
