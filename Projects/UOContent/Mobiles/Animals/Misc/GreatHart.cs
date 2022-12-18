using ModernUO.Serialization;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.Greathart")]
    [SerializationGenerator(0, false)]
    public partial class GreatHart : BaseCreature
    {
        [Constructible]
        public GreatHart() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 0xEA;

            SetStr(41, 71);
            SetDex(47, 77);
            SetInt(27, 57);

            SetHits(27, 41);
            SetMana(0);

            SetDamage(5, 9);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 20, 25);
            SetResistance(ResistanceType.Cold, 5, 10);

            SetSkill(SkillName.MagicResist, 26.8, 44.5);
            SetSkill(SkillName.Tactics, 29.8, 47.5);
            SetSkill(SkillName.Wrestling, 29.8, 47.5);

            Fame = 300;
            Karma = 0;

            VirtualArmor = 24;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 59.1;
        }

        public override string CorpseName => "a deer corpse";
        public override string DefaultName => "a great hart";

        public override int Meat => 6;
        public override int Hides => 15;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;

        public override int GetAttackSound() => 0x82;

        public override int GetHurtSound() => 0x83;

        public override int GetDeathSound() => 0x84;
    }
}

