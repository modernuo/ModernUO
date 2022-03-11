namespace Server.Mobiles
{
    public class SummonedAirElemental : BaseCreature
    {
        [Constructible]
        public SummonedAirElemental() : base(AIType.AI_Mage)
        {
            Body = 13;
            Hue = 0x4001;
            BaseSoundID = 655;

            SetStr(200);
            SetDex(200);
            SetInt(100);

            SetHits(150);
            SetStam(50);

            SetDamage(6, 9);

            SetDamageType(ResistanceType.Physical, 50);
            SetDamageType(ResistanceType.Energy, 50);

            SetResistance(ResistanceType.Physical, 40, 50);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 35, 45);
            SetResistance(ResistanceType.Poison, 50, 60);
            SetResistance(ResistanceType.Energy, 70, 80);

            SetSkill(SkillName.Meditation, 90.0);
            SetSkill(SkillName.EvalInt, 70.0);
            SetSkill(SkillName.Magery, 70.0);
            SetSkill(SkillName.MagicResist, 60.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 80.0);

            VirtualArmor = 40;
            ControlSlots = 2;
        }

        public SummonedAirElemental(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an air elemental corpse";
        public override double DispelDifficulty => 117.5;
        public override double DispelFocus => 45.0;
        public override string DefaultName => "an air elemental";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            if (BaseSoundID == 263)
            {
                BaseSoundID = 655;
            }
        }
    }
}
