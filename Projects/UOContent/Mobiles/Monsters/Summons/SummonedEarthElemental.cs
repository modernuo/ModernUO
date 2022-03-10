namespace Server.Mobiles
{
    public class SummonedEarthElemental : BaseCreature
    {
        [Constructible]
        public SummonedEarthElemental() : base(AIType.AI_Melee)
        {
            Body = 14;
            BaseSoundID = 268;

            SetStr(200);
            SetDex(70);
            SetInt(70);

            SetHits(180);

            SetDamage(14, 21);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 65, 75);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.MagicResist, 65.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 90.0);

            VirtualArmor = 34;
            ControlSlots = 2;
        }

        public SummonedEarthElemental(Serial serial) : base(serial)
        {
        }

        public override string CorpseName => "an earth elemental corpse";
        public override double DispelDifficulty => 117.5;
        public override double DispelFocus => 45.0;
        public override string DefaultName => "an earth elemental";

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
