using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Rabbit : BaseCreature
    {
        [Constructible]
        public Rabbit() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 205;

            if (Utility.RandomBool())
            {
                Hue = Utility.RandomAnimalHue();
            }

            SetStr(6, 10);
            SetDex(26, 38);
            SetInt(6, 14);

            SetHits(4, 6);
            SetMana(0);

            SetDamage(1);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 5, 10);

            SetSkill(SkillName.MagicResist, 5.0);
            SetSkill(SkillName.Tactics, 5.0);
            SetSkill(SkillName.Wrestling, 5.0);

            Fame = 150;
            Karma = 0;

            VirtualArmor = 6;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -18.9;
        }

        public override string CorpseName => "a hare corpse";
        public override string DefaultName => "a rabbit";

        public override int Meat => 1;
        public override int Hides => 1;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies;

        public override int GetAttackSound() => 0xC9;

        public override int GetHurtSound() => 0xCA;

        public override int GetDeathSound() => 0xCB;
    }
}
