using ModernUO.Serialization;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Grizzlybear")]
    [SerializationGenerator(0, false)]
    public partial class GrizzlyBear : BaseCreature
    {
        [Constructible]
        public GrizzlyBear() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 212;
            BaseSoundID = 0xA3;

            SetStr(126, 155);
            SetDex(81, 105);
            SetInt(16, 40);

            SetHits(76, 93);
            SetMana(0);

            SetDamage(8, 13);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 25, 35);
            SetResistance(ResistanceType.Cold, 15, 25);
            SetResistance(ResistanceType.Poison, 5, 10);
            SetResistance(ResistanceType.Energy, 5, 10);

            SetSkill(SkillName.MagicResist, 25.1, 40.0);
            SetSkill(SkillName.Tactics, 70.1, 100.0);
            SetSkill(SkillName.Wrestling, 45.1, 70.0);

            Fame = 1000;
            Karma = 0;

            VirtualArmor = 24;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 59.1;
        }

        public override string CorpseName => "a grizzly bear corpse";
        public override string DefaultName => "a grizzly bear";

        public override int Meat => 2;
        public override int Hides => 16;
        public override FoodType FavoriteFood => FoodType.Fish | FoodType.FruitsAndVegies | FoodType.Meat;
        public override PackInstinct PackInstinct => PackInstinct.Bear;
    }
}
