using ModernUO.Serialization;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class SavageRidgeback : BaseMount
    {
        public override string DefaultName => "a savage ridgeback";

        [Constructible]
        public SavageRidgeback() : base(188, 0x3EB8, AIType.AI_Melee, FightMode.Aggressor)
        {
            BaseSoundID = 0x3F3;

            SetStr(58, 100);
            SetDex(56, 75);
            SetInt(16, 30);

            SetHits(41, 54);
            SetMana(0);

            SetDamage(3, 5);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 15, 20);
            SetResistance(ResistanceType.Fire, 10, 15);
            SetResistance(ResistanceType.Cold, 15, 20);
            SetResistance(ResistanceType.Poison, 10, 15);
            SetResistance(ResistanceType.Energy, 10, 15);

            SetSkill(SkillName.MagicResist, 25.3, 40.0);
            SetSkill(SkillName.Tactics, 29.3, 44.0);
            SetSkill(SkillName.Wrestling, 35.1, 45.0);

            Fame = 300;
            Karma = 0;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 83.1;
        }

        public override string CorpseName => "a savage ridgeback corpse";

        public override int Meat => 1;
        public override int Hides => 12;
        public override HideType HideType => HideType.Spined;
        public override FoodType FavoriteFood => FoodType.FruitsAndVegies | FoodType.GrainsAndHay;

        public override bool OverrideBondingReqs() => true;

        public override double GetControlChance(Mobile m, bool useBaseSkill = false) => 1.0;
    }
}
