using System;
using System.Collections.Generic;
using Server.Events.Halloween;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Events
{
    public static class HalloweenHauntings
    {
        private static Timer m_Timer;
        private static Timer m_ClearTimer;

        private static int m_TotalZombieLimit;
        private static int m_DeathQueueLimit;
        private static int m_QueueDelaySeconds;
        private static int m_QueueClearIntervalSeconds;

        private static List<PlayerMobile> m_DeathQueue;

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

        public static Dictionary<PlayerMobile, ZombieSkeleton> ReAnimated { get; set; }

        public static void Initialize()
        {
            m_TotalZombieLimit = 200;
            m_DeathQueueLimit = 200;
            m_QueueDelaySeconds = 120;
            m_QueueClearIntervalSeconds = 1800;

            var today = Core.Now;
            var tick = TimeSpan.FromSeconds(m_QueueDelaySeconds);
            var clear = TimeSpan.FromSeconds(m_QueueClearIntervalSeconds);

            ReAnimated = new Dictionary<PlayerMobile, ZombieSkeleton>();
            m_DeathQueue = new List<PlayerMobile>();

            if (today >= HolidaySettings.StartHalloween && today <= HolidaySettings.FinishHalloween)
            {
                m_Timer = Timer.DelayCall(tick, tick, Timer_Callback);

                m_ClearTimer = Timer.DelayCall(clear, clear, Clear_Callback);

                EventSink.PlayerDeath += EventSink_PlayerDeath;
            }
        }

        public static void EventSink_PlayerDeath(Mobile m)
        {
            if (m is PlayerMobile pm && !pm.Deleted && m_Timer.Running && !m_DeathQueue.Contains(pm) &&
                m_DeathQueue.Count < m_DeathQueueLimit)
            {
                m_DeathQueue.Add(pm);
            }
        }

        private static void Clear_Callback()
        {
            ReAnimated.Clear();

            m_DeathQueue.Clear();

            if (Core.Now <= HolidaySettings.FinishHalloween)
            {
                m_ClearTimer.Stop();
            }
        }

        private static void Timer_Callback()
        {
            PlayerMobile player = null;

            if (Core.Now <= HolidaySettings.FinishHalloween)
            {
                for (var index = 0; m_DeathQueue.Count > 0 && index < m_DeathQueue.Count; index++)
                {
                    var entry = m_DeathQueue[index];

                    if (!ReAnimated.ContainsKey(entry))
                    {
                        player = entry;
                        break;
                    }
                }

                if (player?.Deleted == false && ReAnimated.Count < m_TotalZombieLimit)
                {
                    var map = Utility.RandomBool() ? Map.Trammel : Map.Felucca;

                    var home = GetRandomPointInRect(m_Cemetaries.RandomElement(), map);

                    if (map.CanSpawnMobile(home))
                    {
                        var zombieskel = new ZombieSkeleton(player);

                        ReAnimated.Add(player, zombieskel);
                        zombieskel.Home = home;
                        zombieskel.RangeHome = 10;

                        zombieskel.MoveToWorld(home, map);

                        m_DeathQueue.Remove(player);
                    }
                }
            }
            else
            {
                m_Timer.Stop();
            }
        }

        private static Point3D GetRandomPointInRect(Rectangle2D rect, Map map)
        {
            var x = Utility.Random(rect.X, rect.Width);
            var y = Utility.Random(rect.Y, rect.Height);

            return new Point3D(x, y, map.GetAverageZ(x, y));
        }
    }

    public class PlayerBones : BaseContainer
    {
        [Constructible]
        public PlayerBones(string name)
            : base(Utility.RandomMinMax(0x0ECA, 0x0ED2))
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

        public PlayerBones(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class ZombieSkeleton : BaseCreature
    {
        private static readonly string m_Name = "Zombie Skeleton";

        private PlayerMobile m_DeadPlayer;

        public ZombieSkeleton(PlayerMobile player = null)
            : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            m_DeadPlayer = player;

            Name = player != null ? $"{player.Name}'s {m_Name}" : m_Name;

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

        public ZombieSkeleton(Serial serial)
            : base(serial)
        {
        }

        public override string CorpseName => "a rotting corpse";

        public override bool BleedImmune => true;

        public override Poison PoisonImmune => Poison.Regular;

        public override void GenerateLoot()
        {
            var deadPlayerExists = m_DeadPlayer?.Deleted == false;

            PackItem(
                Utility.Random(deadPlayerExists ? 8 : 10) switch
                {
                    0 => new LeftArm(),
                    1 => new RightArm(),
                    2 => new Torso(),
                    3 => new Bone(),
                    4 => new RibCage(),
                    9 => deadPlayerExists ? new PlayerBones(m_DeadPlayer.Name) : null,
                    _ => null // 5-8, 10 (50%)
                }
            );

            AddLoot(LootPack.Meager);
        }

        public override void OnDelete()
        {
            if (m_DeadPlayer?.Deleted == false)
            {
                HalloweenHauntings.ReAnimated?.Remove(m_DeadPlayer);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(m_DeadPlayer);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            m_DeadPlayer = reader.ReadEntity<PlayerMobile>();
        }
    }
}
