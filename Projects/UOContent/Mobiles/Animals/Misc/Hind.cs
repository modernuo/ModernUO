using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Hind : BaseCreature
    {
        [Constructible]
        public Hind() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 0xED;

            SetStr(21, 51);
            SetDex(47, 77);
            SetInt(17, 47);

            SetHits(15, 29);
            SetMana(0);

            SetDamage(4);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 5, 15);
            SetResistance(ResistanceType.Cold, 5);

            SetSkill(SkillName.MagicResist, 15.0);
            SetSkill(SkillName.Tactics, 19.0);
            SetSkill(SkillName.Wrestling, 26.0);

            Fame = 300;
            Karma = 0;

            VirtualArmor = 8;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 23.1;
        }

        public override string CorpseName => "a deer corpse";
        public override string DefaultName => "a hind";

        public override int Meat => 5;
        public override int Hides => 8;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;

        public override int GetAttackSound() => 0x82;

        public override int GetHurtSound() => 0x83;

        public override int GetDeathSound() => 0x84;
    }
}
