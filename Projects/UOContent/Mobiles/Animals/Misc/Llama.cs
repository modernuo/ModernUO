using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Llama : BaseCreature
    {
        [Constructible]
        public Llama() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 0xDC;
            BaseSoundID = 0x3F3;

            SetStr(21, 49);
            SetDex(36, 55);
            SetInt(16, 30);

            SetHits(15, 27);
            SetMana(0);

            SetDamage(3, 5);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 20);

            SetSkill(SkillName.MagicResist, 15.1, 20.0);
            SetSkill(SkillName.Tactics, 19.2, 29.0);
            SetSkill(SkillName.Wrestling, 19.2, 29.0);

            Fame = 300;
            Karma = 0;

            VirtualArmor = 16;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 35.1;
        }

        public override string CorpseName => "a llama corpse";
        public override string DefaultName => "a llama";

        public override int Meat => 1;
        public override int Hides => 12;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;
    }
}
