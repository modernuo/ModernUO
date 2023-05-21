using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class WhiteWyrm : BaseCreature
    {
        [Constructible]
        public WhiteWyrm() : base(AIType.AI_Mage)
        {
            Body = Utility.RandomBool() ? 180 : 49;
            BaseSoundID = 362;

            SetStr(721, 760);
            SetDex(101, 130);
            SetInt(386, 425);

            SetHits(433, 456);

            SetDamage(17, 25);

            SetDamageType(ResistanceType.Physical, 50);
            SetDamageType(ResistanceType.Cold, 50);

            SetResistance(ResistanceType.Physical, 55, 70);
            SetResistance(ResistanceType.Fire, 15, 25);
            SetResistance(ResistanceType.Cold, 80, 90);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.EvalInt, 99.1, 100.0);
            SetSkill(SkillName.Magery, 99.1, 100.0);
            SetSkill(SkillName.MagicResist, 99.1, 100.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 18000;
            Karma = -18000;

            VirtualArmor = 64;

            Tamable = true;
            ControlSlots = 3;
            MinTameSkill = 96.3;
        }

        public override string CorpseName => "a white wyrm corpse";
        public override string DefaultName => "a white wyrm";

        public override bool ReacquireOnMovement => true;
        public override int TreasureMapLevel => 4;
        public override int Meat => 19;
        public override int Hides => 20;
        public override HideType HideType => HideType.Barbed;
        public override int Scales => 9;
        public override ScaleType ScaleType => ScaleType.White;
        public override FoodType FavoriteFood => FoodType.Meat | FoodType.Gold;
        public override bool CanAngerOnTame => true;
        public override bool CanFly => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 2);
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Gems, Utility.Random(1, 5));
        }
    }
}
