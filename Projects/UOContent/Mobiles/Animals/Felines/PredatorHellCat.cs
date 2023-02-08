using ModernUO.Serialization;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Preditorhellcat")]
    [SerializationGenerator(0, false)]
    public partial class PredatorHellCat : BaseCreature
    {
        [Constructible]
        public PredatorHellCat() : base(AIType.AI_Melee)
        {
            Body = 127;
            BaseSoundID = 0xBA;

            SetStr(161, 185);
            SetDex(96, 115);
            SetInt(76, 100);

            SetHits(97, 131);

            SetDamage(5, 17);

            SetDamageType(ResistanceType.Physical, 75);
            SetDamageType(ResistanceType.Fire, 25);

            SetResistance(ResistanceType.Physical, 25, 35);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Energy, 5, 15);

            SetSkill(SkillName.MagicResist, 75.1, 90.0);
            SetSkill(SkillName.Tactics, 50.1, 65.0);
            SetSkill(SkillName.Wrestling, 50.1, 65.0);

            Fame = 2500;
            Karma = -2500;

            VirtualArmor = 30;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 89.1;
        }

        public override string CorpseName => "a hell cat corpse";
        public override string DefaultName => "a hell cat";
        public override int Hides => 10;
        public override HideType HideType => HideType.Spined;
        public override FoodType FavoriteFood => FoodType.Meat;
        public override PackInstinct PackInstinct => PackInstinct.Feline;

        private static MonsterAbility[] _abilities = { MonsterAbilities.FireBreath };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
        }
    }
}
