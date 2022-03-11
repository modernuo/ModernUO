namespace Server.Mobiles
{
    public class OphidianMatriarch : BaseCreature
    {
        [Constructible]
        public OphidianMatriarch() : base(AIType.AI_Mage)
        {
            Body = 87;
            BaseSoundID = 644;

            SetStr(416, 505);
            SetDex(96, 115);
            SetInt(366, 455);

            SetHits(250, 303);

            SetDamage(11, 13);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 35, 45);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 35, 45);

            SetSkill(SkillName.EvalInt, 90.1, 100.0);
            SetSkill(SkillName.Magery, 90.1, 100.0);
            SetSkill(SkillName.Meditation, 5.4, 25.0);
            SetSkill(SkillName.MagicResist, 90.1, 100.0);
            SetSkill(SkillName.Tactics, 50.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 16000;
            Karma = -16000;

            VirtualArmor = 50;
        }

        public OphidianMatriarch(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an ophidian corpse";
        public override string DefaultName => "an ophidian matriarch";

        public override Poison PoisonImmune => Poison.Greater;
        public override int TreasureMapLevel => 4;

        public override OppositionGroup OppositionGroup => OppositionGroup.TerathansAndOphidians;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.Average, 2);
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
