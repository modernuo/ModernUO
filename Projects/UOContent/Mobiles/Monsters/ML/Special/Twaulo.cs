using System;
using Server.Engines.CannedEvil;
using Server.Items;

namespace Server.Mobiles
{
    public class Twaulo : BaseChampion
    {
        [Constructible]
        public Twaulo()
            : base(AIType.AI_Melee)
        {
            Title = "of the Glade";
            Body = 101;
            BaseSoundID = 679;
            Hue = 0x455;

            SetStr(1751, 1950);
            SetDex(251, 450);
            SetInt(801, 1000);

            SetHits(7500);

            SetDamage(19, 24);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 65, 75);
            SetResistance(ResistanceType.Fire, 45, 55);
            SetResistance(ResistanceType.Cold, 50, 60);
            SetResistance(ResistanceType.Poison, 50, 60);
            SetResistance(ResistanceType.Energy, 50, 60);

            SetSkill(SkillName.EvalInt, 0);    // Per Stratics?!?
            SetSkill(SkillName.Magery, 0);     // Per Stratics?!?
            SetSkill(SkillName.Meditation, 0); // Per Stratics?!?
            SetSkill(SkillName.Anatomy, 95.1, 115.0);
            SetSkill(SkillName.Archery, 95.1, 100.0);
            SetSkill(SkillName.MagicResist, 50.3, 80.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 95.1, 100.0);

            Fame = 50000;
            Karma = 50000;

            VirtualArmor = 50;

            AddItem(new Bow());
            PackItem(new Arrow(Utility.RandomMinMax(500, 700)));
        }

        public Twaulo(Serial serial)
            : base(serial)
        {
        }

        public override string CorpseName => "a corpse of Twaulo";
        public override ChampionSkullType SkullType => ChampionSkullType.Pain;

        public override Type[] UniqueList => new[] { typeof(Quell) };
        public override Type[] SharedList => new[] { typeof(TheMostKnowledgePerson), typeof(OblivionsNeedle) };
        public override Type[] DecorativeList => new[] { typeof(Pier), typeof(MonsterStatuette) };

        public override MonsterStatuetteType[] StatueTypes => new[] { MonsterStatuetteType.DreadHorn };

        public override string DefaultName => "Twaulo";

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

        public override bool Unprovokable => true;
        public override Poison PoisonImmune => Poison.Regular;
        public override int TreasureMapLevel => 5;
        public override int Meat => 1;
        public override int Hides => 8;
        public override HideType HideType => HideType.Spined;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 2);
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Gems);
        }

        public void SpawnPixies(Mobile target)
        {
            var map = Map;

            if (map == null)
            {
                return;
            }

            var newPixies = Utility.RandomMinMax(3, 6);

            for (var i = 0; i < newPixies; ++i)
            {
                var pixie = new Pixie { Team = Team, FightMode = FightMode.Closest };

                pixie.MoveToWorld(map.GetRandomNearbyLocation(Location), map);
                pixie.Combatant = target;
            }
        }

        public override void AlterDamageScalarFrom(Mobile caster, ref double scalar)
        {
            if (Utility.RandomDouble() < 0.1)
            {
                SpawnPixies(caster);
            }
        }

        public override void OnGaveMeleeAttack(Mobile defender, int damage)
        {
            base.OnGaveMeleeAttack(defender, damage);

            defender.Damage(Utility.Random(20, 10), this);
            defender.Stam -= Utility.Random(20, 10);
            defender.Mana -= Utility.Random(20, 10);
        }

        public override void OnGotMeleeAttack(Mobile attacker, int damage)
        {
            base.OnGotMeleeAttack(attacker, damage);

            if (Utility.RandomDouble() < 0.1)
            {
                SpawnPixies(attacker);
            }
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
}
