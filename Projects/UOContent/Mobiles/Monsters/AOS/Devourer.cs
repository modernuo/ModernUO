namespace Server.Mobiles
{
    public class Devourer : BaseCreature
    {
        [Constructible]
        public Devourer() : base(AIType.AI_Mage)
        {
            Body = 303;
            BaseSoundID = 357;

            SetStr(801, 950);
            SetDex(126, 175);
            SetInt(201, 250);

            SetHits(650);

            SetDamage(22, 26);

            SetDamageType(ResistanceType.Physical, 60);
            SetDamageType(ResistanceType.Cold, 20);
            SetDamageType(ResistanceType.Energy, 20);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 25, 35);
            SetResistance(ResistanceType.Cold, 15, 25);
            SetResistance(ResistanceType.Poison, 60, 70);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.EvalInt, 90.1, 100.0);
            SetSkill(SkillName.Magery, 90.1, 100.0);
            SetSkill(SkillName.Meditation, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 90.1, 105.0);
            SetSkill(SkillName.Tactics, 75.1, 85.0);
            SetSkill(SkillName.Wrestling, 80.1, 100.0);

            Fame = 9500;
            Karma = -9500;

            VirtualArmor = 44;

            PackNecroReg(24, 45);
        }

        public Devourer(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a devourer of souls corpse";
        public override string DefaultName => "a devourer of souls";

        public override Poison PoisonImmune => Poison.Lethal;

        public override int Meat => 3;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 2);
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
