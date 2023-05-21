using ModernUO.Serialization;
using System;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class DemonKnight : BaseCreature
    {
        private static bool m_InHere;

        [Constructible]
        public DemonKnight() : base(AIType.AI_Mage)
        {
            Name = NameList.RandomName("demon knight");
            Title = "the Dark Father";
            Body = 318;
            BaseSoundID = 0x165;

            SetStr(500);
            SetDex(100);
            SetInt(1000);

            SetHits(30000);
            SetMana(5000);

            SetDamage(17, 21);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Fire, 20);
            SetDamageType(ResistanceType.Cold, 20);
            SetDamageType(ResistanceType.Poison, 20);
            SetDamageType(ResistanceType.Energy, 20);

            SetResistance(ResistanceType.Physical, 30);
            SetResistance(ResistanceType.Fire, 30);
            SetResistance(ResistanceType.Cold, 30);
            SetResistance(ResistanceType.Poison, 30);
            SetResistance(ResistanceType.Energy, 30);

            SetSkill(SkillName.Necromancy, 120, 120.0);
            SetSkill(SkillName.SpiritSpeak, 120.0, 120.0);

            SetSkill(SkillName.DetectHidden, 80.0);
            SetSkill(SkillName.EvalInt, 100.0);
            SetSkill(SkillName.Magery, 100.0);
            SetSkill(SkillName.Meditation, 120.0);
            SetSkill(SkillName.MagicResist, 150.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 120.0);

            Fame = 28000;
            Karma = -28000;

            VirtualArmor = 64;
        }

        public override string CorpseName => "a demon knight corpse";
        public override bool IgnoreYoungProtection => Core.ML;

        public static Type[] ArtifactRarity10 { get; } =
        {
            typeof(LegacyOfTheDreadLord),
            typeof(TheTaskmaster)
        };

        public static Type[] ArtifactRarity11 { get; } =
        {
            typeof(TheDragonSlayer),
            typeof(ArmorOfFortune),
            typeof(GauntletsOfNobility),
            typeof(HelmOfInsight),
            typeof(HolyKnightsBreastplate),
            typeof(JackalsCollar),
            typeof(LeggingsOfBane),
            typeof(MidnightBracers),
            typeof(OrnateCrownOfTheHarrower),
            typeof(ShadowDancerLeggings),
            typeof(TunicOfFire),
            typeof(VoiceOfTheFallenKing),
            typeof(BraceletOfHealth),
            typeof(OrnamentOfTheMagician),
            typeof(RingOfTheElements),
            typeof(RingOfTheVile),
            typeof(Aegis),
            typeof(ArcaneShield),
            typeof(AxeOfTheHeavens),
            typeof(BladeOfInsanity),
            typeof(BoneCrusher),
            typeof(BreathOfTheDead),
            typeof(Frostbringer),
            typeof(SerpentsFang),
            typeof(StaffOfTheMagi),
            typeof(TheBeserkersMaul),
            typeof(TheDryadBow),
            typeof(DivineCountenance),
            typeof(HatOfTheMagi),
            typeof(HuntersHeaddress),
            typeof(SpiritOfTheTotem)
        };

        public override bool BardImmune => !Core.SE;
        public override bool Unprovokable => Core.SE;
        public override bool AreaPeaceImmune => Core.SE;
        public override Poison PoisonImmune => Poison.Lethal;

        public override int TreasureMapLevel => 1;

        public static Item CreateRandomArtifact()
        {
            if (!Core.AOS)
            {
                return null;
            }

            var count = ArtifactRarity10.Length * 5 + ArtifactRarity11.Length * 4;
            var random = Utility.Random(count);
            Type type;

            if (random < ArtifactRarity10.Length * 5)
            {
                type = ArtifactRarity10[random / 5];
            }
            else
            {
                random -= ArtifactRarity10.Length * 5;
                type = ArtifactRarity11[random / 4];
            }

            return Loot.Construct(type);
        }

        public static Mobile FindRandomPlayer(BaseCreature creature)
        {
            var rights = GetLootingRights(creature.DamageEntries, creature.HitsMax);

            for (var i = rights.Count - 1; i >= 0; --i)
            {
                var ds = rights[i];

                if (!ds.m_HasRight)
                {
                    rights.RemoveAt(i);
                }
            }

            return rights.RandomElement()?.m_Mobile;
        }

        public static void DistributeArtifact(BaseCreature creature)
        {
            DistributeArtifact(creature, CreateRandomArtifact());
        }

        public static void DistributeArtifact(BaseCreature creature, Item artifact)
        {
            DistributeArtifact(FindRandomPlayer(creature), artifact);
        }

        public static void DistributeArtifact(Mobile to)
        {
            DistributeArtifact(to, CreateRandomArtifact());
        }

        public static void DistributeArtifact(Mobile to, Item artifact)
        {
            if (to == null || artifact == null)
            {
                return;
            }

            var pack = to.Backpack;

            if (pack?.TryDropItem(to, artifact, false) != true)
            {
                to.BankBox.DropItem(artifact);
            }

            to.SendLocalizedMessage(
                1062317
            ); // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
        }

        public static int GetArtifactChance(Mobile boss)
        {
            if (!Core.AOS)
            {
                return 0;
            }

            var luck = LootPack.GetLuckChanceForKiller(boss);

            return boss switch
            {
                DemonKnight => 1500 + luck / 5,
                _           => 750 + luck / 10
            };
        }

        public static bool CheckArtifactChance(Mobile boss) => GetArtifactChance(boss) > Utility.Random(100000);

        public override WeaponAbility GetWeaponAbility()
        {
            return Utility.Random(3) switch
            {
                0 => WeaponAbility.DoubleStrike,
                1 => WeaponAbility.WhirlwindAttack,
                2 => WeaponAbility.CrushingBlow,
                _ => WeaponAbility.DoubleStrike
            };
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (!Summoned && !NoKillAwards && CheckArtifactChance(this))
            {
                DistributeArtifact(this);
            }
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.SuperBoss, 2);
            AddLoot(LootPack.HighScrolls, Utility.RandomMinMax(6, 60));
        }

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            if (from != null && from != this && !m_InHere)
            {
                m_InHere = true;
                AOS.Damage(from, this, Utility.RandomMinMax(8, 20), 100, 0, 0, 0, 0);

                MovingEffect(from, 0xECA, 10, 0, false, false, 0, 0);
                PlaySound(0x491);

                if (Utility.RandomDouble() < 0.05)
                {
                    Timer.StartTimer(TimeSpan.FromSeconds(1.0), () => CreateBones_Callback(from));
                }

                m_InHere = false;
            }
        }

        public virtual void CreateBones_Callback(Mobile from)
        {
            var map = from.Map;

            if (map == null)
            {
                return;
            }

            var count = Utility.RandomMinMax(1, 3);

            for (var i = 0; i < count; ++i)
            {
                var x = from.X + Utility.RandomMinMax(-1, 1);
                var y = from.Y + Utility.RandomMinMax(-1, 1);
                var z = from.Z;

                if (!map.CanFit(x, y, z, 16))
                {
                    z = map.GetAverageZ(x, y);

                    if (z == from.Z || !map.CanFit(x, y, z, 16))
                    {
                        continue;
                    }
                }

                var bone = new UnholyBone
                {
                    Hue = 0,
                    Name = "unholy bones",
                    ItemID = Utility.Random(0xECA, 9)
                };

                bone.MoveToWorld(new Point3D(x, y, z), map);
            }
        }
    }
}
