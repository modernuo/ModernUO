using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Szavetra : Succubus
    {
        [Constructible]
        public Szavetra()
        {
            SetStr(627, 655);
            SetDex(164, 193);
            SetInt(566, 595);

            SetHits(312, 415);

            SetDamage(20, 30);

            SetDamageType(ResistanceType.Physical, 75);
            SetDamageType(ResistanceType.Energy, 25);

            SetResistance(ResistanceType.Physical, 83, 90);
            SetResistance(ResistanceType.Fire, 72, 80);
            SetResistance(ResistanceType.Cold, 40, 49);
            SetResistance(ResistanceType.Poison, 51, 60);
            SetResistance(ResistanceType.Energy, 50, 60);

            SetSkill(SkillName.EvalInt, 90.3, 99.8);
            SetSkill(SkillName.Magery, 100.1, 100.6); // 10.1-10.6 on OSI, bug?
            SetSkill(SkillName.Meditation, 90.1, 110.0);
            SetSkill(SkillName.MagicResist, 112.2, 127.2);
            SetSkill(SkillName.Tactics, 91.2, 92.8);
            SetSkill(SkillName.Wrestling, 80.2, 86.4);

            Fame = 24000;
            Karma = -24000;
        }

        public override string CorpseName => "a Szavetra corpse";
        public override string DefaultName => "Szavetra";
    }
}
