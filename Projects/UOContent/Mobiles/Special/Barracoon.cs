using System;
using Server.Engines.CannedEvil;
using Server.Items;
using Server.Spells.Fifth;
using Server.Spells.Seventh;

namespace Server.Mobiles
{
    public class Barracoon : BaseChampion
    {
        [Constructible]
        public Barracoon() : base(AIType.AI_Melee)
        {
            Title = "the piper";
            Body = 0x190;
            Hue = 0x83EC;

            SetStr(305, 425);
            SetDex(72, 150);
            SetInt(505, 750);

            SetHits(4200);
            SetStam(102, 300);

            SetDamage(25, 35);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 60, 70);
            SetResistance(ResistanceType.Fire, 50, 60);
            SetResistance(ResistanceType.Cold, 50, 60);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.MagicResist, 100.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 97.6, 100.0);

            Fame = 22500;
            Karma = -22500;

            VirtualArmor = 70;

            AddItem(new FancyShirt(Utility.RandomGreenHue()));
            AddItem(new LongPants(Utility.RandomYellowHue()));
            AddItem(new JesterHat(Utility.RandomPinkHue()));
            AddItem(new Cloak(Utility.RandomPinkHue()));
            AddItem(new Sandals());

            HairItemID = 0x203B; // Short Hair
            HairHue = 0x94;
        }

        public Barracoon(Serial serial) : base(serial)
        {
        }

        public override ChampionSkullType SkullType => ChampionSkullType.Greed;

        public override Type[] UniqueList => new[] { typeof(FangOfRactus) };

        public override Type[] SharedList => new[]
        {
            typeof(EmbroideredOakLeafCloak),
            typeof(DjinnisRing),
            typeof(DetectiveBoots),
            typeof(GuantletsOfAnger)
        };

        public override Type[] DecorativeList => new[] { typeof(SwampTile), typeof(MonsterStatuette) };

        public override MonsterStatuetteType[] StatueTypes => new[] { MonsterStatuetteType.Slime };

        public override string DefaultName => "Barracoon";

        public override bool AlwaysMurderer => true;
        public override bool AutoDispel => true;
        public override double AutoDispelChance => 1.0;
        public override bool BardImmune => !Core.SE;
        public override bool Unprovokable => Core.SE;
        public override bool Uncalmable => Core.SE;
        public override Poison PoisonImmune => Poison.Deadly;

        public override bool ShowFameTitle => false;
        public override bool ClickTitle => false;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 3);
        }

        public void Polymorph(Mobile m)
        {
            if (!m.CanBeginAction<PolymorphSpell>() || !m.CanBeginAction<IncognitoSpell>() || m.IsBodyMod)
            {
                return;
            }

            var mount = m.Mount;

            if (mount != null)
            {
                mount.Rider = null;
            }

            if (m.Mounted)
            {
                return;
            }

            if (m.BeginAction<PolymorphSpell>())
            {
                var disarm = m.FindItemOnLayer(Layer.OneHanded);

                if (disarm?.Movable == true)
                {
                    m.AddToBackpack(disarm);
                }

                disarm = m.FindItemOnLayer(Layer.TwoHanded);

                if (disarm?.Movable == true)
                {
                    m.AddToBackpack(disarm);
                }

                m.BodyMod = 42;
                m.HueMod = 0;

                new ExpirePolymorphTimer(m).Start();
            }
        }

        public void SpawnRatmen(Mobile target)
        {
            var map = Map;

            if (map == null)
            {
                return;
            }

            var eable = GetMobilesInRange<BaseCreature>(10);
            var rats = 0;

            foreach (var m in eable)
            {
                if (m is Ratman or RatmanArcher or RatmanMage)
                {
                    rats++;
                    if (rats >= 16)
                    {
                        eable.Free();
                        return;
                    }
                }
            }

            eable.Free();

            PlaySound(0x3D);

            rats = Utility.RandomMinMax(3, 6);

            for (var i = 0; i < rats; ++i)
            {
                var rat = Utility.Random(5) switch
                {
                    2 => (BaseCreature)new RatmanArcher(),
                    3 => new RatmanArcher(),
                    4 => new RatmanMage(),
                    _ => new Ratman()
                };

                rat.Team = Team;
                rat.MoveToWorld(map.GetRandomNearbyLocation(Location), map);
                rat.Combatant = target;
            }
        }

        public void DoSpecialAbility(Mobile target)
        {
            if (target?.Deleted != false) // sanity
            {
                return;
            }

            if (Utility.RandomDouble() < 0.6) // 60% chance to polymorph attacker into a ratman
            {
                Polymorph(target);
            }

            if (Utility.RandomDouble() < 0.2) // 20% chance to spawn more ratmen
            {
                SpawnRatmen(target);
            }

            if (Hits < 500 && !IsBodyMod) // Baracoon is low on life, polymorph into a ratman
            {
                Polymorph(this);
            }
        }

        public override void OnGotMeleeAttack(Mobile attacker, int damage)
        {
            base.OnGotMeleeAttack(attacker, damage);

            DoSpecialAbility(attacker);
        }

        public override void OnGaveMeleeAttack(Mobile defender, int damage)
        {
            base.OnGaveMeleeAttack(defender, damage);

            DoSpecialAbility(defender);
        }

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

        private class ExpirePolymorphTimer : Timer
        {
            private readonly Mobile m_Owner;

            public ExpirePolymorphTimer(Mobile owner) : base(TimeSpan.FromMinutes(3.0))
            {
                m_Owner = owner;
            }

            protected override void OnTick()
            {
                if (!m_Owner.CanBeginAction<PolymorphSpell>())
                {
                    m_Owner.BodyMod = 0;
                    m_Owner.HueMod = -1;
                    m_Owner.EndAction<PolymorphSpell>();
                }
            }
        }
    }
}
