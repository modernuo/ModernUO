using System;
using System.Collections.Generic;
using Server.Engines.CannedEvil;
using Server.Items;
using Server.Network;

namespace Server.Mobiles
{
    public class Ilhenir : BaseChampion
    {
        private static readonly HashSet<Mobile> m_Table = new();

        [Constructible]
        public Ilhenir()
            : base(AIType.AI_Mage)
        {
            Title = "the Stained";
            Body = 0x103;

            BaseSoundID = 589;

            SetStr(1105, 1350);
            SetDex(82, 160);
            SetInt(505, 750);

            SetHits(9000);

            SetDamage(21, 28);

            SetDamageType(ResistanceType.Physical, 60);
            SetDamageType(ResistanceType.Fire, 20);
            SetDamageType(ResistanceType.Poison, 20);

            SetResistance(ResistanceType.Physical, 55, 65);
            SetResistance(ResistanceType.Fire, 50, 60);
            SetResistance(ResistanceType.Cold, 55, 65);
            SetResistance(ResistanceType.Poison, 70, 90);
            SetResistance(ResistanceType.Energy, 65, 75);

            SetSkill(SkillName.EvalInt, 100);
            SetSkill(SkillName.Magery, 100);
            SetSkill(SkillName.Meditation, 0);
            SetSkill(SkillName.Poisoning, 5.4);
            SetSkill(SkillName.Anatomy, 117.5);
            SetSkill(SkillName.MagicResist, 120.0);
            SetSkill(SkillName.Tactics, 119.9);
            SetSkill(SkillName.Wrestling, 119.9);

            Fame = 50000;
            Karma = -50000;

            VirtualArmor = 44;

            if (Core.ML)
            {
                PackResources(8);
                PackTalismans(5);
            }
        }

        public Ilhenir(Serial serial)
            : base(serial)
        {
        }

        public override string CorpseName => "a corpse of Ilhenir";
        public override ChampionSkullType SkullType => ChampionSkullType.Pain;

        public override Type[] UniqueList => Array.Empty<Type>();

        public override Type[] SharedList => new[]
        {
            typeof(ANecromancerShroud),
            typeof(LieutenantOfTheBritannianRoyalGuard),
            typeof(OblivionsNeedle),
            typeof(TheRobeOfBritanniaAri)
        };

        public override Type[] DecorativeList => new[] { typeof(MonsterStatuette) };

        public override MonsterStatuetteType[] StatueTypes => new[]
        {
            MonsterStatuetteType.PlagueBeast,
            MonsterStatuetteType.RedDeath
        };

        public override string DefaultName => "Ilhenir";

        public override bool Unprovokable => true;
        public override bool Uncalmable => true;

        public override Poison PoisonImmune => Poison.Lethal;

        // public override bool GivesMLMinorArtifact => true; // TODO: Needs verification
        public override int TreasureMapLevel => 5;

        public virtual void PackResources(int amount)
        {
            for (var i = 0; i < amount; i++)
            {
                PackItem(
                    Utility.Random(6) switch
                    {
                        0 => new Blight(),
                        1 => new Scourge(),
                        2 => new Taint(),
                        3 => new Putrefication(),
                        4 => new Corruption(),
                        _ => new Muculent() // 5
                    }
                );
            }
        }

        public virtual void PackItems(Item item, int amount)
        {
            for (var i = 0; i < amount; i++)
            {
                PackItem(item);
            }
        }

        public virtual void PackTalismans(int amount)
        {
            var count = Utility.Random(amount);

            for (var i = 0; i < count; i++)
            {
                PackItem(new RandomTalisman());
            }
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 8);
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (Core.ML)
            {
                c.DropItem(new GrizzledBones());

                // TODO: Parrots
                /*if (Utility.RandomDouble() < 0.6)
                  c.DropItem( new ParrotItem() ); */

                if (Utility.RandomDouble() < 0.05)
                {
                    c.DropItem(new GrizzledMareStatuette());
                }

                if (Utility.RandomDouble() < 0.025)
                {
                    c.DropItem(new CrimsonCincture());
                }

                // TODO: Armor sets
                /*if (Utility.RandomDouble() < 0.05)
                {
                  switch ( Utility.Random(5) )
                  {
                    case 0: c.DropItem( new GrizzleGauntlets() ); break;
                    case 1: c.DropItem( new GrizzleGreaves() ); break;
                    case 2: c.DropItem( new GrizzleHelm() ); break;
                    case 3: c.DropItem( new GrizzleTunic() ); break;
                    case 4: c.DropItem( new GrizzleVambraces() ); break;
                  }
                }*/
            }
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (Utility.RandomDouble() < 0.25)
            {
                CacophonicAttack(defender);
            }
        }

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            if (Utility.RandomDouble() < 0.1)
            {
                DropOoze();
            }

            base.OnDamage(amount, from, willKill);
        }

        public override int GetAngerSound() => 0x581;

        public override int GetIdleSound() => 0x582;

        public override int GetAttackSound() => 0x580;

        public override int GetHurtSound() => 0x583;

        public override int GetDeathSound() => 0x584;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        public virtual void CacophonicAttack(Mobile to)
        {
            if (to.Alive && to.Player && !UnderCacophonicAttack(to))
            {
                to.NetState.SendSpeedControl(SpeedControlSetting.Walk);
                to.SendLocalizedMessage(1072069); // A cacophonic sound lambastes you, suppressing your ability to move.
                to.PlaySound(0x584);

                m_Table.Add(to);
                Timer.StartTimer(TimeSpan.FromSeconds(30),
                    () =>
                    {
                        m_Table.Remove(to);
                        to.NetState.SendSpeedControl(SpeedControlSetting.Disable);
                    }
                );
            }
        }

        public static bool UnderCacophonicAttack(Mobile from) => m_Table.Contains(from);

        public virtual void DropOoze()
        {
            var amount = Utility.RandomMinMax(1, 3);
            var corrosive = Utility.RandomBool();

            for (var i = 0; i < amount; i++)
            {
                Item ooze = new StainedOoze(corrosive);
                var p = new Point3D(Location);

                for (var j = 0; j < 5; j++)
                {
                    p = GetSpawnPosition(2);

                    var eable = Map.GetItemsInRange<StainedOoze>(p, 0);
                    using var enumerator = eable.GetEnumerator();
                    bool atLocation = enumerator.MoveNext();
                    eable.Free();
                    if (!atLocation)
                    {
                        break;
                    }
                }

                ooze.MoveToWorld(p, Map);
            }

            if (Combatant != null)
            {
                if (corrosive)
                {
                    Combatant.SendLocalizedMessage(1072071); // A corrosive gas seeps out of your enemy's skin!
                }
                else
                {
                    Combatant.SendLocalizedMessage(1072072); // A poisonous gas seeps out of your enemy's skin!
                }
            }
        }

        private int RandomPoint(int mid) => mid + Utility.RandomMinMax(-2, 2);

        public virtual Point3D GetSpawnPosition(int range) => GetSpawnPosition(Location, Map, range);

        public virtual Point3D GetSpawnPosition(Point3D from, Map map, int range)
        {
            if (map == null)
            {
                return from;
            }

            var loc = new Point3D(RandomPoint(X), RandomPoint(Y), Z);

            loc.Z = Map.GetAverageZ(loc.X, loc.Y);

            return loc;
        }
    }

    public class StainedOoze : Item
    {
        private int m_Ticks;
        private TimerExecutionToken _timerToken;

        [Constructible]
        public StainedOoze(bool corrosive = false) : base(0x122A)
        {
            Movable = false;
            Hue = 0x95;

            Corrosive = corrosive;
            Timer.StartTimer(TimeSpan.Zero, TimeSpan.FromSeconds(1), OnTick, out _timerToken);
            m_Ticks = 0;
        }

        public StainedOoze(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Corrosive { get; set; }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();
            _timerToken.Cancel();
        }

        private void OnTick()
        {
            var toDamage = new List<Mobile>();

            foreach (var m in GetMobilesInRange(0))
            {
                if (m is BaseCreature bc)
                {
                    if (!bc.Controlled && !bc.Summoned)
                    {
                        continue;
                    }
                }
                else if (!m.Player)
                {
                    continue;
                }

                if (m.Alive && !m.IsDeadBondedPet && m.CanBeDamaged())
                {
                    toDamage.Add(m);
                }
            }

            for (var i = 0; i < toDamage.Count; ++i)
            {
                Damage(toDamage[i]);
            }

            ++m_Ticks;

            if (m_Ticks >= 35)
            {
                Delete();
            }
            else if (m_Ticks == 30)
            {
                ItemID = 0x122B;
            }
        }

        public void Damage(Mobile m)
        {
            if (Corrosive)
            {
                var items = m.Items;
                var damaged = false;

                for (var i = 0; i < items.Count; ++i)
                {
                    if (items[i] is IDurability wearable && wearable.HitPoints >= 10 && Utility.RandomDouble() < 0.25)
                    {
                        wearable.HitPoints -= wearable.HitPoints == 10 ? Utility.Random(1, 5) : 10;
                        damaged = true;
                    }
                }

                if (damaged)
                {
                    m.LocalOverheadMessage(
                        MessageType.Regular,
                        0x21,
                        1072070 // The infernal ooze scorches you, setting you and your equipment ablaze!
                    );
                    return;
                }
            }

            AOS.Damage(m, 40, 0, 0, 0, 100, 0);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(Corrosive);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Corrosive = reader.ReadBool();

            Timer.StartTimer(TimeSpan.Zero, TimeSpan.FromSeconds(1), OnTick, out _timerToken);
            m_Ticks = ItemID == 0x122A ? 0 : 30;
        }
    }
}
