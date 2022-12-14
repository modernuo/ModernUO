using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SwampTentacle : BaseCreature
    {
        [Constructible]
        public SwampTentacle() : base(AIType.AI_Melee)
        {
            Body = 66;
            BaseSoundID = 352;

            SetStr(96, 120);
            SetDex(66, 85);
            SetInt(16, 30);

            SetHits(58, 72);
            SetMana(0);

            SetDamage(6, 12);

            SetDamageType(ResistanceType.Physical, 40);
            SetDamageType(ResistanceType.Poison, 60);

            SetResistance(ResistanceType.Physical, 25, 35);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Poison, 60, 80);
            SetResistance(ResistanceType.Energy, 10, 20);

            SetSkill(SkillName.MagicResist, 15.1, 20.0);
            SetSkill(SkillName.Tactics, 65.1, 80.0);
            SetSkill(SkillName.Wrestling, 65.1, 80.0);

            Fame = 3000;
            Karma = -3000;

            VirtualArmor = 28;

            PackReg(3);
        }

        public override string CorpseName => "a swamp tentacle corpse";
        public override string DefaultName => "a swamp tentacle";

        public override Poison PoisonImmune => Poison.Greater;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
        }
    }
}
