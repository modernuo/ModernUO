using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class FrenziedOstard : BaseMount
    {
        public override string DefaultName => "a frenzied ostard";

        [Constructible]
        public FrenziedOstard() : base(0xDA, 0x3EA4, AIType.AI_Melee)
        {
            Hue = Race.Human.RandomHairHue() | 0x8000;

            BaseSoundID = 0x275;

            SetStr(94, 170);
            SetDex(96, 115);
            SetInt(6, 10);

            SetHits(71, 110);
            SetMana(0);

            SetDamage(11, 17);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 25, 30);
            SetResistance(ResistanceType.Fire, 10, 15);
            SetResistance(ResistanceType.Poison, 20, 25);
            SetResistance(ResistanceType.Energy, 20, 25);

            SetSkill(SkillName.MagicResist, 75.1, 80.0);
            SetSkill(SkillName.Tactics, 79.3, 94.0);
            SetSkill(SkillName.Wrestling, 79.3, 94.0);

            Fame = 1500;
            Karma = -1500;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 77.1;
        }

        public override string CorpseName => "an ostard corpse";

        public override int Meat => 3;
        public override FoodType FavoriteFood => FoodType.Meat | FoodType.Fish | FoodType.Eggs | FoodType.FruitsAndVegies;
        public override PackInstinct PackInstinct => PackInstinct.Ostard;
    }
}
