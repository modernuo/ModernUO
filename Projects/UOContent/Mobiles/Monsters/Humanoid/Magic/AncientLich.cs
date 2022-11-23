namespace Server.Mobiles
{
    public class AncientLich : BaseCreature
    {
        [Constructible]
        public AncientLich() : base(AIType.AI_Mage)
        {
            Name = NameList.RandomName("ancient lich");
            Body = 78;
            BaseSoundID = 412;

            SetStr(216, 305);
            SetDex(96, 115);
            SetInt(966, 1045);

            SetHits(560, 595);

            SetDamage(15, 27);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Cold, 40);
            SetDamageType(ResistanceType.Energy, 40);

            SetResistance(ResistanceType.Physical, 55, 65);
            SetResistance(ResistanceType.Fire, 25, 30);
            SetResistance(ResistanceType.Cold, 50, 60);
            SetResistance(ResistanceType.Poison, 50, 60);
            SetResistance(ResistanceType.Energy, 25, 30);

            SetSkill(SkillName.EvalInt, 120.1, 130.0);
            SetSkill(SkillName.Magery, 120.1, 130.0);
            SetSkill(SkillName.Meditation, 100.1, 101.0);
            SetSkill(SkillName.Poisoning, 100.1, 101.0);
            SetSkill(SkillName.MagicResist, 175.2, 200.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 75.1, 100.0);
            SetSkill(SkillName.Necromancy, 120.1, 130.0);
            SetSkill(SkillName.SpiritSpeak, 120.1, 130.0);

            Fame = 23000;
            Karma = -23000;

            VirtualArmor = 60;
            PackNecroReg(30, 275);
        }

        public AncientLich(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an ancient lich's corpse";

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

        public override bool Unprovokable => true;
        public override bool BleedImmune => true;
        public override Poison PoisonImmune => Poison.Lethal;
        public override int TreasureMapLevel => 5;

        public override int GetIdleSound() => 0x19D;

        public override int GetAngerSound() => 0x175;

        public override int GetDeathSound() => 0x108;

        public override int GetAttackSound() => 0xE2;

        public override int GetHurtSound() => 0x28B;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 3);
            AddLoot(LootPack.MedScrolls, 2);
        }

        private static MonsterAbility[] _abilities = { MonsterAbilities.SummonLesserUndeadCounter };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

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
