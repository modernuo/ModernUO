using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class FireGargoyle : BaseCreature
    {
        [Constructible]
        public FireGargoyle() : base(AIType.AI_Mage)
        {
            Name = NameList.RandomName("fire gargoyle");
            Body = 130;
            BaseSoundID = 0x174;

            SetStr(351, 400);
            SetDex(126, 145);
            SetInt(226, 250);

            SetHits(211, 240);

            SetDamage(7, 14);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Fire, 80);

            SetResistance(ResistanceType.Physical, 30, 35);
            SetResistance(ResistanceType.Fire, 50, 60);
            SetResistance(ResistanceType.Poison, 20, 30);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.Anatomy, 75.1, 85.0);
            SetSkill(SkillName.EvalInt, 90.1, 105.0);
            SetSkill(SkillName.Magery, 90.1, 105.0);
            SetSkill(SkillName.Meditation, 90.1, 105.0);
            SetSkill(SkillName.MagicResist, 90.1, 105.0);
            SetSkill(SkillName.Tactics, 80.1, 100.0);
            SetSkill(SkillName.Wrestling, 40.1, 80.0);

            Fame = 3500;
            Karma = -3500;

            VirtualArmor = 32;
        }

        public override string CorpseName => "a charred corpse";

        public override int TreasureMapLevel => 1;
        public override int Meat => 1;
        public override bool CanFly => true;

        private static MonsterAbility[] _abilities = { MonsterAbilities.FireBreath };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.Gems);
        }
    }
}
