using ModernUO.Serialization;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Bear")]
    [SerializationGenerator(0, false)]
    public partial class BlackBear : BaseCreature
    {
        [Constructible]
        public BlackBear() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 211;
            BaseSoundID = 0xA3;

            SetStr(76, 100);
            SetDex(56, 75);
            SetInt(11, 14);

            SetHits(46, 60);
            SetMana(0);

            SetDamage(4, 10);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 20, 25);
            SetResistance(ResistanceType.Cold, 10, 15);
            SetResistance(ResistanceType.Poison, 5, 10);

            SetSkill(SkillName.MagicResist, 20.1, 40.0);
            SetSkill(SkillName.Tactics, 40.1, 60.0);
            SetSkill(SkillName.Wrestling, 40.1, 60.0);

            Fame = 450;
            Karma = 0;

            VirtualArmor = 24;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 35.1;
        }

        public override string CorpseName => "a bear corpse";
        public override string DefaultName => "a black bear";

        public override int Meat => 1;
        public override int Hides => 12;
        public override FoodType FavoriteFood => FoodType.Fish | FoodType.Meat | FoodType.FruitsAndVegies;
        public override PackInstinct PackInstinct => PackInstinct.Bear;
    }
}
