using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SummonedAirElemental : BaseCreature
    {
        [Constructible]
        public SummonedAirElemental() : base(AIType.AI_Mage)
        {
            Body = 13;
            Hue = 0x4001;
            BaseSoundID = 655;

            if (Core.AOS)
            {
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
            }
            else
            {
                SetStr(126, 155);
                SetDex(166, 185);
                SetInt(101, 125);

                SetHits(76, 93);

                SetDamage(8, 10);

                SetDamageType(ResistanceType.Physical, 20);
                SetDamageType(ResistanceType.Cold, 40);
                SetDamageType(ResistanceType.Energy, 40);

                SetResistance(ResistanceType.Physical, 35, 45);
                SetResistance(ResistanceType.Fire, 15, 25);
                SetResistance(ResistanceType.Cold, 10, 20);
                SetResistance(ResistanceType.Poison, 10, 20);
                SetResistance(ResistanceType.Energy, 25, 35);

                SetSkill(SkillName.EvalInt, 60.1, 75.0);
                SetSkill(SkillName.Magery, 60.1, 75.0);
                SetSkill(SkillName.MagicResist, 60.1, 75.0);
                SetSkill(SkillName.Tactics, 60.1, 80.0);
                SetSkill(SkillName.Wrestling, 60.1, 80.0);
            }

            VirtualArmor = 40;
            ControlSlots = 2;
        }

        public override bool DeleteCorpseOnDeath => Summoned;

        public override string CorpseName => "an air elemental corpse";
        public override string DefaultName => "an air elemental";

        public override bool BleedImmune => true;
        
        public override double DispelDifficulty => 117.5;
        public override double DispelFocus => 45.0;

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            if (BaseSoundID == 263)
            {
                BaseSoundID = 655;
            }
        }
    }
}
