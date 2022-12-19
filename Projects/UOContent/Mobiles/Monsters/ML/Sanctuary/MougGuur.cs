using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class MougGuur : Ettin
    {
        [Constructible]
        public MougGuur()
        {
            SetStr(556, 575);
            SetDex(84, 94);
            SetInt(59, 73);

            SetHits(400, 415);

            SetDamage(12, 20);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 61, 65);
            SetResistance(ResistanceType.Fire, 16, 19);
            SetResistance(ResistanceType.Cold, 41, 46);
            SetResistance(ResistanceType.Poison, 21, 24);
            SetResistance(ResistanceType.Energy, 19, 25);

            SetSkill(SkillName.MagicResist, 70.2, 75.0);
            SetSkill(SkillName.Tactics, 80.8, 81.7);
            SetSkill(SkillName.Wrestling, 93.9, 99.4);

            Fame = 3000;
            Karma = -3000;
        }

        public override string CorpseName => "a Moug-Guur corpse";
        public override string DefaultName => "Moug-Guur";
    }
}
