using Server.Engines.Plants;

namespace Server.Mobiles
{
    public class SerpentineDragon : BaseCreature
    {
        [Constructible]
        public SerpentineDragon() : base(AIType.AI_Mage, FightMode.Evil)
        {
            Body = 103;
            BaseSoundID = 362;

            SetStr(111, 140);
            SetDex(201, 220);
            SetInt(1001, 1040);

            SetHits(480);

            SetDamage(5, 12);

            SetDamageType(ResistanceType.Physical, 75);
            SetDamageType(ResistanceType.Poison, 25);

            SetResistance(ResistanceType.Physical, 35, 40);
            SetResistance(ResistanceType.Fire, 25, 35);
            SetResistance(ResistanceType.Cold, 25, 35);
            SetResistance(ResistanceType.Poison, 25, 35);
            SetResistance(ResistanceType.Energy, 25, 35);

            SetSkill(SkillName.EvalInt, 100.1, 110.0);
            SetSkill(SkillName.Magery, 110.1, 120.0);
            SetSkill(SkillName.Meditation, 100.0);
            SetSkill(SkillName.MagicResist, 100.0);
            SetSkill(SkillName.Tactics, 50.1, 60.0);
            SetSkill(SkillName.Wrestling, 30.1, 100.0);

            Fame = 15000;
            Karma = 15000;

            VirtualArmor = 36;

            if (Core.ML && Utility.RandomDouble() < .33)
            {
                PackItem(Seed.RandomPeculiarSeed(2));
            }
        }

        public SerpentineDragon(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a dragon corpse";
        public override string DefaultName => "a serpentine dragon";
        public override bool ReacquireOnMovement => true;
        public override double BonusPetDamageScalar => Core.SE ? 3.0 : 1.0;

        public override bool AutoDispel => true;
        public override HideType HideType => HideType.Barbed;
        public override int Hides => 20;
        public override int Meat => 19;
        public override int Scales => 6;
        public override ScaleType ScaleType => Utility.RandomBool() ? ScaleType.Black : ScaleType.White;
        public override int TreasureMapLevel => 4;

        private static MonsterAbility[] _abilities = { MonsterAbilities.FireBreath };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 2);
            AddLoot(LootPack.Gems, 2);
        }

        public override int GetIdleSound() => 0x2C4;

        public override int GetAttackSound() => 0x2C0;

        public override int GetDeathSound() => 0x2C1;

        public override int GetAngerSound() => 0x2C4;

        public override int GetHurtSound() => 0x2C3;

        public override void OnGotMeleeAttack(Mobile attacker, int damage)
        {
            base.OnGotMeleeAttack(attacker, damage);

            if (!Core.SE && Utility.RandomDouble() < 0.2 && attacker is BaseCreature c && c.Controlled &&
                c.ControlMaster != null)
            {
                c.ControlTarget = c.ControlMaster;
                c.ControlOrder = OrderType.Attack;
                c.Combatant = c.ControlMaster;
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
