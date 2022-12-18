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

            VirtualArmor = 40;
            ControlSlots = 4;

            AddItem(new LightSource());
        }

        public override string CorpseName => "a fire elemental corpse";
        public override double DispelDifficulty => 117.5;
        public override double DispelFocus => 45.0;
        public override string DefaultName => "a fire elemental";
    }
}
