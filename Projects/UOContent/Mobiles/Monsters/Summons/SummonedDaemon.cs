using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SummonedDaemon : BaseCreature
    {
        [Constructible]
        public SummonedDaemon() : base(AIType.AI_Mage)
        {
            Name = NameList.RandomName("daemon");
            Body = Core.AOS ? 10 : 9;
            BaseSoundID = 357;

            SetStr(200);
            SetDex(110);
            SetInt(150);

            SetDamage(14, 21);

            SetDamageType(ResistanceType.Physical, 0);
            SetDamageType(ResistanceType.Poison, 100);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 50, 60);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 70, 80);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.EvalInt, 90.1, 100.0);
            SetSkill(SkillName.Meditation, 90.1, 100.0);
            SetSkill(SkillName.Magery, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 90.1, 100.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 98.1, 99.0);

            VirtualArmor = 58;
            ControlSlots = Core.SE ? 4 : 5;
        }

        public override string CorpseName => "a daemon corpse";
        public override double DispelDifficulty => 125.0;
        public override double DispelFocus => 45.0;

        public override Poison PoisonImmune => Poison.Regular; // TODO: Immune to poison?
        public override bool CanFly => true;
    }
}
