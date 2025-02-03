using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SummonedWaterElemental : BaseCreature
    {
        [Constructible]
        public SummonedWaterElemental() : base(AIType.AI_Mage)
        {
            Body = 16;
            BaseSoundID = 278;

            if (Core.AOS)
            {
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
            }
            else
            {
                SetStr(126, 155);
                SetDex(66, 85);
                SetInt(101, 125);

                SetHits(76, 93);

                SetDamage(7, 9);

                SetDamageType(ResistanceType.Physical, 100);

                SetResistance(ResistanceType.Physical, 35, 45);
                SetResistance(ResistanceType.Fire, 10, 25);
                SetResistance(ResistanceType.Cold, 10, 25);
                SetResistance(ResistanceType.Poison, 60, 70);
                SetResistance(ResistanceType.Energy, 5, 10);

                SetSkill(SkillName.EvalInt, 60.1, 75.0);
                SetSkill(SkillName.Magery, 60.1, 75.0);
                SetSkill(SkillName.MagicResist, 100.1, 115.0);
                SetSkill(SkillName.Tactics, 50.1, 70.0);
                SetSkill(SkillName.Wrestling, 50.1, 70.0);
            }

            VirtualArmor = 40;
            ControlSlots = 3;
            CanSwim = true;
        }

        public override bool DeleteCorpseOnDeath => Summoned;

        public override string CorpseName => "a water elemental corpse";
        public override string DefaultName => "a water elemental";

        public override bool BleedImmune => true;
        
        public override double DispelDifficulty => 117.5;
        public override double DispelFocus => 45.0;
    }
}
