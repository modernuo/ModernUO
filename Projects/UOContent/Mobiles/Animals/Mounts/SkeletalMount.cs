namespace Server.Mobiles
{
    public class SkeletalMount : BaseMount
    {
        [Constructible]
        public SkeletalMount(string name = null) : base(name, 793, 0x3EBB, AIType.AI_Animal, FightMode.Aggressor)
        {
            SetStr(91, 100);
            SetDex(46, 55);
            SetInt(46, 60);

            SetHits(41, 50);

            SetDamage(5, 12);

            SetDamageType(ResistanceType.Physical, 50);
            SetDamageType(ResistanceType.Cold, 50);

            SetResistance(ResistanceType.Physical, 50, 60);
            SetResistance(ResistanceType.Cold, 90, 95);
            SetResistance(ResistanceType.Poison, 100);
            SetResistance(ResistanceType.Energy, 10, 15);

            SetSkill(SkillName.MagicResist, 95.1, 100.0);
            SetSkill(SkillName.Tactics, 50.0);
            SetSkill(SkillName.Wrestling, 70.1, 80.0);

            Fame = 0;
            Karma = 0;
        }

        public SkeletalMount(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an undead horse corpse";
        public override string DefaultName => "a skeletal steed";

        public override Poison PoisonImmune => Poison.Lethal;
        public override bool BleedImmune => true;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Tamable = false;
                        MinTameSkill = 0.0;
                        ControlSlots = 0;
                        break;
                    }
            }
        }
    }
}
