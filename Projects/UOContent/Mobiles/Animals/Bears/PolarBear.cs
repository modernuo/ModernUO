using ModernUO.Serialization;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Polarbear")]
    [SerializationGenerator(0, false)]
    public partial class PolarBear : BaseCreature
    {
        [Constructible]
        public PolarBear() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 213;
            BaseSoundID = 0xA3;

            SetStr(116, 140);
            SetDex(81, 105);
            SetInt(26, 50);

            SetHits(70, 84);
            SetMana(0);

            SetDamage(7, 12);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 25, 35);
            SetResistance(ResistanceType.Cold, 60, 80);
            SetResistance(ResistanceType.Poison, 15, 25);
            SetResistance(ResistanceType.Energy, 10, 15);

            SetSkill(SkillName.MagicResist, 45.1, 60.0);
            SetSkill(SkillName.Tactics, 60.1, 90.0);
            SetSkill(SkillName.Wrestling, 45.1, 70.0);

            Fame = 1500;
            Karma = 0;

            VirtualArmor = 18;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 35.1;
        }

        public override string CorpseName => "a polar bear corpse";
        public override string DefaultName => "a polar bear";

        public override int Meat => 2;
        public override int Hides => 16;
        public override FoodType FavoriteFood => FoodType.Fish | FoodType.FruitsAndVegies | FoodType.Meat;
        public override PackInstinct PackInstinct => PackInstinct.Bear;
    }
}
