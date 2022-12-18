using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Snake : BaseCreature
    {
        [Constructible]
        public Snake() : base(AIType.AI_Melee)
        {
            Body = 52;
            Hue = Utility.RandomSnakeHue();
            BaseSoundID = 0xDB;

            SetStr(22, 34);
            SetDex(16, 25);
            SetInt(6, 10);

            SetHits(15, 19);
            SetMana(0);

            SetDamage(1, 4);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 20);
            SetResistance(ResistanceType.Poison, 20, 30);

            SetSkill(SkillName.Poisoning, 50.1, 70.0);
            SetSkill(SkillName.MagicResist, 15.1, 20.0);
            SetSkill(SkillName.Tactics, 19.3, 34.0);
            SetSkill(SkillName.Wrestling, 19.3, 34.0);

            Fame = 300;
            Karma = -300;

            VirtualArmor = 16;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 59.1;
        }

        public override string CorpseName => "a snake corpse";
        public override string DefaultName => "a snake";

        public override Poison PoisonImmune => Poison.Lesser;
        public override Poison HitPoison => Poison.Lesser;

        public override bool DeathAdderCharmable => true;

        public override int Meat => 1;
        public override FoodType FavoriteFood => FoodType.Eggs;
    }
}
