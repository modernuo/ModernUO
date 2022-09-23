using System;
using System.Collections.Generic;
using Server.Items;
using Server.Utilities;

namespace Server.Engines.Doom
{
    public enum GauntletSpawnerState
    {
        InSequence,
        InProgress,
        Completed
    }

    public class GauntletSpawner : Item
    {
        public const int PlayersPerSpawn = 5;

        public const int InSequenceItemHue = 0x000;
        public const int InProgressItemHue = 0x676;
        public const int CompletedItemHue = 0x455;

        private GauntletSpawnerState m_State;

        private TimerExecutionToken _timerToken;

        [Constructible]
        public GauntletSpawner(string typeName = null) : base(0x36FE)
        {
            Visible = false;
            Movable = false;

            TypeName = typeName;
            Creatures = new List<Mobile>();
            Traps = new List<BaseTrap>();
        }

        public GauntletSpawner(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string TypeName { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseDoor Door { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseAddon Addon { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public GauntletSpawner Sequence { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasCompleted
        {
            get
            {
                if (Creatures.Count == 0)
                {
                    return false;
                }

                for (var i = 0; i < Creatures.Count; ++i)
                {
                    var mob = Creatures[i];

                    if (!mob.Deleted)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D RegionBounds { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public GauntletSpawnerState State
        {
            get => m_State;
            set
            {
                if (m_State != value)
                {
                    m_State = value;
                    Update();
                }
            }
        }

        public List<Mobile> Creatures { get; set; }

        public List<BaseTrap> Traps { get; set; }

        public Region Region { get; set; }

        private void Update(bool shouldSpawn = true)
        {
            var lockDoors = m_State == GauntletSpawnerState.InProgress;

            var hue = m_State switch
            {
                GauntletSpawnerState.InSequence => InSequenceItemHue,
                GauntletSpawnerState.InProgress => InProgressItemHue,
                GauntletSpawnerState.Completed  => CompletedItemHue,
                _                               => 0
            };

            if (Door != null)
            {
                Door.Hue = hue;
                Door.Locked = lockDoors;

                if (lockDoors)
                {
                    Door.KeyValue = Key.RandomValue();
                    Door.Open = false;
                }

                if (Door.Link != null)
                {
                    Door.Link.Hue = hue;
                    Door.Link.Locked = lockDoors;

                    if (lockDoors)
                    {
                        Door.Link.KeyValue = Key.RandomValue();
                        Door.Open = false;
                    }
                }
            }

            if (Addon != null)
            {
                Addon.Hue = hue;
            }

            if (m_State == GauntletSpawnerState.InProgress)
            {
                CreateRegion();

                if (shouldSpawn)
                {
                    FullSpawn();
                }

                Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), Slice, out _timerToken);
            }
            else
            {
                Stop();
            }
        }

        public override string DefaultName => "doom spawner";

        public virtual void CreateRegion()
        {
            if (Region != null)
            {
                return;
            }

            var map = Map;

            if (map == null || map == Map.Internal)
            {
                return;
            }

            Region = new GauntletRegion(this, map);
        }

        public void Stop()
        {
            ClearCreatures();
            ClearTraps();
            DestroyRegion();

            _timerToken.Cancel();
        }

        public override void OnDelete()
        {
            base.OnDelete();
            Stop();

            Door?.Link?.Delete();
            Door?.Delete();
            Addon?.Delete();
        }

        public virtual void DestroyRegion()
        {
            Region?.Unregister();

            Region = null;
        }

        public virtual int ComputeTrapCount()
        {
            var area = RegionBounds.Width * RegionBounds.Height;

            return area / 100;
        }

        public virtual void ClearTraps()
        {
            for (var i = 0; i < Traps.Count; ++i)
            {
                Traps[i].Delete();
            }

            Traps.Clear();
        }

        public virtual void SpawnTrap()
        {
            var map = Map;

            if (map == null)
            {
                return;
            }

            var random = Utility.Random(100);

            BaseTrap trap = random switch
            {
                < 22 => new SawTrap(Utility.RandomBool() ? SawTrapType.WestFloor : SawTrapType.NorthFloor),
                < 44 => new SpikeTrap(Utility.RandomBool() ? SpikeTrapType.WestFloor : SpikeTrapType.NorthFloor),
                < 66 => new GasTrap(Utility.RandomBool() ? GasTrapType.NorthWall : GasTrapType.WestWall),
                < 88 => new FireColumnTrap(),
                _    => new MushroomTrap()
            };

            if (trap is FireColumnTrap or MushroomTrap)
            {
                trap.Hue = 0x451;
            }

            // try 10 times to find a valid location
            for (var i = 0; i < 10; ++i)
            {
                var x = Utility.Random(RegionBounds.X, RegionBounds.Width);
                var y = Utility.Random(RegionBounds.Y, RegionBounds.Height);
                var z = Z;

                if (!map.CanFit(x, y, z, 16, false, false))
                {
                    z = map.GetAverageZ(x, y);
                }

                if (!map.CanFit(x, y, z, 16, false, false))
                {
                    continue;
                }

                trap.MoveToWorld(new Point3D(x, y, z), map);
                Traps.Add(trap);

                return;
            }

            trap.Delete();
        }

        public virtual int ComputeSpawnCount()
        {
            var playerCount = 0;

            var map = Map;

            if (map != null)
            {
                var loc = GetWorldLocation();

                var reg = Region.Find(loc, map).GetRegion("Doom Gauntlet");

                if (reg != null)
                {
                    playerCount = reg.GetPlayerCount();
                }
            }

            if (playerCount == 0 && Region != null)
            {
                playerCount = Region.GetPlayerCount();
            }

            return Math.Max((playerCount + PlayersPerSpawn - 1) / PlayersPerSpawn, 1);
        }

        public virtual void ClearCreatures()
        {
            for (var i = 0; i < Creatures.Count; ++i)
            {
                Creatures[i].Delete();
            }

            Creatures.Clear();
        }

        public virtual void FullSpawn()
        {
            ClearCreatures();

            var count = ComputeSpawnCount();

            for (var i = 0; i < count; ++i)
            {
                Spawn();
            }

            ClearTraps();

            count = ComputeTrapCount();

            for (var i = 0; i < count; ++i)
            {
                SpawnTrap();
            }
        }

        public virtual void Spawn()
        {
            try
            {
                if (TypeName == null)
                {
                    return;
                }

                var type = AssemblyHandler.FindTypeByName(TypeName);

                if (type == null)
                {
                    return;
                }

                var mob = type.CreateEntityInstance<Mobile>();

                if (mob != null)
                {
                    mob.MoveToWorld(GetWorldLocation(), Map);
                    Creatures.Add(mob);
                }
            }
            catch
            {
                // ignored
            }
        }

        public virtual void RecurseReset()
        {
            if (m_State != GauntletSpawnerState.InSequence)
            {
                State = GauntletSpawnerState.InSequence;

                if (Sequence?.Deleted == false)
                {
                    Sequence.RecurseReset();
                }
            }
        }

        public virtual void Slice()
        {
            if (m_State != GauntletSpawnerState.InProgress)
            {
                return;
            }

            var count = ComputeSpawnCount();

            for (var i = Creatures.Count; i < count; ++i)
            {
                Spawn();
            }

            if (HasCompleted)
            {
                State = GauntletSpawnerState.Completed;

                if (Sequence?.Deleted == false)
                {
                    if (Sequence.State == GauntletSpawnerState.Completed)
                    {
                        RecurseReset();
                    }

                    Sequence.State = GauntletSpawnerState.InProgress;
                }
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(RegionBounds);

            writer.Write(Traps);

            writer.Write(Creatures);

            writer.Write(TypeName);
            writer.Write(Door);
            writer.Write(Addon);
            writer.Write(Sequence);

            writer.Write((int)m_State);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        RegionBounds = reader.ReadRect2D();
                        Traps = reader.ReadEntityList<BaseTrap>();

                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 1)
                        {
                            Traps = new List<BaseTrap>();
                            RegionBounds = new Rectangle2D(X - 40, Y - 40, 80, 80);
                        }

                        Creatures = reader.ReadEntityList<Mobile>();

                        TypeName = reader.ReadString();
                        Door = reader.ReadEntity<BaseDoor>();
                        Addon = reader.ReadEntity<BaseAddon>();
                        Sequence = reader.ReadEntity<GauntletSpawner>();

                        m_State = (GauntletSpawnerState)reader.ReadInt();

                        break;
                    }
            }

            Timer.DelayCall(() => Update(false));
        }
    }
}
