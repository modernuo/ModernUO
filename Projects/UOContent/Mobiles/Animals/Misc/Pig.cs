using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Pig : BaseCreature
    {
        [Constructible]
        public Pig() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 0xCB;
            BaseSoundID = 0xC4;

            SetStr(20);
            SetDex(20);
            SetInt(5);

            SetHits(12);
            SetMana(0);

            SetDamage(2, 4);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 10, 15);

            SetSkill(SkillName.MagicResist, 5.0);
            SetSkill(SkillName.Tactics, 5.0);
            SetSkill(SkillName.Wrestling, 5.0);

            Fame = 150;
            Karma = 0;

            VirtualArmor = 12;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 11.1;
        }

        public override string CorpseName => "a pig corpse";
        public override string DefaultName => "a pig";

        public override int Meat => 1;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;
    }
}
