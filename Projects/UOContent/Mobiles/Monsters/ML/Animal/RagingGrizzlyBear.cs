namespace Server.Mobiles
{
    public class RagingGrizzlyBear : BaseCreature
    {
        [Constructible]
        public RagingGrizzlyBear() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 212;
            BaseSoundID = 0xA3;

            SetStr(1251, 1550);
            SetDex(801, 1050);
            SetInt(151, 400);

            SetHits(751, 930);
            SetMana(0);

            SetDamage(18, 23);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 50, 70);
            SetResistance(ResistanceType.Cold, 30, 50);
            SetResistance(ResistanceType.Poison, 10, 20);
            SetResistance(ResistanceType.Energy, 10, 20);

            SetSkill(SkillName.Wrestling, 73.4, 88.1);
            SetSkill(SkillName.Tactics, 73.6, 110.5);
            SetSkill(SkillName.MagicResist, 32.8, 54.6);
            SetSkill(SkillName.Anatomy, 0, 0);

            Fame = 10000;  // Guessing here
            Karma = 10000; // Guessing here

            VirtualArmor = 24;

            Tamable = false;
        }

        public RagingGrizzlyBear(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a grizzly bear corpse";
        public override string DefaultName => "a raging grizzly bear";

        public override int Meat => 4;
        public override int Hides => 32;
        public override PackInstinct PackInstinct => PackInstinct.Bear;

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
