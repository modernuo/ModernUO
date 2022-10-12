using Server.Items;

namespace Server.Mobiles
{
    public class LichLord : BaseCreature
    {
        [Constructible]
        public LichLord() : base(AIType.AI_Mage)
        {
            Body = 79;
            BaseSoundID = 412;

            SetStr(416, 505);
            SetDex(146, 165);
            SetInt(566, 655);

            SetHits(250, 303);

            SetDamage(11, 13);

            SetDamageType(ResistanceType.Physical, 0);
            SetDamageType(ResistanceType.Cold, 60);
            SetDamageType(ResistanceType.Energy, 40);

            SetResistance(ResistanceType.Physical, 40, 50);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 50, 60);
            SetResistance(ResistanceType.Poison, 50, 60);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.Necromancy, 90, 110.0);
            SetSkill(SkillName.SpiritSpeak, 90.0, 110.0);

            SetSkill(SkillName.EvalInt, 90.1, 100.0);
            SetSkill(SkillName.Magery, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 150.5, 200.0);
            SetSkill(SkillName.Tactics, 50.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 18000;
            Karma = -18000;

            VirtualArmor = 50;
            PackItem(new GnarledStaff());
            PackNecroReg(12, 40);
        }

        public LichLord(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a lich's corpse";
        public override string DefaultName => "a lich lord";

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

        public override bool CanRummageCorpses => true;
        public override bool BleedImmune => true;
        public override Poison PoisonImmune => Poison.Lethal;
        public override int TreasureMapLevel => 4;

        private static MonsterAbility[] _abilities = { MonsterAbility.SummonSkeletons };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
            AddLoot(LootPack.MedScrolls, 2);
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
