namespace Server.Mobiles
{
    public class VampireBat : BaseCreature
    {
        [Constructible]
        public VampireBat() : base(AIType.AI_Melee)
        {
            Body = 317;
            BaseSoundID = 0x270;

            SetStr(91, 110);
            SetDex(91, 115);
            SetInt(26, 50);

            SetHits(55, 66);

            SetDamage(7, 9);

            SetDamageType(ResistanceType.Physical, 80);
            SetDamageType(ResistanceType.Poison, 20);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 15, 25);
            SetResistance(ResistanceType.Cold, 15, 25);
            SetResistance(ResistanceType.Poison, 60, 70);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.MagicResist, 70.1, 95.0);
            SetSkill(SkillName.Tactics, 55.1, 80.0);
            SetSkill(SkillName.Wrestling, 30.1, 55.0);

            Fame = 1000;
            Karma = -1000;

            VirtualArmor = 14;
        }

        public VampireBat(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a vampire bat corpse";
        public override string DefaultName => "a vampire bat";

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Poor);
        }

        public override int GetIdleSound() => 0x29B;

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
