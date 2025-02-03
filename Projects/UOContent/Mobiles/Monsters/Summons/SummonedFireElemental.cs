using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SummonedFireElemental : BaseCreature
    {
        [Constructible]
        public SummonedFireElemental() : base(AIType.AI_Mage)
        {
            Body = 15;
            BaseSoundID = 838;

            if (Core.AOS)
            {
                SetStr(200);
                SetDex(200);
                SetInt(100);

                SetDamage(9, 14);

                SetDamageType(ResistanceType.Physical, 0);
                SetDamageType(ResistanceType.Fire, 100);

                SetResistance(ResistanceType.Physical, 50, 60);
                SetResistance(ResistanceType.Fire, 70, 80);
                SetResistance(ResistanceType.Cold, 0, 10);
                SetResistance(ResistanceType.Poison, 50, 60);
                SetResistance(ResistanceType.Energy, 50, 60);

                SetSkill(SkillName.EvalInt, 90.0);
                SetSkill(SkillName.Magery, 90.0);
                SetSkill(SkillName.MagicResist, 85.0);
                SetSkill(SkillName.Tactics, 100.0);
                SetSkill(SkillName.Wrestling, 92.0);
            }
            else
            {
                SetStr(126, 155);
                SetDex(166, 185);
                SetInt(101, 125);

                SetHits(76, 93);

                SetDamage(7, 9);

                SetDamageType(ResistanceType.Physical, 25);
                SetDamageType(ResistanceType.Fire, 75);

                SetResistance(ResistanceType.Physical, 35, 45);
                SetResistance(ResistanceType.Fire, 60, 80);
                SetResistance(ResistanceType.Cold, 5, 10);
                SetResistance(ResistanceType.Poison, 30, 40);
                SetResistance(ResistanceType.Energy, 30, 40);

                SetSkill(SkillName.EvalInt, 60.1, 75.0);
                SetSkill(SkillName.Magery, 60.1, 75.0);
                SetSkill(SkillName.MagicResist, 75.2, 105.0);
                SetSkill(SkillName.Tactics, 80.1, 100.0);
                SetSkill(SkillName.Wrestling, 70.1, 100.0);
            }

            VirtualArmor = 40;
            ControlSlots = 4;

            AddItem(new LightSource());
        }

        public override bool DeleteCorpseOnDeath => Summoned;

        public override string CorpseName => "a fire elemental corpse";
        public override string DefaultName => "a fire elemental";

        public override bool BleedImmune => true;

        public override double DispelDifficulty => 117.5;
        public override double DispelFocus => 45.0;
    }
}
