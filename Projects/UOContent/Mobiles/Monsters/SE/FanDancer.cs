using ModernUO.Serialization;
using Server.Engines.Plants;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class FanDancer : BaseCreature
    {
        [Constructible]
        public FanDancer() : base(AIType.AI_Melee)
        {
            Body = 247;
            BaseSoundID = 0x372;

            SetStr(301, 375);
            SetDex(201, 255);
            SetInt(21, 25);

            SetHits(351, 430);

            SetDamage(12, 17);

            SetDamageType(ResistanceType.Physical, 70);
            SetDamageType(ResistanceType.Fire, 10);
            SetDamageType(ResistanceType.Cold, 10);
            SetDamageType(ResistanceType.Poison, 10);

            SetResistance(ResistanceType.Physical, 40, 60);
            SetResistance(ResistanceType.Fire, 50, 70);
            SetResistance(ResistanceType.Cold, 50, 70);
            SetResistance(ResistanceType.Poison, 50, 70);
            SetResistance(ResistanceType.Energy, 40, 60);

            SetSkill(SkillName.MagicResist, 100.1, 110.0);
            SetSkill(SkillName.Tactics, 85.1, 95.0);
            SetSkill(SkillName.Wrestling, 85.1, 95.0);
            SetSkill(SkillName.Anatomy, 85.1, 95.0);

            Fame = 9000;
            Karma = -9000;

            if (Utility.RandomDouble() < .33)
            {
                PackItem(Seed.RandomBonsaiSeed());
            }

            AddItem(new Tessen());

            if (Utility.RandomDouble() < 0.02)
            {
                PackItem(new OrigamiPaper());
            }
        }

        public override string CorpseName => "a fan dancer corpse";
        public override string DefaultName => "a fan dancer";

        public override bool Uncalmable => true;

        private static MonsterAbility[] _abilities =
        {
            MonsterAbilities.ReflectPhysicalDamage,
            MonsterAbilities.FanningFire,
            MonsterAbilities.FanThrowCounter
        };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich);
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.Gems, 2);
        }
    }
}
