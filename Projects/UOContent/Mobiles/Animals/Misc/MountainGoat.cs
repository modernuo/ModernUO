using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class MountainGoat : BaseCreature
    {
        [Constructible]
        public MountainGoat() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 88;
            BaseSoundID = 0x99;

            SetStr(22, 64);
            SetDex(56, 75);
            SetInt(16, 30);

            SetHits(20, 33);
            SetMana(0);

            SetDamage(3, 7);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 10, 20);
            SetResistance(ResistanceType.Fire, 5, 10);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Poison, 10, 15);
            SetResistance(ResistanceType.Energy, 10, 15);

            SetSkill(SkillName.MagicResist, 25.1, 30.0);
            SetSkill(SkillName.Tactics, 29.3, 44.0);
            SetSkill(SkillName.Wrestling, 29.3, 44.0);

            Fame = 300;
            Karma = 0;

            VirtualArmor = 10;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -0.9;
        }

        public override string CorpseName => "a mountain goat corpse";
        public override string DefaultName => "a mountain goat";

        public override int Meat => 2;
        public override int Hides => 12;
        public override FoodType FavoriteFood => FoodType.GrainsAndHay | FoodType.FruitsAndVegies;
    }
}
