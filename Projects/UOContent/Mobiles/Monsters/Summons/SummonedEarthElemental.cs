using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SummonedEarthElemental : BaseCreature
    {
        [Constructible]
        public SummonedEarthElemental() : base(AIType.AI_Melee)
        {
            Body = 14;
            BaseSoundID = 268;

            if (Core.AOS)
            {
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
            }
            else 
            {
                SetStr(126, 155);
                SetDex(66, 85);
                SetInt(71, 92);

                SetHits(76, 93);

                SetDamage(9, 16);

                SetDamageType(ResistanceType.Physical, 100);

                SetResistance(ResistanceType.Physical, 30, 35);
                SetResistance(ResistanceType.Fire, 10, 20);
                SetResistance(ResistanceType.Cold, 10, 20);
                SetResistance(ResistanceType.Poison, 15, 25);
                SetResistance(ResistanceType.Energy, 15, 25);

                SetSkill(SkillName.MagicResist, 50.1, 95.0);
                SetSkill(SkillName.Tactics, 60.1, 100.0);
                SetSkill(SkillName.Wrestling, 60.1, 100.0);
            }

            VirtualArmor = 34;
            ControlSlots = 2;
        }

        public override bool DeleteCorpseOnDeath => Summoned;

        public override string CorpseName => "an earth elemental corpse";
        public override string DefaultName => "an earth elemental";

        public override bool BleedImmune => true;
        
        public override double DispelDifficulty => 117.5;
        public override double DispelFocus => 45.0;
    }
}
