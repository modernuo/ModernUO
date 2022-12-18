using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Chiikkaha : RatmanMage
    {
        [Constructible]
        public Chiikkaha()
        {
            SetStr(450, 476);
            SetDex(157, 179);
            SetInt(251, 275);

            SetHits(400, 425);

            SetDamage(10, 17);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 40, 45);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Poison, 10, 20);
            SetResistance(ResistanceType.Energy, 100);

            SetSkill(SkillName.EvalInt, 70.1, 80.0);
            SetSkill(SkillName.Magery, 70.1, 90.0);
            SetSkill(SkillName.MagicResist, 65.1, 96.0);
            SetSkill(SkillName.Tactics, 50.1, 75.0);
            SetSkill(SkillName.Wrestling, 50.1, 75.0);

            Fame = 7500;
            Karma = -7500;
        }

        public override string CorpseName => "a Chiikkaha the Toothed corpse";
        public override string DefaultName => "Chiikkaha the Toothed";
    }
}
