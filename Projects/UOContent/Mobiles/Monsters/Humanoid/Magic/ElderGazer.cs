using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class ElderGazer : BaseCreature
    {
        [Constructible]
        public ElderGazer() : base(AIType.AI_Mage)
        {
            Body = 22;
            BaseSoundID = 377;

            SetStr(296, 325);
            SetDex(86, 105);
            SetInt(291, 385);

            SetHits(178, 195);

            SetDamage(8, 19);

            SetDamageType(ResistanceType.Physical, 50);
            SetDamageType(ResistanceType.Energy, 50);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 60, 70);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.Anatomy, 62.0, 100.0);
            SetSkill(SkillName.EvalInt, 90.1, 100.0);
            SetSkill(SkillName.Magery, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 115.1, 130.0);
            SetSkill(SkillName.Tactics, 80.1, 100.0);
            SetSkill(SkillName.Wrestling, 80.1, 100.0);

            Fame = 12500;
            Karma = -12500;

            VirtualArmor = 50;
        }

        public override string CorpseName => "an elder gazer corpse";
        public override string DefaultName => "an elder gazer";

        public override int TreasureMapLevel => Core.AOS ? 4 : 0;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
        }
    }
}
