namespace Server.Mobiles
{
    public class ArcaneFiend : BaseCreature
    {
        [Constructible]
        public ArcaneFiend() : base(AIType.AI_Mage)
        {
            Body = 74;
            BaseSoundID = 422;

            SetStr(55);
            SetDex(40);
            SetInt(60);

            SetDamage(10, 14);

            SetDamageType(ResistanceType.Physical, 0);
            SetDamageType(ResistanceType.Fire, 50);
            SetDamageType(ResistanceType.Poison, 50);

            SetResistance(ResistanceType.Physical, 25, 35);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.EvalInt, 20.1, 30.0);
            SetSkill(SkillName.Magery, 60.1, 70.0);
            SetSkill(SkillName.MagicResist, 30.1, 50.0);
            SetSkill(SkillName.Tactics, 42.1, 50.0);
            SetSkill(SkillName.Wrestling, 40.1, 44.0);

            Fame = 0;
            Karma = 0;

            ControlSlots = 1;
        }

        public ArcaneFiend(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an imp corpse";
        public override double DispelDifficulty => 70.0;
        public override double DispelFocus => 20.0;

        public override PackInstinct PackInstinct => PackInstinct.Daemon;
        public override bool BleedImmune => true; // TODO: Verify on OSI.  Guide says this.
        public override string DefaultName => "an imp";

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
