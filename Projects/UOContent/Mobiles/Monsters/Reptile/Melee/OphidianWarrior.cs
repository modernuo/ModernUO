namespace Server.Mobiles
{
    public class OphidianWarrior : BaseCreature
    {
        private static readonly string[] m_Names =
        {
            "an ophidian warrior",
            "an ophidian enforcer"
        };

        [Constructible]
        public OphidianWarrior() : base(AIType.AI_Melee)
        {
            Name = m_Names.RandomElement();
            Body = 86;
            BaseSoundID = 634;

            SetStr(150, 320);
            SetDex(94, 190);
            SetInt(64, 160);

            SetHits(128, 155);
            SetMana(0);

            SetDamage(5, 11);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 35, 40);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 25, 35);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 25, 35);

            SetSkill(SkillName.MagicResist, 70.1, 85.0);
            SetSkill(SkillName.Swords, 60.1, 85.0);
            SetSkill(SkillName.Tactics, 75.1, 90.0);

            Fame = 4500;
            Karma = -4500;

            VirtualArmor = 36;
        }

        public OphidianWarrior(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an ophidian corpse";

        public override int Meat => 1;
        public override int TreasureMapLevel => 1;

        public override OppositionGroup OppositionGroup => OppositionGroup.TerathansAndOphidians;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Gems);
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
