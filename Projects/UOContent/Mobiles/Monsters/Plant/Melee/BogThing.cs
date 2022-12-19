using ModernUO.Serialization;
using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class BogThing : BaseCreature
    {
        [Constructible]
        public BogThing() : base(AIType.AI_Melee)
        {
            Body = 780;

            SetStr(801, 900);
            SetDex(46, 65);
            SetInt(36, 50);

            SetHits(481, 540);
            SetMana(0);

            SetDamage(10, 23);

            SetDamageType(ResistanceType.Physical, 60);
            SetDamageType(ResistanceType.Poison, 40);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 20, 25);
            SetResistance(ResistanceType.Cold, 10, 15);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 20, 25);

            SetSkill(SkillName.MagicResist, 90.1, 95.0);
            SetSkill(SkillName.Tactics, 70.1, 85.0);
            SetSkill(SkillName.Wrestling, 65.1, 80.0);

            Fame = 8000;
            Karma = -8000;

            VirtualArmor = 28;

            if (Utility.RandomDouble() < 0.25)
            {
                PackItem(new Board(10));
            }
            else
            {
                PackItem(new Log(10));
            }

            PackReg(3);
            PackItem(new Seed());
            PackItem(new Seed());
        }

        public override string CorpseName => "a plant corpse";
        public override string DefaultName => "a bog thing";

        public override bool BardImmune => !Core.AOS;
        public override Poison PoisonImmune => Poison.Lethal;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average, 2);
        }

        public void SpawnBogling(Mobile m)
        {
            var map = Map;

            if (map == null)
            {
                return;
            }

            var spawned = new Bogling { Team = Team };

            spawned.MoveToWorld(map.GetRandomNearbyLocation(Location), map);
            spawned.Combatant = m;
        }

        public void EatBoglings()
        {
            var eable = GetMobilesInRange<Bogling>(2);
            var sound = true;

            foreach (var bogling in eable)
            {
                if (Hits >= HitsMax)
                {
                    break;
                }

                if (sound)
                {
                    PlaySound(Utility.Random(0x3B, 2)); // Eat sound
                    sound = false;
                }

                Hits += bogling.Hits / 2;
                bogling.Delete();
            }

            eable.Free();
        }

        public override void OnGotMeleeAttack(Mobile attacker, int damage)
        {
            base.OnGotMeleeAttack(attacker, damage);

            if (Utility.RandomDouble() < 0.25)
            {
                if (Hits > HitsMax / 4)
                {
                    SpawnBogling(attacker);
                }
                else
                {
                    EatBoglings();
                }
            }
        }
    }
}
