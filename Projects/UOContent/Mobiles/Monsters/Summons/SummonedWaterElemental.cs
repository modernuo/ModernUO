namespace Server.Mobiles
{
    public class SummonedWaterElemental : BaseCreature
    {
        [Constructible]
        public SummonedWaterElemental() : base(AIType.AI_Mage)
        {
            Body = 16;
            BaseSoundID = 278;

            SetStr(200);
            SetDex(70);
            SetInt(100);

            SetHits(165);

            SetDamage(12, 16);

            SetDamageType(ResistanceType.Physical, 0);
            SetDamageType(ResistanceType.Cold, 100);

            SetResistance(ResistanceType.Physical, 50, 60);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 70, 80);
            SetResistance(ResistanceType.Poison, 45, 55);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.Meditation, 90.0);
            SetSkill(SkillName.EvalInt, 80.0);
            SetSkill(SkillName.Magery, 80.0);
            SetSkill(SkillName.MagicResist, 75.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 85.0);

            VirtualArmor = 40;
            ControlSlots = 3;
            CanSwim = true;
        }

        public SummonedWaterElemental(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "a water elemental corpse";
        public override double DispelDifficulty => 117.5;
        public override double DispelFocus => 45.0;
        public override string DefaultName => "a water elemental";

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
