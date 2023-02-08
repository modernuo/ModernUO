using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Boar : BaseCreature
    {
        [Constructible]
        public Boar() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 0x122;
            BaseSoundID = 0xC4;

            SetStr(25);
            SetDex(15);
            SetInt(5);

            SetHits(15);
            SetMana(0);

            SetDamage(3, 6);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 10, 15);
            SetResistance(ResistanceType.Fire, 5, 10);
            SetResistance(ResistanceType.Poison, 5, 10);

            SetSkill(SkillName.MagicResist, 9.0);
            SetSkill(SkillName.Tactics, 9.0);
            SetSkill(SkillName.Wrestling, 9.0);

            Fame = 300;
            Karma = 0;

            VirtualArmor = 10;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 29.1;
        }

        public override string CorpseName => "a pig corpse";
        public override string DefaultName => "a boar";

        public override int Meat => 2;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;
    }
}
