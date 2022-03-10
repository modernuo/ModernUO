namespace Server.Mobiles
{
    public class Harpy : BaseCreature
    {
        [Constructible]
        public Harpy() : base(AIType.AI_Melee)
        {
            Body = 30;
            BaseSoundID = 402;

            SetStr(96, 120);
            SetDex(86, 110);
            SetInt(51, 75);

            SetHits(58, 72);

            SetDamage(5, 7);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 25, 30);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 10, 30);
            SetResistance(ResistanceType.Poison, 20, 30);
            SetResistance(ResistanceType.Energy, 10, 20);

            SetSkill(SkillName.MagicResist, 50.1, 65.0);
            SetSkill(SkillName.Tactics, 70.1, 100.0);
            SetSkill(SkillName.Wrestling, 60.1, 90.0);

            Fame = 2500;
            Karma = -2500;

            VirtualArmor = 28;
        }

        public Harpy(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a harpy corpse";
        public override string DefaultName => "a harpy";

        public override bool CanRummageCorpses => true;
        public override int Meat => 4;
        public override MeatType MeatType => MeatType.Bird;
        public override int Feathers => 50;
        public override bool CanFly => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager, 2);
        }

        public override int GetAttackSound() => 916;

        public override int GetAngerSound() => 916;

        public override int GetDeathSound() => 917;

        public override int GetHurtSound() => 919;

        public override int GetIdleSound() => 918;

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
