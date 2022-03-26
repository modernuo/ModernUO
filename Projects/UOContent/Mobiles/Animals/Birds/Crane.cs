namespace Server.Mobiles
{
    public class Crane : BaseCreature
    {
        [Constructible]
        public Crane() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 254;
            BaseSoundID = 0x4D7;

            SetStr(26, 35);
            SetDex(16, 25);
            SetInt(11, 15);

            SetHits(26, 35);
            SetMana(0);

            SetDamage(1, 1);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 5, 5);

            SetSkill(SkillName.MagicResist, 4.1, 5.0);
            SetSkill(SkillName.Tactics, 10.1, 11.0);
            SetSkill(SkillName.Wrestling, 10.1, 11.0);

            Fame = 0;
            Karma = 200;

            VirtualArmor = 5;
        }

        public Crane(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a bird corpse";
        public override string DefaultName => "a crane";

        public override int Meat => 1;
        public override int Feathers => 25;

        public override int GetAngerSound() => 0x4D9;

        public override int GetIdleSound() => 0x4D8;

        public override int GetAttackSound() => 0x4D7;

        public override int GetHurtSound() => 0x4DA;

        public override int GetDeathSound() => 0x4D6;

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
