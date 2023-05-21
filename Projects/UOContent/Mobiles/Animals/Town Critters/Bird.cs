using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Bird : BaseCreature
    {
        [Constructible]
        public Bird() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            if (Utility.RandomBool())
            {
                Hue = 0x901;

                Name = Utility.Random(3) switch
                {
                    0 => "a crow",
                    2 => "a raven",
                    1 => "a magpie",
                    _ => Name
                };
            }
            else
            {
                Hue = Utility.RandomBirdHue();
                Name = NameList.RandomName("bird");
            }

            Body = 6;
            BaseSoundID = 0x1B;

            VirtualArmor = Utility.RandomMinMax(0, 6);

            SetStr(10);
            SetDex(25, 35);
            SetInt(10);

            SetDamage(0);

            SetDamageType(ResistanceType.Physical, 100);

            SetSkill(SkillName.Wrestling, 4.2, 6.4);
            SetSkill(SkillName.Tactics, 4.0, 6.0);
            SetSkill(SkillName.MagicResist, 4.0, 5.0);

            Fame = 150;
            Karma = 0;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -6.9;
        }

        public override string CorpseName => "a bird corpse";

        public override MeatType MeatType => MeatType.Bird;
        public override int Meat => 1;
        public override int Feathers => 25;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;
    }

    [SerializationGenerator(0, false)]
    public partial class TropicalBird : BaseCreature
    {
        [Constructible]
        public TropicalBird() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Hue = Utility.RandomBirdHue();

            Body = 6;
            BaseSoundID = 0xBF;

            VirtualArmor = Utility.RandomMinMax(0, 6);

            SetStr(10);
            SetDex(25, 35);
            SetInt(10);

            SetDamage(0);

            SetDamageType(ResistanceType.Physical, 100);

            SetSkill(SkillName.Wrestling, 4.2, 6.4);
            SetSkill(SkillName.Tactics, 4.0, 6.0);
            SetSkill(SkillName.MagicResist, 4.0, 5.0);

            Fame = 150;
            Karma = 0;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -6.9;
        }

        public override string CorpseName => "a bird corpse";
        public override string DefaultName => "a tropical bird";

        public override MeatType MeatType => MeatType.Bird;
        public override int Meat => 1;
        public override int Feathers => 25;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;
    }
}
