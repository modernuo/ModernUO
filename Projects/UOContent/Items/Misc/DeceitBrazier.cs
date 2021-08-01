using System;
using Server.Mobiles;
using Server.Network;
using Server.Utilities;

namespace Server.Items
{
    public class DeceitBrazier : Item
    {
        private TimerExecutionToken _timerToken;

        [Constructible]
        public DeceitBrazier() : base(0xE31)
        {
            Movable = false;
            Light = LightType.Circle225;
            NextSpawn = Core.Now;
            NextSpawnDelay = TimeSpan.FromMinutes(15.0);
            SpawnRange = 5;
        }

        public DeceitBrazier(Serial serial) : base(serial)
        {
        }

        public static Type[] Creatures { get; } =
        {
            typeof(FireSteed), // Set the tents up people!

            typeof(Skeleton), typeof(SkeletalKnight), typeof(SkeletalMage), typeof(Mummy),
            typeof(BoneKnight), typeof(Lich), typeof(LichLord), typeof(BoneMagi),
            typeof(Wraith), typeof(Shade), typeof(Spectre), typeof(Zombie),
            typeof(RottingCorpse), typeof(Ghoul), typeof(Balron), typeof(Daemon), typeof(Imp), typeof(GreaterMongbat),
            typeof(Mongbat), typeof(IceFiend), typeof(Gargoyle), typeof(StoneGargoyle),
            typeof(FireGargoyle), typeof(HordeMinion), typeof(Gazer), typeof(ElderGazer), typeof(GazerLarva), typeof(Harpy),
            typeof(StoneHarpy), typeof(HeadlessOne), typeof(HellHound),
            typeof(HellCat), typeof(Phoenix), typeof(LavaLizard), typeof(SandVortex),
            typeof(ShadowWisp), typeof(SwampTentacle), typeof(PredatorHellCat), typeof(Wisp), typeof(GiantSpider),
            typeof(DreadSpider), typeof(FrostSpider), typeof(Scorpion), typeof(ArcticOgreLord), typeof(Cyclops),
            typeof(Ettin), typeof(EvilMage),
            typeof(FrostTroll), typeof(Ogre), typeof(OgreLord), typeof(Orc),
            typeof(OrcishLord), typeof(OrcishMage), typeof(OrcBrute), typeof(Ratman),
            typeof(RatmanMage), typeof(OrcCaptain), typeof(Troll), typeof(Titan),
            typeof(EvilMageLord), typeof(OrcBomber), typeof(RatmanArcher), typeof(Dragon), typeof(Drake), typeof(Snake),
            typeof(GreaterDragon),
            typeof(IceSerpent), typeof(GiantSerpent), typeof(IceSnake), typeof(LavaSerpent),
            typeof(Lizardman), typeof(Wyvern), typeof(WhiteWyrm),
            typeof(ShadowWyrm), typeof(SilverSerpent), typeof(LavaSnake), typeof(EarthElemental), typeof(PoisonElemental),
            typeof(FireElemental), typeof(SnowElemental),
            typeof(IceElemental), typeof(AcidElemental), typeof(WaterElemental), typeof(Efreet),
            typeof(AirElemental), typeof(Golem), typeof(SewerRat), typeof(GiantRat), typeof(DireWolf), typeof(TimberWolf),
            typeof(Cougar), typeof(Alligator)
        };

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextSpawn { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpawnRange { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan NextSpawnDelay { get; set; }

        public override int LabelNumber => 1023633; // Brazier

        public override bool HandlesOnMovement => true;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(SpawnRange);
            writer.Write(NextSpawnDelay);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version >= 0)
            {
                SpawnRange = reader.ReadInt();
                NextSpawnDelay = reader.ReadTimeSpan();
            }

            NextSpawn = Core.Now;
        }

        public virtual void HeedWarning()
        {
            PublicOverheadMessage(
                MessageType.Regular,
                0x3B2,
                500761 // Heed this warning well, and use this brazier at your own peril.
            );

            _timerToken.Cancel();
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            // means we haven't spawned anything if the next spawn is below
            if (NextSpawn < Core.Now &&
                Utility.InRange(m.Location, Location, 1) &&
                !Utility.InRange(oldLocation, Location, 1) &&
                m.Player && !(m.AccessLevel > AccessLevel.Player || m.Hidden) && !_timerToken.Running)
            {
                Timer.StartTimer(TimeSpan.FromSeconds(2), HeedWarning, out _timerToken);
            }

            base.OnMovement(m, oldLocation);
        }

        public Point3D GetSpawnPosition()
        {
            var map = Map;

            if (map == null)
            {
                return Location;
            }

            // Try 10 times to find a Spawnable location.
            for (var i = 0; i < 10; i++)
            {
                var x = Location.X + (Utility.Random(SpawnRange * 2 + 1) - SpawnRange);
                var y = Location.Y + (Utility.Random(SpawnRange * 2 + 1) - SpawnRange);
                var z = Map.GetAverageZ(x, y);

                if (Map.CanSpawnMobile(new Point2D(x, y), Z))
                {
                    return new Point3D(x, y, Z);
                }

                if (Map.CanSpawnMobile(new Point2D(x, y), z))
                {
                    return new Point3D(x, y, z);
                }
            }

            return Location;
        }

        public virtual void DoEffect(Point3D loc, Map map)
        {
            Effects.SendLocationParticles(EffectItem.Create(loc, map, EffectItem.DefaultDuration), 0x3709, 10, 30, 5052);
            Effects.PlaySound(loc, map, 0x225);
        }

        private void SummonCreatureToWorld(BaseCreature bc, Point3D spawnLoc, Map map)
        {
            bc.Home = Location;
            bc.RangeHome = SpawnRange;
            bc.FightMode = FightMode.Closest;

            bc.MoveToWorld(spawnLoc, map);

            DoEffect(spawnLoc, map);

            bc.ForceReacquire();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Utility.InRange(from.Location, Location, 2))
            {
                try
                {
                    if (NextSpawn < Core.Now)
                    {
                        var map = Map;
                        var bc = Creatures.RandomElement().CreateInstance<BaseCreature>();

                        var spawnLoc = GetSpawnPosition();

                        DoEffect(spawnLoc, map);

                        Timer.StartTimer(TimeSpan.FromSeconds(1), () => SummonCreatureToWorld(bc, spawnLoc, map));

                        NextSpawn = Core.Now + NextSpawnDelay;
                    }
                    else
                    {
                        PublicOverheadMessage(
                            MessageType.Regular,
                            0x3B2,
                            500760
                        ); // The brazier fizzes and pops, but nothing seems to happen.
                    }
                }
                catch
                {
                    // ignored
                }
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }
    }
}
