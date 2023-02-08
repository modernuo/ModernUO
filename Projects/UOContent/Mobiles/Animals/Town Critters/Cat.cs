using ModernUO.Serialization;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Housecat")]
    [SerializationGenerator(0, false)]
    public partial class Cat : BaseCreature
    {
        [Constructible]
        public Cat() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 0xC9;
            Hue = Utility.RandomAnimalHue();
            BaseSoundID = 0x69;

            SetStr(9);
            SetDex(35);
            SetInt(5);

            SetHits(6);
            SetMana(0);

            SetDamage(1);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 5, 10);

            SetSkill(SkillName.MagicResist, 5.0);
            SetSkill(SkillName.Tactics, 4.0);
            SetSkill(SkillName.Wrestling, 5.0);

            Fame = 0;
            Karma = 150;

            VirtualArmor = 8;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -0.9;
        }

        public override string CorpseName => "a cat corpse";
        public override string DefaultName => "a cat";

        public override int Meat => 1;
        public override FoodType FavoriteFood => FoodType.Meat | FoodType.Fish;
        public override PackInstinct PackInstinct => PackInstinct.Feline;
    }
}
