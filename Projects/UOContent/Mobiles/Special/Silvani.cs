namespace Server.Mobiles
{
    public class Silvani : BaseCreature
    {
        [Constructible]
        public Silvani() : base(AIType.AI_Mage, FightMode.Evil, 18)
        {
            Body = 176;
            BaseSoundID = 0x467;

            SetStr(253, 400);
            SetDex(157, 850);
            SetInt(503, 800);

            SetHits(600);

            SetDamage(27, 38);

            SetDamageType(ResistanceType.Physical, 75);
            SetDamageType(ResistanceType.Cold, 25);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.EvalInt, 100.0);
            SetSkill(SkillName.Magery, 97.6, 107.5);
            SetSkill(SkillName.Meditation, 100.0);
            SetSkill(SkillName.MagicResist, 100.5, 150.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 97.6, 100.0);

            Fame = 20000;
            Karma = 20000;

            VirtualArmor = 50;
        }

        public Silvani(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "Silvani";

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

        public override bool CanFly => true;
        public override bool Unprovokable => true;
        public override Poison PoisonImmune => Poison.Regular;
        public override int TreasureMapLevel => 5;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.UltraRich, 2);
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
            if (Utility.RandomDouble() <= 0.1)
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

            if (Utility.RandomDouble() <= 0.1)
            {
                SpawnPixies(attacker);
            }
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
    }
}
